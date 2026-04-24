import * as fs from 'fs';
import * as path from 'path';
import { describe, expect, it } from 'vitest';

describe('windows codex lane starter artifacts', () => {
  it('documents the correct handoff command count', () => {
    const docPath = path.resolve('docs', 'dev-env-cicd-handoff-v1.md');
    const content = fs.readFileSync(docPath, 'utf-8');

    expect(content).toContain('The repository now exposes six bounded handoff commands:');
  });

  it('resolves the CLI path from the PowerShell script location', () => {
    const scriptPath = path.resolve('examples', 'windows-codex-lane', 'submit-windows-evidence.ps1');
    const script = fs.readFileSync(scriptPath, 'utf-8');

    expect(script).toContain('$PSScriptRoot');
    expect(script).toContain('..\\\\..\\\\bin\\\\unity-mcp-cli.js');
    expect(script).toContain('node $CliPath handoff submit-windows-evidence');
  });
});
