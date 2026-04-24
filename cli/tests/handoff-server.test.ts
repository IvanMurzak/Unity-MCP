import * as crypto from 'crypto';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { afterEach, describe, expect, it } from 'vitest';
import { createDiscordNotificationSpoolSnapshot, handleDiscordInteractionRequest } from '../src/utils/handoff-server.js';
import { createHandoffRecord, createLeaderWriter, readHandoffRecord, transitionHandoffState, writeHandoffRecord } from '../src/utils/handoff-ledger.js';

const tempDirs: string[] = [];

function makeProject(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-handoff-server-'));
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  tempDirs.push(dir);
  return dir;
}

function signBody(body: string) {
  const { publicKey, privateKey } = crypto.generateKeyPairSync('ed25519');
  const publicDer = publicKey.export({ format: 'der', type: 'spki' }) as Buffer;
  const publicKeyHex = publicDer.subarray(publicDer.length - 32).toString('hex');
  const timestamp = `${Math.floor(Date.now() / 1000)}`;
  const signature = crypto.sign(null, Buffer.from(`${timestamp}${body}`, 'utf-8'), privateKey).toString('hex');
  return { publicKeyHex, timestamp, signature };
}

afterEach(() => {
  while (tempDirs.length > 0) {
    fs.rmSync(tempDirs.pop()!, { recursive: true, force: true });
  }
});

describe('handoff bridge Discord interaction ingestion', () => {
  it('acknowledges Discord pings', () => {
    const body = JSON.stringify({ type: 1 });
    const signed = signBody(body);

    const response = handleDiscordInteractionRequest({
      projectPath: makeProject(),
      config: {
        discordPublicKey: signed.publicKeyHex,
        handoffLeaderActor: 'mac-omx-leader',
        handoffAllowedApproverIds: [],
      },
      rawBody: body,
      signature: signed.signature,
      timestamp: signed.timestamp,
    });

    expect(response.statusCode).toBe(200);
    expect(JSON.parse(response.body)).toEqual({ type: 1 });
  });

  it('applies a verified Discord approval to the leader-owned ledger and disables message buttons', () => {
    const projectPath = makeProject();
    const writer = createLeaderWriter('mac-omx-leader');
    const draft = createHandoffRecord({
      handoffId: 'handoff-1',
      sourceLane: 'mac-omx-leader',
      targetLane: 'windows-codex',
      requestedAction: 'plan_to_execution',
      createdBy: writer,
    });
    const awaiting = transitionHandoffState(draft, writer, 'awaiting_approval', { updatedAt: '2026-04-24T00:01:00.000Z' });
    writeHandoffRecord(projectPath, writer, awaiting);
    createDiscordNotificationSpoolSnapshot({
      projectPath,
      messageId: 'message-1',
      channelId: 'channel-1',
      handoffId: 'handoff-1',
      recordVersion: awaiting.recordVersion,
      requestedAction: awaiting.requestedAction,
      sourceLane: awaiting.sourceLane,
      targetLane: awaiting.targetLane,
    });

    const body = JSON.stringify({
      id: 'interaction-1',
      type: 3,
      member: { user: { id: 'approver-1', username: 'approver' } },
      data: { custom_id: 'unity_mcp_handoff_approve', component_type: 2 },
      message: {
        id: 'message-1',
        channel_id: 'channel-1',
        content: 'Approval requested',
        components: [{
          type: 1,
          components: [{ type: 2, style: 3, label: 'Approve', custom_id: 'unity_mcp_handoff_approve' }],
        }],
      },
    });
    const signed = signBody(body);

    const response = handleDiscordInteractionRequest({
      projectPath,
      config: {
        discordPublicKey: signed.publicKeyHex,
        handoffLeaderActor: 'mac-omx-leader',
        handoffAllowedApproverIds: ['approver-1'],
      },
      rawBody: body,
      signature: signed.signature,
      timestamp: signed.timestamp,
    });

    expect(response.statusCode).toBe(200);
    const interactionResponse = JSON.parse(response.body);
    expect(interactionResponse.type).toBe(7);
    expect(interactionResponse.data.content).toContain('Decision recorded: approve by <@approver-1>.');
    expect(interactionResponse.data.components[0].components[0].disabled).toBe(true);

    const updated = readHandoffRecord(projectPath, 'handoff-1');
    expect(updated.state).toBe('approved_not_dispatched');
    expect(updated.approverIdentity).toBe('approver-1');
    expect(updated.approvalVersion).toBe(1);

    const queuedPath = path.join(projectPath, '.unity-mcp', 'handoff-spool', 'approval-intents', 'interaction-1.json');
    expect(JSON.parse(fs.readFileSync(queuedPath, 'utf-8'))).toMatchObject({
      interactionId: 'interaction-1',
      handoffId: 'handoff-1',
      decision: 'approve',
    });
  });

  it('rejects unauthorized Discord approvers without mutating the ledger', () => {
    const projectPath = makeProject();
    const writer = createLeaderWriter('mac-omx-leader');
    const awaiting = transitionHandoffState(createHandoffRecord({
      handoffId: 'handoff-2',
      sourceLane: 'mac-omx-leader',
      targetLane: 'bot-ci-bridge',
      requestedAction: 'verification_to_cicd',
      createdBy: writer,
    }), writer, 'awaiting_approval');
    writeHandoffRecord(projectPath, writer, awaiting);
    createDiscordNotificationSpoolSnapshot({
      projectPath,
      messageId: 'message-2',
      channelId: 'channel-1',
      handoffId: 'handoff-2',
      recordVersion: awaiting.recordVersion,
      requestedAction: awaiting.requestedAction,
      sourceLane: awaiting.sourceLane,
      targetLane: awaiting.targetLane,
    });

    const body = JSON.stringify({
      id: 'interaction-2',
      type: 3,
      member: { user: { id: 'intruder-1', username: 'intruder' } },
      data: { custom_id: 'unity_mcp_handoff_reject', component_type: 2 },
      message: { id: 'message-2', channel_id: 'channel-1', content: 'Approval requested', components: [] },
    });
    const signed = signBody(body);

    const response = handleDiscordInteractionRequest({
      projectPath,
      config: {
        discordPublicKey: signed.publicKeyHex,
        handoffLeaderActor: 'mac-omx-leader',
        handoffAllowedApproverIds: ['approver-1'],
      },
      rawBody: body,
      signature: signed.signature,
      timestamp: signed.timestamp,
    });

    expect(response.statusCode).toBe(200);
    const interactionResponse = JSON.parse(response.body);
    expect(interactionResponse.type).toBe(4);
    expect(interactionResponse.data.content).toContain('not allowed');

    expect(readHandoffRecord(projectPath, 'handoff-2').state).toBe('awaiting_approval');
  });
});
