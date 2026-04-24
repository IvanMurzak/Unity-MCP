import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import {
  assertWindowsEvidenceEnvelope,
  type WindowsEvidenceEnvelope,
  type WindowsEvidenceOutcome,
} from './dev-env-handoff-boundaries.js';

const WINDOWS_EVIDENCE_SPOOL_SCHEMA_VERSION = 1;
const HANDOFF_SPOOL_DIR = path.join('.unity-mcp', 'handoff-spool');
const WINDOWS_EVIDENCE_DIR = 'windows-evidence';

export interface QueuedWindowsEvidenceSpoolRecord {
  schemaVersion: number;
  kind: 'windows_lane_evidence_envelope';
  recordId: string;
  handoffId: string;
  handoffVersion: number;
  sourceLaneId: string;
  outcome: WindowsEvidenceOutcome;
  summary: string;
  submittedAt: string;
  storedAt: string;
  consumedAt: string | null;
  appliedRecordVersion: number | null;
  lastError: string | null;
  envelope: WindowsEvidenceEnvelope;
}

export class WindowsEvidenceSpoolError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'WindowsEvidenceSpoolError';
  }
}

export function getWindowsEvidenceSpoolDirectory(projectPath: string): string {
  return path.join(path.resolve(projectPath), HANDOFF_SPOOL_DIR, WINDOWS_EVIDENCE_DIR);
}

export function getWindowsEvidenceSpoolRecordFilePath(projectPath: string, recordId: string): string {
  return path.join(getWindowsEvidenceSpoolDirectory(projectPath), `${recordId}.json`);
}

export function queueWindowsEvidenceEnvelope(projectPath: string, input: unknown): {
  record: QueuedWindowsEvidenceSpoolRecord;
  filePath: string;
  duplicate: boolean;
} {
  const envelope = assertWindowsEvidenceEnvelope(input);
  const record = createQueuedWindowsEvidenceSpoolRecord(envelope);
  const filePath = getWindowsEvidenceSpoolRecordFilePath(projectPath, record.recordId);

  if (fs.existsSync(filePath)) {
    return {
      record: readQueuedWindowsEvidenceSpoolRecord(projectPath, record.recordId),
      filePath,
      duplicate: true,
    };
  }

  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(record, null, 2) + '\n');

  return { record, filePath, duplicate: false };
}

export function listQueuedWindowsEvidenceSpoolRecords(projectPath: string): QueuedWindowsEvidenceSpoolRecord[] {
  const dir = getWindowsEvidenceSpoolDirectory(projectPath);
  if (!fs.existsSync(dir)) {
    return [];
  }

  return fs.readdirSync(dir)
    .filter(fileName => fileName.endsWith('.json'))
    .sort()
    .map(fileName => {
      const filePath = path.join(dir, fileName);
      return parseQueuedWindowsEvidenceSpoolRecord(fs.readFileSync(filePath, 'utf-8'), filePath);
    });
}

export function readQueuedWindowsEvidenceSpoolRecord(projectPath: string, recordId: string): QueuedWindowsEvidenceSpoolRecord {
  const filePath = getWindowsEvidenceSpoolRecordFilePath(projectPath, recordId);
  if (!fs.existsSync(filePath)) {
    throw new WindowsEvidenceSpoolError(`No Windows evidence spool record found for: ${recordId}`);
  }

  return parseQueuedWindowsEvidenceSpoolRecord(fs.readFileSync(filePath, 'utf-8'), filePath);
}

export function markQueuedWindowsEvidenceApplied(input: {
  projectPath: string;
  recordId: string;
  consumedAt?: string;
  appliedRecordVersion: number;
}): QueuedWindowsEvidenceSpoolRecord {
  const record = readQueuedWindowsEvidenceSpoolRecord(input.projectPath, input.recordId);
  if (!Number.isInteger(input.appliedRecordVersion) || input.appliedRecordVersion < 1) {
    throw new WindowsEvidenceSpoolError('Invalid appliedRecordVersion for Windows evidence spool record.');
  }

  const nextRecord: QueuedWindowsEvidenceSpoolRecord = {
    ...record,
    consumedAt: input.consumedAt ?? new Date().toISOString(),
    appliedRecordVersion: input.appliedRecordVersion,
    lastError: null,
  };

  writeQueuedWindowsEvidenceSpoolRecord(input.projectPath, nextRecord);
  return nextRecord;
}

export function markQueuedWindowsEvidencePending(input: {
  projectPath: string;
  recordId: string;
  lastError: string;
}): QueuedWindowsEvidenceSpoolRecord {
  const record = readQueuedWindowsEvidenceSpoolRecord(input.projectPath, input.recordId);
  const nextRecord: QueuedWindowsEvidenceSpoolRecord = {
    ...record,
    lastError: assertNonEmptyString(input.lastError, 'lastError'),
  };

  writeQueuedWindowsEvidenceSpoolRecord(input.projectPath, nextRecord);
  return nextRecord;
}

export function formatWindowsEvidenceRefsForLedger(envelope: WindowsEvidenceEnvelope): string[] {
  return envelope.evidenceRefs.map(ref => {
    const suffix = ref.sha256 ? `#sha256=${ref.sha256}` : '';
    return `${ref.type}:${ref.uri}${suffix}`;
  });
}

export function parseQueuedWindowsEvidenceSpoolRecord(
  json: string,
  source = 'Windows evidence spool record',
): QueuedWindowsEvidenceSpoolRecord {
  const parsed = parseJsonRecord(json, source);
  const handoffVersion = assertPositiveInteger(parsed.handoffVersion, `${source}.handoffVersion`);
  const appliedRecordVersion = parsed.appliedRecordVersion;

  return {
    schemaVersion: assertSchemaVersion(parsed.schemaVersion, source),
    kind: assertLiteral(parsed.kind, 'windows_lane_evidence_envelope', `${source}.kind`),
    recordId: assertNonEmptyString(parsed.recordId, `${source}.recordId`),
    handoffId: assertNonEmptyString(parsed.handoffId, `${source}.handoffId`),
    handoffVersion,
    sourceLaneId: assertNonEmptyString(parsed.sourceLaneId, `${source}.sourceLaneId`),
    outcome: assertOutcome(parsed.outcome, `${source}.outcome`),
    summary: assertNonEmptyString(parsed.summary, `${source}.summary`),
    submittedAt: assertIsoTimestamp(parsed.submittedAt, `${source}.submittedAt`),
    storedAt: assertIsoTimestamp(parsed.storedAt, `${source}.storedAt`),
    consumedAt: parsed.consumedAt === null ? null : assertIsoTimestamp(parsed.consumedAt, `${source}.consumedAt`),
    appliedRecordVersion: appliedRecordVersion === null ? null : assertPositiveInteger(appliedRecordVersion, `${source}.appliedRecordVersion`),
    lastError: parsed.lastError === null ? null : assertNonEmptyString(parsed.lastError, `${source}.lastError`),
    envelope: assertWindowsEvidenceEnvelope(parsed.envelope),
  };
}

function createQueuedWindowsEvidenceSpoolRecord(envelope: WindowsEvidenceEnvelope): QueuedWindowsEvidenceSpoolRecord {
  const recordId = computeWindowsEvidenceRecordId(envelope);
  return {
    schemaVersion: WINDOWS_EVIDENCE_SPOOL_SCHEMA_VERSION,
    kind: 'windows_lane_evidence_envelope',
    recordId,
    handoffId: envelope.handoffId,
    handoffVersion: envelope.handoffVersion,
    sourceLaneId: envelope.sourceLane.laneId,
    outcome: envelope.outcome,
    summary: envelope.summary,
    submittedAt: envelope.submittedAt,
    storedAt: new Date().toISOString(),
    consumedAt: null,
    appliedRecordVersion: null,
    lastError: null,
    envelope,
  };
}

function writeQueuedWindowsEvidenceSpoolRecord(projectPath: string, record: QueuedWindowsEvidenceSpoolRecord): string {
  const filePath = getWindowsEvidenceSpoolRecordFilePath(projectPath, record.recordId);
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(record, null, 2) + '\n');
  return filePath;
}

function computeWindowsEvidenceRecordId(envelope: WindowsEvidenceEnvelope): string {
  const canonical = JSON.stringify({
    schemaVersion: envelope.schemaVersion,
    kind: envelope.kind,
    handoffId: envelope.handoffId,
    handoffVersion: envelope.handoffVersion,
    sourceLane: envelope.sourceLane,
    submittedAt: envelope.submittedAt,
    outcome: envelope.outcome,
    summary: envelope.summary,
    evidenceRefs: envelope.evidenceRefs,
  });

  return crypto.createHash('sha256').update(canonical).digest('hex');
}

function parseJsonRecord(json: string, source: string): Record<string, unknown> {
  let parsed: unknown;
  try {
    parsed = JSON.parse(json) as unknown;
  } catch (err) {
    throw new WindowsEvidenceSpoolError(`Malformed JSON in ${source}: ${(err as Error).message}`);
  }

  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
    throw new WindowsEvidenceSpoolError(`Invalid ${source}; expected object`);
  }

  return parsed as Record<string, unknown>;
}

function assertSchemaVersion(value: unknown, source: string): number {
  if (value !== WINDOWS_EVIDENCE_SPOOL_SCHEMA_VERSION) {
    throw new WindowsEvidenceSpoolError(`Unsupported schemaVersion in ${source}`);
  }
  return WINDOWS_EVIDENCE_SPOOL_SCHEMA_VERSION;
}

function assertLiteral<T extends string>(value: unknown, expected: T, field: string): T {
  if (value !== expected) {
    throw new WindowsEvidenceSpoolError(`Invalid ${field}`);
  }
  return expected;
}

function assertNonEmptyString(value: unknown, field: string): string {
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new WindowsEvidenceSpoolError(`Invalid or missing ${field}`);
  }
  return value;
}

function assertIsoTimestamp(value: unknown, field: string): string {
  const timestamp = assertNonEmptyString(value, field);
  if (Number.isNaN(Date.parse(timestamp))) {
    throw new WindowsEvidenceSpoolError(`Invalid ${field}; expected ISO timestamp`);
  }
  return timestamp;
}

function assertPositiveInteger(value: unknown, field: string): number {
  if (typeof value !== 'number' || !Number.isInteger(value) || value < 1) {
    throw new WindowsEvidenceSpoolError(`Invalid or missing ${field}; expected positive integer`);
  }
  return value;
}

function assertOutcome(value: unknown, field: string): WindowsEvidenceOutcome {
  if (value !== 'passed' && value !== 'failed' && value !== 'blocked') {
    throw new WindowsEvidenceSpoolError(`Invalid ${field}`);
  }
  return value;
}
