import { mcpReadinessProbe, waitForReady as pollUntilReady } from '@baizor/gamedev-cli-core';

const MAX_PROBE_TIMEOUT_MS = 10_000;

export interface WaitForReadyOptions {
  /** Base URLs to probe; the first one to answer the ping wins. */
  urls: string[];
  /** Bearer token sent with every probe. */
  token?: string;
  /** Give up after this many milliseconds. */
  timeoutMs: number;
  /** Delay between probe rounds, also the per-probe cap (at most 10 s). */
  intervalMs: number;
  /** Called before every probe round with the time left. */
  onTick?: (remainingMs: number) => void;
  /** Checked before every probe round: a returned message ends the wait as a failure (e.g. the editor exited). */
  stopReason?: () => string | undefined;
  /** Injection point for tests. */
  fetchImpl?: typeof fetch;
}

export interface WaitForReadySuccess {
  kind: 'success';
  success: true;
  /** The URL that answered. */
  baseUrl: string;
  elapsedMs: number;
}

export interface WaitForReadyFailure {
  kind: 'failure';
  success: false;
  elapsedMs: number;
  error: Error;
}

export type WaitForReadyResult = WaitForReadySuccess | WaitForReadyFailure;

/**
 * Poll the MCP server's ping endpoint on every URL until one answers or the timeout elapses. An auth rejection
 * (401/403) counts as an answer: the server is up, and the next call reports the auth problem. Library-safe: never
 * exits, never prints.
 */
export async function waitForReady(options: WaitForReadyOptions): Promise<WaitForReadyResult> {
  const startTime = Date.now();
  const probe = mcpReadinessProbe({
    urls: options.urls,
    token: options.token,
    timeoutMs: Math.min(options.intervalMs, MAX_PROBE_TIMEOUT_MS),
    ...(options.fetchImpl ? { fetchImpl: options.fetchImpl } : {}),
  });
  const result = await pollUntilReady({
    probe: () => {
      options.onTick?.(options.timeoutMs - (Date.now() - startTime));
      return probe();
    },
    timeoutMs: options.timeoutMs,
    intervalMs: options.intervalMs,
    ...(options.stopReason ? { stopReason: options.stopReason } : {}),
  });
  if (result.kind === 'ready') return { kind: 'success', success: true, baseUrl: result.value, elapsedMs: result.elapsedMs };
  return { kind: 'failure', success: false, elapsedMs: result.elapsedMs, error: new Error(result.message) };
}
