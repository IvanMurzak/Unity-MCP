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
import type { TeamRuntimeAdapter, TeamRuntimeRoleInspection, TeamRuntimeSessionInspection } from './team-runtime.js';

export interface TeamLaunchOptions {
  layout?: string;
  sessionName?: string;
}

export interface TeamSessionInspection {
  state: TeamSessionState;
  runtime: TeamRuntimeSessionInspection;
  issues: string[];
}

export interface TeamSessionListResult {
  inspections: TeamSessionInspection[];
  invalid: TeamStateLoadFailure[];
  runtimeUnavailableMessage?: string;
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

function findLiveRole(role: TeamRoleState, liveRoles: TeamRuntimeRoleInspection[]): TeamRuntimeRoleInspection | undefined {
  return liveRoles.find(liveRole => liveRole.roleName === role.roleName)
    ?? (role.runtimeHandle
      ? liveRoles.find(liveRole => liveRole.runtimeHandle === role.runtimeHandle)
      : undefined);
}

function determineRoleStatus(
  liveRole: TeamRuntimeRoleInspection | undefined,
): TeamRoleState['status'] {
  if (!liveRole) {
    return 'degraded';
  }

  return liveRole.status === 'ready' ? 'ready' : 'degraded';
}

function describeMissingRole(state: TeamSessionState, role: TeamRoleState): string {
  const handle = role.runtimeHandle || 'unassigned';
  const noun = state.runtime.kind === 'tmux' ? 'pane' : 'runtime role';
  return `missing ${noun} for role ${role.roleName} (${handle})`;
}

function assertRuntimeCompatibility(state: TeamSessionState, runtime: TeamRuntimeAdapter): void {
  if (state.runtime.kind !== runtime.kind) {
    throw new TeamOrchestrationError(
      `Session ${state.sessionId} was saved for the "${state.runtime.kind}" runtime, but the current CLI resolved "${runtime.kind}". Re-run with a compatible runtime once it is implemented.`,
    );
  }
}

function inspectSessionState(state: TeamSessionState, runtime: TeamRuntimeAdapter): TeamSessionInspection {
  assertRuntimeCompatibility(state, runtime);

  if (state.status === 'stopped') {
    return {
      state,
      runtime: {
        sessionHandle: state.runtime.sessionHandle,
        available: false,
        roles: [],
        issues: [],
      },
      issues: [],
    };
  }

  const runtimeInspection = runtime.inspectSession(state.runtime.sessionHandle);
  const issues = [...runtimeInspection.issues];
  const updatedAt = new Date().toISOString();

  const nextRoles = state.roles.map(role => {
    const liveRole = findLiveRole(role, runtimeInspection.roles);

    if (!liveRole || liveRole.status === 'missing') {
      issues.push(describeMissingRole(state, role));
    } else if (liveRole.status === 'degraded') {
      const handle = liveRole.displayName || liveRole.runtimeHandle || 'unassigned';
      issues.push(`runtime reported degraded role ${role.roleName} (${handle})`);
    }

    return {
      ...role,
      status: determineRoleStatus(liveRole),
    };
  });

  const nextStatus: TeamSessionStatus = issues.length > 0 ? 'degraded' : 'ready';

  return {
    state: {
      ...state,
      status: nextStatus,
      updatedAt,
      roles: nextRoles,
    },
    runtime: runtimeInspection,
    issues,
  };
}

export function launchTeamSession(
  projectPath: string,
  launcherVersion: string,
  runtime: TeamRuntimeAdapter,
  options: TeamLaunchOptions = {},
  now = new Date(),
): TeamSessionState {
  const resolvedProjectPath = path.resolve(projectPath);
  const layout = getTeamLayoutTemplate(options.layout, resolvedProjectPath);
  const sessionId = sanitizeSessionName(options.sessionName ?? createDefaultSessionName(resolvedProjectPath, now));

  runtime.ensureAvailable();

  if (runtime.hasSession(sessionId)) {
    throw new TeamOrchestrationError(`A ${runtime.displayName} session named "${sessionId}" already exists. Use --session-name to choose a different name or stop the existing session first.`);
  }

  const launchTimestamp = now.toISOString();

  let state = createTeamSessionState({
    sessionId,
    projectPath: resolvedProjectPath,
    launcherVersion,
    runtime: {
      kind: runtime.kind,
      sessionHandle: sessionId,
    },
    layoutPreset: layout.name,
    verificationPolicy: layout.verificationPolicy,
    templateVersion: layout.templateVersion,
    roles: layout.roles,
    createdAt: launchTimestamp,
    updatedAt: launchTimestamp,
  });

  writeTeamSessionState(resolvedProjectPath, state);

  try {
    const launchResult = runtime.launchSession({
      sessionId,
      projectPath: resolvedProjectPath,
      windowName: layout.windowName,
      roles: layout.roles,
    });

    state = {
      ...state,
      runtime: {
        kind: runtime.kind,
        sessionHandle: launchResult.sessionHandle,
      },
      notes: [...state.notes, ...(launchResult.notes ?? [])],
      updatedAt: launchTimestamp,
    };

    for (const [index, role] of layout.roles.entries()) {
      const runtimeRole = launchResult.roles.find(candidate => candidate.roleName === role.roleName);
      state = withUpdatedRole(state, index, {
        runtimeHandle: runtimeRole?.runtimeHandle ?? '',
        status: 'ready',
      }, launchTimestamp);
      writeTeamSessionState(resolvedProjectPath, state);
    }

    state = transitionTeamSessionState(state, 'ready', launchTimestamp);
    writeTeamSessionState(resolvedProjectPath, state);
    return state;
  } catch (err) {
    const message = (err as Error).message || String(err);

    try {
      if (runtime.hasSession(state.runtime.sessionHandle)) {
        runtime.stopSession(state.runtime.sessionHandle);
      }
    } catch {
      // best-effort cleanup only
    }

    state = {
      ...transitionTeamSessionState(state, 'degraded', launchTimestamp),
      notes: [...state.notes, `Launch failed: ${message}`],
      roles: state.roles.map(role => ({
        ...role,
        status: role.runtimeHandle ? 'degraded' : role.status,
      })),
    };
    writeTeamSessionState(resolvedProjectPath, state);
    throw err;
  }
}

export function getTeamSessionStatus(projectPath: string, runtime: TeamRuntimeAdapter, sessionRef?: string): TeamSessionInspection {
  const resolvedProjectPath = path.resolve(projectPath);
  const state = resolveTeamSessionState(resolvedProjectPath, sessionRef);
  const inspection = inspectSessionState(state, runtime);
  writeTeamSessionState(resolvedProjectPath, inspection.state);
  return inspection;
}

export function listTeamSessions(projectPath: string, runtime: TeamRuntimeAdapter): TeamSessionListResult {
  const resolvedProjectPath = path.resolve(projectPath);
  const { sessions, invalid } = listTeamSessionStates(resolvedProjectPath);

  try {
    runtime.ensureAvailable();
    const inspections = sessions.map(session => {
      assertRuntimeCompatibility(session, runtime);
      const inspection = inspectSessionState(session, runtime);
      writeTeamSessionState(resolvedProjectPath, inspection.state);
      return inspection;
    });

    return { inspections, invalid };
  } catch (err) {
    return {
      inspections: sessions.map(session => ({
        state: session,
        runtime: {
          sessionHandle: session.runtime.sessionHandle,
          available: false,
          roles: [],
          issues: [],
        },
        issues: [],
      })),
      invalid,
      runtimeUnavailableMessage: (err as Error).message || String(err),
    };
  }
}

export function stopTeamSession(projectPath: string, runtime: TeamRuntimeAdapter, sessionRef?: string): TeamSessionState {
  const resolvedProjectPath = path.resolve(projectPath);
  const state = resolveTeamSessionState(resolvedProjectPath, sessionRef);
  assertRuntimeCompatibility(state, runtime);

  if (state.status !== 'stopped' && runtime.hasSession(state.runtime.sessionHandle)) {
    runtime.stopSession(state.runtime.sessionHandle);
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
