import * as crypto from 'crypto';
import { describe, expect, it } from 'vitest';
import {
  DISCORD_APPROVE_CUSTOM_ID,
  DISCORD_REJECT_CUSTOM_ID,
  assertDiscordApproverAllowed,
  buildDiscordApprovalMessage,
  createDiscordEphemeralResponse,
  createDiscordUpdateResponse,
  getDiscordApprovalDecision,
  loadHandoffBridgeConfig,
  parseEnvFileContents,
  sendDiscordApprovalNotification,
  verifyDiscordRequest,
} from '../src/utils/discord-approval.js';
import { createHandoffRecord, createLeaderWriter } from '../src/utils/handoff-ledger.js';

describe('discord handoff approval helpers', () => {
  it('parses env files and applies process env overrides', () => {
    const config = loadHandoffBridgeConfig({
      env: {
        UNITY_MCP_HANDOFF_ENV_FILE: '/tmp/ignored.env',
        UNITY_MCP_HANDOFF_DISCORD_BOT_TOKEN: 'env-bot-token',
        UNITY_MCP_HANDOFF_ALLOWED_APPROVER_IDS: '123, 456',
      },
    });

    expect(parseEnvFileContents('A=1\n# comment\nB = "two"\n')).toEqual({ A: '1', B: 'two' });
    expect(config.discordBotToken).toBe('env-bot-token');
    expect(config.handoffAllowedApproverIds).toEqual(['123', '456']);
  });

  it('builds a bounded Discord approval message with approve/reject controls', () => {
    const record = createHandoffRecord({
      handoffId: 'handoff-1',
      sourceLane: 'mac-omx-leader',
      targetLane: 'windows-codex',
      requestedAction: 'plan_to_execution',
      createdBy: createLeaderWriter('leader'),
      evidenceRefs: ['logs/plan.md'],
    });

    const message = buildDiscordApprovalMessage(record);
    expect(message.content).toContain('Handoff ID: handoff-1');
    expect(message.content).toContain('Record version: 1');
    expect(message.components).toHaveLength(1);
    expect((message.components[0] as { components: { custom_id: string }[] }).components.map(button => button.custom_id))
      .toEqual([DISCORD_APPROVE_CUSTOM_ID, DISCORD_REJECT_CUSTOM_ID]);
  });

  it('verifies Discord request signatures using the raw application public key', () => {
    const { publicKey, privateKey } = crypto.generateKeyPairSync('ed25519');
    const publicDer = publicKey.export({ format: 'der', type: 'spki' }) as Buffer;
    const publicKeyHex = publicDer.subarray(publicDer.length - 32).toString('hex');
    const body = JSON.stringify({ type: 1 });
    const timestamp = `${Math.floor(Date.now() / 1000)}`;
    const signature = crypto.sign(null, Buffer.from(`${timestamp}${body}`, 'utf-8'), privateKey).toString('hex');

    expect(verifyDiscordRequest({
      publicKeyHex,
      signatureHex: signature,
      timestamp,
      body,
    })).toEqual({ signatureVerified: true, timestamp });

    expect(() => verifyDiscordRequest({
      publicKeyHex,
      signatureHex: signature,
      timestamp: `${Math.floor(Date.now() / 1000) - 1000}`,
      body,
      now: Date.now(),
    })).toThrowError(/stale/);
  });

  it('normalizes decisions and approver allowlists', () => {
    expect(getDiscordApprovalDecision(DISCORD_APPROVE_CUSTOM_ID)).toBe('approve');
    expect(getDiscordApprovalDecision(DISCORD_REJECT_CUSTOM_ID)).toBe('reject');
    expect(() => getDiscordApprovalDecision('other')).toThrowError(/Unsupported/);

    expect(() => assertDiscordApproverAllowed({
      handoffLeaderActor: 'leader',
      handoffAllowedApproverIds: ['123'],
    }, '456')).toThrowError(/not allowed/);
  });

  it('creates ephemeral and update responses without re-enabling buttons', () => {
    expect(createDiscordEphemeralResponse('Nope')).toEqual({
      type: 4,
      data: {
        content: 'Nope',
        flags: 64,
        allowed_mentions: { parse: [] },
      },
    });

    const response = createDiscordUpdateResponse({
      originalContent: 'Original',
      decision: 'approve',
      actorId: '123',
      components: [{
        type: 1,
        components: [{
          type: 2,
          style: 3,
          label: 'Approve',
          custom_id: DISCORD_APPROVE_CUSTOM_ID,
        }],
      }],
    });

    expect(response.type).toBe(7);
    expect(response.data?.content).toContain('Decision recorded: approve by <@123>.');
    expect(((response.data?.components?.[0] as { components: { disabled?: boolean }[] }).components[0]).disabled).toBe(true);
  });

  it('sends notifications through Discord channel messages', async () => {
    const record = createHandoffRecord({
      handoffId: 'handoff-2',
      sourceLane: 'mac-omx-leader',
      targetLane: 'bot-ci-bridge',
      requestedAction: 'verification_to_cicd',
      createdBy: createLeaderWriter('leader'),
    });

    const result = await sendDiscordApprovalNotification({
      discordBotToken: 'bot-token',
      discordApprovalChannelId: 'channel-1',
      handoffLeaderActor: 'leader',
      handoffAllowedApproverIds: [],
    }, record, async (url, init) => {
      expect(String(url)).toContain('/channels/channel-1/messages');
      expect(init?.method).toBe('POST');
      expect((init?.headers as Record<string, string>)['Authorization']).toBe('Bot bot-token');
      return new Response(JSON.stringify({ id: 'message-1', channel_id: 'channel-1' }), { status: 200 });
    });

    expect(result).toEqual({ messageId: 'message-1', channelId: 'channel-1' });
  });
});
