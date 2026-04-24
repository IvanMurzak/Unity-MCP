import * as fs from 'fs';
import * as path from 'path';

export const HANDOFF_LEDGER_SCHEMA_VERSION = 1;
const HANDOFF_LEDGER_DIR = path.join('.unity-mcp', 'handoff-ledger');

export const HANDOFF_STATES = [
  'draft',
  'awaiting_approval',
  'approved_not_dispatched',
  'dispatched',
  'completed',
  'rejected',
  'frozen',
  'reconcile_needed',
] as const;

export type HandoffState = typeof HANDOFF_STATES[number];
export type HandoffApprovalDecision = 'approve' | 'reject';

export interface HandoffLifecycleWriter {
  kind: 'leader';
  actor: string;
}

export interface HandoffEvidenceEnvelope {
  handoffId: string;
  submittedBy: string;
  sourceLane: string;
  evidenceRefs: string[];
  createdAt: string;
}

export interface HandoffApprovalIntent {
  handoffId: string;
  recordVersion: number;
  decision: HandoffApprovalDecision;
  approverIdentity: string;
  provider: string;
  createdAt: string;
}

export interface HandoffDispatchProvenance {
  provider: string;
  target: string;
  dispatchId?: string;
  runId?: string;
  createdAt: string;
}

export interface HandoffAuditEntry {
  sequence: number;
  actor: string;
  action: string;
  fromState?: HandoffState;
  toState?: HandoffState;
  recordVersion: number;
  createdAt: string;
  notes?: string;
}

export interface HandoffRecord {
  schemaVersion: number;
  handoffId: string;
  recordVersion: number;
  state: HandoffState;
  sourceLane: string;
  targetLane: string;
  requestedAction: string;
  evidenceRefs: string[];
  approverIdentity: string | null;
  approvalVersion: number | null;
  downstreamDispatchTarget: string | null;
  dispatchProvenance: HandoffDispatchProvenance | null;
  createdAt: string;
  updatedAt: string;
  audit: HandoffAuditEntry[];
}

export class HandoffLedgerError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'HandoffLedgerError';
  }
}

const ALLOWED_HANDOFF_TRANSITIONS: Record<HandoffState, HandoffState[]> = {
  draft: ['awaiting_approval', 'frozen'],
  awaiting_approval: ['approved_not_dispatched', 'rejected', 'frozen', 'reconcile_needed'],
  approved_not_dispatched: ['dispatched', 'frozen', 'reconcile_needed'],
  dispatched: ['completed', 'reconcile_needed'],
  completed: [],
  rejected: [],
  frozen: ['awaiting_approval', 'approved_not_dispatched', 'reconcile_needed'],
  reconcile_needed: ['awaiting_approval', 'approved_not_dispatched', 'dispatched', 'completed', 'rejected', 'frozen'],
};

export function isHandoffState(value: unknown): value is HandoffState {
  return typeof value === 'string' && (HANDOFF_STATES as readonly string[]).includes(value);
}

export function canTransitionHandoffState(from: HandoffState, to: HandoffState): boolean {
  return from === to || ALLOWED_HANDOFF_TRANSITIONS[from].includes(to);
}

export function createLeaderWriter(actor: string): HandoffLifecycleWriter {
  if (!actor.trim()) {
    throw new HandoffLedgerError('Leader writer actor is required.');
  }
  return { kind: 'leader', actor };
}

function assertLeaderWriter(writer: HandoffLifecycleWriter): void {
  if (writer.kind !== 'leader' || !writer.actor.trim()) {
    throw new HandoffLedgerError('Only the leader writer may mutate handoff lifecycle state.');
  }
}

function assertNonEmptyString(value: unknown, field: string): string {
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new HandoffLedgerError(`Invalid or missing ${field}`);
  }
  return value;
}

function assertStringArray(value: unknown, field: string): string[] {
  if (!Array.isArray(value) || value.some(item => typeof item !== 'string' || item.trim().length === 0)) {
    throw new HandoffLedgerError(`Invalid ${field}; expected non-empty string[] entries`);
  }
  return [...value] as string[];
}

function nextAuditEntry(input: {
  currentAudit: HandoffAuditEntry[];
  actor: string;
  action: string;
  recordVersion: number;
  createdAt: string;
  fromState?: HandoffState;
  toState?: HandoffState;
  notes?: string;
}): HandoffAuditEntry {
  return {
    sequence: input.currentAudit.length + 1,
    actor: input.actor,
    action: input.action,
    recordVersion: input.recordVersion,
    createdAt: input.createdAt,
    ...(input.fromState ? { fromState: input.fromState } : {}),
    ...(input.toState ? { toState: input.toState } : {}),
    ...(input.notes ? { notes: input.notes } : {}),
  };
}

function appendLeaderAudit(
  record: HandoffRecord,
  writer: HandoffLifecycleWriter,
  action: string,
  nextVersion: number,
  createdAt: string,
  input: { fromState?: HandoffState; toState?: HandoffState; notes?: string } = {},
): HandoffAuditEntry[] {
  return [
    ...record.audit,
    nextAuditEntry({
      currentAudit: record.audit,
      actor: writer.actor,
      action,
      recordVersion: nextVersion,
      createdAt,
      ...input,
    }),
  ];
}

export function createHandoffRecord(input: {
  handoffId: string;
  sourceLane: string;
  targetLane: string;
  requestedAction: string;
  createdBy: HandoffLifecycleWriter;
  evidenceRefs?: string[];
  downstreamDispatchTarget?: string | null;
  createdAt?: string;
}): HandoffRecord {
  assertLeaderWriter(input.createdBy);
  const createdAt = input.createdAt ?? new Date().toISOString();
  const baseAudit: HandoffAuditEntry[] = [];

  return {
    schemaVersion: HANDOFF_LEDGER_SCHEMA_VERSION,
    handoffId: assertNonEmptyString(input.handoffId, 'handoffId'),
    recordVersion: 1,
    state: 'draft',
    sourceLane: assertNonEmptyString(input.sourceLane, 'sourceLane'),
    targetLane: assertNonEmptyString(input.targetLane, 'targetLane'),
    requestedAction: assertNonEmptyString(input.requestedAction, 'requestedAction'),
    evidenceRefs: [...(input.evidenceRefs ?? [])],
    approverIdentity: null,
    approvalVersion: null,
    downstreamDispatchTarget: input.downstreamDispatchTarget ?? null,
    dispatchProvenance: null,
    createdAt,
    updatedAt: createdAt,
    audit: [nextAuditEntry({
      currentAudit: baseAudit,
      actor: input.createdBy.actor,
      action: 'created',
      recordVersion: 1,
      createdAt,
      toState: 'draft',
    })],
  };
}

export function createEvidenceEnvelope(input: {
  handoffId: string;
  submittedBy: string;
  sourceLane: string;
  evidenceRefs: string[];
  createdAt?: string;
}): HandoffEvidenceEnvelope {
  return {
    handoffId: assertNonEmptyString(input.handoffId, 'handoffId'),
    submittedBy: assertNonEmptyString(input.submittedBy, 'submittedBy'),
    sourceLane: assertNonEmptyString(input.sourceLane, 'sourceLane'),
    evidenceRefs: assertStringArray(input.evidenceRefs, 'evidenceRefs'),
    createdAt: input.createdAt ?? new Date().toISOString(),
  };
}

export function createApprovalIntent(input: {
  handoffId: string;
  recordVersion: number;
  decision: HandoffApprovalDecision;
  approverIdentity: string;
  provider: string;
  createdAt?: string;
}): HandoffApprovalIntent {
  if (!Number.isInteger(input.recordVersion) || input.recordVersion < 1) {
    throw new HandoffLedgerError('Invalid recordVersion for approval intent.');
  }

  return {
    handoffId: assertNonEmptyString(input.handoffId, 'handoffId'),
    recordVersion: input.recordVersion,
    decision: input.decision,
    approverIdentity: assertNonEmptyString(input.approverIdentity, 'approverIdentity'),
    provider: assertNonEmptyString(input.provider, 'provider'),
    createdAt: input.createdAt ?? new Date().toISOString(),
  };
}

export function transitionHandoffState(
  record: HandoffRecord,
  writer: HandoffLifecycleWriter,
  nextState: HandoffState,
  options: {
    updatedAt?: string;
    notes?: string;
    dispatchProvenance?: HandoffDispatchProvenance | null;
  } = {},
): HandoffRecord {
  assertLeaderWriter(writer);

  if (!canTransitionHandoffState(record.state, nextState)) {
    throw new HandoffLedgerError(`Invalid handoff state transition: ${record.state} -> ${nextState}`);
  }

  const updatedAt = options.updatedAt ?? new Date().toISOString();
  const nextVersion = record.recordVersion + (record.state === nextState ? 0 : 1);

  return {
    ...record,
    state: nextState,
    recordVersion: nextVersion,
    updatedAt,
    dispatchProvenance: options.dispatchProvenance === undefined ? record.dispatchProvenance : options.dispatchProvenance,
    audit: appendLeaderAudit(record, writer, 'state_transition', nextVersion, updatedAt, {
      fromState: record.state,
      toState: nextState,
      notes: options.notes,
    }),
  };
}

export function applyEvidenceEnvelope(
  record: HandoffRecord,
  writer: HandoffLifecycleWriter,
  envelope: HandoffEvidenceEnvelope,
  updatedAt = new Date().toISOString(),
): HandoffRecord {
  assertLeaderWriter(writer);
  if (envelope.handoffId !== record.handoffId) {
    throw new HandoffLedgerError(`Evidence envelope ${envelope.handoffId} does not match handoff ${record.handoffId}.`);
  }

  const nextRefs = [...new Set([...record.evidenceRefs, ...envelope.evidenceRefs])];
  const nextVersion = record.recordVersion + 1;

  return {
    ...record,
    recordVersion: nextVersion,
    evidenceRefs: nextRefs,
    updatedAt,
    audit: appendLeaderAudit(record, writer, 'evidence_applied', nextVersion, updatedAt, {
      notes: `accepted evidence from ${envelope.submittedBy} (${envelope.sourceLane})`,
    }),
  };
}

export function applyApprovalIntent(
  record: HandoffRecord,
  writer: HandoffLifecycleWriter,
  intent: HandoffApprovalIntent,
  updatedAt = new Date().toISOString(),
): HandoffRecord {
  assertLeaderWriter(writer);
  if (intent.handoffId !== record.handoffId) {
    throw new HandoffLedgerError(`Approval intent ${intent.handoffId} does not match handoff ${record.handoffId}.`);
  }
  if (intent.recordVersion !== record.recordVersion) {
    throw new HandoffLedgerError(`Stale approval intent for ${record.handoffId}: expected version ${record.recordVersion}, got ${intent.recordVersion}.`);
  }
  if (record.state !== 'awaiting_approval') {
    throw new HandoffLedgerError(`Approval intent cannot be applied while handoff ${record.handoffId} is ${record.state}.`);
  }

  const nextState: HandoffState = intent.decision === 'approve' ? 'approved_not_dispatched' : 'rejected';
  const transitioned = transitionHandoffState(record, writer, nextState, {
    updatedAt,
    notes: `${intent.decision} from ${intent.provider}`,
  });

  return {
    ...transitioned,
    approverIdentity: intent.approverIdentity,
    approvalVersion: (record.approvalVersion ?? 0) + 1,
  };
}

function assertNullableString(value: unknown, field: string): string | null {
  if (value === null) return null;
  return assertNonEmptyString(value, field);
}

function assertNullableDispatchProvenance(value: unknown, field: string): HandoffDispatchProvenance | null {
  if (value === null) return null;
  if (typeof value !== 'object' || value === null) {
    throw new HandoffLedgerError(`Invalid ${field}`);
  }

  const record = value as Record<string, unknown>;
  return {
    provider: assertNonEmptyString(record.provider, `${field}.provider`),
    target: assertNonEmptyString(record.target, `${field}.target`),
    ...(typeof record.dispatchId === 'string' ? { dispatchId: record.dispatchId } : {}),
    ...(typeof record.runId === 'string' ? { runId: record.runId } : {}),
    createdAt: assertNonEmptyString(record.createdAt, `${field}.createdAt`),
  };
}

function parseAuditEntry(value: unknown, index: number): HandoffAuditEntry {
  if (typeof value !== 'object' || value === null) {
    throw new HandoffLedgerError(`Invalid audit[${index}] entry`);
  }

  const record = value as Record<string, unknown>;
  const fromState = record.fromState;
  const toState = record.toState;

  if (fromState !== undefined && !isHandoffState(fromState)) {
    throw new HandoffLedgerError(`Invalid audit[${index}].fromState`);
  }
  if (toState !== undefined && !isHandoffState(toState)) {
    throw new HandoffLedgerError(`Invalid audit[${index}].toState`);
  }

  const sequence = record.sequence;
  const recordVersion = record.recordVersion;
  if (!Number.isInteger(sequence) || Number(sequence) < 1) {
    throw new HandoffLedgerError(`Invalid audit[${index}].sequence`);
  }
  if (!Number.isInteger(recordVersion) || Number(recordVersion) < 1) {
    throw new HandoffLedgerError(`Invalid audit[${index}].recordVersion`);
  }

  return {
    sequence: Number(sequence),
    actor: assertNonEmptyString(record.actor, `audit[${index}].actor`),
    action: assertNonEmptyString(record.action, `audit[${index}].action`),
    ...(fromState ? { fromState } : {}),
    ...(toState ? { toState } : {}),
    recordVersion: Number(recordVersion),
    createdAt: assertNonEmptyString(record.createdAt, `audit[${index}].createdAt`),
    ...(typeof record.notes === 'string' ? { notes: record.notes } : {}),
  };
}

export function parseHandoffRecord(json: string, source = 'handoff record'): HandoffRecord {
  let parsed: unknown;
  try {
    parsed = JSON.parse(json) as unknown;
  } catch (err) {
    throw new HandoffLedgerError(`Malformed JSON in ${source}: ${(err as Error).message}`);
  }

  if (typeof parsed !== 'object' || parsed === null) {
    throw new HandoffLedgerError(`Invalid ${source}; expected object`);
  }

  const record = parsed as Record<string, unknown>;
  if (record.schemaVersion !== HANDOFF_LEDGER_SCHEMA_VERSION) {
    throw new HandoffLedgerError(`Unsupported schemaVersion in ${source}: ${String(record.schemaVersion)}`);
  }

  const state = record.state;
  if (!isHandoffState(state)) {
    throw new HandoffLedgerError(`Invalid ${source}.state`);
  }

  const recordVersion = record.recordVersion;
  if (!Number.isInteger(recordVersion) || Number(recordVersion) < 1) {
    throw new HandoffLedgerError(`Invalid ${source}.recordVersion`);
  }

  const approvalVersion = record.approvalVersion;
  if (approvalVersion !== null && (!Number.isInteger(approvalVersion) || Number(approvalVersion) < 1)) {
    throw new HandoffLedgerError(`Invalid ${source}.approvalVersion`);
  }

  const audit = record.audit;
  if (!Array.isArray(audit) || audit.length === 0) {
    throw new HandoffLedgerError(`Invalid ${source}.audit; expected non-empty array`);
  }

  return {
    schemaVersion: HANDOFF_LEDGER_SCHEMA_VERSION,
    handoffId: assertNonEmptyString(record.handoffId, `${source}.handoffId`),
    recordVersion: Number(recordVersion),
    state,
    sourceLane: assertNonEmptyString(record.sourceLane, `${source}.sourceLane`),
    targetLane: assertNonEmptyString(record.targetLane, `${source}.targetLane`),
    requestedAction: assertNonEmptyString(record.requestedAction, `${source}.requestedAction`),
    evidenceRefs: assertStringArray(record.evidenceRefs, `${source}.evidenceRefs`),
    approverIdentity: assertNullableString(record.approverIdentity, `${source}.approverIdentity`),
    approvalVersion: approvalVersion === null ? null : Number(approvalVersion),
    downstreamDispatchTarget: assertNullableString(record.downstreamDispatchTarget, `${source}.downstreamDispatchTarget`),
    dispatchProvenance: assertNullableDispatchProvenance(record.dispatchProvenance, `${source}.dispatchProvenance`),
    createdAt: assertNonEmptyString(record.createdAt, `${source}.createdAt`),
    updatedAt: assertNonEmptyString(record.updatedAt, `${source}.updatedAt`),
    audit: audit.map((entry, index) => parseAuditEntry(entry, index)),
  };
}

export function getHandoffLedgerDirectory(projectPath: string): string {
  return path.join(path.resolve(projectPath), HANDOFF_LEDGER_DIR);
}

export function getHandoffRecordFilePath(projectPath: string, handoffId: string): string {
  return path.join(getHandoffLedgerDirectory(projectPath), `${handoffId}.json`);
}

export function readHandoffRecord(projectPath: string, handoffId: string): HandoffRecord {
  const filePath = getHandoffRecordFilePath(projectPath, handoffId);
  if (!fs.existsSync(filePath)) {
    throw new HandoffLedgerError(`No handoff record found for: ${handoffId}`);
  }

  return parseHandoffRecord(fs.readFileSync(filePath, 'utf-8'), filePath);
}

export function writeHandoffRecord(projectPath: string, writer: HandoffLifecycleWriter, record: HandoffRecord): string {
  assertLeaderWriter(writer);
  const filePath = getHandoffRecordFilePath(projectPath, record.handoffId);
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(record, null, 2) + '\n');
  return filePath;
}
