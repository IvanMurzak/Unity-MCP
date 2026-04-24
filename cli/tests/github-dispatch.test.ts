import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { afterEach, describe, expect, it } from 'vitest';
import { dispatchApprovedVerificationHandoff } from '../src/utils/github-dispatch.js';
import { createHandoffRecord, createLeaderWriter, readHandoffRecord, transitionHandoffState, writeHandoffRecord } from '../src/utils/handoff-ledger.js';

const tempDirs: string[] = [];

function makeProject(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-github-dispatch-'));
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  tempDirs.push(dir);
  return dir;
}

afterEach(() => {
  while (tempDirs.length > 0) {
    fs.rmSync(tempDirs.pop()!, { recursive: true, force: true });
  }
});

describe('github handoff dispatch', () => {
  it('dispatches approved verification handoffs and persists provenance', async () => {
    const projectPath = makeProject();
    const writer = createLeaderWriter('mac-omx-leader');
    const awaiting = transitionHandoffState(createHandoffRecord({
      handoffId: 'handoff-1',
      sourceLane: 'windows-codex',
      targetLane: 'bot-ci-bridge',
      requestedAction: 'verification_to_cicd',
      createdBy: writer,
    }), writer, 'awaiting_approval');
    const approved = transitionHandoffState({
      ...awaiting,
      approverIdentity: 'approver-1',
      approvalVersion: 1,
    }, writer, 'approved_not_dispatched');
    writeHandoffRecord(projectPath, writer, approved);

    const dispatch = await dispatchApprovedVerificationHandoff({
      projectPath,
      handoffId: 'handoff-1',
      bridgeConfig: {
        handoffLeaderActor: 'mac-omx-leader',
        handoffAllowedApproverIds: [],
      },
      env: {
        UNITY_MCP_HANDOFF_GITHUB_TOKEN: 'token-1',
        UNITY_MCP_HANDOFF_GITHUB_REPOSITORY: 'owner/repo',
      },
      fetchImpl: async (url, init) => {
        expect(String(url)).toBe('https://api.github.com/repos/owner/repo/dispatches');
        expect(init?.method).toBe('POST');
        const body = JSON.parse(String(init?.body));
        expect(body.event_type).toBe('unity-mcp-approved-verification');
        expect(body.client_payload).toMatchObject({
          handoff_id: 'handoff-1',
          approval_gate: 'verification_to_cicd',
        });
        return new Response(null, { status: 204 });
      },
    });

    expect(dispatch.eventType).toBe('unity-mcp-approved-verification');
    expect(readHandoffRecord(projectPath, 'handoff-1')).toMatchObject({
      state: 'dispatched',
      dispatchProvenance: {
        provider: 'github',
        target: 'owner/repo:unity-mcp-approved-verification',
      },
    });
  });
});
