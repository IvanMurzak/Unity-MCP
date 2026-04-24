export const DEV_ENV_HANDOFF_CONTRACT_VERSION = 1;

export const DEV_ENV_CONTROL_PLANE_LANE_ID = 'mac-omx-leader' as const;

export type DevEnvLaneId =
  | typeof DEV_ENV_CONTROL_PLANE_LANE_ID
  | 'windows-codex'
  | 'chat-approval-hub'
  | 'bot-ci-bridge';

export type DevEnvLaneRole =
  | 'control-plane'
  | 'execution-validation'
  | 'approval-intent'
  | 'dispatch-bridge';

export interface DevEnvLaneDefinition {
  id: DevEnvLaneId;
  displayName: string;
  role: DevEnvLaneRole;
  canMutateLifecycleState: boolean;
  responsibilities: readonly string[];
  prohibitedActions: readonly string[];
}


export const DEV_ENV_BOUNDED_ROLE_TYPES = ['planner', 'qa'] as const;

export type DevEnvBoundedRoleType = typeof DEV_ENV_BOUNDED_ROLE_TYPES[number];

export interface DevEnvBoundedRoleBinding {
  roleType: DevEnvBoundedRoleType;
  laneId: typeof DEV_ENV_CONTROL_PLANE_LANE_ID;
  canMutateLifecycleState: false;
  lifecycleAuthority: 'leader-only';
}

export const DEV_ENV_BOUNDED_ROLE_BINDINGS: readonly DevEnvBoundedRoleBinding[] = [
  {
    roleType: 'planner',
    laneId: DEV_ENV_CONTROL_PLANE_LANE_ID,
    canMutateLifecycleState: false,
    lifecycleAuthority: 'leader-only',
  },
  {
    roleType: 'qa',
    laneId: DEV_ENV_CONTROL_PLANE_LANE_ID,
    canMutateLifecycleState: false,
    lifecycleAuthority: 'leader-only',
  },
] as const;

export function isDevEnvBoundedRoleType(value: unknown): value is DevEnvBoundedRoleType {
  return typeof value === 'string' && (DEV_ENV_BOUNDED_ROLE_TYPES as readonly string[]).includes(value);
}

export function getDevEnvBoundedRoleBinding(roleType: DevEnvBoundedRoleType): DevEnvBoundedRoleBinding {
  const binding = DEV_ENV_BOUNDED_ROLE_BINDINGS.find(entry => entry.roleType === roleType);
  if (!binding) {
    throw new DevEnvHandoffContractError(`Unknown bounded dev-env role type: ${roleType}`);
  }
  return binding;
}

export class DevEnvHandoffContractError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'DevEnvHandoffContractError';
  }
}

export const DEV_ENV_LANE_DEFINITIONS: readonly DevEnvLaneDefinition[] = [
  {
    id: DEV_ENV_CONTROL_PLANE_LANE_ID,
    displayName: 'mac + OMX leader',
    role: 'control-plane',
    canMutateLifecycleState: true,
    responsibilities: [
      'Owns planning and orchestration state',
      'Creates and advances handoff lifecycle records',
      'Routes work to execution, approval, and dispatch lanes',
      'Freezes promotions when the leader is unavailable',
    ],
    prohibitedActions: [
      'Delegating canonical lifecycle mutation authority to another lane in v1',
    ],
  },
  {
    id: 'windows-codex',
    displayName: 'Windows Codex CLI',
    role: 'execution-validation',
    canMutateLifecycleState: false,
    responsibilities: [
      'Executes Windows-native implementation tasks assigned by the leader',
      'Publishes bounded validation evidence for leader reconcile',
    ],
    prohibitedActions: [
      'Opening new handoffs',
      'Approving handoffs',
      'Dispatching CI/CD workflows',
      'Mutating canonical lifecycle state',
    ],
  },
  {
    id: 'chat-approval-hub',
    displayName: 'Slack or Discord approval hub',
    role: 'approval-intent',
    canMutateLifecycleState: false,
    responsibilities: [
      'Surfaces handoff notifications',
      'Submits signed approve/reject intents for known handoff IDs and versions',
    ],
    prohibitedActions: [
      'Executing arbitrary commands',
      'Controlling the Unity runtime directly',
      'Mutating canonical lifecycle state',
    ],
  },
  {
    id: 'bot-ci-bridge',
    displayName: 'Bot-mediated CI/CD bridge',
    role: 'dispatch-bridge',
    canMutateLifecycleState: false,
    responsibilities: [
      'Consumes leader-approved immutable dispatch snapshots',
      'Triggers allowlisted GitHub Actions dispatch targets',
      'Reports dispatch provenance back to the leader',
    ],
    prohibitedActions: [
      'Choosing dispatch targets outside the leader-approved allowlist',
      'Mutating canonical lifecycle state',
    ],
  },
] as const;

export function getDevEnvLaneDefinition(laneId: DevEnvLaneId): DevEnvLaneDefinition {
  const lane = DEV_ENV_LANE_DEFINITIONS.find(entry => entry.id === laneId);
  if (!lane) {
    throw new DevEnvHandoffContractError(`Unknown dev-env handoff lane: ${laneId}`);
  }
  return lane;
}

export function getDevEnvLifecycleMutator(): DevEnvLaneDefinition {
  const mutators = DEV_ENV_LANE_DEFINITIONS.filter(lane => lane.canMutateLifecycleState);
  if (mutators.length !== 1) {
    throw new DevEnvHandoffContractError(`Expected exactly one lifecycle mutator, found ${mutators.length}`);
  }
  return mutators[0];
}

export function assertDevEnvLifecycleMutationAllowed(laneId: DevEnvLaneId): void {
  const mutator = getDevEnvLifecycleMutator();
  if (laneId !== mutator.id) {
    throw new DevEnvHandoffContractError(
      `Only ${mutator.displayName} may mutate v1 handoff lifecycle state; ${getDevEnvLaneDefinition(laneId).displayName} may submit evidence or intents only.`,
    );
  }
}

export function isDevEnvControlPlane(laneId: DevEnvLaneId): boolean {
  return laneId === DEV_ENV_CONTROL_PLANE_LANE_ID;
}
