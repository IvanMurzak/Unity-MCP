import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection, resolveProbeUrls } from '../utils/connection.js';
import { waitForReady } from '../lib/wait-for-ready.js';
import { ExitCode } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';
import { failIfAwaitingInput } from '../utils/awaiting-input.js';

interface WaitForReadyOptions {
  path?: string;
  url?: string;
  token?: string;
  timeout?: string;
  interval?: string;
  format?: string;
}

export const waitForReadyCommand = new Command('wait-for-ready')
  .description('Wait until Unity Editor and MCP server are ready to accept tool calls')
  .argument('[path]', 'Unity project path (used for config and auto port detection)')
  .option('--path <path>', 'Unity project path (config and auto port detection)')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .option('--timeout <ms>', 'Maximum time to wait in milliseconds (default: 120000)', '120000')
  .option('--interval <ms>', 'Polling interval in milliseconds (default: 3000)', '3000')
  .option('--format <format>', FORMAT_OPTION_DESCRIPTION, 'text')
  .addHelpText('after', '\nExit codes: 0 ready, 14 timed out, 18 a modal dialog blocks the editor (Windows).')
  .action(async (positionalPath: string | undefined, options: WaitForReadyOptions) => {
    // Explicitly typed: `out.fail` returns `never`, which narrows only through a declared type.
    const out: CommandOutput = new CommandOutput('wait-for-ready', parseFormat(options.format));
    const projectPath = resolveAndValidateProjectPath(positionalPath, options);
    const { url, token } = await resolveConnection(projectPath, options);
    const probeUrls = resolveProbeUrls(projectPath, url, options.url);

    const timeoutMs = parseInt(options.timeout ?? '120000', 10);
    const intervalMs = parseInt(options.interval ?? '3000', 10);

    if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
      out.fail(ExitCode.Usage, { code: 'USAGE', message: `Invalid --timeout value: "${options.timeout}". Must be a positive integer (milliseconds).` });
    }
    if (!Number.isFinite(intervalMs) || intervalMs <= 0) {
      out.fail(ExitCode.Usage, { code: 'USAGE', message: `Invalid --interval value: "${options.interval}". Must be a positive integer (milliseconds).` });
    }

    for (const url of probeUrls) {
      verbose(`Probe target: ${url}`);
    }
    verbose(`Timeout: ${timeoutMs}ms, Interval: ${intervalMs}ms`);

    const spinner = out.machine ? null : ui.startSpinner('Waiting for Unity Editor and MCP server...');
    const result = await waitForReady({
      urls: probeUrls,
      token,
      timeoutMs,
      intervalMs,
      onTick: remainingMs => {
        out.progress(`Waiting for Unity Editor and MCP server (${Math.ceil(remainingMs / 1000)}s remaining)`);
        if (spinner) spinner.text = `Waiting for Unity Editor and MCP server... (${Math.ceil(remainingMs / 1000)}s remaining)`;
      },
    });

    if (result.kind === 'success') {
      spinner?.success(`MCP server is ready at ${result.baseUrl} (connected in ${(result.elapsedMs / 1000).toFixed(1)}s)`);
      out.succeed({ baseUrl: result.baseUrl, elapsedMs: result.elapsedMs });
      process.exit(ExitCode.Success);
    }
    spinner?.stop();
    await failIfAwaitingInput(out, projectPath, ExitCode.Timeout, { elapsedMs: result.elapsedMs });
    out.fail(ExitCode.Timeout, { code: 'TIMEOUT', message: `Timed out after ${(timeoutMs / 1000).toFixed(1)}s waiting for MCP server` }, { elapsedMs: result.elapsedMs });
  });
