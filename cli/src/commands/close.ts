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
 * Strategy (cheapest first):
 *  1. Read `<project>/Temp/UnityLockfile` (4 bytes LE uint32).
 *  2. Verify the PID is alive AND its command line includes `-projectPath <path>`
 *     pointing at the same project — handles stale lockfiles.
 *  3. Fall back to enumerating Unity processes by command line.
 *
 * Returns `null` when no live editor matches the project. Exported for tests.
 */
export function resolveEditorPid(projectPath: string, platform: SupportedPlatform = nodePlatform() as SupportedPlatform): number | null {
  const lockPid = readLockfilePid(projectPath);
  if (lockPid !== null && isProcessAlive(lockPid, platform)) {
    // Cross-check: the live PID's project path must match our target. The
    // unity-process util already does case-insensitive normalisation on
    // Windows, so reuse it rather than re-implementing comparison here.
    const proc = findUnityProcess(projectPath);
    if (proc && proc.pid === lockPid) {
      verbose(`Lockfile PID ${lockPid} confirmed via process enumeration`);
      return lockPid;
    }
    verbose(`Lockfile PID ${lockPid} did not match enumerated Unity process for project — treating as stale`);
  }

  const proc = findUnityProcess(projectPath);
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
      ui.error(`not a Unity project root: ${projectPath}`);
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
