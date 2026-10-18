import { probe as defaultProbe, type ProbeResult } from '../utils/probe.js';

const MAX_PROBE_TIMEOUT_MS = 10_000;

export interface WaitForReadyOptions {
  /** Base URLs to probe; the first one to answer the ping wins. */
  urls: string[];
  /** Request headers (e.g. `Authorization`). */
  headers?: Record<string, string>;
  /** Give up after this many milliseconds. */
  timeoutMs: number;
  /** Delay between probe rounds, also the per-probe cap (at most 10 s). */
  intervalMs: number;
  /** Called before every probe round with the time left. */
  onTick?: (remainingMs: number) => void;
  /** Injection point for tests. */
  probeImpl?: typeof defaultProbe;
}

export type WaitForReadyResult =
  | { kind: 'success'; baseUrl: string; elapsedMs: number }
  | { kind: 'failure'; reason: 'timeout'; elapsedMs: number };

/**
 * Poll the MCP server's ping endpoint on every URL until one answers or the timeout elapses. Library-safe: never
 * exits, never prints.
 */
export async function waitForReady(options: WaitForReadyOptions): Promise<WaitForReadyResult> {
  const probe = options.probeImpl ?? defaultProbe;
  const headers = { 'Content-Type': 'application/json', ...options.headers };
  const startTime = Date.now();

  while (true) {
    const elapsed = Date.now() - startTime;
    if (elapsed >= options.timeoutMs) return { kind: 'failure', reason: 'timeout', elapsedMs: elapsed };
    options.onTick?.(options.timeoutMs - elapsed);

    const probeTimeout = Math.min(options.intervalMs, MAX_PROBE_TIMEOUT_MS);
    // Race probes: resolve as soon as any succeeds, don't wait for slow ones.
    const ready = await new Promise<ProbeResult | null>(resolve => {
      let pending = options.urls.length;
      if (pending === 0) resolve(null);
      for (const url of options.urls) {
        probe(url, headers, probeTimeout).then(result => {
          if (result.ok) resolve(result);
          else if (--pending === 0) resolve(null);
        });
      }
    });
    if (ready?.ok) return { kind: 'success', baseUrl: ready.baseUrl, elapsedMs: Date.now() - startTime };

    const remainingMs = options.timeoutMs - (Date.now() - startTime);
    if (remainingMs > 0) await new Promise(resolve => setTimeout(resolve, Math.min(options.intervalMs, remainingMs)));
  }
}
