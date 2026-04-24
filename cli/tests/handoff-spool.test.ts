import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { afterEach, describe, expect, it } from 'vitest';
import {
  createDiscordNotificationSpoolRecord,
  createQueuedApprovalIntentSpoolRecord,
  getQueuedApprovalIntentFilePath,
  hasQueuedApprovalIntent,
  markDiscordNotificationConsumed,
  readDiscordNotificationSpoolRecord,
  writeDiscordNotificationSpoolRecord,
  writeQueuedApprovalIntent,
} from '../src/utils/handoff-spool.js';

const tempDirs: string[] = [];

function makeProject(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-handoff-spool-'));
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  tempDirs.push(dir);
  return dir;
}

afterEach(() => {
  while (tempDirs.length > 0) {
    fs.rmSync(tempDirs.pop()!, { recursive: true, force: true });
  }
});

describe('handoff spool persistence', () => {
  it('writes and consumes Discord notification spool records', () => {
    const projectPath = makeProject();
    const record = createDiscordNotificationSpoolRecord({
      messageId: 'message-1',
      channelId: 'channel-1',
      handoffId: 'handoff-1',
      recordVersion: 2,
      requestedAction: 'plan_to_execution',
      sourceLane: 'mac-omx-leader',
      targetLane: 'windows-codex',
      sentAt: '2026-04-24T00:00:00.000Z',
    });

    writeDiscordNotificationSpoolRecord(projectPath, record);
    expect(readDiscordNotificationSpoolRecord(projectPath, 'message-1').decision).toBeNull();

    const consumed = markDiscordNotificationConsumed(projectPath, 'message-1', 'approve', '2026-04-24T00:01:00.000Z');
    expect(consumed.decision).toBe('approve');
    expect(consumed.consumedAt).toBe('2026-04-24T00:01:00.000Z');
  });

  it('stores queued approval intents idempotently by interaction id', () => {
    const projectPath = makeProject();
    const record = createQueuedApprovalIntentSpoolRecord({
      interactionId: 'interaction-1',
      handoffId: 'handoff-1',
      handoffVersion: 3,
      decision: 'reject',
      actorId: 'user-1',
      providerEvent: {
        messageId: 'message-1',
        channelId: 'channel-1',
      },
      verification: {
        signatureVerified: true,
        timestamp: '1713916800',
      },
      createdAt: '2026-04-24T00:02:00.000Z',
    });

    writeQueuedApprovalIntent(projectPath, record);
    expect(hasQueuedApprovalIntent(projectPath, 'interaction-1')).toBe(true);
    const filePath = getQueuedApprovalIntentFilePath(projectPath, 'interaction-1');
    expect(JSON.parse(fs.readFileSync(filePath, 'utf-8'))).toMatchObject({
      interactionId: 'interaction-1',
      decision: 'reject',
      actorId: 'user-1',
    });
  });
});
