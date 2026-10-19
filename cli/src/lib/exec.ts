// Library-safe `execCode`: run C# in a project's Unity Editor through `script-execute`, starting a warm
// headless editor when none is running. No console output, no `process.exit`.

import {
  DEFAULT_SESSION_IDLE_SECONDS,
  EditorSessionError,
  type EditorSession,
  type EditorSessionErrorReason,
  type WaitedEditorSource,
} from '@baizor/gamedev-cli-core';
import { createUnitySession, isLoopbackUrl, sessionErrorMessage, type UnitySessionOptions } from './editor-session.js';
import { buildOpenEnv, dismissLaunchErrorsUntil } from './open.js';
import { runTool } from './run-tool.js';
import type { RunToolFailure, RunToolOptions, RunToolResult } from './types.js';

export const SCRIPT_EXECUTE_TOOL = 'script-execute';

const DEFAULT_TIMEOUT_MS = 180_000;

/** Injection points for tests. */
export interface ExecDeps {
  session: (options: UnitySessionOptions) => Pick<EditorSession<string>, 'ensure'>;
  invoke: (options: RunToolOptions) => Promise<RunToolResult>;
}

export interface ExecCodeOptions {
  projectPath: string;
  code: string;
  usings?: string[];
  include?: string[];
  /** Start the editor with a window instead of headless (only when it is not running yet). */
  window?: boolean;
  /** An editor started here quits after this many seconds without tool calls (0 = never). Default 300. */
  idleTimeoutSeconds?: number;
  /** Default 300000. */
  readyTimeoutMs?: number;
  /** Wait for an editor exec did not start (the user's GUI) to answer; see `UnitySessionOptions.foreignReadyTimeoutMs`. */
  foreignReadyTimeoutMs?: number;
  /** Tool call timeout. Default 180000. */
  timeoutMs?: number;
  /** Server URLs to try, first answer wins (the CLI passes the config URL plus the project's local port). */
  probeUrls: string[];
  /** Bearer for the server. */
  token?: string;
  /** `token` came from the shared machine store: a 401 refreshes it once. */
  tokenFromCloudStore?: boolean;
  /** Explicit server URL / token: handed to a newly started editor so its plugin connects there. */
  overrideUrl?: string;
  overrideToken?: string;
  onStatus?: (message: string) => void;
  deps?: Partial<ExecDeps>;
}

export interface ExecCodeSuccess {
  kind: 'success';
  success: true;
  /** The server that ran the code. */
  baseUrl: string;
  /** Parsed `script-execute` response body. */
  data: unknown;
}

export interface ExecCodeFailure {
  kind: 'failure';
  success: false;
  /** `launch-failed`: no editor could be started; `not-ready`: its server never answered; `tool-failed`: the call failed. */
  reason: 'launch-failed' | 'not-ready' | 'tool-failed';
  /** The editor-session failure behind `launch-failed` / `not-ready`. */
  sessionReason?: EditorSessionErrorReason;
  /** `not-ready`: the editor that never answered — `foreign` is one exec did not start (e.g. the user's GUI). */
  editorSource?: WaitedEditorSource;
  /** The server that was called (`tool-failed` only). */
  baseUrl?: string;
  /** The underlying call failure (`tool-failed` only). */
  toolFailure?: RunToolFailure;
  message: string;
  error?: Error;
}

export type ExecCodeResult = ExecCodeSuccess | ExecCodeFailure;

/**
 * Run C# in the project's editor. A running editor (GUI or headless) is reused and never closed; otherwise one is
 * started headless (or windowed) under the project's session lease and start lock, so parallel calls share one
 * editor. It quits by itself after `idleTimeoutSeconds` without tool calls, so the next call stays warm.
 */
export async function execCode(options: ExecCodeOptions): Promise<ExecCodeResult> {
  const deps: ExecDeps = { session: createUnitySession, invoke: runTool, ...options.deps };

  // The server a started editor connects to (recorded in its lease): the explicit URL, normalized as the CLI resolves
  // it (no trailing slash), else the resolved one.
  const serverUrl = options.overrideUrl?.replace(/\/$/, '') ?? options.probeUrls[0];
  let baseUrl: string;
  // A windowed editor we start may stop on the "compile errors at launch" dialog; click it away until it is ready.
  const ready = new AbortController();
  let dismissing: Promise<void> | undefined;
  try {
    const session = deps.session({
      projectPath: options.projectPath,
      urls: options.probeUrls,
      token: options.token,
      ...(serverUrl !== undefined ? { serverUrl } : {}),
      env: buildOpenEnv({
        keepConnected: true,
        url: options.overrideUrl,
        token: options.overrideToken,
        // A local server is only reachable if someone starts it; the plugin does when asked.
        ...(options.overrideUrl && isLoopbackUrl(options.overrideUrl) ? { startServer: true } : {}),
      }),
      idleSeconds: options.idleTimeoutSeconds ?? DEFAULT_SESSION_IDLE_SECONDS,
      ...(options.readyTimeoutMs !== undefined ? { readyTimeoutMs: options.readyTimeoutMs } : {}),
      ...(options.foreignReadyTimeoutMs !== undefined ? { foreignReadyTimeoutMs: options.foreignReadyTimeoutMs } : {}),
      ...(options.onStatus ? { onStatus: options.onStatus } : {}),
      ...(options.window ? { onLaunched: () => { dismissing = dismissLaunchErrorsUntil(ready.signal, []); } } : {}),
    });
    baseUrl = (await session.ensure({ mode: options.window ? 'window' : 'headless' })).ready;
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    const sessionError = err instanceof EditorSessionError ? err : undefined;
    const sessionReason = sessionError?.reason;
    const reason = sessionReason === 'exited' || sessionReason === 'not-ready' ? 'not-ready' : 'launch-failed';
    return {
      kind: 'failure',
      success: false,
      reason,
      ...(sessionReason ? { sessionReason } : {}),
      ...(sessionError?.source ? { editorSource: sessionError.source } : {}),
      message: sessionErrorMessage(error, serverUrl),
      error,
    };
  } finally {
    ready.abort();
    await dismissing;
  }

  options.onStatus?.('Running...');
  const result = await deps.invoke({
    toolName: SCRIPT_EXECUTE_TOOL,
    url: baseUrl,
    ...(options.token ? { token: options.token } : {}),
    tokenFromCloudStore: options.tokenFromCloudStore,
    input: buildScriptExecuteInput(options.code, options.usings ?? [], options.include ?? []),
    timeoutMs: options.timeoutMs ?? DEFAULT_TIMEOUT_MS,
  });
  if (result.kind === 'failure') {
    return {
      kind: 'failure', success: false, reason: 'tool-failed', baseUrl, toolFailure: result, message: result.message, error: result.error,
    };
  }
  return { kind: 'success', success: true, baseUrl, data: result.data };
}

/** Request body for `script-execute`; empty lists are left out so older plugins without them still accept it. */
export function buildScriptExecuteInput(code: string, usings: string[], include: string[]): Record<string, unknown> {
  return {
    csharpCode: code,
    ...(usings.length > 0 ? { usings } : {}),
    ...(include.length > 0 ? { include } : {}),
  };
}

/**
 * The tool's text output and whether it reported success. A tool that returns a value (as `script-execute` does) is
 * answered as `{ status, structured: { result } }`; the result alone is the output then.
 */
export function readToolResponse(data: unknown): { ok: boolean; text: string } {
  if (typeof data === 'string') return { ok: true, text: data };
  const response = data as {
    status?: string;
    content?: Array<{ type?: string; text?: string }>;
    structured?: unknown;
  } | null;
  const ok = response?.status === undefined || response.status === 'success';
  const structured = response?.structured;
  if (structured !== undefined && structured !== null) {
    const value = typeof structured === 'object' && 'result' in structured
      ? (structured as { result: unknown }).result
      : structured;
    return { ok, text: typeof value === 'string' ? value : JSON.stringify(value) ?? '' };
  }
  const texts = (response?.content ?? []).filter(c => c?.type === 'text' && typeof c.text === 'string').map(c => c.text!);
  return {
    ok,
    text: texts.length > 0 ? texts.join('\n') : data === undefined ? '' : JSON.stringify(data),
  };
}
