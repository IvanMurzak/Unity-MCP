export const DEV_ENV_HANDOFF_BOUNDARY_SCHEMA_VERSION = 1;

export const HANDOFF_LIFECYCLE_STATES = [
  'draft',
  'awaiting_approval',
  'approved_not_dispatched',
  'dispatched',
  'completed',
  'rejected',
  'frozen',
  'reconcile_needed',
] as const;

export const HANDOFF_ACTOR_ROLES = [
  'leader',
  'windows_lane',
  'discord_adapter',
  'bot_bridge',
] as const;

export const WINDOWS_EVIDENCE_OUTCOMES = [
  'passed',
  'failed',
  'blocked',
] as const;

export const DISPATCH_PROVENANCE_RESULTS = [
  'accepted',
  'rejected',
  'failed',
] as const;

const FORBIDDEN_LEDGER_MUTATION_FIELDS = [
  'state',
  'status',
  'lifecycleState',
  'nextState',
  'toState',
  'fromState',
  'transition',
  'auditHistory',
  'ledgerPatch',
  'mutations',
] as const;

const WINDOWS_HANDOFF_SNAPSHOT_FIELDS = [
  'snapshotId',
  'handoffId',
  'handoffRecordVersion',
  'requestedAction',
  'sourceLane',
  'targetLane',
  'createdAt',
  'evidenceExpectations',
  'projectHints',
  'workspaceHints',
  'notes',
] as const;

const WINDOWS_HANDOFF_EVIDENCE_EXPECTATION_FIELDS = [
  'evidenceType',
  'description',
  'required',
  'exampleUri',
] as const;

const WINDOWS_HANDOFF_PROJECT_HINT_FIELDS = [
  'projectPathHint',
  'unityProjectPathHint',
  'unityEditorVersionHint',
] as const;

const WINDOWS_HANDOFF_WORKSPACE_HINT_FIELDS = [
  'workingDirectoryHint',
  'artifactDirectoryHint',
  'logDirectoryHint',
  'branchHint',
] as const;

const FORBIDDEN_SNAPSHOT_FIELDS = [
  'assignmentId',
  'assignee',
  'assignedTo',
  'argv',
  'args',
  'command',
  'commands',
  'dispatch',
  'dispatchTarget',
  'entrypoint',
  'executable',
  'mailbox',
  'mailboxId',
  'mailboxPath',
  'owner',
  'ownerLane',
  'pid',
  'poll',
  'polling',
  'pollingIntervalMs',
  'processId',
  'queue',
  'queuePath',
  'retry',
  'retryCount',
  'retryOwner',
  'routingKey',
  'script',
  'scriptPath',
  'sessionId',
  'submissionUri',
  'submitAssignment',
  'supervisor',
  'taskId',
  'workerId',
] as const;

export type HandoffLifecycleState = typeof HANDOFF_LIFECYCLE_STATES[number];
export type HandoffActorRole = typeof HANDOFF_ACTOR_ROLES[number];
export type WindowsEvidenceOutcome = typeof WINDOWS_EVIDENCE_OUTCOMES[number];
export type DispatchProvenanceResult = typeof DISPATCH_PROVENANCE_RESULTS[number];

export interface HandoffLifecycleMutationRequest {
  actorRole: HandoffActorRole;
  handoffId: string;
  handoffVersion: number;
  fromState: HandoffLifecycleState;
  toState: HandoffLifecycleState;
  reason: string;
}

export interface HandoffEvidenceRef {
  type: 'log' | 'test_report' | 'build_artifact' | 'screenshot' | 'note';
  uri: string;
  sha256?: string;
}

export interface WindowsEvidenceEnvelope {
  schemaVersion: typeof DEV_ENV_HANDOFF_BOUNDARY_SCHEMA_VERSION;
  kind: 'windows_lane_evidence_envelope';
  handoffId: string;
  handoffVersion: number;
  sourceLane: {
    kind: 'windows_codex';
    laneId: string;
  };
  submittedAt: string;
  outcome: WindowsEvidenceOutcome;
  summary: string;
  evidenceRefs: HandoffEvidenceRef[];
}

export interface WindowsHandoffEvidenceExpectation {
  evidenceType: HandoffEvidenceRef['type'];
  description: string;
  required?: boolean;
  exampleUri?: string;
}

export interface WindowsHandoffProjectHints {
  projectPathHint?: string;
  unityProjectPathHint?: string;
  unityEditorVersionHint?: string;
}

export interface WindowsHandoffWorkspaceHints {
  workingDirectoryHint?: string;
  artifactDirectoryHint?: string;
  logDirectoryHint?: string;
  branchHint?: string;
}

export interface WindowsHandoffSnapshot {
  snapshotId: string;
  handoffId: string;
  handoffRecordVersion: number;
  requestedAction: string;
  sourceLane: string;
  targetLane: string;
  createdAt: string;
  evidenceExpectations?: WindowsHandoffEvidenceExpectation[];
  projectHints?: WindowsHandoffProjectHints;
  workspaceHints?: WindowsHandoffWorkspaceHints;
  notes?: string[];
}

export interface DispatchTargetSnapshot {
  provider: 'github_actions';
  repository: string;
  eventType: string;
  workflowRef?: string;
}

export interface BotDispatchProvenance {
  schemaVersion: typeof DEV_ENV_HANDOFF_BOUNDARY_SCHEMA_VERSION;
  kind: 'bot_dispatch_provenance';
  handoffId: string;
  handoffVersion: number;
  dispatchId: string;
  emittedAt: string;
  botId: string;
  target: DispatchTargetSnapshot;
  result: DispatchProvenanceResult;
  externalRunId?: string;
  error?: string;
}

export class HandoffBoundaryError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'HandoffBoundaryError';
  }
}

export function canActorMutateHandoffLifecycleState(actorRole: HandoffActorRole): boolean {
  return actorRole === 'leader';
}

export function assertLeaderLifecycleMutation(request: HandoffLifecycleMutationRequest): HandoffLifecycleMutationRequest {
  if (!canActorMutateHandoffLifecycleState(request.actorRole)) {
    throw new HandoffBoundaryError(
      `Only the leader may mutate handoff lifecycle state; ${request.actorRole} must submit a bounded intent, evidence envelope, or provenance event instead.`,
    );
  }

  assertNonEmptyString(request.handoffId, 'handoffId');
  assertPositiveInteger(request.handoffVersion, 'handoffVersion');
  assertNonEmptyString(request.reason, 'reason');

  if (!isHandoffLifecycleState(request.fromState)) {
    throw new HandoffBoundaryError(`Invalid fromState: ${String(request.fromState)}`);
  }
  if (!isHandoffLifecycleState(request.toState)) {
    throw new HandoffBoundaryError(`Invalid toState: ${String(request.toState)}`);
  }

  return request;
}

export function assertWindowsEvidenceEnvelope(input: unknown): WindowsEvidenceEnvelope {
  const record = assertRecord(input, 'Windows evidence envelope');
  assertNoLedgerMutationFields(record, 'Windows evidence envelope');

  if (record.schemaVersion !== DEV_ENV_HANDOFF_BOUNDARY_SCHEMA_VERSION) {
    throw new HandoffBoundaryError('Invalid Windows evidence envelope schemaVersion');
  }
  if (record.kind !== 'windows_lane_evidence_envelope') {
    throw new HandoffBoundaryError('Invalid Windows evidence envelope kind');
  }

  const sourceLane = assertRecord(record.sourceLane, 'Windows evidence envelope sourceLane');
  if (sourceLane.kind !== 'windows_codex') {
    throw new HandoffBoundaryError('Windows evidence envelopes must come from the windows_codex lane kind');
  }

  const outcome = record.outcome;
  if (!isWindowsEvidenceOutcome(outcome)) {
    throw new HandoffBoundaryError(`Invalid Windows evidence outcome: ${String(outcome)}`);
  }

  const envelope: WindowsEvidenceEnvelope = {
    schemaVersion: DEV_ENV_HANDOFF_BOUNDARY_SCHEMA_VERSION,
    kind: 'windows_lane_evidence_envelope',
    handoffId: assertNonEmptyString(record.handoffId, 'handoffId'),
    handoffVersion: assertPositiveInteger(record.handoffVersion, 'handoffVersion'),
    sourceLane: {
      kind: 'windows_codex',
      laneId: assertNonEmptyString(sourceLane.laneId, 'sourceLane.laneId'),
    },
    submittedAt: assertIsoTimestamp(record.submittedAt, 'submittedAt'),
    outcome,
    summary: assertNonEmptyString(record.summary, 'summary'),
    evidenceRefs: assertEvidenceRefs(record.evidenceRefs),
  };

  return envelope;
}

export function assertWindowsHandoffSnapshot(input: unknown): WindowsHandoffSnapshot {
  const record = assertRecord(input, 'Windows handoff snapshot');
  assertAllowedKeys(record, WINDOWS_HANDOFF_SNAPSHOT_FIELDS, 'Windows handoff snapshot');
  assertNoLedgerMutationFields(record, 'Windows handoff snapshot');
  assertNoForbiddenSnapshotFields(record, 'Windows handoff snapshot');

  const snapshot: WindowsHandoffSnapshot = {
    snapshotId: assertNonEmptyString(record.snapshotId, 'snapshotId'),
    handoffId: assertNonEmptyString(record.handoffId, 'handoffId'),
    handoffRecordVersion: assertPositiveInteger(record.handoffRecordVersion, 'handoffRecordVersion'),
    requestedAction: assertDescriptiveString(record.requestedAction, 'requestedAction'),
    sourceLane: assertNonEmptyString(record.sourceLane, 'sourceLane'),
    targetLane: assertNonEmptyString(record.targetLane, 'targetLane'),
    createdAt: assertIsoTimestamp(record.createdAt, 'createdAt'),
    evidenceExpectations: record.evidenceExpectations === undefined
      ? undefined
      : assertWindowsHandoffEvidenceExpectations(record.evidenceExpectations),
    projectHints: record.projectHints === undefined
      ? undefined
      : assertWindowsHandoffProjectHints(record.projectHints),
    workspaceHints: record.workspaceHints === undefined
      ? undefined
      : assertWindowsHandoffWorkspaceHints(record.workspaceHints),
    notes: record.notes === undefined
      ? undefined
      : assertStringArray(record.notes, 'notes'),
  };

  return snapshot;
}

export function assertBotDispatchProvenance(input: unknown): BotDispatchProvenance {
  const record = assertRecord(input, 'Bot dispatch provenance');
  assertNoLedgerMutationFields(record, 'Bot dispatch provenance');

  if (record.schemaVersion !== DEV_ENV_HANDOFF_BOUNDARY_SCHEMA_VERSION) {
    throw new HandoffBoundaryError('Invalid bot dispatch provenance schemaVersion');
  }
  if (record.kind !== 'bot_dispatch_provenance') {
    throw new HandoffBoundaryError('Invalid bot dispatch provenance kind');
  }

  const target = assertRecord(record.target, 'Bot dispatch provenance target');
  if (target.provider !== 'github_actions') {
    throw new HandoffBoundaryError('Bot dispatch provenance target provider must be github_actions');
  }

  const result = record.result;
  if (!isDispatchProvenanceResult(result)) {
    throw new HandoffBoundaryError(`Invalid bot dispatch provenance result: ${String(result)}`);
  }

  const provenance: BotDispatchProvenance = {
    schemaVersion: DEV_ENV_HANDOFF_BOUNDARY_SCHEMA_VERSION,
    kind: 'bot_dispatch_provenance',
    handoffId: assertNonEmptyString(record.handoffId, 'handoffId'),
    handoffVersion: assertPositiveInteger(record.handoffVersion, 'handoffVersion'),
    dispatchId: assertNonEmptyString(record.dispatchId, 'dispatchId'),
    emittedAt: assertIsoTimestamp(record.emittedAt, 'emittedAt'),
    botId: assertNonEmptyString(record.botId, 'botId'),
    target: {
      provider: 'github_actions',
      repository: assertNonEmptyString(target.repository, 'target.repository'),
      eventType: assertNonEmptyString(target.eventType, 'target.eventType'),
      workflowRef: typeof target.workflowRef === 'string' && target.workflowRef.trim()
        ? target.workflowRef
        : undefined,
    },
    result,
    externalRunId: typeof record.externalRunId === 'string' && record.externalRunId.trim()
      ? record.externalRunId
      : undefined,
    error: typeof record.error === 'string' && record.error.trim()
      ? record.error
      : undefined,
  };

  return provenance;
}

export function listForbiddenLedgerMutationFields(record: Record<string, unknown>): string[] {
  return FORBIDDEN_LEDGER_MUTATION_FIELDS.filter(field => Object.prototype.hasOwnProperty.call(record, field));
}

export function listForbiddenWindowsSnapshotFields(record: Record<string, unknown>): string[] {
  return findForbiddenFields(record, value => (FORBIDDEN_SNAPSHOT_FIELDS as readonly string[]).includes(value));
}

function isHandoffLifecycleState(value: unknown): value is HandoffLifecycleState {
  return typeof value === 'string' && (HANDOFF_LIFECYCLE_STATES as readonly string[]).includes(value);
}

function isWindowsEvidenceOutcome(value: unknown): value is WindowsEvidenceOutcome {
  return typeof value === 'string' && (WINDOWS_EVIDENCE_OUTCOMES as readonly string[]).includes(value);
}

function isDispatchProvenanceResult(value: unknown): value is DispatchProvenanceResult {
  return typeof value === 'string' && (DISPATCH_PROVENANCE_RESULTS as readonly string[]).includes(value);
}

function assertRecord(value: unknown, field: string): Record<string, unknown> {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) {
    throw new HandoffBoundaryError(`Invalid ${field}; expected object`);
  }

  return value as Record<string, unknown>;
}

function assertAllowedKeys(
  record: Record<string, unknown>,
  allowedKeys: readonly string[],
  field: string,
): void {
  const unknownKeys = Object.keys(record).filter(key => !allowedKeys.includes(key));
  if (unknownKeys.length > 0) {
    throw new HandoffBoundaryError(`${field} contains unsupported fields: ${unknownKeys.join(', ')}`);
  }
}

function assertNoLedgerMutationFields(record: Record<string, unknown>, field: string): void {
  const forbidden = listForbiddenLedgerMutationFields(record);
  if (forbidden.length > 0) {
    throw new HandoffBoundaryError(`${field} may not contain ledger lifecycle mutation fields: ${forbidden.join(', ')}`);
  }
}

function assertNoForbiddenSnapshotFields(record: Record<string, unknown>, field: string): void {
  const forbidden = listForbiddenWindowsSnapshotFields(record);
  if (forbidden.length > 0) {
    throw new HandoffBoundaryError(`${field} may not contain assignment, polling, dispatch, retry, or process-control fields: ${forbidden.join(', ')}`);
  }
}

function assertNonEmptyString(value: unknown, field: string): string {
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new HandoffBoundaryError(`Invalid or missing ${field}`);
  }

  return value;
}

function assertDescriptiveString(value: unknown, field: string): string {
  const text = assertNonEmptyString(value, field);
  return text;
}

function assertStringArray(value: unknown, field: string): string[] {
  if (!Array.isArray(value) || value.some(item => typeof item !== 'string' || item.trim().length === 0)) {
    throw new HandoffBoundaryError(`Invalid ${field}; expected non-empty string[] entries`);
  }

  return [...value] as string[];
}

function assertPositiveInteger(value: unknown, field: string): number {
  if (!Number.isInteger(value) || typeof value !== 'number' || value < 1) {
    throw new HandoffBoundaryError(`Invalid or missing ${field}; expected positive integer`);
  }

  return value;
}

function assertIsoTimestamp(value: unknown, field: string): string {
  const timestamp = assertNonEmptyString(value, field);
  const parsed = Date.parse(timestamp);
  if (Number.isNaN(parsed)) {
    throw new HandoffBoundaryError(`Invalid ${field}; expected ISO timestamp`);
  }

  return timestamp;
}

function assertEvidenceRefs(value: unknown): HandoffEvidenceRef[] {
  if (!Array.isArray(value) || value.length === 0) {
    throw new HandoffBoundaryError('Invalid evidenceRefs; expected non-empty array');
  }

  return value.map((item, index) => {
    const ref = assertRecord(item, `evidenceRefs[${index}]`);
    const type = ref.type;
    if (!['log', 'test_report', 'build_artifact', 'screenshot', 'note'].includes(String(type))) {
      throw new HandoffBoundaryError(`Invalid evidenceRefs[${index}].type`);
    }

    return {
      type: type as HandoffEvidenceRef['type'],
      uri: assertNonEmptyString(ref.uri, `evidenceRefs[${index}].uri`),
      sha256: typeof ref.sha256 === 'string' && ref.sha256.trim() ? ref.sha256 : undefined,
    };
  });
}

function assertWindowsHandoffEvidenceExpectations(value: unknown): WindowsHandoffEvidenceExpectation[] {
  if (!Array.isArray(value) || value.length === 0) {
    throw new HandoffBoundaryError('Invalid evidenceExpectations; expected non-empty array');
  }

  return value.map((item, index) => {
    const expectation = assertRecord(item, `evidenceExpectations[${index}]`);
    assertAllowedKeys(expectation, WINDOWS_HANDOFF_EVIDENCE_EXPECTATION_FIELDS, `evidenceExpectations[${index}]`);
    assertNoForbiddenSnapshotFields(expectation, `evidenceExpectations[${index}]`);

    const evidenceType = expectation.evidenceType;
    if (!['log', 'test_report', 'build_artifact', 'screenshot', 'note'].includes(String(evidenceType))) {
      throw new HandoffBoundaryError(`Invalid evidenceExpectations[${index}].evidenceType`);
    }

    return {
      evidenceType: evidenceType as HandoffEvidenceRef['type'],
      description: assertDescriptiveString(expectation.description, `evidenceExpectations[${index}].description`),
      required: expectation.required === undefined
        ? undefined
        : assertBoolean(expectation.required, `evidenceExpectations[${index}].required`),
      exampleUri: expectation.exampleUri === undefined
        ? undefined
        : assertNonEmptyString(expectation.exampleUri, `evidenceExpectations[${index}].exampleUri`),
    };
  });
}

function assertWindowsHandoffProjectHints(value: unknown): WindowsHandoffProjectHints {
  const hints = assertRecord(value, 'projectHints');
  assertAllowedKeys(hints, WINDOWS_HANDOFF_PROJECT_HINT_FIELDS, 'projectHints');
  assertNoForbiddenSnapshotFields(hints, 'projectHints');

  return {
    projectPathHint: hints.projectPathHint === undefined
      ? undefined
      : assertDescriptiveString(hints.projectPathHint, 'projectHints.projectPathHint'),
    unityProjectPathHint: hints.unityProjectPathHint === undefined
      ? undefined
      : assertDescriptiveString(hints.unityProjectPathHint, 'projectHints.unityProjectPathHint'),
    unityEditorVersionHint: hints.unityEditorVersionHint === undefined
      ? undefined
      : assertDescriptiveString(hints.unityEditorVersionHint, 'projectHints.unityEditorVersionHint'),
  };
}

function assertWindowsHandoffWorkspaceHints(value: unknown): WindowsHandoffWorkspaceHints {
  const hints = assertRecord(value, 'workspaceHints');
  assertAllowedKeys(hints, WINDOWS_HANDOFF_WORKSPACE_HINT_FIELDS, 'workspaceHints');
  assertNoForbiddenSnapshotFields(hints, 'workspaceHints');

  return {
    workingDirectoryHint: hints.workingDirectoryHint === undefined
      ? undefined
      : assertDescriptiveString(hints.workingDirectoryHint, 'workspaceHints.workingDirectoryHint'),
    artifactDirectoryHint: hints.artifactDirectoryHint === undefined
      ? undefined
      : assertDescriptiveString(hints.artifactDirectoryHint, 'workspaceHints.artifactDirectoryHint'),
    logDirectoryHint: hints.logDirectoryHint === undefined
      ? undefined
      : assertDescriptiveString(hints.logDirectoryHint, 'workspaceHints.logDirectoryHint'),
    branchHint: hints.branchHint === undefined
      ? undefined
      : assertDescriptiveString(hints.branchHint, 'workspaceHints.branchHint'),
  };
}

function assertBoolean(value: unknown, field: string): boolean {
  if (typeof value !== 'boolean') {
    throw new HandoffBoundaryError(`Invalid ${field}; expected boolean`);
  }

  return value;
}

function findForbiddenFields(
  value: unknown,
  isForbiddenKey: (key: string) => boolean,
  parentPath = '',
): string[] {
  if (Array.isArray(value)) {
    return value.flatMap((item, index) => findForbiddenFields(item, isForbiddenKey, `${parentPath}[${index}]`));
  }

  if (typeof value !== 'object' || value === null) {
    return [];
  }

  const record = value as Record<string, unknown>;
  return Object.entries(record).flatMap(([key, nestedValue]) => {
    const path = parentPath ? `${parentPath}.${key}` : key;
    const forbiddenAtKey = isForbiddenKey(key) ? [path] : [];
    return [...forbiddenAtKey, ...findForbiddenFields(nestedValue, isForbiddenKey, path)];
  });
}
