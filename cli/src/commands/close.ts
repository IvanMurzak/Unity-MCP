import { Command } from 'commander';
import * as fs from 'fs';
import * as path from 'path';
import { platform as nodePlatform } from 'os';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { canonicalPath, findUnityProcess } from '../utils/unity-process.js';
import { readSessionLease, type CloseOwnedResult } from '@baizor/gamedev-cli-core';
import { createProjectSession } from '../lib/editor-session.js';
import { describeDialog, findBlockingDialogs } from '../utils/awaiting-input.js';
import type { EditorDialog } from '../utils/editor-dialogs.js';
import { ExitCode } from '../utils/exit-codes.js';
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
  ownedOnly?: boolean;
}

const DEFAULT_TIMEOUT_SECONDS = 30;

/** First dialog scan after the polite quit, then every interval: a save prompt appears within a second or two. */
const DIALOG_SCAN_DELAY_MS = 2000;
const DIALOG_SCAN_INTERVAL_MS = 5000;

export type ExitWaitResult =
  | { kind: 'exited' }
  | { kind: 'dialog'; dialogs: EditorDialog[] }
  | { kind: 'timeout' };

export interface ExitWaitDeps {
  isAlive: (pid: number) => boolean;
  findDialogs: () => Promise<EditorDialog[]>;
  sleep?: (ms: number) => Promise<void>;
  now?: () => number;
}

/**
 * Wait until `pid` exits or `timeoutMs` elapses, scanning for modal dialogs meanwhile: a polite quit of an editor
 * with unsaved changes (an untitled or modified scene) opens "Scene(s) Have Been Modified" and the editor then waits
 * for a human, so waiting out the timeout would only end in a bare "did not exit". Exported for tests.
 */
export async function waitForExitOrDialog(pid: number, timeoutMs: number, deps: ExitWaitDeps): Promise<ExitWaitResult> {
  const sleep = deps.sleep ?? (ms => new Promise<void>(resolve => setTimeout(resolve, ms)));
  const now = deps.now ?? Date.now;
  const start = now();
  const deadline = start + timeoutMs;
  let nextScan = start + DIALOG_SCAN_DELAY_MS;
  while (now() < deadline) {
    if (!deps.isAlive(pid)) return { kind: 'exited' };
    if (now() >= nextScan) {
      const dialogs = await deps.findDialogs();
      // A dialog that closed the editor on its way out is no reason to report one.
      if (dialogs.length > 0 && deps.isAlive(pid)) return { kind: 'dialog', dialogs };
      nextScan = now() + DIALOG_SCAN_INTERVAL_MS;
    }
    await sleep(250);
  }
  if (!deps.isAlive(pid)) return { kind: 'exited' };
  const dialogs = await deps.findDialogs();
  return dialogs.length > 0 && deps.isAlive(pid) ? { kind: 'dialog', dialogs } : { kind: 'timeout' };
}

/** Why the editor did not quit: the dialog it waits on, and how to get past it. Exported for tests. */
export function blockedByDialogMessage(pid: number, dialogs: EditorDialog[], force: boolean): string {
  return `Unity Editor (PID ${pid}) did not quit: it is waiting on a modal dialog: ${dialogs.map(describeDialog).join('; ')}. ` +
    'Answer it in the editor, or with `unity-mcp-cli dialog --press <button>`' +
    (force ? '.' : ', or re-run with --force to hard-kill (unsaved changes are lost).');
}

/**
 * Resolve the project path argument to an absolute, canonical path.
 *
 * Mirrors the convention used by `open` / `wait-for-ready` — accepts a
 * positional argument or `process.cwd()` fallback, normalises symlinks where
 * possible, and trims any trailing path separator. Exported for unit tests.
 */
export function resolveCloseProjectPath(positionalPath: string | undefined, cwd: string): string {
  // A missing path stays resolved, so the caller can say it does not exist.
  return canonicalPath(positionalPath ?? cwd);
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
 * Symlinks: `findUnityProcess` compares both paths after realpath, so an editor
 * launched through a symlink is found by the canonical project path.
 *
 * Returns `null` when no live editor matches the project. Exported for tests.
 */
export function resolveEditorPid(projectPath: string, platform: SupportedPlatform = nodePlatform() as SupportedPlatform): number | null {
  const lockPid = readLockfilePid(projectPath);
  // Enumerate Unity processes once. `findUnityProcess` shells out (3s timeout
  // on Windows via PowerShell+CIM, 5s on POSIX via `ps`), so calling it twice
  // on the stale-lockfile path used to roughly double close latency.
  const proc = findUnityProcess(projectPath);

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

/**
 * Close the editor the project's session lease names, if it is live: exits with the outcome. Returns (nothing done)
 * when no editor of ours runs; a stale lease is removed, and no process is touched. `unverified`: a lease is left
 * that names no editor provably ours (e.g. its process start time is unreadable), so it was not closed.
 */
async function closeOwnedEditor(
  projectPath: string,
  timeoutSeconds: number,
  force: boolean,
  ownedOnly: boolean,
): Promise<'none' | 'unverified'> {
  if (!readSessionLease(projectPath)) return 'none';
  const spinner = ui.startSpinner('Asking the Unity Editor started by unity-mcp-cli to save and quit...');
  let result: CloseOwnedResult;
  try {
    const session = await createProjectSession(projectPath, {});
    result = await session.closeOwned({ timeoutMs: timeoutSeconds * 1000, force });
  } catch (err) {
    const message = `Could not close the Unity Editor started by unity-mcp-cli: ${err instanceof Error ? err.message : String(err)}`;
    if (ownedOnly) {
      spinner.error(message);
      process.exit(1);
    }
    // Without --owned-only the OS polite quit below still applies.
    spinner.stop();
    verbose(message);
    return 'none';
  }
  if (result.kind === 'closed') {
    spinner.success(`Unity Editor (PID ${result.pid}) started by unity-mcp-cli ${result.forced ? 'force-killed' : 'exited cleanly'}`);
    process.exit(0);
  }
  if (result.kind === 'still-running') {
    const dialogs = await findBlockingDialogs(projectPath);
    if (dialogs.length > 0) {
      spinner.error(blockedByDialogMessage(result.pid, dialogs, force));
      process.exit(ExitCode.AwaitingInput);
    }
    spinner.error(
      `Unity Editor (PID ${result.pid}) did not exit within ${timeoutSeconds}s.` +
        (force ? '' : ' Re-run with --force to hard-kill.'),
    );
    process.exit(1);
  }
  spinner.stop();
  return readSessionLease(projectPath) ? 'unverified' : 'none';
}

export const closeCommand = new Command('close')
  .description('Gracefully terminate the Unity Editor instance running for a given project path')
  .argument('[path]', 'Path to the Unity project (defaults to current directory)')
  .option('--timeout <seconds>', `Polite-quit timeout in seconds (default: ${DEFAULT_TIMEOUT_SECONDS})`, String(DEFAULT_TIMEOUT_SECONDS))
  .option('--force', 'Hard-kill the Editor if it does not exit within --timeout')
  .option('--owned-only', 'Close only an Editor started by unity-mcp-cli (exec, open --headless); never one opened by the user')
  .addHelpText('after', '\nExit codes: 0 closed (or none running), 1 failed or timed out, 18 a modal dialog (e.g. unsaved changes) keeps the editor from quitting.')
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

    // An editor this CLI started (the session lease names it) is asked to quit through its plugin: it saves, and
    // unlike WM_CLOSE this reaches a headless editor too.
    const owned = await closeOwnedEditor(projectPath, timeoutSeconds, options.force === true, options.ownedOnly === true);
    if (options.ownedOnly) {
      if (owned === 'unverified') {
        ui.warn(`the session lease of ${projectPath} names no Editor that is provably unity-mcp-cli's; nothing was closed`);
      } else {
        ui.success(`no Editor started by unity-mcp-cli is running for project at ${projectPath}`);
      }
      process.exit(0);
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
    const waited = options.force
      // --force hard-kills at the timeout anyway; a person may still answer a dialog until then.
      ? ((await waitForExit(pid, timeoutMs, platform)) ? { kind: 'exited' as const } : { kind: 'timeout' as const })
      : await waitForExitOrDialog(pid, timeoutMs, {
        isAlive: p => isProcessAlive(p, platform),
        findDialogs: () => findBlockingDialogs(projectPath),
      });

    if (waited.kind === 'exited') {
      spinner.success(`Unity Editor (PID ${pid}) exited cleanly`);
      process.exit(0);
    }

    if (waited.kind === 'dialog') {
      spinner.error(blockedByDialogMessage(pid, waited.dialogs, false));
      process.exit(ExitCode.AwaitingInput);
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
