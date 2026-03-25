import { verbose } from './ui.js';

export const PING_ENDPOINT = '/api/system-tools/ping';

export interface ProbeSuccess {
  ok: true;
  baseUrl: string;
  data: unknown;
}

export interface ProbeFailure {
  ok: false;
  reason: string;
}

export type ProbeResult = ProbeSuccess | ProbeFailure;

/**
 * Probe an MCP server's ping endpoint. Returns structured result.
 */
export async function probe(
  baseUrl: string,
  headers: Record<string, string>,
  timeoutMs: number,
): Promise<ProbeResult> {
  const endpoint = `${baseUrl}${PING_ENDPOINT}`;
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(endpoint, {
      method: 'POST',
      headers,
      body: '{}',
      signal: controller.signal,
    });

    const text = await response.text();
    if (response.ok) {
      let data: unknown;
      try { data = JSON.parse(text); } catch { data = text; }
      return { ok: true, baseUrl, data };
    }

    verbose(`[${baseUrl}] HTTP ${response.status} — not ready yet`);
    return { ok: false, reason: `HTTP ${response.status}` };
  } catch (err) {
    const cause = err instanceof Error && 'cause' in err ? (err.cause as Error & { code?: string }) : null;
    const code = cause?.code ?? '';
    const isAbort = err instanceof Error && err.name === 'AbortError';

    let reason: string;
    if (isAbort) {
      reason = 'timed out';
      verbose(`[${baseUrl}] probe timed out`);
    } else if (code === 'ECONNREFUSED') {
      reason = 'connection refused';
      verbose(`[${baseUrl}] connection refused`);
    } else if (code === 'ECONNRESET') {
      reason = 'connection reset';
      verbose(`[${baseUrl}] connection reset`);
    } else {
      reason = err instanceof Error ? err.message : String(err);
      verbose(`[${baseUrl}] probe failed: ${reason}`);
    }
    return { ok: false, reason };
  } finally {
    clearTimeout(timer);
  }
}
