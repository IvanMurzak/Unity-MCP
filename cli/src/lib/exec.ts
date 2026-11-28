// Library-safe `execCode`: run C# in a project's Unity Editor through `csharp-exec`, starting a warm
// headless editor when none is running. No console output, no `process.exit`.

import type { EditorSessionErrorReason, EnsureSource, WaitedEditorSource } from '@baizor/gamedev-cli-core';
import type { UnitySessionOptions } from './editor-session.js';
import { ensureEditor, type EnsureEditorSession } from './ensure-editor.js';
import { runTool } from './run-tool.js';
import type { RunToolFailure, RunToolOptions, RunToolResult } from './types.js';

export const CSHARP_EXEC_TOOL = 'csharp-exec';

/** The tool's name before DEV-314: a plugin released before the rename has only this one. */
export const LEGACY_CSHARP_EXEC_TOOL = 'script-execute';

/** Which name of the C# tool the editor answers to, remembered across the calls of one command. */
export interface CSharpExecToolState {
  toolName?: string;
}

/**
 * Call `csharp-exec`. A plugin from before the rename answers "Tool with Name 'csharp-exec' not found": then the call is
 * sent again by the old name, which `state` keeps for the next calls.
 */
export async function invokeCSharpExec(
  invoke: (options: RunToolOptions) => Promise<RunToolResult>,
  options: Omit<RunToolOptions, 'toolName'>,
  state: CSharpExecToolState = {},
): Promise<RunToolResult> {
  const toolName = state.toolName ?? CSHARP_EXEC_TOOL;
  const result = await invoke({ ...options, toolName });
  if (toolName !== CSHARP_EXEC_TOOL || !toolNotFound(result, CSHARP_EXEC_TOOL)) return result;
  state.toolName = LEGACY_CSHARP_EXEC_TOOL;
  return invoke({ ...options, toolName: LEGACY_CSHARP_EXEC_TOOL });
}

function toolNotFound(result: RunToolResult, toolName: string): boolean {
  const text = result.kind === 'failure'
    ? `${result.message} ${typeof result.data === 'string' ? result.data : JSON.stringify(result.data ?? '')}`
    : readToolResponse(result.data).text;
  return text.includes(`'${toolName}' not found`);
}

/**
 * Default `exec` call timeout: the MCP server's default `tool-timeout` (300 s, McpPlugin 8.11.0+, which the Unity plugin
 * launches its server with) plus 10 s, so the CLI never gives up before the server answers with its own timeout error.
 */
export const DEFAULT_EXEC_TIMEOUT_MS = 310_000;

/** The plugin's answer while a compile waits for its reload: the snippet did not run (Script.Execute.Reload.cs). */
export const RELOAD_PENDING_TEXT = 'Scripts are compiling and reload next';

/** How long `exec` keeps re-sending a snippet the plugin deferred for a reload. */
export const RELOAD_WAIT_MS = 120_000;

function deferredForReload(result: RunToolResult): boolean {
  if (result.kind === 'failure') return result.message.includes(RELOAD_PENDING_TEXT);
  const response = readToolResponse(result.data);
  return !response.ok && response.text.includes(RELOAD_PENDING_TEXT);
}

/** Injection points for tests. */
export interface ExecDeps {
  session: (options: UnitySessionOptions) => EnsureEditorSession;
  invoke: (options: RunToolOptions) => Promise<RunToolResult>;
  /** Waits between re-sends of a deferred snippet (default: a timer). */
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
  /** Parsed `csharp-exec` response body. */
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
  const sleep = deps.sleep ?? ((ms: number) => new Promise<void>((resolve) => setTimeout(resolve, ms)));
  const deadline = Date.now() + RELOAD_WAIT_MS;
  const tool: CSharpExecToolState = {};
  let result: RunToolResult;
  for (;;) {
    result = await invokeCSharpExec(deps.invoke, {
      url: baseUrl,
      ...(options.token ? { token: options.token } : {}),
      tokenFromCloudStore: options.tokenFromCloudStore,
      input: buildCSharpExecInput(options.code, options.usings ?? [], options.include ?? []),
      timeoutMs: options.timeoutMs ?? DEFAULT_EXEC_TIMEOUT_MS,
    }, tool);
    // The snippet did not run because scripts are about to reload: send it again once they have.
    if (!deferredForReload(result) || Date.now() >= deadline) break;
    options.onStatus?.('Scripts are compiling; waiting for the reload...');
    await sleep(2_000);
  }
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

/** Request body for `csharp-exec`; empty lists are left out so older plugins without them still accept it. */
export function buildCSharpExecInput(code: string, usings: string[], include: string[]): Record<string, unknown> {
  return {
    csharpCode: code,
    ...(usings.length > 0 ? { usings } : {}),
    ...(include.length > 0 ? { include } : {}),
  };
}

/** @deprecated DEV-314: renamed to {@link buildCSharpExecInput}, with the tool. Remove together with the plugin's alias. */
export const buildScriptExecuteInput = buildCSharpExecInput;

/**
 * The tool's text output and whether it reported success. A tool that returns a value (as `csharp-exec` does) is
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
