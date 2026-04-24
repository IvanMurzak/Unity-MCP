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

export type DevEnvHandoffState =
  | 'draft'
  | 'awaiting_approval'
  | 'approved_not_dispatched'
  | 'dispatched'
  | 'completed'
  | 'rejected'
  | 'frozen'
  | 'reconcile_needed';

export type DevEnvHandoffType =
  | 'plan_to_execution'
  | 'execution_to_verification'
  | 'verification_to_cicd'
  | 'cicd_result_to_release_recovery';

export type DevEnvEvidenceKind = 'execution_result' | 'verification_result' | 'dispatch_result';

export interface DevEnvEvidenceEnvelope {
  id: string;
  laneId: Exclude<DevEnvLaneId, 'chat-approval-hub'>;
  kind: DevEnvEvidenceKind;
  handoffId: string;
  recordVersion: number;
  createdAt: string;
  refs: readonly string[];
}

export interface DevEnvHandoffRecord {
  id: string;
  type: DevEnvHandoffType;
  state: DevEnvHandoffState;
  recordVersion: number;
  sourceLane: DevEnvLaneId;
  targetLane: DevEnvLaneId;
  evidenceRefs: readonly string[];
  appliedEvidenceIds: readonly string[];
}

export interface DevEnvReconcileResult {
  state: DevEnvHandoffState;
  evidenceRefs: readonly string[];
  appliedEvidenceIds: readonly string[];
  ignoredEvidenceIds: readonly string[];
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

const DEV_ENV_HANDOFF_TRANSITIONS: Record<DevEnvHandoffState, readonly DevEnvHandoffState[]> = {
  draft: ['awaiting_approval', 'reconcile_needed', 'frozen'],
  awaiting_approval: ['approved_not_dispatched', 'rejected', 'frozen', 'reconcile_needed'],
  approved_not_dispatched: ['dispatched', 'frozen', 'reconcile_needed'],
  dispatched: ['completed', 'reconcile_needed'],
  completed: [],
  rejected: [],
  frozen: ['reconcile_needed'],
  reconcile_needed: ['awaiting_approval', 'approved_not_dispatched', 'dispatched', 'completed', 'rejected', 'frozen'],
};

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

export function canTransitionDevEnvHandoff(from: DevEnvHandoffState, to: DevEnvHandoffState): boolean {
  return from === to || DEV_ENV_HANDOFF_TRANSITIONS[from].includes(to);
}

export function assertDevEnvHandoffTransition(input: {
  actorLane: DevEnvLaneId;
  from: DevEnvHandoffState;
  to: DevEnvHandoffState;
}): void {
  assertDevEnvLifecycleMutationAllowed(input.actorLane);
  if (!canTransitionDevEnvHandoff(input.from, input.to)) {
    throw new DevEnvHandoffContractError(`Invalid v1 handoff transition: ${input.from} -> ${input.to}`);
  }
}

export function getDevEnvOutageState(current: DevEnvHandoffState): DevEnvHandoffState {
  if (current === 'approved_not_dispatched' || current === 'awaiting_approval') {
    return 'frozen';
  }
  if (current === 'dispatched') {
    return 'reconcile_needed';
  }
  return current;
}

export function reconcileDevEnvQueuedEvidence(
  record: DevEnvHandoffRecord,
  envelopes: readonly DevEnvEvidenceEnvelope[],
): DevEnvReconcileResult {
  const appliedEvidenceIds = new Set(record.appliedEvidenceIds);
  const evidenceRefs = new Set(record.evidenceRefs);
  const ignoredEvidenceIds: string[] = [];

  for (const envelope of envelopes) {
    const validForRecord = envelope.handoffId === record.id && envelope.recordVersion === record.recordVersion;
    const duplicate = appliedEvidenceIds.has(envelope.id);

    if (!validForRecord || duplicate) {
      ignoredEvidenceIds.push(envelope.id);
      continue;
    }

    appliedEvidenceIds.add(envelope.id);
    envelope.refs.forEach(ref => evidenceRefs.add(ref));
  }

  return {
    state: appliedEvidenceIds.size > record.appliedEvidenceIds.length ? 'reconcile_needed' : record.state,
    evidenceRefs: [...evidenceRefs],
    appliedEvidenceIds: [...appliedEvidenceIds],
    ignoredEvidenceIds,
  };
}
