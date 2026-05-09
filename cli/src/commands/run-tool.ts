import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { parseInput } from '../utils/input.js';
import { runTool } from '../lib/run-tool.js';
import type { RunToolFailure } from '../lib/types.js';

interface RunToolOptions {
  path?: string;
  url?: string;
  token?: string;
  input?: string;
  inputFile?: string;
  raw?: boolean;
  timeout?: string;
}

export const runToolCommand = new Command('run-tool')
  .description('Execute an MCP tool via the HTTP API')
  .argument('<tool-name>', 'Name of the MCP tool to execute')
  .argument('[path]', 'Unity project path (used for config and auto port detection)')
  .option('--path <path>', 'Unity project path (config and auto port detection)')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .option('--input <json>', 'JSON string of tool arguments')
  .option('--input-file <file>', 'Read JSON arguments from file')
  .option('--raw', 'Output raw JSON (no formatting)')
  .option('--timeout <ms>', 'Request timeout in milliseconds (default: 60000)', '60000')
  .action(async (toolName: string, positionalPath: string | undefined, options: RunToolOptions) => {
    // Resolve path + connection up front so the CLI emits its usual
    // headings before delegating to the library. The library would do
    // its own resolution, but we want the resolved endpoint visible in
    // the heading and `verbose()` output before the HTTP call fires.
    const projectPath = resolveAndValidateProjectPath(positionalPath, options);
    const { url: baseUrl, token } = resolveConnection(projectPath, options);
    const body = parseInput(options);
    const endpoint = `${baseUrl}/api/tools/${encodeURIComponent(toolName)}`;

    verbose(`Tool: ${toolName}`);
    verbose(`Endpoint: ${endpoint}`);
    verbose(`Body: ${body}`);
    if (token) {
      verbose(`Authorization header set (source: ${options.token ? '--token flag' : 'config'})`);
    }

    const authSource = options.token ? '--token flag' : 'config';

    if (!options.raw) {
      ui.heading('Run Tool');
      ui.label('Tool', toolName);
      ui.label('URL', endpoint);
      if (token) {
        ui.label('Auth', `from ${authSource}`);
      }
      ui.divider();
    }

    const spinner = options.raw ? null : ui.startSpinner(`Calling ${toolName}...`);

    const timeoutMs = parseInt(options.timeout ?? '60000', 10);
    if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
      ui.error(`Invalid --timeout value: "${options.timeout}". Must be a positive integer (milliseconds).`);
      process.exit(1);
    }

    // Library call. We pass `url` + `token` explicitly so the lib's
    // resolution path is a no-op (the CLI just resolved them above).
    const result = await runTool({
      toolName,
      url: baseUrl,
      ...(token ? { token } : {}),
      input: body,
      timeoutMs,
    });

    if (result.kind === 'success') {
      spinner?.success(`${toolName} completed`);
      const responseText = stringifyForRaw(result.data);
      if (options.raw) {
        process.stdout.write(responseText);
      } else {
        ui.success('Response:');
        console.log(typeof result.data === 'string'
          ? result.data
          : JSON.stringify(result.data, null, 2));
      }
      return;
    }

    spinner?.stop();
    handleFailure(result, { toolName, endpoint, raw: options.raw, timeoutMs });
  });

function stringifyForRaw(data: unknown): string {
  if (typeof data === 'string') return data;
  if (data === undefined) return '';
  return JSON.stringify(data);
}

interface FailureContext {
  toolName: string;
  endpoint: string;
  raw: boolean | undefined;
  timeoutMs: number;
}

function handleFailure(failure: RunToolFailure, ctx: FailureContext): never {
  switch (failure.reason) {
    case 'http-error': {
      if (ctx.raw) {
        process.stdout.write(stringifyForRaw(failure.data));
      } else {
        ui.error(`HTTP ${failure.httpStatus}: ${failure.message}`);
        if (failure.data !== undefined) {
          ui.info(typeof failure.data === 'string'
            ? failure.data
            : JSON.stringify(failure.data, null, 2));
        }
      }
      process.exit(1);
    }
    case 'timeout': {
      const message = `Tool call timed out after ${ctx.timeoutMs / 1000} seconds: ${ctx.toolName}`;
      if (ctx.raw) process.stderr.write(message + '\n');
      else ui.error(`Failed to call tool: ${message}`);
      process.exit(1);
    }
    case 'connection-refused': {
      const message = `Connection refused at ${ctx.endpoint}. Is the MCP server running? Start Unity Editor with the MCP plugin first.`;
      if (ctx.raw) process.stderr.write(message + '\n');
      else ui.error(`Failed to call tool: ${message}`);
      process.exit(1);
    }
    case 'connection-reset': {
      const message = `Connection was reset by the server at ${ctx.endpoint}. The server may have crashed or restarted.`;
      if (ctx.raw) process.stderr.write(message + '\n');
      else ui.error(`Failed to call tool: ${message}`);
      process.exit(1);
    }
    case 'network-error': {
      const message = `Cannot reach ${ctx.endpoint}. Check your network connection and server URL.`;
      if (ctx.raw) process.stderr.write(message + '\n');
      else ui.error(`Failed to call tool: ${message}`);
      process.exit(1);
    }
    case 'invalid-input':
    case 'unknown':
    default: {
      const message = failure.message;
      if (ctx.raw) process.stderr.write(message + '\n');
      else ui.error(`Failed to call tool: ${message}`);
      process.exit(1);
    }
  }
}
