import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection, resolveProbeUrls } from '../utils/connection.js';
import { waitForReady } from '../lib/wait-for-ready.js';

interface WaitForReadyOptions {
  path?: string;
  url?: string;
  token?: string;
  timeout?: string;
  interval?: string;
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
    const { url, token } = await resolveConnection(projectPath, options);
    const probeUrls = resolveProbeUrls(projectPath, url, options.url);

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

    for (const url of probeUrls) {
      verbose(`Probe target: ${url}`);
    }
    verbose(`Timeout: ${timeoutMs}ms, Interval: ${intervalMs}ms`);

    const spinner = ui.startSpinner('Waiting for Unity Editor and MCP server...');
    const result = await waitForReady({
      urls: probeUrls,
      token,
      timeoutMs,
      intervalMs,
      onTick: remainingMs => {
        spinner.text = `Waiting for Unity Editor and MCP server... (${Math.ceil(remainingMs / 1000)}s remaining)`;
      },
    });

    if (result.kind === 'success') {
      spinner.success(`MCP server is ready at ${result.baseUrl} (connected in ${(result.elapsedMs / 1000).toFixed(1)}s)`);
      process.exit(0);
    }
    spinner.error(`Timed out after ${(timeoutMs / 1000).toFixed(1)}s waiting for MCP server`);
    process.exit(1);
  });
