import * as http from 'http';
import {
  applyApprovalIntent,
  createApprovalIntent,
  createLeaderWriter,
  readHandoffRecord,
  writeHandoffRecord,
} from './handoff-ledger.js';
import {
  assertDiscordApproverAllowed,
  assertDiscordServeConfig,
  createDiscordEphemeralResponse,
  createDiscordPingResponse,
  createDiscordUpdateResponse,
  type DiscordInteractionRequest,
  type DiscordInteractionResponse,
  type HandoffBridgeConfig,
  DiscordApprovalError,
  getDiscordApprovalDecision,
  getDiscordInteractionActor,
  parseDiscordInteractionRequest,
  verifyDiscordRequest,
} from './discord-approval.js';
import {
  createDiscordNotificationSpoolRecord,
  createQueuedApprovalIntentSpoolRecord,
  hasQueuedApprovalIntent,
  markDiscordNotificationConsumed,
  readDiscordNotificationSpoolRecord,
  writeDiscordNotificationSpoolRecord,
  writeQueuedApprovalIntent,
} from './handoff-spool.js';

export interface HandoffServeOptions {
  projectPath: string;
  config: HandoffBridgeConfig;
  host?: string;
  port: number;
}

export interface HandoffHttpResponse {
  statusCode: number;
  headers: Record<string, string>;
  body: string;
}

export class HandoffServerError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'HandoffServerError';
  }
}

export function createHealthResponse(): HandoffHttpResponse {
  return jsonResponse(200, {
    ok: true,
    service: 'unity-mcp-handoff-bridge',
  });
}

export function createNotFoundResponse(): HandoffHttpResponse {
  return jsonResponse(404, { error: 'Not found' });
}

export function createMethodNotAllowedResponse(): HandoffHttpResponse {
  return jsonResponse(405, { error: 'Method not allowed' });
}

export function handleDiscordInteractionRequest(input: {
  projectPath: string;
  config: HandoffBridgeConfig;
  rawBody: string;
  signature: string | undefined;
  timestamp: string | undefined;
  now?: number;
}): HandoffHttpResponse {
  const { discordPublicKey } = assertDiscordServeConfig(input.config);
  const verification = verifyDiscordRequest({
    publicKeyHex: discordPublicKey,
    signatureHex: input.signature,
    timestamp: input.timestamp,
    body: input.rawBody,
    now: input.now,
  });

  const interaction = parseDiscordInteractionRequest(input.rawBody);
  if (interaction.type === 1) {
    return jsonResponse(200, createDiscordPingResponse());
  }

  if (interaction.type !== 3) {
    return jsonResponse(400, createDiscordEphemeralResponse(`Unsupported Discord interaction type: ${String(interaction.type)}`));
  }

  try {
    const approvalResponse = consumeDiscordApprovalInteraction({
      projectPath: input.projectPath,
      config: input.config,
      interaction,
      verification,
    });
    return jsonResponse(200, approvalResponse);
  } catch (err) {
    if (err instanceof DiscordApprovalError || err instanceof HandoffServerError || err instanceof Error) {
      return jsonResponse(200, createDiscordEphemeralResponse(err.message));
    }
    return jsonResponse(500, createDiscordEphemeralResponse('Unknown handoff bridge error.'));
  }
}

export function startHandoffServer(options: HandoffServeOptions): Promise<http.Server> {
  const server = http.createServer((req, res) => {
    void handleRequest(req, res, options);
  });

  return new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(options.port, options.host ?? '127.0.0.1', () => {
      server.off('error', reject);
      resolve(server);
    });
  });
}

function consumeDiscordApprovalInteraction(input: {
  projectPath: string;
  config: HandoffBridgeConfig;
  interaction: DiscordInteractionRequest;
  verification: { signatureVerified: true; timestamp: string };
}): DiscordInteractionResponse {
  if (!input.interaction.id) {
    throw new HandoffServerError('Discord interaction did not include an interaction id.');
  }
  if (!input.interaction.message?.id) {
    throw new HandoffServerError('Discord interaction did not include the source message id.');
  }
  if (hasQueuedApprovalIntent(input.projectPath, input.interaction.id)) {
    throw new HandoffServerError('This Discord approval interaction was already consumed.');
  }

  const notification = readDiscordNotificationSpoolRecord(input.projectPath, input.interaction.message.id);
  if (notification.consumedAt) {
    throw new HandoffServerError(`This handoff was already ${notification.decision ?? 'processed'}.`);
  }

  const actor = getDiscordInteractionActor(input.interaction);
  assertDiscordApproverAllowed(input.config, actor.id);

  const decision = getDiscordApprovalDecision(input.interaction.data?.custom_id);
  const queuedIntent = createQueuedApprovalIntentSpoolRecord({
    interactionId: input.interaction.id,
    handoffId: notification.handoffId,
    handoffVersion: notification.recordVersion,
    decision,
    actorId: actor.id,
    providerEvent: {
      messageId: notification.messageId,
      channelId: notification.channelId,
    },
    verification: input.verification,
  });
  writeQueuedApprovalIntent(input.projectPath, queuedIntent);

  const writer = createLeaderWriter(input.config.handoffLeaderActor);
  const record = readHandoffRecord(input.projectPath, notification.handoffId);
  const intent = createApprovalIntent({
    handoffId: notification.handoffId,
    recordVersion: notification.recordVersion,
    decision,
    approverIdentity: actor.id,
    provider: 'discord',
  });
  const nextRecord = applyApprovalIntent(record, writer, intent);
  writeHandoffRecord(input.projectPath, writer, nextRecord);
  markDiscordNotificationConsumed(input.projectPath, notification.messageId, decision, new Date().toISOString());

  return createDiscordUpdateResponse({
    originalContent: input.interaction.message.content,
    decision,
    actorId: actor.id,
    components: input.interaction.message.components,
  });
}

async function handleRequest(
  req: http.IncomingMessage,
  res: http.ServerResponse,
  options: HandoffServeOptions,
): Promise<void> {
  try {
    const response = await routeRequest(req, options);
    res.statusCode = response.statusCode;
    for (const [key, value] of Object.entries(response.headers)) {
      res.setHeader(key, value);
    }
    res.end(response.body);
  } catch (err) {
    const message = err instanceof Error ? err.message : 'Unknown handoff server error';
    const response = jsonResponse(500, { error: message });
    res.statusCode = response.statusCode;
    for (const [key, value] of Object.entries(response.headers)) {
      res.setHeader(key, value);
    }
    res.end(response.body);
  }
}

async function routeRequest(req: http.IncomingMessage, options: HandoffServeOptions): Promise<HandoffHttpResponse> {
  const method = req.method ?? 'GET';
  const url = req.url ?? '/';

  if (method === 'GET' && url === '/healthz') {
    return createHealthResponse();
  }

  if (method === 'POST' && url === '/discord/interactions') {
    const rawBody = await readRequestBody(req);
    return handleDiscordInteractionRequest({
      projectPath: options.projectPath,
      config: options.config,
      rawBody,
      signature: headerValue(req.headers['x-signature-ed25519']),
      timestamp: headerValue(req.headers['x-signature-timestamp']),
    });
  }

  if (url === '/discord/interactions' || url === '/healthz') {
    return createMethodNotAllowedResponse();
  }

  return createNotFoundResponse();
}

async function readRequestBody(req: http.IncomingMessage): Promise<string> {
  const chunks: Buffer[] = [];
  for await (const chunk of req) {
    chunks.push(typeof chunk === 'string' ? Buffer.from(chunk) : chunk);
  }
  return Buffer.concat(chunks).toString('utf-8');
}

function headerValue(value: string | string[] | undefined): string | undefined {
  if (Array.isArray(value)) {
    return value[0];
  }
  return value;
}

function jsonResponse(statusCode: number, body: unknown): HandoffHttpResponse {
  return {
    statusCode,
    headers: {
      'content-type': 'application/json; charset=utf-8',
    },
    body: JSON.stringify(body),
  };
}

export function createDiscordNotificationSpoolSnapshot(input: {
  projectPath: string;
  messageId: string;
  channelId: string;
  handoffId: string;
  recordVersion: number;
  requestedAction: string;
  sourceLane: string;
  targetLane: string;
}): string {
  const record = createDiscordNotificationSpoolRecord(input);
  return writeDiscordNotificationSpoolRecord(input.projectPath, record);
}
