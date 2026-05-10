import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { parseInput } from '../utils/input.js';
import type { RunToolFailure, RunToolOptions, RunToolResult } from '../lib/types.js';

interface BuilderOptions {
  /** CLI subcommand name, e.g. `'run-tool'`. */
  name: string;
  /** `description()` text for the commander command. */
  description: string;
  /** Description shown next to the `<tool-name>` argument. */
  argDescription: string;
  /** URL path prefix the lib function targets, e.g. `'/api/tools'`. */
  routePrefix: string;
  /** Verbose-log label, e.g. `'Tool'` or `'System tool'`. */
  verboseLabel: string;
  /** Heading printed by `ui.heading`, e.g. `'Run Tool'`. */
  headingLabel: string;
  /**
   * Lower-case noun used in user-facing failure copy:
   * `Failed to call <noun>:` and `<Capitalized> call timed out…`.
   */
  errorNoun: string;
  /** Library function — `runTool` or `runSystemTool`. */
  invoke: (opts: RunToolOptions) => Promise<RunToolResult>;
}

interface CliOptions {
  path?: string;
  url?: string;
  token?: string;
  input?: string;
  inputFile?: string;
  raw?: boolean;
  timeout?: string;
}

interface FailureContext {
  toolName: string;
  endpoint: string;
  raw: boolean | undefined;
  timeoutMs: number;
  errorNoun: string;
}

export function buildRunToolCommand(cfg: BuilderOptions): Command {
  return new Command(cfg.name)
    .description(cfg.description)
    .argument('<tool-name>', cfg.argDescription)
    .argument('[path]', 'Unity project path (used for config and auto port detection)')
    .option('--path <path>', 'Unity project path (config and auto port detection)')
    .option('--url <url>', 'Direct server URL override (bypasses config)')
    .option('--token <token>', 'Bearer token override (bypasses config)')
    .option('--input <json>', 'JSON string of tool arguments')
    .option('--input-file <file>', 'Read JSON arguments from file')
    .option('--raw', 'Output raw JSON (no formatting)')
    .option('--timeout <ms>', 'Request timeout in milliseconds (default: 60000)', '60000')
    .action(async (toolName: string, positionalPath: string | undefined, options: CliOptions) => {
      // Validate --timeout first so a bad value short-circuits before
      // we read --input-file or hit the network.
      const timeoutMs = parseInt(options.timeout!, 10);
      if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
        ui.error(`Invalid --timeout value: "${options.timeout}". Must be a positive integer (milliseconds).`);
        process.exit(1);
      }

      // Resolve path + connection up front so the heading and verbose
      // output reflect the final endpoint before the HTTP call fires.
      const projectPath = resolveAndValidateProjectPath(positionalPath, options);
      const { url: baseUrl, token } = resolveConnection(projectPath, options);
      const body = parseInput(options);
      const endpoint = `${baseUrl}${cfg.routePrefix}/${encodeURIComponent(toolName)}`;
      const authSource = options.token ? '--token flag' : 'config';

      verbose(`${cfg.verboseLabel}: ${toolName}`);
      verbose(`Endpoint: ${endpoint}`);
      verbose(`Body: ${body}`);
      if (token) verbose(`Authorization header set (source: ${authSource})`);

      if (!options.raw) {
        ui.heading(cfg.headingLabel);
        ui.label('Tool', toolName);
        ui.label('URL', endpoint);
        if (token) ui.label('Auth', `from ${authSource}`);
        ui.divider();
      }

      const spinner = options.raw ? null : ui.startSpinner(`Calling ${toolName}...`);

      // The CLI already resolved url/token, so passing them explicitly
      // makes the lib's resolver a no-op rather than re-reading config.
      const result = await cfg.invoke({
        toolName,
        url: baseUrl,
        ...(token ? { token } : {}),
        input: body,
        timeoutMs,
      });

      if (result.kind === 'success') {
        spinner?.success(`${toolName} completed`);
        if (options.raw) {
          process.stdout.write(stringifyForRaw(result.data));
        } else {
          ui.success('Response:');
          console.log(typeof result.data === 'string'
            ? result.data
            : JSON.stringify(result.data, null, 2));
        }
        return;
      }

      spinner?.stop();
      handleFailure(result, {
        toolName,
        endpoint,
        raw: options.raw,
        timeoutMs,
        errorNoun: cfg.errorNoun,
      });
    });
}

function stringifyForRaw(data: unknown): string {
  if (typeof data === 'string') return data;
  if (data === undefined) return '';
  return JSON.stringify(data);
}

function handleFailure(failure: RunToolFailure, ctx: FailureContext): never {
  if (failure.reason === 'http-error') {
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

  const message = buildFailureMessage(failure, ctx);
  if (ctx.raw) process.stderr.write(message + '\n');
  else ui.error(`Failed to call ${ctx.errorNoun}: ${message}`);
  process.exit(1);
}

function buildFailureMessage(failure: RunToolFailure, ctx: FailureContext): string {
  switch (failure.reason) {
    case 'timeout':
      return `${capitalize(ctx.errorNoun)} call timed out after ${ctx.timeoutMs / 1000} seconds: ${ctx.toolName}`;
    case 'connection-refused':
      return `Connection refused at ${ctx.endpoint}. Is the MCP server running? Start Unity Editor with the MCP plugin first.`;
    case 'connection-reset':
      return `Connection was reset by the server at ${ctx.endpoint}. The server may have crashed or restarted.`;
    case 'network-error':
      return `Cannot reach ${ctx.endpoint}. Check your network connection and server URL.`;
    default:
      return failure.message;
  }
}

function capitalize(s: string): string {
  return s.length === 0 ? s : s.charAt(0).toUpperCase() + s.slice(1);
}
