import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { afterEach, describe, expect, it } from 'vitest';
import { createHandoffRecord, createLeaderWriter, readHandoffRecord, writeHandoffRecord } from '../src/utils/handoff-ledger.js';
import { listQueuedWindowsEvidenceSpoolRecords } from '../src/utils/windows-evidence-spool.js';
import { reconcileQueuedWindowsEvidence, submitQueuedWindowsEvidence } from '../src/utils/windows-evidence-reconcile.js';

const tempDirs: string[] = [];

function makeProject(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-windows-evidence-reconcile-'));
  tempDirs.push(dir);
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  return dir;
}

function makeEnvelope(handoffVersion = 1) {
  return {
    schemaVersion: 1,
    kind: 'windows_lane_evidence_envelope',
    handoffId: 'handoff-1',
    handoffVersion,
    sourceLane: {
      kind: 'windows_codex',
      laneId: 'windows-runner-1',
    },
    submittedAt: '2026-04-24T00:00:00.000Z',
    outcome: 'failed',
    summary: 'One Windows validation failed.',
    evidenceRefs: [
      {
        type: 'log',
        uri: 'file:///tmp/windows-run.log',
      },
    ],
  } as const;
}

afterEach(() => {
  while (tempDirs.length > 0) {
    fs.rmSync(tempDirs.pop()!, { recursive: true, force: true });
  }
});

describe('windows evidence reconcile', () => {
  it('applies queued evidence to the leader-owned ledger', () => {
    const projectPath = makeProject();
    const writer = createLeaderWriter('mac-omx-leader');
    writeHandoffRecord(projectPath, writer, createHandoffRecord({
      handoffId: 'handoff-1',
      sourceLane: 'windows-codex',
      targetLane: 'mac-omx-leader',
      requestedAction: 'verification -> CI/CD',
      createdBy: writer,
    }));

    submitQueuedWindowsEvidence(projectPath, makeEnvelope());
    const result = reconcileQueuedWindowsEvidence({
      projectPath,
      leaderActor: 'mac-omx-leader',
    });

    expect(result.applied).toHaveLength(1);
    expect(result.pending).toHaveLength(0);
    expect(readHandoffRecord(projectPath, 'handoff-1')).toMatchObject({
      recordVersion: 2,
      evidenceRefs: ['log:file:///tmp/windows-run.log'],
    });
    expect(listQueuedWindowsEvidenceSpoolRecords(projectPath)[0]).toMatchObject({
      consumedAt: expect.any(String),
      appliedRecordVersion: 2,
      lastError: null,
    });
  });

  it('keeps stale future-version evidence pending until a later reconcile pass', () => {
    const projectPath = makeProject();
    const writer = createLeaderWriter('mac-omx-leader');
    writeHandoffRecord(projectPath, writer, createHandoffRecord({
      handoffId: 'handoff-1',
      sourceLane: 'windows-codex',
      targetLane: 'mac-omx-leader',
      requestedAction: 'verification -> CI/CD',
      createdBy: writer,
    }));

    submitQueuedWindowsEvidence(projectPath, makeEnvelope(3));
    const result = reconcileQueuedWindowsEvidence({
      projectPath,
      leaderActor: 'mac-omx-leader',
    });

    expect(result.applied).toHaveLength(0);
    expect(result.pending).toHaveLength(1);
    expect(result.pending[0]?.reason).toContain('behind the queued evidence version');
    expect(readHandoffRecord(projectPath, 'handoff-1').evidenceRefs).toEqual([]);
    expect(listQueuedWindowsEvidenceSpoolRecords(projectPath)[0]?.lastError).toContain('behind the queued evidence version');
  });
});
