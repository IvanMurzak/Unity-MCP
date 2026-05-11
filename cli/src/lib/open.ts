import * as fs from 'fs';
import * as path from 'path';
import { platform as nodePlatform } from 'os';
import {
  findEditorPath,
  getProjectEditorVersion,
  launchEditor,
} from '../utils/unity-editor.js';
import { clearCachedEditorPath } from '../utils/editor-cache.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { readConfig, isCloudMode, writeConfig } from '../utils/config.js';
import {
  tryDismissLaunchErrorsDialog,
  LINUX_XDOTOOL_MISSING_PREFIX,
  UNSUPPORTED_PLATFORM_PREFIX,
  type DismissPlatform,
} from '../utils/launch-error-dismiss.js';
import { emitProgress } from './progress.js';
import type {
  OpenEnvInputs,
  OpenProjectAuthOption,
  OpenProjectOptions,
  OpenProjectResult,
  OpenProjectTransport,
  ProgressCallback,
} from './types.js';

/**
 * Resolve the project path from an explicit option or fall back to
 * the supplied cwd. Always returns an absolute, resolved path plus a
 * flag indicating whether the cwd fallback was used (so the CLI
 * surface can emit a friendlier "no path provided" message).
 *
 * Pure / no I/O — exported for tests and for the CLI command's own
 * use so the two paths can share resolution semantics.
 */
export function resolveProjectPath(
  optionPath: string | undefined,
  cwd: string,
): { projectPath: string; usedCwdFallback: boolean } {
  const explicit = optionPath;
  const resolvedPath = explicit ?? cwd;
  return {
    projectPath: path.resolve(resolvedPath),
    usedCwdFallback: explicit === undefined,
  };
}

/**
 * Returns true if `projectPath` looks like a Unity project — i.e. it
 * contains an `Assets/` directory and a
 * `ProjectSettings/ProjectVersion.txt` file. Pure / no I/O beyond
 * `fs.existsSync`.
 */
export function isUnityProjectDir(projectPath: string): boolean {
  const hasAssets = fs.existsSync(path.join(projectPath, 'Assets'));
  const hasProjectVersion = fs.existsSync(
    path.join(projectPath, 'ProjectSettings', 'ProjectVersion.txt'),
  );
  return hasAssets && hasProjectVersion;
}

function isValidAuth(v: unknown): v is OpenProjectAuthOption {
  return v === 'none' || v === 'required';
}

function isValidTransport(v: unknown): v is OpenProjectTransport {
  return v === 'streamableHttp' || v === 'stdio';
}

/**
 * Extract the leading executable path from a Unity process command
 * line. Quote-aware: a leading double-quoted token (the typical
 * Windows CIM `CommandLine` shape for paths containing spaces) is
 * consumed whole, otherwise the first whitespace-delimited token is
 * used. Pure / no I/O.
 */
function extractEditorPathFromCommandLine(commandLine: string): string {
  const trimmed = commandLine.trim();
  if (trimmed.length === 0) return '';
  const quoted = trimmed.match(/^"([^"]+)"/);
  if (quoted) return quoted[1];
  const unquoted = trimmed.match(/^(\S+)/);
  return unquoted ? unquoted[1] : '';
}

/**
 * Build the env-var map propagated to the editor process when
 * `noConnect !== true`. Pure (returns a fresh object); validates the
 * `auth` and `transport` enums and throws on bad input — the
 * `openProject` boundary catches the throw and returns it as a
 * `{ kind: 'failure' }` result so nothing escapes past the public
 * boundary.
 *
 * Exported for tests so the env-var assembly can be exercised
 * without a real editor launch.
 */
export function buildOpenEnv(
  options: OpenEnvInputs,
): Record<string, string> | undefined {
  if (options.noConnect === true) return undefined;

  const env: Record<string, string> = {};

  if (options.url !== undefined) env['UNITY_MCP_HOST'] = options.url;
  if (options.keepConnected) env['UNITY_MCP_KEEP_CONNECTED'] = 'true';
  if (options.tools !== undefined) env['UNITY_MCP_TOOLS'] = options.tools;
  if (options.token !== undefined) env['UNITY_MCP_TOKEN'] = options.token;

  if (options.auth !== undefined) {
    if (!isValidAuth(options.auth)) {
      throw new Error('auth must be "none" or "required"');
    }
    env['UNITY_MCP_AUTH_OPTION'] = options.auth;
  }

  if (options.transport !== undefined) {
    if (!isValidTransport(options.transport)) {
      throw new Error('transport must be "streamableHttp" or "stdio"');
    }
    env['UNITY_MCP_TRANSPORT'] = options.transport;
  }

  if (options.startServer !== undefined) {
    env['UNITY_MCP_START_SERVER'] = options.startServer ? 'true' : 'false';
  }

  return Object.keys(env).length > 0 ? env : undefined;
}

/**
 * Open a Unity project in the Unity Editor — the library-callable
 * equivalent of the `open` CLI command. Library-safe: never calls
 * `process.exit`, never prints to stdout / stderr, never throws past
 * the public boundary.
 *
 * The launch logic (Unity-version detection, editor location,
 * project-path validation, environment-variable wiring, editor
 * spawn) is shared with the CLI command — `commands/open.ts`
 * delegates to this function so the two paths cannot drift.
 *
 * Returns a discriminated union — narrow with
 * `result.kind === 'success'` to access `editorPath` / `editorPid`
 * / `unityVersion` / `projectPath`, or `result.kind === 'failure'`
 * to access `errorMessage` / `error`.
 */
export async function openProject(
  options: OpenProjectOptions,
): Promise<OpenProjectResult> {
  const warnings: string[] = [];
  let resolvedProjectPath: string | undefined;
  let resolvedEditorPath: string | undefined;
  let resolvedVersion: string | undefined;

  try {
    const { projectPath } = resolveProjectPath(options.projectPath, process.cwd());
    resolvedProjectPath = projectPath;

    if (!fs.existsSync(projectPath)) {
      throw new Error(`Project path does not exist: ${projectPath}`);
    }

    if (!isUnityProjectDir(projectPath)) {
      throw new Error(
        `Not a Unity project (missing Assets/ or ProjectSettings/ProjectVersion.txt): ${projectPath}`,
      );
    }

    emitProgress(options.onProgress, {
      phase: 'start',
      message: `Opening Unity project at ${projectPath}`,
    });

    // Validate auth/transport BEFORE work begins so callers learn of
    // a typo without first paying for editor-discovery I/O. The
    // returned env is recomputed below if cloud-mode auto-detect
    // flips keepConnected; otherwise we reuse this same map.
    let env = buildOpenEnv(options);

    // Already-running short-circuit. Same semantics as the CLI:
    // success, with the existing PID surfaced.
    const existingProcess = findUnityProcess(projectPath);
    if (existingProcess) {
      warnings.push(
        `Unity is already running with this project (PID: ${existingProcess.pid}). Skipping launch.`,
      );
      emitProgress(options.onProgress, {
        phase: 'done',
        message: 'Unity is already running for this project — launch skipped.',
      });
      return {
        kind: 'success',
        success: true,
        editorPath: extractEditorPathFromCommandLine(existingProcess.commandLine),
        editorPid: existingProcess.pid,
        unityVersion: getProjectEditorVersion(projectPath) ?? undefined,
        projectPath,
        warnings,
        alreadyRunning: true,
      };
    }

    // Resolve editor version: explicit option wins, otherwise read
    // from ProjectVersion.txt.
    emitProgress(options.onProgress, {
      phase: 'detecting-editor-version',
      message: 'Detecting Unity Editor version',
    });
    let version = options.unityVersion;
    if (!version) {
      version = getProjectEditorVersion(projectPath) ?? undefined;
    }
    resolvedVersion = version;

    // Locate editor binary (Unity Hub / common locations).
    const editorPath = await findEditorPath(version);

    // Boolean signal for caller telemetry — we surface only whether
    // editor discovery succeeded, not how many editors are installed.
    emitProgress(options.onProgress, {
      phase: 'editors-located',
      message: editorPath
        ? 'Located Unity Editor candidates'
        : 'Failed to locate any Unity Editor',
      found: editorPath !== null,
    });

    if (!editorPath) {
      const detail = version
        ? `Unity Editor ${version} is not installed.`
        : 'No Unity Editor found.';
      throw new Error(detail);
    }
    resolvedEditorPath = editorPath;

    emitProgress(options.onProgress, {
      phase: 'editor-resolved',
      message: `Resolved Unity Editor at ${editorPath}`,
      editorPath,
      version,
    });

    // Cloud-mode auto-detect: if the project's config is in Cloud
    // mode AND has a cloudToken, ensure keepConnected so the plugin
    // connects on startup; also enable claude-code skill auto-gen.
    let effectiveOptions = options;
    {
      const config = readConfig(projectPath);
      if (config && isCloudMode(config) && config.cloudToken) {
        if (!effectiveOptions.keepConnected) {
          effectiveOptions = { ...effectiveOptions, keepConnected: true };
          warnings.push('Cloud mode with token detected — auto-enabling keep-connected.');
          // keepConnected flipped — rebuild the env map so the editor
          // receives UNITY_MCP_KEEP_CONNECTED=true.
          env = buildOpenEnv(effectiveOptions);
        }
        const skillAutoGenerate = { ...(config.skillAutoGenerate ?? {}) } as Record<string, boolean>;
        if (!skillAutoGenerate['claude-code']) {
          skillAutoGenerate['claude-code'] = true;
          writeConfig(projectPath, { ...config, skillAutoGenerate });
        }
      }
    }

    emitProgress(options.onProgress, {
      phase: 'connection-details',
      message: env
        ? 'MCP connection environment variables prepared'
        : 'No MCP connection environment variables (--no-connect or no options provided)',
      projectPath,
      editorPath,
      envVars: env ?? {},
    });

    emitProgress(options.onProgress, {
      phase: 'launching-editor',
      message: `Launching Unity Editor`,
      editorPath,
      projectPath,
    });

    // Spawn the editor via the shared util. We capture the spawn /
    // error events with callbacks and bridge them to the
    // onProgress channel.
    const child = launchEditor(editorPath, projectPath, env, {
      onSpawn: (pid) => {
        emitProgress(options.onProgress, {
          phase: 'editor-launched',
          message: `Launched Unity Editor (PID: ${pid ?? 'unknown'})`,
          pid,
        });
      },
      onError: (err) => {
        // Don't throw out of the event listener — the caller can't
        // catch it. Surface the warning for diagnostics.
        warnings.push(`Editor spawn reported error: ${err.message}`);
        // The resolved path failed to spawn (binary corrupted,
        // wrong arch, perms revoked, …). Drop the cache so the
        // next invocation re-resolves from Unity Hub instead of
        // handing back the same broken path.
        clearCachedEditorPath(version);
      },
    });

    // The `spawn` event fires asynchronously after `spawn(...)` returns.
    // We wait briefly so the PID gets surfaced in the result. The
    // child is already detached + unref'd, so this does NOT block
    // process exit.
    const pid = await waitForSpawn(child);

    // Auto-dismiss the Unity "compile errors at launch" dialog if it
    // appears. The polling loop runs synchronously inside this call
    // (so the CLI's event loop stays alive until it completes) but
    // never throws — a transient platform error is logged via the
    // progress callback as a warning and the loop continues until
    // either the dialog is dismissed and stays dismissed, the abort
    // signal fires, the no-dialog grace window elapses, or the
    // overall timeout elapses.
    //
    // When `autoDismissLaunchErrors === false` the loop is skipped
    // entirely; behaviour is identical to the pre-feature baseline.
    if (options.autoDismissLaunchErrors !== false) {
      await pollAndDismissLaunchErrors({
        timeoutMs: options.launchDismissTimeoutMs ?? 30000,
        intervalMs: options.launchDismissPollIntervalMs ?? 1500,
        onProgress: options.onProgress,
        warnings,
        abortSignal: options.launchDismissAbortSignal,
      });
    }

    emitProgress(options.onProgress, {
      phase: 'done',
      message: 'Editor launched.',
    });

    return {
      kind: 'success',
      success: true,
      editorPath,
      editorPid: pid,
      unityVersion: version,
      projectPath,
      warnings,
    };
  } catch (err: unknown) {
    const errorObj = err instanceof Error ? err : new Error(String(err));
    return {
      kind: 'failure',
      success: false,
      projectPath: resolvedProjectPath,
      editorPath: resolvedEditorPath,
      unityVersion: resolvedVersion,
      warnings,
      errorMessage: errorObj.message,
      error: errorObj,
    };
  }
}

/**
 * Options for `pollAndDismissLaunchErrors`. The `_` prefix marks the
 * test-only export; production code never imports this directly —
 * `openProject` is the only caller.
 */
export interface _PollAndDismissOptionsForTests {
  timeoutMs: number;
  intervalMs: number;
  onProgress: ProgressCallback | undefined;
  warnings: string[];
  platform?: DismissPlatform;
  probe?: typeof tryDismissLaunchErrorsDialog;
  abortSignal?: AbortSignal;
  /**
   * Grace window (ms) after polling starts: if no dialog has been
   * observed within this window, exit early. Test override only —
   * production callers do not configure this.
   *
   * @see PollAndDismissOptions.noDialogGraceMs for the production
   * default and rationale.
   */
  noDialogGraceMs?: number;
}

interface PollAndDismissOptions {
  timeoutMs: number;
  intervalMs: number;
  onProgress: ProgressCallback | undefined;
  warnings: string[];
  /**
   * Override the platform — exposed for tests so the same polling
   * logic can be exercised across all three OS branches without
   * hopping `process.platform`. Defaults to the running platform.
   */
  platform?: DismissPlatform;
  /**
   * Override the dismiss probe — exposed for tests so the polling
   * loop can be exercised without invoking PowerShell / osascript /
   * xdotool. Defaults to `tryDismissLaunchErrorsDialog`.
   */
  probe?: typeof tryDismissLaunchErrorsDialog;
  /**
   * When fired, the polling loop exits immediately. Intended for
   * callers that have an authoritative readiness signal in scope
   * (e.g. a parallel `wait-for-ready` poll). When omitted, the loop
   * uses the `noDialogGraceMs` fallback.
   */
  abortSignal?: AbortSignal;
  /**
   * Grace window in ms after polling starts: if no dialog has been
   * observed (and no permanent error has been seen) within this
   * window, exit early. Defaults to `15000`. The Unity launch-errors
   * dialog (`"Enter Safe Mode?"` on Unity 2020.2+) is shown after the
   * editor process has booted, connected to the Package Manager, and
   * started compiling — empirically that takes ~6s on a fast machine
   * and longer on a slow one. The default has to cover that startup
   * window or the loop exits before Unity has had a chance to surface
   * the dialog at all (issue #737). Running the full `timeoutMs` in
   * the no-dialog case is still wasteful, so we keep a grace cutoff
   * — just one large enough to pass Unity's startup phase.
   */
  noDialogGraceMs?: number;
}

/**
 * Poll the OS desktop for the Unity "compile errors at launch"
 * dialog and click `Ignore` (or the platform-equivalent) every time
 * the probe reports it as found. Returns once the abort signal has
 * fired, the no-dialog grace window has elapsed, the overall timeout
 * has elapsed, or a permanent platform error has been observed.
 *
 * Re-entrant dismissals are supported: when the dialog re-appears
 * after a successful click (the resolver-fix → dialog-reappears
 * cycle described in the issue), each subsequent dismissal emits its
 * own `launch-errors-dismissed` progress event so the user sees the
 * recurrence in the log.
 *
 * Library-safe: never throws. Transient platform errors (e.g. a
 * missing `xdotool` on Linux, or a momentary syscall failure on
 * Windows) are recorded once as a warning on the supplied
 * `warnings` array; subsequent identical errors are silently
 * suppressed so the warnings array does not balloon over the polling
 * window. Permanent errors (e.g. xdotool truly absent) bail out of
 * the loop after the first occurrence so the helper does not spawn
 * 60 doomed tool invocations on a 30s budget.
 */
export async function _pollAndDismissLaunchErrorsForTests(
  opts: _PollAndDismissOptionsForTests,
): Promise<void> {
  return pollAndDismissLaunchErrors(opts);
}

/**
 * Substring markers that identify a `kind: 'error'` outcome as
 * permanent for this run. When seen, the polling loop bails out
 * after recording the warning once; ticking again would just respawn
 * the same doomed tool. Keep this list narrow — only conditions that
 * cannot self-heal mid-launch belong here.
 *
 * The entries reference the producer-side constants in
 * `launch-error-dismiss.ts` so a re-word at the producer site stays
 * in sync with the bailout matcher (and the test in
 * `launch-error-dismiss.test.ts` locks the constants down).
 */
const PERMANENT_DISMISS_ERROR_MARKERS: readonly string[] = [
  LINUX_XDOTOOL_MISSING_PREFIX,
  UNSUPPORTED_PLATFORM_PREFIX,
];

function isPermanentDismissError(message: string): boolean {
  return PERMANENT_DISMISS_ERROR_MARKERS.some((m) => message.includes(m));
}

async function pollAndDismissLaunchErrors(opts: PollAndDismissOptions): Promise<void> {
  const platform = (opts.platform ?? (nodePlatform() as DismissPlatform));
  const probe = opts.probe ?? tryDismissLaunchErrorsDialog;
  const start = Date.now();
  const deadline = start + Math.max(0, opts.timeoutMs);
  const interval = Math.max(50, opts.intervalMs);
  const graceMs = Math.max(0, opts.noDialogGraceMs ?? 15000);
  const seenErrorMessages = new Set<string>();
  let dismissedAtLeastOnce = false;
  let aborted = opts.abortSignal?.aborted ?? false;
  const onAbort = (): void => { aborted = true; };
  opts.abortSignal?.addEventListener('abort', onAbort, { once: true });
  try {
    while (Date.now() < deadline && !aborted) {
      const outcome = await probe(platform);
      // Re-check the exit conditions BEFORE applying the outcome:
      // if abort fired (or the deadline lapsed) while `probe` was
      // in flight, the in-flight result is stale and applying it
      // would emit a misleading progress event or warning.
      if (aborted || Date.now() >= deadline) break;
      if (outcome.kind === 'dismissed') {
        dismissedAtLeastOnce = true;
        emitProgress(opts.onProgress, {
          phase: 'launch-errors-dismissed',
          message: `[open] dismissed Unity launch-errors dialog (button=${outcome.button}, platform=${platform})`,
          button: outcome.button,
          platform,
        });
        // Keep polling — the dialog may re-appear after the resolver
        // fixes one error and surfaces the next.
      } else if (outcome.kind === 'error') {
        if (!seenErrorMessages.has(outcome.message)) {
          seenErrorMessages.add(outcome.message);
          opts.warnings.push(`launch-errors auto-dismiss: ${outcome.message}`);
        }
        if (isPermanentDismissError(outcome.message)) {
          // No point ticking another 59 times — bail out.
          return;
        }
      } else if (
        // outcome.kind === 'not-found'
        !dismissedAtLeastOnce &&
        opts.abortSignal === undefined &&
        Date.now() - start >= graceMs
      ) {
        // No dialog within the grace window AND no caller-supplied
        // abort signal to wait on. Exit early — the README's "no
        // extra delay" claim depends on this.
        return;
      }
      const remaining = deadline - Date.now();
      if (remaining <= 0 || aborted) break;
      await sleep(Math.min(interval, remaining), opts.abortSignal);
    }
  } finally {
    opts.abortSignal?.removeEventListener('abort', onAbort);
  }
  // Surface a timeout warning ONLY when the deadline was actually
  // reached (not the abort path) AND a platform error was observed.
  // The grace-window exit returns silently above; the abort path is
  // a caller-driven cancellation, not a timeout.
  if (
    Date.now() >= deadline &&
    !aborted &&
    seenErrorMessages.size > 0 &&
    !dismissedAtLeastOnce
  ) {
    opts.warnings.push(
      `launch-errors auto-dismiss timed out after ${opts.timeoutMs}ms — continuing with wait-for-ready`,
    );
  }
}

function sleep(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve) => {
    if (signal?.aborted) {
      resolve();
      return;
    }
    const t = setTimeout(() => {
      signal?.removeEventListener('abort', onAbort);
      resolve();
    }, ms);
    const onAbort = (): void => {
      clearTimeout(t);
      resolve();
    };
    signal?.addEventListener('abort', onAbort, { once: true });
    // Do NOT unref() — the polling timer is the reason `openProject`
    // is awaiting in the first place, and unref'ing would let Node
    // exit before the loop completes.
  });
}

/**
 * Resolve to the spawned PID once the child process emits its
 * `spawn` event, or to `undefined` if a short timeout elapses or
 * the process emits `error` first. Library-safe: never throws,
 * never blocks process exit (the caller has already detached).
 */
function waitForSpawn(
  child: import('child_process').ChildProcess,
  timeoutMs = 2000,
): Promise<number | undefined> {
  return new Promise((resolve) => {
    if (child.pid !== undefined && child.pid !== null) {
      // Some Node versions populate `pid` synchronously on spawn().
      resolve(child.pid);
      return;
    }
    let settled = false;
    const finish = (pid: number | undefined): void => {
      if (settled) return;
      settled = true;
      resolve(pid);
    };
    child.once('spawn', () => finish(child.pid ?? undefined));
    child.once('error', () => finish(undefined));
    setTimeout(() => finish(child.pid ?? undefined), timeoutMs).unref();
  });
}
