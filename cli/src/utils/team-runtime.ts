import { createTmuxAdapter } from './tmux.js';
import { createTmuxTeamRuntime } from './team-runtime-tmux.js';
import type { TeamRoleTemplate } from './team-templates.js';

export const IMPLEMENTED_TEAM_RUNTIME_KINDS = ['tmux'] as const;
export const TEAM_RUNTIME_KINDS = [...IMPLEMENTED_TEAM_RUNTIME_KINDS, 'process'] as const;

export type ImplementedTeamRuntimeKind = typeof IMPLEMENTED_TEAM_RUNTIME_KINDS[number];
export type TeamRuntimeKind = typeof TEAM_RUNTIME_KINDS[number];
export type TeamRuntimeRequest = TeamRuntimeKind | 'auto';
export type TeamRuntimeRoleHealth = 'ready' | 'degraded' | 'missing';

export interface TeamRuntimeSelection {
  requestedKind: TeamRuntimeRequest;
  preferredKind: TeamRuntimeKind;
  resolvedKind: ImplementedTeamRuntimeKind;
  fallbackReason?: string;
}

export interface TeamRuntimeCapabilities {
  runtimeKind: TeamRuntimeKind;
  paneTitles: boolean;
  roleHandles: boolean;
  splitLayout: boolean;
  sessionListing: boolean;
}

export interface TeamRuntimeLaunchRequest {
  sessionId: string;
  projectPath: string;
  windowName: string;
  roles: TeamRoleTemplate[];
}

export interface TeamRuntimeRoleLaunchResult {
  roleName: string;
  runtimeHandle: string;
  displayName: string;
}

export interface TeamRuntimeLaunchResult {
  sessionHandle: string;
  roles: TeamRuntimeRoleLaunchResult[];
  notes?: string[];
}

export interface TeamRuntimeRoleInspection {
  roleName: string;
  runtimeHandle: string;
  displayName: string;
  status: TeamRuntimeRoleHealth;
  workingDirectory?: string;
  currentCommand?: string;
}

export interface TeamRuntimeSessionInspection {
  sessionHandle: string;
  available: boolean;
  roles: TeamRuntimeRoleInspection[];
  issues: string[];
}

export interface TeamRuntimeAdapter {
  readonly kind: ImplementedTeamRuntimeKind;
  readonly displayName: string;
  readonly selection: TeamRuntimeSelection;
  ensureAvailable(): void;
  capabilities(): TeamRuntimeCapabilities;
  hasSession(sessionHandle: string): boolean;
  launchSession(request: TeamRuntimeLaunchRequest): TeamRuntimeLaunchResult;
  inspectSession(sessionHandle: string): TeamRuntimeSessionInspection;
  stopSession(sessionHandle: string): void;
}

export class TeamRuntimeSelectionError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'TeamRuntimeSelectionError';
  }
}

export function isTeamRuntimeKind(value: unknown): value is TeamRuntimeKind {
  return typeof value === 'string' && (TEAM_RUNTIME_KINDS as readonly string[]).includes(value);
}

export function getPreferredTeamRuntimeKind(platform = process.platform): TeamRuntimeKind {
  return platform === 'win32' ? 'process' : 'tmux';
}

export function resolveTeamRuntimeSelection(
  requestedKind: TeamRuntimeRequest = 'auto',
  platform = process.platform,
): TeamRuntimeSelection {
  if (requestedKind !== 'auto' && !isTeamRuntimeKind(requestedKind)) {
    throw new TeamRuntimeSelectionError(`Unsupported team runtime: ${String(requestedKind)}`);
  }

  const preferredKind = requestedKind === 'auto' ? getPreferredTeamRuntimeKind(platform) : requestedKind;

  if ((IMPLEMENTED_TEAM_RUNTIME_KINDS as readonly string[]).includes(preferredKind)) {
    return {
      requestedKind,
      preferredKind,
      resolvedKind: preferredKind as ImplementedTeamRuntimeKind,
    };
  }

  return {
    requestedKind,
    preferredKind,
    resolvedKind: 'tmux',
    fallbackReason: `Preferred runtime "${preferredKind}" is not implemented yet; using "tmux" for now.`,
  };
}

export function createTeamRuntime(requestedKind: TeamRuntimeRequest = 'auto'): TeamRuntimeAdapter {
  const selection = resolveTeamRuntimeSelection(requestedKind);

  switch (selection.resolvedKind) {
    case 'tmux':
      return createTmuxTeamRuntime(createTmuxAdapter(), selection);
    default:
      throw new TeamRuntimeSelectionError(`Unsupported resolved runtime: ${selection.resolvedKind}`);
  }
}
