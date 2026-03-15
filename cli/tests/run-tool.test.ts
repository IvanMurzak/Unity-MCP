import { describe, it, expect } from 'vitest';
import { execFileSync } from 'child_process';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');

function runCli(args: string[], options?: { cwd?: string }): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 10000,
      cwd: options?.cwd,
    });
    return { stdout, exitCode: 0 };
  } catch (err: unknown) {
    const error = err as { stdout?: string; stderr?: string; status?: number };
    return {
      stdout: (error.stdout ?? '') + (error.stderr ?? ''),
      exitCode: error.status ?? 1,
    };
  }
}

describe('run-tool command', () => {
  it('shows help with --help', () => {
    const { stdout, exitCode } = runCli(['run-tool', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('run-tool');
    expect(stdout).toContain('--url');
    expect(stdout).toContain('--input');
    expect(stdout).toContain('--input-file');
    expect(stdout).toContain('--token');
    expect(stdout).toContain('--raw');
    expect(stdout).toContain('--path');
  });

  it('requires tool name argument', () => {
    const { exitCode, stdout } = runCli(['run-tool']);
    expect(exitCode).toBe(1);
    expect(stdout).toContain("missing required argument 'tool-name'");
  });

  it('appears in global help', () => {
    const { stdout, exitCode } = runCli(['--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('run-tool');
  });

  it('validates --input is valid JSON', () => {
    const { exitCode, stdout } = runCli([
      'run-tool', 'test-tool',
      '--url', 'http://localhost:99999',
      '--input', 'not-valid-json',
    ]);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('--input must be valid JSON');
  });
});
