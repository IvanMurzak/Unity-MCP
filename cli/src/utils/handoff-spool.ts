import * as fs from 'fs';
import * as path from 'path';
import type { HandoffApprovalDecision } from './handoff-ledger.js';

const HANDOFF_SPOOL_SCHEMA_VERSION = 1;
const HANDOFF_SPOOL_DIR = path.join('.unity-mcp', 'handoff-spool');
const DISCORD_NOTIFICATIONS_DIR = 'discord-notifications';
const APPROVAL_INTENTS_DIR = 'approval-intents';

export interface DiscordNotificationSpoolRecord {
  schemaVersion: number;
  provider: 'discord';
  messageId: string;
  channelId: string;
  handoffId: string;
  recordVersion: number;
  requestedAction: string;
  sourceLane: string;
  targetLane: string;
  sentAt: string;
  consumedAt: string | null;
  decision: HandoffApprovalDecision | null;
}

export interface QueuedApprovalIntentSpoolRecord {
  schemaVersion: number;
  provider: 'discord';
  intentId: string;
  interactionId: string;
  handoffId: string;
  handoffVersion: number;
  decision: HandoffApprovalDecision;
  actorId: string;
  createdAt: string;
  providerEvent: {
    messageId: string;
    channelId?: string;
  };
  verification: {
    signatureVerified: boolean;
    timestamp: string;
  };
}

export class HandoffSpoolError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'HandoffSpoolError';
  }
}

export function getHandoffSpoolDirectory(projectPath: string): string {
  return path.join(path.resolve(projectPath), HANDOFF_SPOOL_DIR);
}

export function getDiscordNotificationFilePath(projectPath: string, messageId: string): string {
  return path.join(getHandoffSpoolDirectory(projectPath), DISCORD_NOTIFICATIONS_DIR, `${messageId}.json`);
}

export function getQueuedApprovalIntentFilePath(projectPath: string, interactionId: string): string {
  return path.join(getHandoffSpoolDirectory(projectPath), APPROVAL_INTENTS_DIR, `${interactionId}.json`);
}

export function writeDiscordNotificationSpoolRecord(projectPath: string, record: DiscordNotificationSpoolRecord): string {
  const filePath = getDiscordNotificationFilePath(projectPath, record.messageId);
  ensureParentDirectory(filePath);
  fs.writeFileSync(filePath, JSON.stringify(record, null, 2) + '\n');
  return filePath;
}

export function readDiscordNotificationSpoolRecord(projectPath: string, messageId: string): DiscordNotificationSpoolRecord {
  const filePath = getDiscordNotificationFilePath(projectPath, messageId);
  if (!fs.existsSync(filePath)) {
    throw new HandoffSpoolError(`No Discord notification spool record found for message: ${messageId}`);
  }
  return parseDiscordNotificationSpoolRecord(fs.readFileSync(filePath, 'utf-8'), filePath);
}

export function markDiscordNotificationConsumed(projectPath: string, messageId: string, decision: HandoffApprovalDecision, consumedAt: string): DiscordNotificationSpoolRecord {
  const record = readDiscordNotificationSpoolRecord(projectPath, messageId);
  const nextRecord: DiscordNotificationSpoolRecord = {
    ...record,
    consumedAt,
    decision,
  };
  writeDiscordNotificationSpoolRecord(projectPath, nextRecord);
  return nextRecord;
}

export function hasQueuedApprovalIntent(projectPath: string, interactionId: string): boolean {
  return fs.existsSync(getQueuedApprovalIntentFilePath(projectPath, interactionId));
}

export function writeQueuedApprovalIntent(projectPath: string, record: QueuedApprovalIntentSpoolRecord): string {
  const filePath = getQueuedApprovalIntentFilePath(projectPath, record.interactionId);
  ensureParentDirectory(filePath);
  fs.writeFileSync(filePath, JSON.stringify(record, null, 2) + '\n');
  return filePath;
}

export function parseDiscordNotificationSpoolRecord(json: string, source = 'Discord notification spool record'): DiscordNotificationSpoolRecord {
  const parsed = parseJsonRecord(json, source);
  const recordVersion = parsed.recordVersion;
  if (!Number.isInteger(recordVersion) || Number(recordVersion) < 1) {
    throw new HandoffSpoolError(`Invalid ${source}.recordVersion`);
  }

  return {
    schemaVersion: assertSchemaVersion(parsed.schemaVersion, source),
    provider: assertLiteral(parsed.provider, 'discord', `${source}.provider`),
    messageId: assertNonEmptyString(parsed.messageId, `${source}.messageId`),
    channelId: assertNonEmptyString(parsed.channelId, `${source}.channelId`),
    handoffId: assertNonEmptyString(parsed.handoffId, `${source}.handoffId`),
    recordVersion: Number(recordVersion),
    requestedAction: assertNonEmptyString(parsed.requestedAction, `${source}.requestedAction`),
    sourceLane: assertNonEmptyString(parsed.sourceLane, `${source}.sourceLane`),
    targetLane: assertNonEmptyString(parsed.targetLane, `${source}.targetLane`),
    sentAt: assertNonEmptyString(parsed.sentAt, `${source}.sentAt`),
    consumedAt: parsed.consumedAt === null ? null : assertNonEmptyString(parsed.consumedAt, `${source}.consumedAt`),
    decision: parsed.decision === null ? null : assertApprovalDecision(parsed.decision, `${source}.decision`),
  };
}

export function createDiscordNotificationSpoolRecord(input: {
  messageId: string;
  channelId: string;
  handoffId: string;
  recordVersion: number;
  requestedAction: string;
  sourceLane: string;
  targetLane: string;
  sentAt?: string;
}): DiscordNotificationSpoolRecord {
  if (!Number.isInteger(input.recordVersion) || input.recordVersion < 1) {
    throw new HandoffSpoolError('Invalid recordVersion for Discord notification spool record.');
  }

  return {
    schemaVersion: HANDOFF_SPOOL_SCHEMA_VERSION,
    provider: 'discord',
    messageId: assertNonEmptyString(input.messageId, 'messageId'),
    channelId: assertNonEmptyString(input.channelId, 'channelId'),
    handoffId: assertNonEmptyString(input.handoffId, 'handoffId'),
    recordVersion: input.recordVersion,
    requestedAction: assertNonEmptyString(input.requestedAction, 'requestedAction'),
    sourceLane: assertNonEmptyString(input.sourceLane, 'sourceLane'),
    targetLane: assertNonEmptyString(input.targetLane, 'targetLane'),
    sentAt: input.sentAt ?? new Date().toISOString(),
    consumedAt: null,
    decision: null,
  };
}

export function createQueuedApprovalIntentSpoolRecord(input: {
  interactionId: string;
  handoffId: string;
  handoffVersion: number;
  decision: HandoffApprovalDecision;
  actorId: string;
  createdAt?: string;
  providerEvent: {
    messageId: string;
    channelId?: string;
  };
  verification: {
    signatureVerified: boolean;
    timestamp: string;
  };
}): QueuedApprovalIntentSpoolRecord {
  if (!Number.isInteger(input.handoffVersion) || input.handoffVersion < 1) {
    throw new HandoffSpoolError('Invalid handoffVersion for queued approval intent spool record.');
  }

  return {
    schemaVersion: HANDOFF_SPOOL_SCHEMA_VERSION,
    provider: 'discord',
    intentId: `discord:${assertNonEmptyString(input.interactionId, 'interactionId')}`,
    interactionId: input.interactionId,
    handoffId: assertNonEmptyString(input.handoffId, 'handoffId'),
    handoffVersion: input.handoffVersion,
    decision: input.decision,
    actorId: assertNonEmptyString(input.actorId, 'actorId'),
    createdAt: input.createdAt ?? new Date().toISOString(),
    providerEvent: {
      messageId: assertNonEmptyString(input.providerEvent.messageId, 'providerEvent.messageId'),
      ...(input.providerEvent.channelId ? { channelId: input.providerEvent.channelId } : {}),
    },
    verification: {
      signatureVerified: input.verification.signatureVerified,
      timestamp: assertNonEmptyString(input.verification.timestamp, 'verification.timestamp'),
    },
  };
}

function ensureParentDirectory(filePath: string): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
}

function parseJsonRecord(json: string, source: string): Record<string, unknown> {
  let parsed: unknown;
  try {
    parsed = JSON.parse(json) as unknown;
  } catch (err) {
    throw new HandoffSpoolError(`Malformed JSON in ${source}: ${(err as Error).message}`);
  }

  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
    throw new HandoffSpoolError(`Invalid ${source}; expected object`);
  }

  return parsed as Record<string, unknown>;
}

function assertSchemaVersion(value: unknown, field: string): number {
  if (value !== HANDOFF_SPOOL_SCHEMA_VERSION) {
    throw new HandoffSpoolError(`Unsupported schemaVersion in ${field}`);
  }
  return HANDOFF_SPOOL_SCHEMA_VERSION;
}

function assertLiteral<T extends string>(value: unknown, expected: T, field: string): T {
  if (value !== expected) {
    throw new HandoffSpoolError(`Invalid ${field}`);
  }
  return expected;
}

function assertNonEmptyString(value: unknown, field: string): string {
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new HandoffSpoolError(`Invalid or missing ${field}`);
  }
  return value;
}

function assertApprovalDecision(value: unknown, field: string): HandoffApprovalDecision {
  if (value !== 'approve' && value !== 'reject') {
    throw new HandoffSpoolError(`Invalid ${field}`);
  }
  return value;
}
