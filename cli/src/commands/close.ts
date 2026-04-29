import { Command } from 'commander';
import * as fs from 'fs';
import * as path from 'path';
import { platform as nodePlatform } from 'os';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { findUnityProcess } from '../utils/unity-process.js';
import {
  readLockfilePid,
  isProcessAlive,
  sendGracefulShutdown,
  sendForceKill,
  waitForExit,
  type SupportedPlatform,
} from '../utils/unity-shutdown.js';

export interface CloseOptions {
  timeout?: string;
  force?: boolean;
}

const DEFAULT_TIMEOUT_SECONDS = 30;

/**
 * Resolve the project path argument to an absolute, canonical path.
 *
 * Mirrors the convention used by `open` / `wait-for-ready` — accepts a
 * positional argument or `process.cwd()` fallback, normalises symlinks where
 * possible, and trims any trailing path separator. Exported for unit tests.
 */
export function resolveCloseProjectPath(positionalPath: string | undefined, cwd: string): string {
  const explicit = positionalPath ?? cwd;
  let resolved = path.resolve(explicit);

  // realpathSync collapses symlinks, ".." segments, and trailing separators
  // when the path exists. When it does not, fall through to the resolved
  // form so the caller can produce a "path does not exist" error.
  try {
    resolved = fs.realpathSync(resolved);
  } catch {
    // Path may not exist — let the caller decide what to do.
  }

  return resolved;
}

/**
 * Returns true when `<projectPath>/ProjectSettings/ProjectVersion.txt` exists.
 *
 * The issue's acceptance criteria require refusing to act on any path that
 * does not look like a Unity project root (`ProjectVersion.txt` is the
 * canonical marker). This is intentionally narrower than the `open`
 * subcommand's check — the goal here is to defend against accidental
 * "kill all Unity processes on host" invocations, so a positive ID of
 * "this is a Unity project" is required.
 *
 * Exported for unit tests.
 */
export function isUnityProjectRoot(projectPath: string): boolean {
  return fs.existsSync(path.join(projectPath, 'ProjectSettings', 'ProjectVersion.txt'));
}

/**
 * Parse a positive-integer `--timeout` value (seconds). Returns the parsed
 * value, or `null` when the input is not a valid positive integer.
 *
 * The `undefined` branch falls back to the default for ergonomic
 * direct-call use (e.g., unit tests). The action handler never reaches it
 * because commander always materialises the configured default string.
 *
 * Exported for unit tests.
 */
export function parseTimeoutSeconds(raw: string | undefined): number | null {
  if (raw === undefined) return DEFAULT_TIMEOUT_SECONDS;
  const parsed = Number(raw);
  if (!Number.isInteger(parsed) || parsed <= 0) return null;
  return parsed;
}

/**
 * Resolve the editor PID for a given project path.
 *
 * Reads `<project>/Temp/UnityLockfile` (4 bytes LE uint32) and enumerates
 * Unity processes by command line, then prefers the lockfile PID when it
 * is alive and matches the enumerated process for this project (handles
 * stale lockfiles); otherwise uses the enumerated PID.
 *
 * Symlink/realpath handling: `resolveCloseProjectPath` canonicalises via
 * `realpathSync` while `findUnityProcess` only `path.resolve`s the cmdline
 * `-projectPath` argument. If Unity was launched via a symlink, the first
 * lookup misses. We retry against the un-canonicalised form so a symlinked
 * launch path doesn't silently degrade close to a no-op.
 *
 * Returns `null` when no live editor matches the project. Exported for tests.
 */
export function resolveEditorPid(projectPath: string, platform: SupportedPlatform = nodePlatform() as SupportedPlatform): number | null {
  const lockPid = readLockfilePid(projectPath);
  // Enumerate Unity processes once. `findUnityProcess` shells out (3s timeout
  // on Windows via PowerShell+CIM, 5s on POSIX via `ps`), so calling it twice
  // on the stale-lockfile path used to roughly double close latency.
  let proc = findUnityProcess(projectPath);

  // Symlink fallback: if the canonical realpath form found no process, retry
  // with `path.resolve` only (no realpath collapse) — that matches what
  // `findUnityProcess` derives from the cmdline `-projectPath` argument.
  if (!proc) {
    const resolvedNoRealpath = path.resolve(projectPath);
    if (resolvedNoRealpath !== projectPath) {
      proc = findUnityProcess(resolvedNoRealpath);
    }
  }

  if (lockPid !== null && isProcessAlive(lockPid, platform)) {
    if (proc && proc.pid === lockPid) {
      verbose(`Lockfile PID ${lockPid} confirmed via process enumeration`);
      return lockPid;
    }
    // Lockfile alive but no matching enumerated process — the lockfile may
    // still be authoritative if the asymmetry is purely realpath/symlink
    // related (process listed under symlink path while we queried by realpath).
    // Trust the live lockfile PID in that case rather than discarding it.
    if (!proc) {
      verbose(`Lockfile PID ${lockPid} alive but no enumerated match — using lockfile PID (likely symlink/path mismatch)`);
      return lockPid;
    }
    verbose(`Lockfile PID ${lockPid} did not match enumerated Unity process for project — treating as stale`);
  }

  return proc ? proc.pid : null;
}

export const closeCommand = new Command('close')
  .description('Gracefully terminate the Unity Editor instance running for a given project path')
  .argument('[path]', 'Path to the Unity project (defaults to current directory)')
  .option('--timeout <seconds>', `Polite-quit timeout in seconds (default: ${DEFAULT_TIMEOUT_SECONDS})`, String(DEFAULT_TIMEOUT_SECONDS))
  .option('--force', 'Hard-kill the Editor if it does not exit within --timeout')
  .action(async (positionalPath: string | undefined, options: CloseOptions) => {
    const projectPath = resolveCloseProjectPath(positionalPath, process.cwd());
    verbose(`close invoked for project: ${projectPath}`);

    if (!fs.existsSync(projectPath)) {
      ui.error(`Project path does not exist: ${projectPath}`);
      process.exit(1);
    }

    if (!isUnityProjectRoot(projectPath)) {
      ui.error(`Not a Unity project root: ${projectPath}`);
      process.exit(1);
    }

    const timeoutSeconds = parseTimeoutSeconds(options.timeout);
    if (timeoutSeconds === null) {
      ui.error(`Invalid --timeout value: "${options.timeout}". Must be a positive integer (seconds).`);
      process.exit(1);
    }

    const platform = nodePlatform() as SupportedPlatform;
    const pid = resolveEditorPid(projectPath, platform);

    if (pid === null) {
      ui.success(`no running Editor for project at ${projectPath}`);
      process.exit(0);
    }

    ui.heading('Closing Unity Editor');
    ui.label('Project', projectPath);
    ui.label('PID', String(pid));
    ui.label('Timeout', `${timeoutSeconds}s`);
    ui.label('Force', options.force ? 'yes' : 'no');
    ui.divider();

    // TOCTOU window: between resolveEditorPid returning and the signal call
    // below, the original Unity process could exit and the OS could reuse
    // the same numeric PID for an unrelated process (more likely on POSIX,
    // where PIDs cycle aggressively). We accept the residual risk — the
    // lockfile cross-check above already narrows it, holding a lock across
    // a spawned Editor's lifetime is impractical, and the signals we send
    // (SIGTERM / WM_CLOSE) are best-effort, not destructive.
    const spinner = ui.startSpinner(`Sending polite-quit to PID ${pid}...`);
    const sent = sendGracefulShutdown(pid, platform);
    if (!sent) {
      spinner.error(`Failed to deliver polite-quit signal to PID ${pid}`);
      process.exit(1);
    }

    spinner.text = `Waiting up to ${timeoutSeconds}s for PID ${pid} to exit...`;
    const timeoutMs = timeoutSeconds * 1000;
    const exited = await waitForExit(pid, timeoutMs, platform);

    if (exited) {
      spinner.success(`Unity Editor (PID ${pid}) exited cleanly`);
      process.exit(0);
    }

    if (!options.force) {
      spinner.error(
        `Unity Editor (PID ${pid}) did not exit within ${timeoutSeconds}s. ` +
          `Re-run with --force to hard-kill, or close it manually.`,
      );
      process.exit(1);
    }

    spinner.text = `Force-killing PID ${pid}...`;
    const killed = sendForceKill(pid, platform);
    if (!killed) {
      spinner.error(`Failed to force-kill PID ${pid}`);
      process.exit(1);
    }

    // Brief secondary wait so we report a true terminal state — the OS may
    // need a moment to reap the process even after SIGKILL / `taskkill /F`.
    const reaped = await waitForExit(pid, 5000, platform);
    if (reaped) {
      spinner.success(`Unity Editor (PID ${pid}) force-killed`);
      process.exit(0);
    }

    spinner.error(`Unity Editor (PID ${pid}) is still alive after force-kill`);
    process.exit(1);
  });
