import * as path from 'path';
import {
  createTeamSessionState,
  listTeamSessionStates,
  resolveTeamSessionState,
  transitionTeamSessionState,
  writeTeamSessionState,
  type TeamRoleState,
  type TeamSessionState,
  type TeamSessionStatus,
  type TeamStateLoadFailure,
} from './team-state.js';
import { getTeamLayoutTemplate } from './team-templates.js';
import type { TmuxAdapter, TmuxPaneSnapshot } from './tmux.js';

export interface TeamLaunchOptions {
  layout?: string;
  sessionName?: string;
}

export interface TeamSessionInspection {
  state: TeamSessionState;
  livePanes: TmuxPaneSnapshot[];
  issues: string[];
}

export interface TeamSessionListResult {
  inspections: TeamSessionInspection[];
  invalid: TeamStateLoadFailure[];
  tmuxUnavailableMessage?: string;
}

export class TeamOrchestrationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'TeamOrchestrationError';
  }
}

function compactTimestamp(value: Date): string {
  return value.toISOString().replace(/[-:]/g, '').replace(/\.\d{3}Z$/, 'Z');
}

export function sanitizeSessionName(value: string): string {
  const sanitized = value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9_-]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');

  if (!sanitized) {
    throw new TeamOrchestrationError('Session name must include at least one alphanumeric character.');
  }

  return sanitized.slice(0, 48);
}

export function createDefaultSessionName(projectPath: string, now: Date): string {
  return sanitizeSessionName(`${path.basename(projectPath)}-team-${compactTimestamp(now)}`);
}

function cloneRoles(roles: TeamRoleState[]): TeamRoleState[] {
  return roles.map(role => ({ ...role }));
}

function withUpdatedRole(
  state: TeamSessionState,
  roleIndex: number,
  patch: Partial<TeamRoleState>,
  updatedAt: string,
): TeamSessionState {
  const roles = cloneRoles(state.roles);
  roles[roleIndex] = {
    ...roles[roleIndex],
    ...patch,
  };

  return {
    ...state,
    roles,
    updatedAt,
  };
}

function determineRoleStatus(role: TeamRoleState, livePanes: TmuxPaneSnapshot[], sessionStatus: TeamSessionStatus): TeamRoleState['status'] {
  if (sessionStatus === 'stopped') {
    return 'stopped';
  }

  return livePanes.some(pane => pane.paneId === role.paneId) ? 'ready' : 'degraded';
}

function inspectSessionState(state: TeamSessionState, tmux: TmuxAdapter): TeamSessionInspection {
  if (state.status === 'stopped') {
    return {
      state,
      livePanes: [],
      issues: [],
    };
  }

  const issues: string[] = [];
  const livePanes = tmux.hasSession(state.tmuxSessionName)
    ? tmux.listPanes(state.tmuxSessionName)
    : [];

  if (livePanes.length === 0) {
    issues.push(`tmux session ${state.tmuxSessionName} is not available`);
  }

  for (const role of state.roles) {
    if (!livePanes.some(pane => pane.paneId === role.paneId)) {
      issues.push(`missing pane for role ${role.roleName} (${role.paneId || 'unassigned'})`);
    }
  }

  const nextStatus: TeamSessionStatus = issues.length > 0 ? 'degraded' : 'ready';
  const updatedAt = new Date().toISOString();
  const nextRoles = state.roles.map(role => ({
    ...role,
    status: determineRoleStatus(role, livePanes, nextStatus),
  }));

  return {
    state: {
      ...state,
      status: nextStatus,
      updatedAt,
      roles: nextRoles,
    },
    livePanes,
    issues,
  };
}

export function launchTeamSession(
  projectPath: string,
  launcherVersion: string,
  tmux: TmuxAdapter,
  options: TeamLaunchOptions = {},
  now = new Date(),
): TeamSessionState {
  const resolvedProjectPath = path.resolve(projectPath);
  const layout = getTeamLayoutTemplate(options.layout, resolvedProjectPath);
  const sessionId = sanitizeSessionName(options.sessionName ?? createDefaultSessionName(resolvedProjectPath, now));

  tmux.ensureAvailable();

  if (tmux.hasSession(sessionId)) {
    throw new TeamOrchestrationError(`A tmux session named "${sessionId}" already exists. Use --session-name to choose a different name or stop the existing session first.`);
  }

  const launchTimestamp = now.toISOString();

  let state = createTeamSessionState({
    sessionId,
    projectPath: resolvedProjectPath,
    launcherVersion,
    tmuxSessionName: sessionId,
    layoutPreset: layout.name,
    verificationPolicy: layout.verificationPolicy,
    templateVersion: layout.templateVersion,
    roles: layout.roles,
    createdAt: launchTimestamp,
    updatedAt: launchTimestamp,
  });

  writeTeamSessionState(resolvedProjectPath, state);

  try {
    const firstPaneId = tmux.createSession(sessionId, resolvedProjectPath, layout.windowName);
    const paneIds = [
      firstPaneId,
      tmux.splitWindow(firstPaneId, resolvedProjectPath, 'horizontal'),
      tmux.splitWindow(firstPaneId, resolvedProjectPath, 'vertical'),
      tmux.splitWindow(firstPaneId, resolvedProjectPath, 'vertical'),
    ];

    tmux.selectLayout(sessionId, 'tiled');

    for (const [index, role] of layout.roles.entries()) {
      const paneId = paneIds[index] ?? '';
      if (!paneId) {
        throw new TeamOrchestrationError(`Unable to assign pane for role ${role.roleName}`);
      }
      tmux.setPaneTitle(paneId, role.paneTitle);
      state = withUpdatedRole(state, index, { paneId, status: 'ready' }, launchTimestamp);
      writeTeamSessionState(resolvedProjectPath, state);
    }

    state = transitionTeamSessionState(state, 'ready', launchTimestamp);
    writeTeamSessionState(resolvedProjectPath, state);
    return state;
  } catch (err) {
    const message = (err as Error).message || String(err);

    try {
      if (tmux.hasSession(sessionId)) {
        tmux.killSession(sessionId);
      }
    } catch {
      // best-effort cleanup only
    }

    state = {
      ...transitionTeamSessionState(state, 'degraded', launchTimestamp),
      notes: [...state.notes, `Launch failed: ${message}`],
      roles: state.roles.map(role => ({
        ...role,
        status: role.paneId ? 'degraded' : role.status,
      })),
    };
    writeTeamSessionState(resolvedProjectPath, state);
    throw err;
  }
}

export function getTeamSessionStatus(projectPath: string, tmux: TmuxAdapter, sessionRef?: string): TeamSessionInspection {
  const resolvedProjectPath = path.resolve(projectPath);
  const state = resolveTeamSessionState(resolvedProjectPath, sessionRef);
  const inspection = inspectSessionState(state, tmux);
  writeTeamSessionState(resolvedProjectPath, inspection.state);
  return inspection;
}

export function listTeamSessions(projectPath: string, tmux: TmuxAdapter): TeamSessionListResult {
  const resolvedProjectPath = path.resolve(projectPath);
  const { sessions, invalid } = listTeamSessionStates(resolvedProjectPath);

  try {
    const inspections = sessions.map(session => {
      const inspection = inspectSessionState(session, tmux);
      writeTeamSessionState(resolvedProjectPath, inspection.state);
      return inspection;
    });

    return { inspections, invalid };
  } catch (err) {
    return {
      inspections: sessions.map(session => ({
        state: session,
        livePanes: [],
        issues: [],
      })),
      invalid,
      tmuxUnavailableMessage: (err as Error).message || String(err),
    };
  }
}

export function stopTeamSession(projectPath: string, tmux: TmuxAdapter, sessionRef?: string): TeamSessionState {
  const resolvedProjectPath = path.resolve(projectPath);
  const state = resolveTeamSessionState(resolvedProjectPath, sessionRef);

  if (state.status !== 'stopped' && tmux.hasSession(state.tmuxSessionName)) {
    tmux.killSession(state.tmuxSessionName);
  }

  const updatedAt = new Date().toISOString();
  const nextState = {
    ...transitionTeamSessionState(state, 'stopped', updatedAt),
    roles: state.roles.map(role => ({
      ...role,
      status: 'stopped' as const,
    })),
  };
  writeTeamSessionState(resolvedProjectPath, nextState);
  return nextState;
}
