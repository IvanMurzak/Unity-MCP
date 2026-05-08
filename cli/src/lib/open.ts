import * as fs from 'fs';
import * as path from 'path';
import {
  findEditorPath,
  getProjectEditorVersion,
  launchEditor,
} from '../utils/unity-editor.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { readConfig, isCloudMode, writeConfig } from '../utils/config.js';
import { emitProgress } from './progress.js';
import type {
  OpenEnvInputs,
  OpenProjectAuthOption,
  OpenProjectOptions,
  OpenProjectResult,
  OpenProjectTransport,
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
      },
    });

    // The `spawn` event fires asynchronously after `spawn(...)` returns.
    // We wait briefly so the PID gets surfaced in the result. The
    // child is already detached + unref'd, so this does NOT block
    // process exit.
    const pid = await waitForSpawn(child);

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
