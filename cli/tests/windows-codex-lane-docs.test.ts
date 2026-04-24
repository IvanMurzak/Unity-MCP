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
    expect(content).toContain('windows_validation_smoke_v1');
    expect(content).toContain('The runner stops here. It does **not** poll Unity-MCP with `list-windows-evidence`');
    expect(content).toContain('Companion `outbox/` is not a Unity-MCP queue');
    expect(content).not.toContain('handoff assignment artifact');
  });

  it('provides a companion runbook with explicit external ownership and validation-first profile details', () => {
    const runbookPath = path.resolve('docs', 'windows-codex-runner-companion-v1.md');
    const content = fs.readFileSync(runbookPath, 'utf-8');

    expect(content).toContain('Repo-owned vs companion-owned boundary');
    expect(content).toContain('running the `windows_validation_smoke_v1` validation recipe');
    expect(content).toContain('stopping after submit and waiting for the mac leader to reconcile or request another run');
    expect(content).toContain('Companion `outbox/` is **not** Unity-MCP\'s queue.');
    expect(content).toContain('Do **not** call `handoff list-windows-evidence` as a runner polling loop.');
    expect(content).toContain('Windows validation hardening / version-matched fixture policy');
    expect(content).toContain('broader implementation-lane execution beyond validation-first smoke');
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

  it('ships a runner example that stays inside the bounded validation path', () => {
    const scriptPath = path.resolve('examples', 'windows-codex-lane', 'run-windows-validation-runner-v1.ps1');
    const script = fs.readFileSync(scriptPath, 'utf-8');

    expect(script).toContain('windows_validation_smoke_v1');
    expect(script).toContain("'team', 'launch'");
    expect(script).toContain("'team', 'status'");
    expect(script).toContain("'team', 'list'");
    expect(script).toContain("'team', 'stop'");
    expect(script).toContain('Stop-Process -Id');
    expect(script).toContain('submit-windows-evidence');
    expect(script).toContain('no list-windows-evidence polling or reconcile call is performed by this runner');
    expect(script).not.toContain('list-windows-evidence $ProjectPath');
    expect(script).not.toContain('reconcile-windows-evidence $ProjectPath');
  });

  it('resolves the CLI path from the PowerShell submit script location', () => {
    const scriptPath = path.resolve('examples', 'windows-codex-lane', 'submit-windows-evidence.ps1');
    const script = fs.readFileSync(scriptPath, 'utf-8');

    expect(script).toContain('$PSScriptRoot');
    expect(script).toContain('..\\\\..\\\\bin\\\\unity-mcp-cli.js');
    expect(script).toContain('node $CliPath handoff submit-windows-evidence');
  });
});
