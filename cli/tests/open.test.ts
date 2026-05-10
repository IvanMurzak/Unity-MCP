import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { execFileSync } from 'child_process';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { fileURLToPath } from 'url';
import {
  isUnityProjectDir,
  parsePositiveIntFlag,
  resolveOpenProjectPath,
} from '../src/commands/open.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');

interface RunCliOptions {
  cwd?: string;
}

function runCli(args: string[], opts: RunCliOptions = {}): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 10000,
      cwd: opts.cwd,
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

/** Create a minimal Unity-project-shaped directory (Assets + ProjectVersion.txt). */
function makeFakeUnityProject(dir: string): void {
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  const settingsDir = path.join(dir, 'ProjectSettings');
  fs.mkdirSync(settingsDir, { recursive: true });
  fs.writeFileSync(
    path.join(settingsDir, 'ProjectVersion.txt'),
    'm_EditorVersion: 6000.0.0f1\n',
  );
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

  it('exposes the new launch-errors auto-dismiss flags in help', () => {
    const { stdout, exitCode } = runCli(['open', '--help']);
    expect(exitCode).toBe(0);
    // Commander renders `.option('--no-auto-dismiss-launch-errors', ...)`
    // as `--no-auto-dismiss-launch-errors` in help; the boolean
    // negation flag is what consumers actually pass to opt out.
    expect(stdout).toContain('--no-auto-dismiss-launch-errors');
    expect(stdout).toContain('--launch-dismiss-timeout-ms');
    expect(stdout).toContain('--launch-dismiss-poll-interval-ms');
  });

  it('mentions the macOS Accessibility-permission requirement in --help', () => {
    const { stdout, exitCode } = runCli(['open', '--help']);
    expect(exitCode).toBe(0);
    // The issue requires this be visible in --help (not just the
    // README) so CI runners and humans see it without leaving the
    // terminal.
    expect(stdout.toLowerCase()).toContain('accessibility');
  });

  it('mentions the Linux/X11 xdotool dependency in --help', () => {
    const { stdout, exitCode } = runCli(['open', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('xdotool');
  });

  it('rejects a non-numeric --launch-dismiss-timeout-ms', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-bad-flag-'));
    try {
      makeFakeUnityProject(tmpDir);
      const { exitCode, stdout } = runCli([
        'open',
        tmpDir,
        '--launch-dismiss-timeout-ms',
        'banana',
      ]);
      expect(exitCode).toBe(1);
      expect(stdout).toContain('--launch-dismiss-timeout-ms');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('includes --unity option in help', () => {
    const { stdout, exitCode } = runCli(['open', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('--unity');
  });

  it('documents the path argument as optional in help', () => {
    const { stdout, exitCode } = runCli(['open', '--help']);
    expect(exitCode).toBe(0);
    // Commander renders optional positionals with square brackets.
    expect(stdout).toContain('[path]');
    expect(stdout).toContain('current directory');
  });

  it('fails when project path does not exist', () => {
    const { exitCode, stdout } = runCli(['open', '/nonexistent/path/12345']);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('does not exist');
  });

  it('rejects an existing path that is not a Unity project', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-open-test-'));
    try {
      const { exitCode, stdout } = runCli(['open', tmpDir]);
      expect(exitCode).toBe(1);
      expect(stdout).toContain('Not a Unity project');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });
});

describe('open command — cwd fallback (no path argument)', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-open-cwd-test-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('fails with a helpful message when cwd is not a Unity project', () => {
    const { exitCode, stdout } = runCli(['open'], { cwd: tmpDir });
    expect(exitCode).toBe(1);
    expect(stdout).toContain('Current directory is not a Unity project');
    // The hint should mention the explicit-path workaround.
    expect(stdout).toContain('unity-mcp-cli open <path>');
  });
});

// ── Unit tests for the extracted validation helpers ───────────────────────────
//
// These exercise the pure helpers directly so we can cover the positive path
// (cwd IS a Unity project) without risking a real Unity Editor launch inside a
// subprocess test.

describe('resolveOpenProjectPath', () => {
  it('returns the positional argument resolved to absolute when provided', () => {
    const result = resolveOpenProjectPath('some/path', undefined, '/fake/cwd');
    expect(result.projectPath).toBe(path.resolve('some/path'));
    expect(result.usedCwdFallback).toBe(false);
  });

  it('prefers the positional argument over --path when both are given', () => {
    const result = resolveOpenProjectPath('positional/path', 'option/path', '/fake/cwd');
    expect(result.projectPath).toBe(path.resolve('positional/path'));
    expect(result.usedCwdFallback).toBe(false);
  });

  it('falls back to --path when no positional argument is given', () => {
    const result = resolveOpenProjectPath(undefined, 'option/path', '/fake/cwd');
    expect(result.projectPath).toBe(path.resolve('option/path'));
    expect(result.usedCwdFallback).toBe(false);
  });

  it('falls back to cwd when neither positional nor --path is given', () => {
    const cwd = path.resolve('/fake/cwd');
    const result = resolveOpenProjectPath(undefined, undefined, cwd);
    expect(result.projectPath).toBe(cwd);
    expect(result.usedCwdFallback).toBe(true);
  });
});

describe('parsePositiveIntFlag', () => {
  // The helper calls process.exit(1) on bad input, which would kill
  // the test runner. Spy on it and throw instead so we can assert on
  // the failure mode.
  const exitMock = vi.spyOn(process, 'exit').mockImplementation(((code?: number) => {
    throw new Error(`process.exit(${code})`);
  }) as never);

  beforeEach(() => {
    exitMock.mockClear();
  });

  it('returns the fallback when raw is undefined', () => {
    expect(parsePositiveIntFlag(undefined, '--foo', 30000)).toBe(30000);
    expect(exitMock).not.toHaveBeenCalled();
  });

  it('returns the fallback when raw is empty whitespace', () => {
    expect(parsePositiveIntFlag('   ', '--foo', 30000)).toBe(30000);
  });

  it('parses a plain integer string', () => {
    expect(parsePositiveIntFlag('1234', '--foo', 30000)).toBe(1234);
  });

  it('parses zero', () => {
    expect(parsePositiveIntFlag('0', '--foo', 30000)).toBe(0);
  });

  it('rejects scientific notation', () => {
    expect(() => parsePositiveIntFlag('3e4', '--foo', 30000)).toThrow(/process\.exit/);
  });

  it('rejects fractional values', () => {
    expect(() => parsePositiveIntFlag('3.14', '--foo', 30000)).toThrow(/process\.exit/);
  });

  it('rejects negative numbers', () => {
    expect(() => parsePositiveIntFlag('-5', '--foo', 30000)).toThrow(/process\.exit/);
  });

  it('rejects non-numeric junk', () => {
    expect(() => parsePositiveIntFlag('banana', '--foo', 30000)).toThrow(/process\.exit/);
  });
});

describe('isUnityProjectDir', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-is-unity-test-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('returns false for an empty directory', () => {
    expect(isUnityProjectDir(tmpDir)).toBe(false);
  });

  it('returns false when only Assets/ exists', () => {
    fs.mkdirSync(path.join(tmpDir, 'Assets'));
    expect(isUnityProjectDir(tmpDir)).toBe(false);
  });

  it('returns false when only ProjectSettings/ProjectVersion.txt exists', () => {
    fs.mkdirSync(path.join(tmpDir, 'ProjectSettings'));
    fs.writeFileSync(
      path.join(tmpDir, 'ProjectSettings', 'ProjectVersion.txt'),
      'm_EditorVersion: 6000.0.0f1\n',
    );
    expect(isUnityProjectDir(tmpDir)).toBe(false);
  });

  it('returns true when both Assets/ and ProjectSettings/ProjectVersion.txt exist', () => {
    makeFakeUnityProject(tmpDir);
    expect(isUnityProjectDir(tmpDir)).toBe(true);
  });
});
