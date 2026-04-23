import * as fs from 'fs';
import * as path from 'path';
import type { TeamRoleTemplate } from './team-templates.js';

export const TEAM_STATE_SCHEMA_VERSION = 1;
const TEAM_STATE_DIR = path.join('.unity-mcp', 'team-state');

const TEAM_SESSION_STATUSES = ['launching', 'ready', 'degraded', 'stopped'] as const;
const TEAM_ROLE_STATUSES = ['pending', 'ready', 'degraded', 'stopped'] as const;

export type TeamSessionStatus = typeof TEAM_SESSION_STATUSES[number];
export type TeamRoleStatus = typeof TEAM_ROLE_STATUSES[number];

export interface TeamRoleState extends TeamRoleTemplate {
  paneId: string;
  status: TeamRoleStatus;
}

export interface TeamSessionState {
  schemaVersion: number;
  sessionId: string;
  projectPath: string;
  createdAt: string;
  updatedAt: string;
  launcherVersion: string;
  tmuxSessionName: string;
  status: TeamSessionStatus;
  layoutPreset: string;
  roles: TeamRoleState[];
  verificationPolicy: string;
  notes: string[];
  templateVersion: string;
}

export interface TeamStateLoadFailure {
  filePath: string;
  error: string;
}

export class InvalidTeamStateError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidTeamStateError';
  }
}

const VALID_STATUS_TRANSITIONS: Record<TeamSessionStatus, TeamSessionStatus[]> = {
  launching: ['ready', 'degraded', 'stopped'],
  ready: ['degraded', 'stopped'],
  degraded: ['ready', 'stopped'],
  stopped: [],
};

export function isTeamSessionStatus(value: unknown): value is TeamSessionStatus {
  return typeof value === 'string' && (TEAM_SESSION_STATUSES as readonly string[]).includes(value);
}

export function isTeamRoleStatus(value: unknown): value is TeamRoleStatus {
  return typeof value === 'string' && (TEAM_ROLE_STATUSES as readonly string[]).includes(value);
}

export function canTransitionTeamSessionStatus(from: TeamSessionStatus, to: TeamSessionStatus): boolean {
  return from === to || VALID_STATUS_TRANSITIONS[from].includes(to);
}

export function getTeamStateDirectory(projectPath: string): string {
  return path.join(path.resolve(projectPath), TEAM_STATE_DIR);
}

export function getTeamStateFilePath(projectPath: string, sessionId: string): string {
  return path.join(getTeamStateDirectory(projectPath), `${sessionId}.json`);
}

export function createTeamSessionState(input: {
  sessionId: string;
  projectPath: string;
  launcherVersion: string;
  tmuxSessionName: string;
  layoutPreset: string;
  verificationPolicy: string;
  templateVersion: string;
  roles: TeamRoleTemplate[];
  createdAt?: string;
  updatedAt?: string;
  status?: TeamSessionStatus;
  notes?: string[];
}): TeamSessionState {
  const createdAt = input.createdAt ?? new Date().toISOString();
  const updatedAt = input.updatedAt ?? createdAt;

  return {
    schemaVersion: TEAM_STATE_SCHEMA_VERSION,
    sessionId: input.sessionId,
    projectPath: path.resolve(input.projectPath),
    createdAt,
    updatedAt,
    launcherVersion: input.launcherVersion,
    tmuxSessionName: input.tmuxSessionName,
    status: input.status ?? 'launching',
    layoutPreset: input.layoutPreset,
    roles: input.roles.map(role => ({
      ...role,
      paneId: '',
      status: 'pending',
    })),
    verificationPolicy: input.verificationPolicy,
    notes: [...(input.notes ?? [])],
    templateVersion: input.templateVersion,
  };
}

function assertString(value: unknown, field: string): string {
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new InvalidTeamStateError(`Invalid or missing ${field}`);
  }
  return value;
}

function assertStringArray(value: unknown, field: string): string[] {
  if (!Array.isArray(value) || value.some(item => typeof item !== 'string')) {
    throw new InvalidTeamStateError(`Invalid ${field}; expected string[]`);
  }
  return value as string[];
}

function assertRoleState(value: unknown, index: number): TeamRoleState {
  if (typeof value !== 'object' || value === null) {
    throw new InvalidTeamStateError(`Invalid roles[${index}] entry`);
  }

  const role = value as Record<string, unknown>;
  const status = role.status;
  if (!isTeamRoleStatus(status)) {
    throw new InvalidTeamStateError(`Invalid roles[${index}].status`);
  }

  return {
    roleName: assertString(role.roleName, `roles[${index}].roleName`),
    paneTitle: assertString(role.paneTitle, `roles[${index}].paneTitle`),
    command: assertString(role.command, `roles[${index}].command`),
    workingDirectory: assertString(role.workingDirectory, `roles[${index}].workingDirectory`),
    readinessHint: assertString(role.readinessHint, `roles[${index}].readinessHint`),
    paneId: typeof role.paneId === 'string' ? role.paneId : '',
    status,
  };
}

export function parseTeamSessionState(json: string, source = 'team state'): TeamSessionState {
  let parsed: unknown;
  try {
    parsed = JSON.parse(json) as unknown;
  } catch (err) {
    throw new InvalidTeamStateError(`Malformed JSON in ${source}: ${(err as Error).message}`);
  }

  if (typeof parsed !== 'object' || parsed === null) {
    throw new InvalidTeamStateError(`Invalid ${source}; expected object`);
  }

  const record = parsed as Record<string, unknown>;
  const schemaVersion = record.schemaVersion;
  if (schemaVersion !== TEAM_STATE_SCHEMA_VERSION) {
    throw new InvalidTeamStateError(`Unsupported schemaVersion in ${source}: ${String(schemaVersion)}`);
  }

  const status = record.status;
  if (!isTeamSessionStatus(status)) {
    throw new InvalidTeamStateError(`Invalid ${source}.status`);
  }

  const rolesValue = record.roles;
  if (!Array.isArray(rolesValue) || rolesValue.length === 0) {
    throw new InvalidTeamStateError(`Invalid ${source}.roles; expected non-empty array`);
  }

  return {
    schemaVersion,
    sessionId: assertString(record.sessionId, `${source}.sessionId`),
    projectPath: assertString(record.projectPath, `${source}.projectPath`),
    createdAt: assertString(record.createdAt, `${source}.createdAt`),
    updatedAt: assertString(record.updatedAt, `${source}.updatedAt`),
    launcherVersion: assertString(record.launcherVersion, `${source}.launcherVersion`),
    tmuxSessionName: assertString(record.tmuxSessionName, `${source}.tmuxSessionName`),
    status,
    layoutPreset: assertString(record.layoutPreset, `${source}.layoutPreset`),
    roles: rolesValue.map((role, index) => assertRoleState(role, index)),
    verificationPolicy: assertString(record.verificationPolicy, `${source}.verificationPolicy`),
    notes: assertStringArray(record.notes, `${source}.notes`),
    templateVersion: assertString(record.templateVersion, `${source}.templateVersion`),
  };
}

export function readTeamSessionState(projectPath: string, sessionId: string): TeamSessionState {
  const filePath = getTeamStateFilePath(projectPath, sessionId);
  if (!fs.existsSync(filePath)) {
    throw new InvalidTeamStateError(`No team state found for session: ${sessionId}`);
  }

  return parseTeamSessionState(fs.readFileSync(filePath, 'utf-8'), filePath);
}

export function writeTeamSessionState(projectPath: string, state: TeamSessionState): string {
  const filePath = getTeamStateFilePath(projectPath, state.sessionId);
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(state, null, 2) + '\n');
  return filePath;
}

export function listTeamSessionStates(projectPath: string): {
  sessions: TeamSessionState[];
  invalid: TeamStateLoadFailure[];
} {
  const stateDir = getTeamStateDirectory(projectPath);
  if (!fs.existsSync(stateDir)) {
    return { sessions: [], invalid: [] };
  }

  const sessions: TeamSessionState[] = [];
  const invalid: TeamStateLoadFailure[] = [];

  for (const entry of fs.readdirSync(stateDir).filter(name => name.endsWith('.json')).sort()) {
    const filePath = path.join(stateDir, entry);
    try {
      sessions.push(parseTeamSessionState(fs.readFileSync(filePath, 'utf-8'), filePath));
    } catch (err) {
      invalid.push({
        filePath,
        error: (err as Error).message || String(err),
      });
    }
  }

  sessions.sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
  return { sessions, invalid };
}

export function resolveTeamSessionState(projectPath: string, sessionRef?: string): TeamSessionState {
  const { sessions } = listTeamSessionStates(projectPath);

  if (sessions.length === 0) {
    throw new InvalidTeamStateError(`No team sessions found for project: ${path.resolve(projectPath)}`);
  }

  if (!sessionRef) {
    const active = sessions.find(session => session.status !== 'stopped');
    return active ?? sessions[0];
  }

  const match = sessions.find(session => session.sessionId === sessionRef || session.tmuxSessionName === sessionRef);
  if (!match) {
    throw new InvalidTeamStateError(`No team session found matching: ${sessionRef}`);
  }

  return match;
}

export function transitionTeamSessionState(
  state: TeamSessionState,
  nextStatus: TeamSessionStatus,
  updatedAt = new Date().toISOString(),
): TeamSessionState {
  if (!canTransitionTeamSessionStatus(state.status, nextStatus)) {
    throw new InvalidTeamStateError(`Invalid team status transition: ${state.status} -> ${nextStatus}`);
  }

  return {
    ...state,
    status: nextStatus,
    updatedAt,
  };
}
