import { describe, it, expect } from 'vitest';
import { execFileSync } from 'child_process';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');

function runCli(args: string[]): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 10000,
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

describe('open command (merged open + connect)', () => {
  it('includes --no-connect option in help', () => {
    const { stdout, exitCode } = runCli(['open', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('--no-connect');
  });

  it('includes all MCP connection options in help', () => {
    const { stdout, exitCode } = runCli(['open', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('--url');
    expect(stdout).toContain('--tools');
    expect(stdout).toContain('--token');
    expect(stdout).toContain('--auth');
    expect(stdout).toContain('--keep-connected');
    expect(stdout).toContain('--transport');
    expect(stdout).toContain('--start-server');
  });

  it('includes --unity option in help', () => {
    const { stdout, exitCode } = runCli(['open', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('--unity');
  });

  it('fails when no path is provided', () => {
    const { exitCode, stdout } = runCli(['open']);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('Path is required');
  });

  it('fails when project path does not exist', () => {
    const { exitCode, stdout } = runCli(['open', '/nonexistent/path/12345']);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('does not exist');
  });
});
