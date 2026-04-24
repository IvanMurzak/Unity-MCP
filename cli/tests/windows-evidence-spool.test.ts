import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { afterEach, describe, expect, it } from 'vitest';
import {
  formatWindowsEvidenceRefsForLedger,
  listQueuedWindowsEvidenceSpoolRecords,
  queueWindowsEvidenceEnvelope,
  readQueuedWindowsEvidenceSpoolRecord,
} from '../src/utils/windows-evidence-spool.js';

const tempDirs: string[] = [];

function makeProject(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-windows-evidence-spool-'));
  tempDirs.push(dir);
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  return dir;
}

function makeEnvelope() {
  return {
    schemaVersion: 1,
    kind: 'windows_lane_evidence_envelope',
    handoffId: 'handoff-1',
    handoffVersion: 2,
    sourceLane: {
      kind: 'windows_codex',
      laneId: 'windows-runner-1',
    },
    submittedAt: '2026-04-24T00:00:00.000Z',
    outcome: 'passed',
    summary: 'Windows validation passed.',
    evidenceRefs: [
      {
        type: 'log',
        uri: 'file:///tmp/windows-run.log',
        sha256: 'abc123',
      },
      {
        type: 'test_report',
        uri: 'file:///tmp/windows-tests.xml',
      },
    ],
  } as const;
}

afterEach(() => {
  while (tempDirs.length > 0) {
    fs.rmSync(tempDirs.pop()!, { recursive: true, force: true });
  }
});

describe('windows evidence spool', () => {
  it('queues and deduplicates the same envelope payload', () => {
    const projectPath = makeProject();
    const envelope = makeEnvelope();

    const first = queueWindowsEvidenceEnvelope(projectPath, envelope);
    const second = queueWindowsEvidenceEnvelope(projectPath, envelope);

    expect(first.duplicate).toBe(false);
    expect(second.duplicate).toBe(true);
    expect(first.record.recordId).toBe(second.record.recordId);
    expect(listQueuedWindowsEvidenceSpoolRecords(projectPath)).toHaveLength(1);
    expect(readQueuedWindowsEvidenceSpoolRecord(projectPath, first.record.recordId)).toMatchObject({
      handoffId: 'handoff-1',
      handoffVersion: 2,
      sourceLaneId: 'windows-runner-1',
      outcome: 'passed',
      consumedAt: null,
      appliedRecordVersion: null,
    });
  });

  it('formats structured evidence refs for the string-based ledger', () => {
    expect(formatWindowsEvidenceRefsForLedger(makeEnvelope())).toEqual([
      'log:file:///tmp/windows-run.log#sha256=abc123',
      'test_report:file:///tmp/windows-tests.xml',
    ]);
  });
});
