import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { execFileSync } from 'child_process';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { fileURLToPath } from 'url';
import {
  isUnityProjectRoot,
  parseTimeoutSeconds,
  resolveCloseProjectPath,
} from '../src/commands/close.js';
import {
  forceKillSignal,
  gracefulShutdownSignal,
  isProcessAlive,
  readLockfilePid,
  waitForExit,
} from '../src/utils/unity-shutdown.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');

interface RunCliOptions {
  cwd?: string;
}

function runCli(args: string[], opts: RunCliOptions = {}): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 15000,
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

/** Build a minimal Unity-project-shaped directory so `close` does not refuse it. */
function makeFakeUnityProject(dir: string): void {
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  const settingsDir = path.join(dir, 'ProjectSettings');
  fs.mkdirSync(settingsDir, { recursive: true });
  fs.writeFileSync(
    path.join(settingsDir, 'ProjectVersion.txt'),
    'm_EditorVersion: 6000.0.0f1\n',
  );
}

// ─── close command — flag parsing, refusals, idempotent no-op ─────────────────

describe('close command — help and flag surface', () => {
  it('shows help with --help', () => {
    const { stdout, exitCode } = runCli(['close', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('close');
    expect(stdout).toContain('[path]');
    expect(stdout).toContain('--timeout');
    expect(stdout).toContain('--force');
  });

  it('lists close in the root --help command list', () => {
    const { stdout, exitCode } = runCli(['--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('close');
  });
});

describe('close command — refusals', () => {
  it('fails when project path does not exist', () => {
    const { exitCode, stdout } = runCli(['close', '/nonexistent/path/abc12345']);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('does not exist');
  });

  it('refuses to act on a non-Unity-project directory', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-close-refuse-'));
    try {
      const { exitCode, stdout } = runCli(['close', tmpDir]);
      expect(exitCode).toBe(1);
      expect(stdout).toContain('not a Unity project root');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('rejects a non-positive --timeout value', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-close-timeout-'));
    try {
      makeFakeUnityProject(tmpDir);
      const { exitCode, stdout } = runCli(['close', tmpDir, '--timeout', '0']);
      expect(exitCode).toBe(1);
      expect(stdout).toContain('Invalid --timeout');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('rejects a non-numeric --timeout value', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-close-timeout-'));
    try {
      makeFakeUnityProject(tmpDir);
      const { exitCode, stdout } = runCli(['close', tmpDir, '--timeout', 'abc']);
      expect(exitCode).toBe(1);
      expect(stdout).toContain('Invalid --timeout');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });
});

describe('close command — idempotent no-op when no editor is running', () => {
  it('exits 0 when the project has no running editor', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-close-noop-'));
    try {
      makeFakeUnityProject(tmpDir);
      const { exitCode, stdout } = runCli(['close', tmpDir]);
      expect(exitCode).toBe(0);
      expect(stdout).toContain('no running Editor');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });
});

// ─── path normalisation helpers (pure unit tests) ────────────────────────────

describe('resolveCloseProjectPath', () => {
  it('returns the positional argument resolved to absolute when provided', () => {
    const result = resolveCloseProjectPath('some/relative/path', '/fake/cwd');
    expect(result).toBe(path.resolve('some/relative/path'));
  });

  it('falls back to cwd when no positional argument is given', () => {
    const cwd = path.resolve('/fake/cwd');
    const result = resolveCloseProjectPath(undefined, cwd);
    expect(result).toBe(cwd);
  });

  it('strips a trailing path separator via canonicalisation', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-close-norm-'));
    try {
      const withTrailing = tmpDir + path.sep;
      const result = resolveCloseProjectPath(withTrailing, '/fake/cwd');
      expect(result).toBe(fs.realpathSync(tmpDir));
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('canonicalises a relative path through realpath when the dir exists', () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-close-real-'));
    try {
      const subdir = path.join(tmpDir, 'sub');
      fs.mkdirSync(subdir);
      const messy = path.join(tmpDir, 'sub', '..', 'sub');
      const result = resolveCloseProjectPath(messy, '/fake/cwd');
      expect(result).toBe(fs.realpathSync(subdir));
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });
});

describe('isUnityProjectRoot', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-close-isproj-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('returns false for an empty directory', () => {
    expect(isUnityProjectRoot(tmpDir)).toBe(false);
  });

  it('returns false when only Assets/ exists', () => {
    fs.mkdirSync(path.join(tmpDir, 'Assets'));
    expect(isUnityProjectRoot(tmpDir)).toBe(false);
  });

  it('returns true when ProjectSettings/ProjectVersion.txt exists', () => {
    fs.mkdirSync(path.join(tmpDir, 'ProjectSettings'), { recursive: true });
    fs.writeFileSync(
      path.join(tmpDir, 'ProjectSettings', 'ProjectVersion.txt'),
      'm_EditorVersion: 6000.0.0f1\n',
    );
    expect(isUnityProjectRoot(tmpDir)).toBe(true);
  });
});

describe('parseTimeoutSeconds', () => {
  it('returns the default when undefined', () => {
    expect(parseTimeoutSeconds(undefined)).toBe(30);
  });

  it('parses a positive integer', () => {
    expect(parseTimeoutSeconds('45')).toBe(45);
  });

  it('returns null on zero', () => {
    expect(parseTimeoutSeconds('0')).toBeNull();
  });

  it('returns null on negative', () => {
    expect(parseTimeoutSeconds('-5')).toBeNull();
  });

  it('returns null on non-integer', () => {
    expect(parseTimeoutSeconds('1.5')).toBeNull();
  });

  it('returns null on non-numeric', () => {
    expect(parseTimeoutSeconds('xyz')).toBeNull();
  });
});

// ─── shutdown utility unit tests ──────────────────────────────────────────────

describe('gracefulShutdownSignal — cross-platform matrix', () => {
  it('returns SIGTERM on linux', () => {
    expect(gracefulShutdownSignal('linux')).toBe('SIGTERM');
  });

  it('returns SIGTERM on macOS', () => {
    expect(gracefulShutdownSignal('darwin')).toBe('SIGTERM');
  });

  it('returns WM_CLOSE sentinel on Windows', () => {
    expect(gracefulShutdownSignal('win32')).toBe('WM_CLOSE');
  });
});

describe('forceKillSignal — cross-platform matrix', () => {
  it('returns SIGKILL on linux', () => {
    expect(forceKillSignal('linux')).toBe('SIGKILL');
  });

  it('returns SIGKILL on macOS', () => {
    expect(forceKillSignal('darwin')).toBe('SIGKILL');
  });

  it('returns TASKKILL_FORCE sentinel on Windows', () => {
    expect(forceKillSignal('win32')).toBe('TASKKILL_FORCE');
  });
});

describe('readLockfilePid', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-close-lock-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('returns null when the lockfile does not exist', () => {
    expect(readLockfilePid(tmpDir)).toBeNull();
  });

  it('returns null when the lockfile is too small (<4 bytes)', () => {
    fs.mkdirSync(path.join(tmpDir, 'Temp'));
    fs.writeFileSync(path.join(tmpDir, 'Temp', 'UnityLockfile'), Buffer.from([0x01, 0x02]));
    expect(readLockfilePid(tmpDir)).toBeNull();
  });

  it('parses a little-endian uint32 from the first 4 bytes', () => {
    fs.mkdirSync(path.join(tmpDir, 'Temp'));
    // 0x39300000 LE = 12345 — typical Unity PID range.
    const buf = Buffer.from([0x39, 0x30, 0x00, 0x00]);
    fs.writeFileSync(path.join(tmpDir, 'Temp', 'UnityLockfile'), buf);
    expect(readLockfilePid(tmpDir)).toBe(12345);
  });

  it('returns null when the lockfile holds zero', () => {
    fs.mkdirSync(path.join(tmpDir, 'Temp'));
    fs.writeFileSync(
      path.join(tmpDir, 'Temp', 'UnityLockfile'),
      Buffer.from([0x00, 0x00, 0x00, 0x00]),
    );
    expect(readLockfilePid(tmpDir)).toBeNull();
  });

  it('ignores trailing bytes beyond the first 4', () => {
    fs.mkdirSync(path.join(tmpDir, 'Temp'));
    // PID 1 + arbitrary trailing payload.
    const buf = Buffer.concat([
      Buffer.from([0x01, 0x00, 0x00, 0x00]),
      Buffer.from([0xAB, 0xCD, 0xEF]),
    ]);
    fs.writeFileSync(path.join(tmpDir, 'Temp', 'UnityLockfile'), buf);
    expect(readLockfilePid(tmpDir)).toBe(1);
  });
});

describe('isProcessAlive', () => {
  it('returns true for the current process pid', () => {
    expect(isProcessAlive(process.pid)).toBe(true);
  });

  it('returns false for a sentinel non-existent pid', () => {
    // PID 0 is special on POSIX; using a very large number that is unlikely
    // to be assigned. The helper rejects non-positive inputs, so 0 returns
    // false directly without touching the OS.
    expect(isProcessAlive(0)).toBe(false);
  });

  it('returns false for a negative pid', () => {
    expect(isProcessAlive(-1)).toBe(false);
  });

  it('returns false for a non-finite pid', () => {
    expect(isProcessAlive(NaN)).toBe(false);
  });
});

describe('waitForExit', () => {
  it('returns true immediately when the pid is already gone', async () => {
    const start = Date.now();
    const result = await waitForExit(0, 2000);
    const elapsed = Date.now() - start;
    expect(result).toBe(true);
    // First poll happens before any sleep, so we should return well under
    // the configured timeout.
    expect(elapsed).toBeLessThan(500);
  });

  it('returns false when the timeout elapses on a live pid', async () => {
    const start = Date.now();
    const result = await waitForExit(process.pid, 600);
    const elapsed = Date.now() - start;
    expect(result).toBe(false);
    expect(elapsed).toBeGreaterThanOrEqual(500);
  });
});

// ─── live-editor integration smoke test (gated by UMCP_LIVE=1) ───────────────
//
// This block is skipped by default so CI does not require Unity. To exercise
// it locally, run `UMCP_LIVE=1 npm test` inside `Unity-MCP/cli/` after
// `unity-mcp-cli open <project>` has launched a real editor for
// `UMCP_LIVE_PROJECT`. The acceptance criterion validated here is the
// graceful-close path; the timeout and force-kill paths require a wedged
// editor that scripted setup cannot reliably reproduce.

const liveDescribe = process.env.UMCP_LIVE === '1' ? describe : describe.skip;

liveDescribe('close command — live editor (UMCP_LIVE=1)', () => {
  const liveProject = process.env.UMCP_LIVE_PROJECT;

  it('terminates a running editor for the project (UMCP_LIVE_PROJECT must point at one)', () => {
    if (!liveProject) {
      throw new Error('UMCP_LIVE=1 requires UMCP_LIVE_PROJECT to be set to a Unity project path with a live editor');
    }
    const { exitCode, stdout } = runCli(['close', liveProject, '--timeout', '60']);
    expect(exitCode).toBe(0);
    expect(stdout).toMatch(/exited cleanly|no running Editor/);
  }, 90_000);
});
