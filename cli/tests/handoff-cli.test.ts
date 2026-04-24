import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { afterEach, describe, expect, it } from 'vitest';
import { createHandoffRecord, createLeaderWriter, readHandoffRecord, writeHandoffRecord } from '../src/utils/handoff-ledger.js';
import { runCliAsync } from './helpers/cli.js';

const tempDirs: string[] = [];

function makeProject(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-handoff-cli-'));
  tempDirs.push(dir);
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  return dir;
}

afterEach(() => {
  while (tempDirs.length > 0) {
    fs.rmSync(tempDirs.pop()!, { recursive: true, force: true });
  }
});

describe('handoff command surface', () => {
  it('shows the handoff subcommands in help output', async () => {
    const result = await runCliAsync(['handoff', '--help']);

    expect(result.exitCode).toBe(0);
    expect(result.stdout).toContain('serve');
    expect(result.stdout).toContain('notify-discord');
    expect(result.stdout).toContain('dispatch-approved');
    expect(result.stdout).toContain('submit-windows-evidence');
    expect(result.stdout).toContain('list-windows-evidence');
    expect(result.stdout).toContain('reconcile-windows-evidence');
  });

  it('queues and reconciles Windows evidence through the CLI', async () => {
    const projectPath = makeProject();
    const writer = createLeaderWriter('mac-omx-leader');
    writeHandoffRecord(projectPath, writer, createHandoffRecord({
      handoffId: 'handoff-1',
      sourceLane: 'windows-codex',
      targetLane: 'mac-omx-leader',
      requestedAction: 'verification -> CI/CD',
      createdBy: writer,
    }));

    const evidencePath = path.join(projectPath, 'windows-evidence.json');
    fs.writeFileSync(evidencePath, JSON.stringify({
      schemaVersion: 1,
      kind: 'windows_lane_evidence_envelope',
      handoffId: 'handoff-1',
      handoffVersion: 1,
      sourceLane: {
        kind: 'windows_codex',
        laneId: 'windows-runner-1',
      },
      submittedAt: '2026-04-24T00:00:00.000Z',
      outcome: 'passed',
      summary: 'CLI integration evidence',
      evidenceRefs: [
        {
          type: 'note',
          uri: 'file:///tmp/windows-evidence-note.txt',
        },
      ],
    }, null, 2));

    const submit = await runCliAsync(['handoff', 'submit-windows-evidence', '--path', projectPath, '--input-file', evidencePath]);
    expect(submit.exitCode).toBe(0);
    expect(submit.stdout).toContain('Queued Windows evidence');

    const reconcile = await runCliAsync(['handoff', 'reconcile-windows-evidence', '--path', projectPath, '--leader-actor', 'mac-omx-leader']);
    expect(reconcile.exitCode).toBe(0);
    expect(reconcile.stdout).toContain('APPLIED handoff-1@1');
    expect(readHandoffRecord(projectPath, 'handoff-1').evidenceRefs).toEqual([
      'note:file:///tmp/windows-evidence-note.txt',
    ]);
  });

  it('lists queued Windows evidence through the CLI', async () => {
    const projectPath = makeProject();
    const writer = createLeaderWriter('mac-omx-leader');
    writeHandoffRecord(projectPath, writer, createHandoffRecord({
      handoffId: 'handoff-2',
      sourceLane: 'windows-codex',
      targetLane: 'mac-omx-leader',
      requestedAction: 'verification -> CI/CD',
      createdBy: writer,
    }));

    const evidencePath = path.join(projectPath, 'windows-evidence-list.json');
    fs.writeFileSync(evidencePath, JSON.stringify({
      schemaVersion: 1,
      kind: 'windows_lane_evidence_envelope',
      handoffId: 'handoff-2',
      handoffVersion: 1,
      sourceLane: {
        kind: 'windows_codex',
        laneId: 'windows-runner-2',
      },
      submittedAt: '2026-04-24T00:00:00.000Z',
      outcome: 'blocked',
      summary: 'Waiting on Unity editor lock.',
      evidenceRefs: [
        {
          type: 'log',
          uri: 'file:///tmp/windows-evidence-log.txt',
        },
      ],
    }, null, 2));

    await runCliAsync(['handoff', 'submit-windows-evidence', '--path', projectPath, '--input-file', evidencePath]);
    const list = await runCliAsync(['handoff', 'list-windows-evidence', '--path', projectPath]);
    expect(list.exitCode).toBe(0);
    expect(list.stdout).toContain('handoff-2@1');
    expect(list.stdout).toContain('windows-runner-2');
    expect(list.stdout).toContain('blocked');
    expect(list.stdout).toContain('pending');
  });
});
