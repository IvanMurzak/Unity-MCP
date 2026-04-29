import { execFileSync } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import { platform as nodePlatform } from 'os';
import { verbose } from './ui.js';

/**
 * Supported Node.js `process.platform` values for the close subcommand.
 * Narrowed alias of NodeJS.Platform — keeps signal-selection logic exhaustive
 * in tests without forcing callers to import a Node-internal type.
 */
export type SupportedPlatform = 'win32' | 'darwin' | 'linux';

/**
 * The polite-quit "graceful shutdown" signal for the current platform.
 *
 * - Linux / macOS: `SIGTERM` — Unity catches this and runs its normal exit
 *   path (autosave, asset-import finalisation, plugin disconnect).
 * - Windows: `WM_CLOSE` via `taskkill` (no `/F`) — same idea, sent through
 *   `taskkill /PID <pid>` in the runtime helper. We return the string
 *   sentinel `WM_CLOSE` here so the call site can branch on it.
 *
 * Exposed as a pure function so the unit test suite can assert the platform
 * matrix without booting an editor.
 */
export function gracefulShutdownSignal(platform: SupportedPlatform): NodeJS.Signals | 'WM_CLOSE' {
  if (platform === 'win32') return 'WM_CLOSE';
  return 'SIGTERM';
}

/**
 * The hard-kill signal for the current platform.
 *
 * - Linux / macOS: `SIGKILL`.
 * - Windows: `taskkill /F` — represented here by the sentinel `TASKKILL_FORCE`.
 */
export function forceKillSignal(platform: SupportedPlatform): NodeJS.Signals | 'TASKKILL_FORCE' {
  if (platform === 'win32') return 'TASKKILL_FORCE';
  return 'SIGKILL';
}

/**
 * Read the editor PID stored in `<project>/Temp/UnityLockfile`.
 *
 * Unity writes the running editor's PID into the first 4 bytes of this file
 * as a little-endian uint32 the moment it acquires the project lock. Returns
 * `null` when the lockfile is missing, too small, or holds a zero/negative PID.
 *
 * The caller is responsible for cross-checking the PID against a live process
 * list — stale lockfiles can survive an unclean editor exit.
 */
export function readLockfilePid(projectPath: string): number | null {
  const lockfilePath = path.join(projectPath, 'Temp', 'UnityLockfile');
  if (!fs.existsSync(lockfilePath)) {
    verbose(`No Unity lockfile at ${lockfilePath}`);
    return null;
  }

  let buf: Buffer;
  try {
    buf = fs.readFileSync(lockfilePath);
  } catch (err) {
    verbose(`Failed to read Unity lockfile: ${err instanceof Error ? err.message : String(err)}`);
    return null;
  }

  if (buf.length < 4) {
    verbose(`Unity lockfile too small (${buf.length} bytes)`);
    return null;
  }

  // `readUInt32LE` always returns a finite integer in [0, 2^32-1], so a
  // non-positive check is the only meaningful filter here.
  const pid = buf.readUInt32LE(0);
  if (pid <= 0) {
    verbose(`Unity lockfile holds non-positive PID: ${pid}`);
    return null;
  }
  return pid;
}

/**
 * Returns true when a process with the given PID is alive.
 *
 * Both platforms first try `process.kill(pid, 0)` — Node maps it through
 * `OpenProcess` on Windows, so the check is in-process and effectively free
 * compared to spawning `tasklist`. At a 250ms `waitForExit` cadence with a
 * 30s default timeout that's ~120 fewer subprocesses per close on Windows.
 *
 * The Windows fallback to `tasklist` is kept as defence-in-depth: if a
 * future Node release ever changes behaviour or `OpenProcess` denies access
 * for a process owned by another session, we still get the right answer
 * (just more slowly).
 *
 * On any failure the function errs on the side of "not running" and returns
 * false, which keeps the wait loop progressing rather than spinning forever
 * on a transient error.
 */
export function isProcessAlive(pid: number, platform: SupportedPlatform = nodePlatform() as SupportedPlatform): boolean {
  if (!Number.isFinite(pid) || pid <= 0) return false;

  try {
    process.kill(pid, 0);
    return true;
  } catch (err) {
    const code = (err as NodeJS.ErrnoException).code;
    // EPERM means the process exists but we can't signal it — treat as alive.
    if (code === 'EPERM') return true;
    if (code !== 'ESRCH') {
      // Anything other than ESRCH is unexpected; fall through to the
      // Windows-only verification path so we don't return a wrong answer
      // on a transient OS hiccup. POSIX paths just return false.
      if (platform !== 'win32') return false;
    }
  }

  if (platform !== 'win32') return false;

  // Windows fallback: `tasklist /FI "PID eq <pid>" /NH /FO CSV`.
  // The first CSV column is the image name, the second is the PID. Parse
  // field-wise so a digit run that happens to appear in another column
  // (image name, session, memory, status) cannot produce a false positive.
  try {
    const out = execFileSync('tasklist', ['/FI', `PID eq ${pid}`, '/NH', '/FO', 'CSV'], {
      encoding: 'utf-8',
      timeout: 3000,
      stdio: ['pipe', 'pipe', 'pipe'],
    });
    // tasklist prints "INFO: No tasks are running ..." when nothing matches.
    const target = `"${pid}"`;
    for (const line of out.split(/\r?\n/)) {
      const trimmed = line.trim();
      if (trimmed.length === 0) continue;
      // Second CSV field — split on `,` and check column index 1. CSV from
      // tasklist quotes every field, so a simple split is safe. Image names
      // with embedded commas would shift the PID into a later column and
      // produce a false negative; that's an accepted trade-off — Unity
      // executable names don't contain commas in practice, and any false
      // negative simply ends the wait loop one tick early.
      const cols = trimmed.split(',');
      if (cols.length >= 2 && cols[1] === target) return true;
    }
    return false;
  } catch {
    return false;
  }
}

/**
 * Send the platform's polite-quit signal to the given PID.
 *
 * Returns true on apparent success (the OS accepted the signal); false if the
 * underlying call threw. The caller still has to poll `isProcessAlive` — the
 * editor may take seconds to wind down.
 *
 * Windows caveat: `taskkill /PID <pid>` (no `/F`) delivers `WM_CLOSE`, which
 * only reaches processes that own a top-level window on the same desktop /
 * session as the caller. In headless contexts (Unity launched in session 0
 * by a Windows service, or another non-interactive desktop) `taskkill` will
 * exit 0 and this function will report `true`, but the editor will never
 * receive the message. The `--timeout` will then elapse, and `--force` is
 * the only path that brings the process down. This is documented in the
 * close subcommand README and is consistent with the design decision to
 * stick to OS-level polite-quit (rather than a Unity-side MCP `editor-quit`
 * tool which would route through SignalR and bypass session walls).
 */
export function sendGracefulShutdown(pid: number, platform: SupportedPlatform = nodePlatform() as SupportedPlatform): boolean {
  if (!Number.isFinite(pid) || pid <= 0) return false;
  const sig = gracefulShutdownSignal(platform);
  verbose(`Sending graceful shutdown to PID ${pid} via ${sig}`);

  try {
    if (sig === 'WM_CLOSE') {
      execFileSync('taskkill', ['/PID', String(pid)], {
        timeout: 3000,
        stdio: ['pipe', 'pipe', 'pipe'],
      });
      return true;
    }
    process.kill(pid, sig);
    return true;
  } catch (err) {
    // Race: the editor can exit between PID resolution and signal delivery.
    // POSIX surfaces this as ESRCH; Windows `taskkill` exits non-zero with
    // "process … not found". The desired end state (process gone) is already
    // reached, so we re-check liveness and report success when the PID is
    // truly absent. This preserves close's idempotency contract.
    if (!isProcessAlive(pid, platform)) {
      verbose(`Graceful shutdown: PID ${pid} already gone — treating as success`);
      return true;
    }
    verbose(`Graceful shutdown failed: ${err instanceof Error ? err.message : String(err)}`);
    return false;
  }
}

/**
 * Hard-kill the given PID. Used only when `--force` is set and the polite
 * shutdown timed out.
 */
export function sendForceKill(pid: number, platform: SupportedPlatform = nodePlatform() as SupportedPlatform): boolean {
  if (!Number.isFinite(pid) || pid <= 0) return false;
  const sig = forceKillSignal(platform);
  verbose(`Force-killing PID ${pid} via ${sig}`);

  try {
    if (sig === 'TASKKILL_FORCE') {
      execFileSync('taskkill', ['/F', '/PID', String(pid)], {
        timeout: 3000,
        stdio: ['pipe', 'pipe', 'pipe'],
      });
      return true;
    }
    process.kill(pid, sig);
    return true;
  } catch (err) {
    // Same race as sendGracefulShutdown — the polite-quit path may have
    // already won between waitForExit's last poll and our SIGKILL call.
    if (!isProcessAlive(pid, platform)) {
      verbose(`Force kill: PID ${pid} already gone — treating as success`);
      return true;
    }
    verbose(`Force kill failed: ${err instanceof Error ? err.message : String(err)}`);
    return false;
  }
}

/**
 * Poll until the given PID disappears or the timeout elapses.
 *
 * Returns true if the process exited within `timeoutMs`; false otherwise.
 * Uses a 250ms poll interval — short enough to give crisp UX on a fast exit
 * without busy-looping.
 */
export async function waitForExit(
  pid: number,
  timeoutMs: number,
  platform: SupportedPlatform = nodePlatform() as SupportedPlatform,
): Promise<boolean> {
  const pollIntervalMs = 250;
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    if (!isProcessAlive(pid, platform)) {
      return true;
    }
    await new Promise(resolve => setTimeout(resolve, pollIntervalMs));
  }
  // One last check after the deadline elapses — the process may have exited
  // in the trailing poll-interval slice we slept through.
  return !isProcessAlive(pid, platform);
}
