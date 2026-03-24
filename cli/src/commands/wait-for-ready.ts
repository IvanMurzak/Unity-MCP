import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { generatePortFromDirectory } from '../utils/port.js';
import { probe, type ProbeResult } from '../utils/probe.js';

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

      // Race probes: resolve as soon as any succeeds, don't wait for slow ones
      const ready = await new Promise<ProbeResult | null>(resolve => {
        let pending = probeUrls.length;
        for (const url of probeUrls) {
          probe(url, headers, probeTimeout).then(result => {
            if (result.ok) resolve(result);
            else if (--pending === 0) resolve(null);
          });
        }
      });

      if (ready?.ok) {
        const totalSeconds = ((Date.now() - startTime) / 1000).toFixed(1);
        spinner.success(`MCP server is ready at ${ready.baseUrl} (connected in ${totalSeconds}s)`);
        process.exit(0);
      }

      const remainingMs = timeoutMs - (Date.now() - startTime);
      if (remainingMs <= 0) continue;
      await new Promise(resolve => setTimeout(resolve, Math.min(intervalMs, remainingMs)));
    }
  });
