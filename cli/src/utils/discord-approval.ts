import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type { HandoffApprovalDecision, HandoffRecord } from './handoff-ledger.js';

const DISCORD_API_BASE_URL = 'https://discord.com/api/v10';
const DISCORD_MESSAGE_FLAGS_EPHEMERAL = 1 << 6;
const DISCORD_SIGNATURE_MAX_AGE_MS = 5 * 60 * 1000;
const ED25519_SPKI_PREFIX_HEX = '302a300506032b6570032100';

export const DISCORD_APPROVE_CUSTOM_ID = 'unity_mcp_handoff_approve';
export const DISCORD_REJECT_CUSTOM_ID = 'unity_mcp_handoff_reject';

export interface HandoffBridgeConfig {
  envFilePath?: string;
  discordBotToken?: string;
  discordPublicKey?: string;
  discordApprovalChannelId?: string;
  handoffLeaderActor: string;
  handoffAllowedApproverIds: string[];
}

export interface DiscordNotificationSendResult {
  messageId: string;
  channelId: string;
}

export interface DiscordRequestVerificationResult {
  signatureVerified: true;
  timestamp: string;
}

export interface DiscordInteractionActor {
  id: string;
  username?: string;
}

export interface DiscordInteractionRequest {
  id: string;
  type: number;
  token?: string;
  member?: {
    user?: {
      id?: string;
      username?: string;
    };
  };
  user?: {
    id?: string;
    username?: string;
  };
  message?: {
    id?: string;
    channel_id?: string;
    content?: string;
    components?: DiscordComponent[];
  };
  data?: {
    custom_id?: string;
    component_type?: number;
  };
}

export interface DiscordActionRowComponent {
  type: 1;
  components: DiscordButtonComponent[];
}

export interface DiscordButtonComponent {
  type: 2;
  style: number;
  label: string;
  custom_id: string;
  disabled?: boolean;
}

export type DiscordComponent = DiscordActionRowComponent | DiscordButtonComponent | Record<string, unknown>;

export interface DiscordInteractionResponse {
  type: 1 | 4 | 7;
  data?: {
    content?: string;
    flags?: number;
    components?: DiscordComponent[];
    allowed_mentions?: { parse: string[] };
  };
}

export class DiscordApprovalError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'DiscordApprovalError';
  }
}

export function parseEnvFileContents(contents: string): Record<string, string> {
  const parsed: Record<string, string> = {};
  for (const rawLine of contents.split(/\r?\n/u)) {
    const line = rawLine.trim();
    if (!line || line.startsWith('#')) {
      continue;
    }

    const equalsIndex = line.indexOf('=');
    if (equalsIndex <= 0) {
      continue;
    }

    const key = line.slice(0, equalsIndex).trim();
    let value = line.slice(equalsIndex + 1).trim();
    if ((value.startsWith('"') && value.endsWith('"')) || (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }
    parsed[key] = value;
  }

  return parsed;
}

export function loadHandoffBridgeConfig(options: {
  envFilePath?: string;
  env?: NodeJS.ProcessEnv;
} = {}): HandoffBridgeConfig {
  const env = options.env ?? process.env;
  const envFilePath = options.envFilePath
    ? path.resolve(options.envFilePath)
    : env['UNITY_MCP_HANDOFF_ENV_FILE']
      ? path.resolve(env['UNITY_MCP_HANDOFF_ENV_FILE'])
      : undefined;

  let fileValues: Record<string, string> = {};
  if (envFilePath && fs.existsSync(envFilePath)) {
    fileValues = parseEnvFileContents(fs.readFileSync(envFilePath, 'utf-8'));
  }

  const readValue = (key: string): string | undefined => env[key] ?? fileValues[key];

  return {
    envFilePath,
    discordBotToken: readValue('UNITY_MCP_HANDOFF_DISCORD_BOT_TOKEN'),
    discordPublicKey: readValue('UNITY_MCP_HANDOFF_DISCORD_PUBLIC_KEY'),
    discordApprovalChannelId: readValue('UNITY_MCP_HANDOFF_DISCORD_APPROVAL_CHANNEL_ID'),
    handoffLeaderActor: readValue('UNITY_MCP_HANDOFF_LEADER_ACTOR') ?? 'mac-omx-leader',
    handoffAllowedApproverIds: parseCsvList(readValue('UNITY_MCP_HANDOFF_ALLOWED_APPROVER_IDS')),
  };
}

export function assertDiscordNotifyConfig(config: HandoffBridgeConfig): Required<Pick<HandoffBridgeConfig, 'discordBotToken' | 'discordApprovalChannelId'>> {
  if (!config.discordBotToken?.trim()) {
    throw new DiscordApprovalError('Missing UNITY_MCP_HANDOFF_DISCORD_BOT_TOKEN for Discord notifications.');
  }
  if (!config.discordApprovalChannelId?.trim()) {
    throw new DiscordApprovalError('Missing UNITY_MCP_HANDOFF_DISCORD_APPROVAL_CHANNEL_ID for Discord notifications.');
  }

  return {
    discordBotToken: config.discordBotToken,
    discordApprovalChannelId: config.discordApprovalChannelId,
  };
}

export function assertDiscordServeConfig(config: HandoffBridgeConfig): Required<Pick<HandoffBridgeConfig, 'discordPublicKey'>> {
  if (!config.discordPublicKey?.trim()) {
    throw new DiscordApprovalError('Missing UNITY_MCP_HANDOFF_DISCORD_PUBLIC_KEY for Discord interaction verification.');
  }

  return {
    discordPublicKey: config.discordPublicKey,
  };
}

export function buildDiscordApprovalMessage(record: HandoffRecord): {
  content: string;
  components: DiscordComponent[];
  allowed_mentions: { parse: string[] };
} {
  const evidenceSummary = record.evidenceRefs.length > 0
    ? record.evidenceRefs.map(ref => `- ${ref}`).join('\n')
    : '- none yet';

  return {
    content: [
      `Unity-MCP handoff approval requested.`,
      `Gate: ${record.requestedAction}`,
      `Handoff ID: ${record.handoffId}`,
      `Record version: ${record.recordVersion}`,
      `Route: ${record.sourceLane} -> ${record.targetLane}`,
      `State: ${record.state}`,
      'Evidence refs:',
      evidenceSummary,
    ].join('\n'),
    components: [
      {
        type: 1,
        components: [
          {
            type: 2,
            style: 3,
            label: 'Approve',
            custom_id: DISCORD_APPROVE_CUSTOM_ID,
          },
          {
            type: 2,
            style: 4,
            label: 'Reject',
            custom_id: DISCORD_REJECT_CUSTOM_ID,
          },
        ],
      },
    ],
    allowed_mentions: { parse: [] },
  };
}

export async function sendDiscordApprovalNotification(
  config: HandoffBridgeConfig,
  record: HandoffRecord,
  fetchImpl: typeof fetch = fetch,
): Promise<DiscordNotificationSendResult> {
  const { discordBotToken, discordApprovalChannelId } = assertDiscordNotifyConfig(config);
  const response = await fetchImpl(`${DISCORD_API_BASE_URL}/channels/${discordApprovalChannelId}/messages`, {
    method: 'POST',
    headers: {
      'Authorization': `Bot ${discordBotToken}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(buildDiscordApprovalMessage(record)),
  });

  const responseText = await response.text();
  if (!response.ok) {
    throw new DiscordApprovalError(`Discord notification failed (${response.status}): ${responseText}`);
  }

  const parsed = JSON.parse(responseText) as { id?: string; channel_id?: string };
  if (!parsed.id || !parsed.channel_id) {
    throw new DiscordApprovalError('Discord notification response did not include message id/channel id.');
  }

  return {
    messageId: parsed.id,
    channelId: parsed.channel_id,
  };
}

export function verifyDiscordRequest(options: {
  publicKeyHex: string;
  signatureHex: string | undefined;
  timestamp: string | undefined;
  body: string;
  now?: number;
}): DiscordRequestVerificationResult {
  const { publicKeyHex, signatureHex, timestamp, body } = options;
  if (!signatureHex?.trim()) {
    throw new DiscordApprovalError('Missing X-Signature-Ed25519 header.');
  }
  if (!timestamp?.trim()) {
    throw new DiscordApprovalError('Missing X-Signature-Timestamp header.');
  }
  if (!/^[0-9]+$/u.test(timestamp)) {
    throw new DiscordApprovalError('Invalid X-Signature-Timestamp header.');
  }

  const requestTimestampMs = Number(timestamp) * 1000;
  const now = options.now ?? Date.now();
  if (!Number.isFinite(requestTimestampMs) || Math.abs(now - requestTimestampMs) > DISCORD_SIGNATURE_MAX_AGE_MS) {
    throw new DiscordApprovalError('Discord interaction timestamp is stale.');
  }

  const key = createDiscordPublicKeyObject(publicKeyHex);
  const isValid = crypto.verify(
    null,
    Buffer.from(`${timestamp}${body}`, 'utf-8'),
    key,
    Buffer.from(signatureHex, 'hex'),
  );

  if (!isValid) {
    throw new DiscordApprovalError('Invalid Discord request signature.');
  }

  return {
    signatureVerified: true,
    timestamp,
  };
}

export function parseDiscordInteractionRequest(body: string): DiscordInteractionRequest {
  let parsed: unknown;
  try {
    parsed = JSON.parse(body) as unknown;
  } catch (err) {
    throw new DiscordApprovalError(`Malformed Discord interaction payload: ${(err as Error).message}`);
  }

  if (typeof parsed !== 'object' || parsed === null) {
    throw new DiscordApprovalError('Invalid Discord interaction payload.');
  }

  return parsed as DiscordInteractionRequest;
}

export function getDiscordApprovalDecision(customId: string | undefined): HandoffApprovalDecision {
  if (customId === DISCORD_APPROVE_CUSTOM_ID) return 'approve';
  if (customId === DISCORD_REJECT_CUSTOM_ID) return 'reject';
  throw new DiscordApprovalError(`Unsupported Discord handoff action: ${String(customId)}`);
}

export function getDiscordInteractionActor(interaction: DiscordInteractionRequest): DiscordInteractionActor {
  const user = interaction.member?.user ?? interaction.user;
  if (!user?.id) {
    throw new DiscordApprovalError('Discord interaction did not include an actor id.');
  }

  return {
    id: user.id,
    username: user.username,
  };
}

export function assertDiscordApproverAllowed(config: HandoffBridgeConfig, actorId: string): void {
  if (config.handoffAllowedApproverIds.length === 0) {
    return;
  }
  if (!config.handoffAllowedApproverIds.includes(actorId)) {
    throw new DiscordApprovalError(`Discord actor ${actorId} is not allowed to approve this handoff.`);
  }
}

export function createDiscordPingResponse(): DiscordInteractionResponse {
  return { type: 1 };
}

export function createDiscordEphemeralResponse(content: string): DiscordInteractionResponse {
  return {
    type: 4,
    data: {
      content,
      flags: DISCORD_MESSAGE_FLAGS_EPHEMERAL,
      allowed_mentions: { parse: [] },
    },
  };
}

export function createDiscordUpdateResponse(input: {
  originalContent?: string;
  decision: HandoffApprovalDecision;
  actorId: string;
  components?: DiscordComponent[];
}): DiscordInteractionResponse {
  const verdictLine = `Decision recorded: ${input.decision} by <@${input.actorId}>.`;
  return {
    type: 7,
    data: {
      content: [input.originalContent?.trim(), verdictLine].filter(Boolean).join('\n\n'),
      components: disableDiscordComponents(input.components ?? []),
      allowed_mentions: { parse: [] },
    },
  };
}

export function disableDiscordComponents(components: DiscordComponent[]): DiscordComponent[] {
  return components.map(component => {
    if (typeof component !== 'object' || component === null || !('type' in component)) {
      return component;
    }

    if (component.type === 1 && Array.isArray((component as DiscordActionRowComponent).components)) {
      return {
        type: 1,
        components: (component as DiscordActionRowComponent).components.map(child => ({
          ...child,
          disabled: true,
        })),
      } satisfies DiscordActionRowComponent;
    }

    if (component.type === 2) {
      return {
        ...(component as DiscordButtonComponent),
        disabled: true,
      } satisfies DiscordButtonComponent;
    }

    return component;
  });
}

function createDiscordPublicKeyObject(publicKeyHex: string): crypto.KeyObject {
  const normalized = publicKeyHex.trim().toLowerCase();
  if (!/^[0-9a-f]{64}$/u.test(normalized)) {
    throw new DiscordApprovalError('Invalid Discord public key; expected 32-byte hex string.');
  }

  const der = Buffer.from(`${ED25519_SPKI_PREFIX_HEX}${normalized}`, 'hex');
  return crypto.createPublicKey({
    key: der,
    format: 'der',
    type: 'spki',
  });
}

function parseCsvList(value: string | undefined): string[] {
  if (!value) return [];
  return value
    .split(',')
    .map(entry => entry.trim())
    .filter(Boolean);
}
