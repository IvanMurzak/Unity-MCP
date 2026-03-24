import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { generatePortFromDirectory } from '../utils/port.js';

const PING_ENDPOINT = '/api/system-tools/ping';
const MAX_PROBE_TIMEOUT_MS = 10_000;

interface WaitForReadyOptions {
  path?: string;
  url?: string;
  token?: string;
  timeout?: string;
  interval?: string;
}

/**
 * Build the list of base URLs to probe.
 * When --url is explicit, only that URL is probed.
 * Otherwise, probe both the config-resolved URL and the deterministic local port
 * (if they differ), succeeding on whichever responds first.
 */
function resolveProbeUrls(
  projectPath: string,
  options: WaitForReadyOptions,
): string[] {
  const { url: configUrl } = resolveConnection(projectPath, options);
  const urls = [configUrl];

  if (!options.url) {
    const localPort = generatePortFromDirectory(projectPath);
    const localUrl = `http://localhost:${localPort}`;
    if (localUrl !== configUrl) {
      urls.push(localUrl);
      verbose(`Will probe both config URL (${configUrl}) and local URL (${localUrl})`);
    }
  }

  return urls;
}

/**
 * Probe a single endpoint. Returns the base URL on success, null on failure.
 */
async function probeEndpoint(
  baseUrl: string,
  headers: Record<string, string>,
  timeoutMs: number,
): Promise<string | null> {
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

    if (response.ok) {
      await response.text();
      return baseUrl;
    }

    verbose(`[${baseUrl}] HTTP ${response.status} — not ready yet`);
    return null;
  } catch (err) {
    const cause = err instanceof Error && 'cause' in err ? (err.cause as Error & { code?: string }) : null;
    const code = cause?.code ?? '';
    const isAbort = err instanceof Error && err.name === 'AbortError';

    if (isAbort) {
      verbose(`[${baseUrl}] probe timed out`);
    } else if (code === 'ECONNREFUSED') {
      verbose(`[${baseUrl}] connection refused`);
    } else if (code === 'ECONNRESET') {
      verbose(`[${baseUrl}] connection reset`);
    } else {
      verbose(`[${baseUrl}] probe failed: ${err instanceof Error ? err.message : String(err)}`);
    }
    return null;
  } finally {
    clearTimeout(timer);
  }
}

export const waitForReadyCommand = new Command('wait-for-ready')
  .description('Wait until Unity Editor and MCP server are ready to accept tool calls')
  .argument('[path]', 'Unity project path (used for config and auto port detection)')
  .option('--path <path>', 'Unity project path (config and auto port detection)')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .option('--timeout <ms>', 'Maximum time to wait in milliseconds (default: 120000)', '120000')
  .option('--interval <ms>', 'Polling interval in milliseconds (default: 3000)', '3000')
  .action(async (positionalPath: string | undefined, options: WaitForReadyOptions) => {
    const projectPath = resolveAndValidateProjectPath(positionalPath, options);
    const { token } = resolveConnection(projectPath, options);
    const probeUrls = resolveProbeUrls(projectPath, options);

    const timeoutMs = parseInt(options.timeout ?? '120000', 10);
    const intervalMs = parseInt(options.interval ?? '3000', 10);

    if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
      ui.error(`Invalid --timeout value: "${options.timeout}". Must be a positive integer (milliseconds).`);
      process.exit(1);
    }
    if (!Number.isFinite(intervalMs) || intervalMs <= 0) {
      ui.error(`Invalid --interval value: "${options.interval}". Must be a positive integer (milliseconds).`);
      process.exit(1);
    }

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    for (const url of probeUrls) {
      verbose(`Probe target: ${url}`);
    }
    verbose(`Timeout: ${timeoutMs}ms, Interval: ${intervalMs}ms`);

    const spinner = ui.startSpinner('Waiting for Unity Editor and MCP server...');
    const startTime = Date.now();

    while (true) {
      const elapsed = Date.now() - startTime;
      if (elapsed >= timeoutMs) {
        spinner.error(`Timed out after ${(timeoutMs / 1000).toFixed(1)}s waiting for MCP server`);
        process.exit(1);
      }

      const remaining = Math.ceil((timeoutMs - elapsed) / 1000);
      spinner.text = `Waiting for Unity Editor and MCP server... (${remaining}s remaining)`;

      const probeTimeout = Math.min(intervalMs, MAX_PROBE_TIMEOUT_MS);
      const results = await Promise.all(
        probeUrls.map(url => probeEndpoint(url, headers, probeTimeout))
      );

      const readyUrl = results.find(r => r !== null);
      if (readyUrl) {
        const totalSeconds = ((Date.now() - startTime) / 1000).toFixed(1);
        spinner.success(`MCP server is ready at ${readyUrl} (connected in ${totalSeconds}s)`);
        process.exit(0);
      }

      const remainingMs = timeoutMs - (Date.now() - startTime);
      if (remainingMs <= 0) continue;
      await new Promise(resolve => setTimeout(resolve, Math.min(intervalMs, remainingMs)));
    }
  });
