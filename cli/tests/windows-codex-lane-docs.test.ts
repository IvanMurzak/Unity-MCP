import * as fs from 'fs';
import * as path from 'path';
import { describe, expect, it } from 'vitest';
import { assertWindowsHandoffSnapshot } from '../src/utils/dev-env-handoff-boundaries.js';

describe('windows codex lane starter artifacts', () => {
  it('documents the correct handoff command count', () => {
    const docPath = path.resolve('docs', 'dev-env-cicd-handoff-v1.md');
    const content = fs.readFileSync(docPath, 'utf-8');

    expect(content).toContain('The repository now exposes six bounded handoff commands:');
  });

  it('keeps the Windows lane documentation passive, external, and bounded', () => {
    const docPath = path.resolve('docs', 'windows-codex-lane-v1.md');
    const content = fs.readFileSync(docPath, 'utf-8');

    expect(content).toContain('passive Windows handoff snapshot');
    expect(content).toContain('Unity-MCP does **not** receive or store snapshot submissions');
    expect(content).toContain('assignment submission, assignment spool ownership, polling, or dispatch inside Unity-MCP ❌');
    expect(content).toContain('passive snapshot -> external runner -> evidence envelope -> `submit-windows-evidence` -> `reconcile-windows-evidence`');
    expect(content).not.toContain('handoff assignment artifact');
  });

  it('provides a companion runbook with explicit external ownership and deferred follow-ups', () => {
    const runbookPath = path.resolve('docs', 'windows-codex-runner-companion-v1.md');
    const content = fs.readFileSync(runbookPath, 'utf-8');

    expect(content).toContain('Repo-owned vs companion-owned boundary');
    expect(content).toContain('Unity-MCP does **not** own companion process supervision, mailbox polling, assignment submission, assignment spool ownership, or dispatch to Windows workers.');
    expect(content).toContain('Windows validation hardening / version-matched fixture policy');
    expect(content).toContain('Discord bridge live-ops validation and deployment runbook');
    expect(content).toContain('planner/QA executionization on top of the existing bounded role model');
  });

  it('ships a passive snapshot example aligned with the boundary validator', () => {
    const snapshotPath = path.resolve('examples', 'windows-codex-lane', 'sample-windows-handoff-snapshot.json');
    const snapshot = JSON.parse(fs.readFileSync(snapshotPath, 'utf-8'));

    expect(assertWindowsHandoffSnapshot(snapshot)).toEqual(snapshot);
    expect(snapshot.handoffId).toBe('verification-handoff-1');
    expect(snapshot.handoffRecordVersion).toBe(3);
    expect(snapshot.targetLane).toBe('windows-codex');
  });

  it('keeps the passive snapshot aligned with the sample evidence envelope semantics', () => {
    const snapshotPath = path.resolve('examples', 'windows-codex-lane', 'sample-windows-handoff-snapshot.json');
    const evidencePath = path.resolve('examples', 'windows-codex-lane', 'sample-windows-evidence.json');
    const snapshot = JSON.parse(fs.readFileSync(snapshotPath, 'utf-8'));
    const evidence = JSON.parse(fs.readFileSync(evidencePath, 'utf-8'));

    expect(snapshot.handoffId).toBe(evidence.handoffId);
    expect(snapshot.handoffRecordVersion).toBe(evidence.handoffVersion);
    expect(snapshot.targetLane).toBe('windows-codex');
    expect(evidence.sourceLane.kind).toBe('windows_codex');
  });

  it('resolves the CLI path from the PowerShell script location', () => {
    const scriptPath = path.resolve('examples', 'windows-codex-lane', 'submit-windows-evidence.ps1');
    const script = fs.readFileSync(scriptPath, 'utf-8');

    expect(script).toContain('$PSScriptRoot');
    expect(script).toContain('..\\\\..\\\\bin\\\\unity-mcp-cli.js');
    expect(script).toContain('node $CliPath handoff submit-windows-evidence');
  });
});
