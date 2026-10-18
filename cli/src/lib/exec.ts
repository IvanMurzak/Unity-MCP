// Library-safe `execCode`: run C# in a project's Unity Editor through `script-execute`, starting a warm
// headless editor when none is running. No console output, no `process.exit`.

import { isLoopbackHost } from '@baizor/gamedev-cli-core';
import { openProject } from './open.js';
import { runTool } from './run-tool.js';
import { waitForReady } from './wait-for-ready.js';
import { isProcessAlive } from '../utils/unity-shutdown.js';
import type { OpenProjectOptions, OpenProjectResult, RunToolFailure, RunToolOptions, RunToolResult } from './types.js';
import type { WaitForReadyOptions, WaitForReadyResult } from './wait-for-ready.js';

export const SCRIPT_EXECUTE_TOOL = 'script-execute';

const DEFAULT_IDLE_SECONDS = 300;
const DEFAULT_READY_TIMEOUT_MS = 300_000;
const DEFAULT_TIMEOUT_MS = 180_000;
/** One quick ping round: a running editor with a local server answers within this. */
const WARM_PROBE_MS = 500;

/** Injection points for tests. */
export interface ExecDeps {
  open: (options: OpenProjectOptions) => Promise<OpenProjectResult>;
  wait: (options: WaitForReadyOptions) => Promise<WaitForReadyResult>;
  invoke: (options: RunToolOptions) => Promise<RunToolResult>;
  isAlive: (pid: number) => boolean;
}

export interface ExecCodeOptions {
  projectPath: string;
  code: string;
  usings?: string[];
  include?: string[];
  /** Start the editor with a window instead of headless (only when it is not running yet). */
  window?: boolean;
  /** An editor started here quits after this many seconds without tool calls. Default 300. */
  idleTimeoutSeconds?: number;
  /** Default 300000. */
  readyTimeoutMs?: number;
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
 * started headless (or windowed) that quits by itself after `idleTimeoutSeconds` without tool calls, so the next call
 * stays warm.
 */
export async function execCode(options: ExecCodeOptions): Promise<ExecCodeResult> {
  const deps: ExecDeps = {
    open: openProject, wait: waitForReady, invoke: runTool, isAlive: pid => isProcessAlive(pid), ...options.deps,
  };
  const status = options.onStatus ?? (() => {});
  const idleTimeoutSeconds = options.idleTimeoutSeconds ?? DEFAULT_IDLE_SECONDS;

  // Warm path: a local server that answers right away means an editor is up; skip the process scan and launch.
  // A remote (cloud) URL answers whether or not this editor runs, so it never counts here.
  const localUrls = options.probeUrls.filter(isLoopbackUrl);
  let baseUrl = localUrls.length > 0
    ? readyUrl(await deps.wait({ urls: localUrls, token: options.token, timeoutMs: 1, intervalMs: WARM_PROBE_MS }))
    : undefined;

  if (baseUrl === undefined) {
    status(options.window ? 'Starting Unity Editor...' : 'Starting Unity Editor (headless)...');
    const opened = await deps.open({
      projectPath: options.projectPath,
      headless: !options.window,
      keepConnected: true,
      idleTimeoutSeconds,
      url: options.overrideUrl,
      token: options.overrideToken,
      // A local server is only reachable if someone starts it; the plugin does when asked.
      ...(options.overrideUrl && isLoopbackUrl(options.overrideUrl) ? { startServer: true } : {}),
    });
    if (opened.kind === 'failure') {
      return { kind: 'failure', success: false, reason: 'launch-failed', message: opened.errorMessage, error: opened.error };
    }
    if (!opened.alreadyRunning) status(`Editor started; it quits after ${idleTimeoutSeconds}s without tool calls.`);

    status('Waiting for the MCP server...');
    // An editor that dies while starting (e.g. batchmode aborts on compile errors) would otherwise cost the whole
    // ready timeout.
    const launchedPid = opened.alreadyRunning ? undefined : opened.editorPid;
    const ready = await deps.wait({
      urls: options.probeUrls,
      token: options.token,
      timeoutMs: options.readyTimeoutMs ?? DEFAULT_READY_TIMEOUT_MS,
      intervalMs: 2000,
      stopReason: () => launchedPid !== undefined && !deps.isAlive(launchedPid)
        ? `The Unity Editor (PID ${launchedPid}) exited before its MCP server answered; see the Editor log`
        : undefined,
    });
    if (ready.kind === 'failure') {
      return { kind: 'failure', success: false, reason: 'not-ready', message: ready.error.message, error: ready.error };
    }
    baseUrl = ready.baseUrl;
  }

  status('Running...');
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

function readyUrl(result: WaitForReadyResult): string | undefined {
  return result.kind === 'success' ? result.baseUrl : undefined;
}

function isLoopbackUrl(url: string): boolean {
  try {
    const host = new URL(url).hostname;
    return host === 'localhost' || isLoopbackHost(host);
  } catch {
    return false;
  }
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
