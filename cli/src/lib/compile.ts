// Library-safe `compileProject`: "does my C# compile?" against the project's editor (started headless when none
// runs, then kept warm): refresh assets so changed scripts compile, wait for the compile and its domain reload, and
// read the compiler diagnostics from the editor's Console. No console output, no `process.exit`.

import { COMPILE_STATUS_SNIPPET, parseCompileStatus, REFRESH_SNIPPET, type CompileState } from './compile-snippets.js';
import { ensureEditor, type EnsureEditorOptions, type EnsureEditorFailure } from './ensure-editor.js';
import { readToolResponse, SCRIPT_EXECUTE_TOOL, buildScriptExecuteInput, RELOAD_PENDING_TEXT } from './exec.js';
import { runTool } from './run-tool.js';
import type { RunToolFailure, RunToolOptions, RunToolResult } from './types.js';
import type { CompilerMessage } from '../utils/compiler-messages.js';

/** Default wait for the compile and the domain reload after it. */
export const DEFAULT_COMPILE_TIMEOUT_MS = 180_000;

/** One tool call's timeout while polling (a reloading editor answers late or not at all). */
const POLL_CALL_TIMEOUT_MS = 30_000;
const POLL_INTERVAL_MS = 1_000;
/** Settled answers in a row before the state counts: a compile may start a moment after the refresh. */
const SETTLED_POLLS = 2;

export interface CompileProjectOptions extends Omit<EnsureEditorOptions, 'onStatus' | 'session'> {
  /** Overall wait for the compile, default `DEFAULT_COMPILE_TIMEOUT_MS`. */
  timeoutMs?: number;
  /** Close the editor afterwards when this call started it. */
  once?: boolean;
  tokenFromCloudStore?: boolean;
  onStatus?: (message: string) => void;
  deps?: Partial<CompileDeps>;
}

export interface CompileDeps {
  ensure: typeof ensureEditor;
  invoke: (options: RunToolOptions) => Promise<RunToolResult>;
  sleep: (ms: number) => Promise<void>;
  now: () => number;
}

export interface CompileProjectSuccess {
  kind: 'completed';
  /** `failed`: the editor reports a failed script compile. */
  state: Exclude<CompileState, 'busy'>;
  errors: CompilerMessage[];
  warnings: CompilerMessage[];
  baseUrl: string;
  editorSource?: string;
  closedPid?: number;
}

export interface CompileProjectFailure {
  kind: 'failure';
  /** `unreachable`: no editor could be started or reached; `timeout`: the compile never settled; `tool-failed`. */
  reason: 'unreachable' | 'timeout' | 'tool-failed';
  message: string;
  editorFailure?: EnsureEditorFailure;
  toolFailure?: RunToolFailure;
  closedPid?: number;
}

export type CompileProjectResult = CompileProjectSuccess | CompileProjectFailure;

/** A call failure a reloading editor produces: retry it until the deadline. */
function transient(failure: RunToolFailure): boolean {
  return failure.reason === 'connection-refused' || failure.reason === 'connection-reset' || failure.reason === 'network-error'
    || failure.reason === 'timeout' || (failure.reason === 'http-error' && failure.httpStatus === 503)
    || failure.message.includes(RELOAD_PENDING_TEXT);
}

export async function compileProject(options: CompileProjectOptions): Promise<CompileProjectResult> {
  const deps: CompileDeps = {
    ensure: ensureEditor,
    invoke: runTool,
    sleep: ms => new Promise(resolve => setTimeout(resolve, ms)),
    now: () => Date.now(),
    ...options.deps,
  };
  const { timeoutMs, once, tokenFromCloudStore, deps: _deps, ...ensureOptions } = options;
  const ensured = await deps.ensure({ ...ensureOptions, closeOnFailure: once });
  if (ensured.kind === 'failure') return { kind: 'failure', reason: 'unreachable', message: ensured.message, editorFailure: ensured };

  const deadline = deps.now() + (timeoutMs ?? DEFAULT_COMPILE_TIMEOUT_MS);
  const call = (code: string) => deps.invoke({
    toolName: SCRIPT_EXECUTE_TOOL,
    url: ensured.baseUrl,
    ...(options.token ? { token: options.token } : {}),
    tokenFromCloudStore,
    input: buildScriptExecuteInput(code, [], []),
    timeoutMs: POLL_CALL_TIMEOUT_MS,
  });
  const finish = async <T extends CompileProjectResult>(result: T): Promise<T> => {
    const closedPid = once ? await ensured.closeIfStarted() : undefined;
    return closedPid !== undefined ? { ...result, closedPid } : result;
  };

  // 1. Refresh: changed scripts compile (and the editor reloads its domain after a successful compile).
  options.onStatus?.('Refreshing assets...');
  for (;;) {
    const refreshed = await call(REFRESH_SNIPPET);
    if (refreshed.kind === 'failure') {
      if (!transient(refreshed)) return finish({ kind: 'failure', reason: 'tool-failed', message: refreshed.message, toolFailure: refreshed });
    } else {
      const response = readToolResponse(refreshed.data);
      if (response.ok) break;
      if (!response.text.includes(RELOAD_PENDING_TEXT)) return finish({ kind: 'failure', reason: 'tool-failed', message: response.text });
    }
    if (deps.now() >= deadline) return finish({ kind: 'failure', reason: 'timeout', message: 'The editor did not accept the refresh before the timeout.' });
    await deps.sleep(POLL_INTERVAL_MS);
  }

  // 2. Poll until the compile (and reload) settled, then read the diagnostics.
  options.onStatus?.('Compiling...');
  let settled = 0;
  let lastFailure: RunToolFailure | undefined;
  for (;;) {
    await deps.sleep(POLL_INTERVAL_MS);
    const polled = await call(COMPILE_STATUS_SNIPPET);
    if (polled.kind === 'failure') {
      if (!transient(polled)) return finish({ kind: 'failure', reason: 'tool-failed', message: polled.message, toolFailure: polled });
      lastFailure = polled;
      settled = 0;
      options.onStatus?.('Waiting for the editor to reload...');
    } else {
      const response = readToolResponse(polled.data);
      const status = response.ok ? parseCompileStatus(response.text) : undefined;
      if (!response.ok && !response.text.includes(RELOAD_PENDING_TEXT)) {
        return finish({ kind: 'failure', reason: 'tool-failed', message: response.text });
      }
      if (status && status.state !== 'busy') {
        settled += 1;
        if (settled >= SETTLED_POLLS) {
          return finish({
            kind: 'completed',
            state: status.state,
            errors: status.messages.filter(m => m.severity === 'error'),
            warnings: status.messages.filter(m => m.severity === 'warning'),
            baseUrl: ensured.baseUrl,
            ...(ensured.source ? { editorSource: ensured.source } : {}),
          });
        }
      } else {
        settled = 0;
      }
    }
    if (deps.now() >= deadline) {
      return finish({
        kind: 'failure',
        reason: 'timeout',
        message: `The compile did not finish within ${Math.round((timeoutMs ?? DEFAULT_COMPILE_TIMEOUT_MS) / 1000)} seconds.`,
        ...(lastFailure ? { toolFailure: lastFailure } : {}),
      });
    }
  }
}
