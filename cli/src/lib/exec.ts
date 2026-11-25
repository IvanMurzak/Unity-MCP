// Library-safe `execCode`: run C# in a project's Unity Editor through `script-execute`, starting a warm
// headless editor when none is running. No console output, no `process.exit`.

import type { EditorSessionErrorReason, EnsureSource, WaitedEditorSource } from '@baizor/gamedev-cli-core';
import type { UnitySessionOptions } from './editor-session.js';
import { ensureEditor, type EnsureEditorSession } from './ensure-editor.js';
import { runTool } from './run-tool.js';
import type { RunToolFailure, RunToolOptions, RunToolResult } from './types.js';

export const SCRIPT_EXECUTE_TOOL = 'script-execute';

/**
 * Default `exec` call timeout: the MCP server's default `tool-timeout` (300 s, McpPlugin 8.11.0+, which the Unity plugin
 * launches its server with) plus 10 s, so the CLI never gives up before the server answers with its own timeout error.
 */
export const DEFAULT_EXEC_TIMEOUT_MS = 310_000;

/** The reload refusal text, also used by compile's read-only status polling. */
export const RELOAD_PENDING_TEXT = 'Scripts are compiling and reload next';

/** Injection points for tests. */
export interface ExecDeps {
  session: (options: UnitySessionOptions) => EnsureEditorSession;
  invoke: (options: RunToolOptions) => Promise<RunToolResult>;
  /** @deprecated Retries are owned by the server; retained for source compatibility. */
  sleep?: (ms: number) => Promise<void>;
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
  /** Tool call timeout. Default `DEFAULT_EXEC_TIMEOUT_MS` (310000). */
  timeoutMs?: number;
  /** Server URLs to try, first answer wins (the CLI passes the config URL plus the project's local port). */
  probeUrls: string[];
  /** Close the editor after the call when this call started it (`--once`). */
  once?: boolean;
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
  /** How the editor was obtained (`started`: this call launched it). */
  editorSource?: EnsureSource;
  /** With `once`: the pid of the editor this call started and closed again. */
  closedPid?: number;
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
  /** With `once`: the pid of the editor this call started and closed again. */
  closedPid?: number;
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
  const deps: Partial<ExecDeps> & Pick<ExecDeps, 'invoke'> = { invoke: runTool, ...options.deps };

  const ensured = await ensureEditor({
    projectPath: options.projectPath,
    probeUrls: options.probeUrls,
    token: options.token,
    overrideUrl: options.overrideUrl,
    overrideToken: options.overrideToken,
    window: options.window,
    idleTimeoutSeconds: options.idleTimeoutSeconds,
    readyTimeoutMs: options.readyTimeoutMs,
    foreignReadyTimeoutMs: options.foreignReadyTimeoutMs,
    onStatus: options.onStatus,
    session: deps.session,
    closeOnFailure: options.once,
  });
  if (ensured.kind === 'failure') {
    const { kind: _kind, ...failure } = ensured;
    return { kind: 'failure', success: false, ...failure };
  }
  const { baseUrl } = ensured;

  options.onStatus?.('Running...');
  // The server retries only calls explicitly marked not-executed. Its HTTP
  // error body contains only a message, so the CLI cannot safely infer that
  // user code did not run. Preserve the final response without re-sending it.
  const result = await deps.invoke({
    toolName: SCRIPT_EXECUTE_TOOL,
    url: baseUrl,
    ...(options.token ? { token: options.token } : {}),
    tokenFromCloudStore: options.tokenFromCloudStore,
    input: buildScriptExecuteInput(options.code, options.usings ?? [], options.include ?? []),
    timeoutMs: options.timeoutMs ?? DEFAULT_EXEC_TIMEOUT_MS,
  });
  const closedPid = options.once ? await ensured.closeIfStarted() : undefined;
  const closed = closedPid !== undefined ? { closedPid } : {};
  if (result.kind === 'failure') {
    return {
      kind: 'failure', success: false, reason: 'tool-failed', baseUrl, toolFailure: result, message: result.message, error: result.error, ...closed,
    };
  }
  return {
    kind: 'success', success: true, baseUrl, data: result.data, ...(ensured.source ? { editorSource: ensured.source } : {}), ...closed,
  };
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
