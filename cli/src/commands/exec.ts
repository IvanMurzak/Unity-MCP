import * as fs from 'fs';
import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection, resolveProbeUrls } from '../utils/connection.js';
import { execCode, readToolResponse, SCRIPT_EXECUTE_TOOL } from '../lib/exec.js';
import { parsePositiveIntFlag } from './open.js';
import { handleFailure, stringifyForRaw } from './run-tool-builder.js';

interface ExecCliOptions {
  path?: string;
  file?: string;
  using: string[];
  include: string[];
  window?: boolean;
  idle?: string;
  readyTimeout?: string;
  timeout?: string;
  raw?: boolean;
  url?: string;
  token?: string;
}

/** `parsePositiveIntFlag` also accepts 0, which none of the exec timeouts can mean. */
function parseNonZeroIntFlag(raw: string | undefined, flagName: string, fallback: number): number {
  const value = parsePositiveIntFlag(raw, flagName, fallback);
  if (value === 0) {
    ui.error(`${flagName} must be greater than 0`);
    process.exit(1);
  }
  return value;
}

function collect(value: string, previous: string[]): string[] {
  return [...previous, value];
}

function readCode(code: string | undefined, file: string | undefined): string {
  if (code !== undefined && file !== undefined) throw new Error('Pass the code either as an argument or with --file, not both.');
  if (code !== undefined) return code;
  const fromStdin = file === '-' || (file === undefined && !process.stdin.isTTY);
  if (!fromStdin && file === undefined) throw new Error('No code given: pass it as an argument, with --file <file>, or on stdin.');
  const text = fs.readFileSync(fromStdin ? 0 : file!, 'utf-8');
  if (text.trim() === '') throw new Error(fromStdin ? 'No code received from stdin.' : `The file is empty: ${file}`);
  return text;
}

export const execCommand = new Command('exec')
  .description('Run C# in the Unity Editor; starts a headless editor when none is open and keeps it warm')
  .argument('[code]', 'C# snippet: statements, the last expression is the result')
  .option('--path <path>', 'Unity project path (default: current directory)')
  .option('-f, --file <file>', 'Read the code from a file ("-" for stdin)')
  .option('--using <namespace>', 'Extra namespace (repeatable)', collect, [])
  .option('--include <path>', 'Helper .cs file or folder, relative to the project snippets folder (repeatable)', collect, [])
  .option('--window', 'Start the editor with a window instead of headless (only when it is not running yet)')
  .option('--idle <seconds>', 'Quit an editor started by exec after this many idle seconds', '300')
  .option('--ready-timeout <ms>', 'How long to wait for the editor to become ready', '300000')
  .option('--timeout <ms>', 'Tool call timeout in milliseconds', '180000')
  .option('--raw', 'Print the raw tool response JSON')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .action(async (codeArg: string | undefined, options: ExecCliOptions) => {
    const idleTimeoutSeconds = parseNonZeroIntFlag(options.idle, '--idle', 300);
    const readyTimeoutMs = parseNonZeroIntFlag(options.readyTimeout, '--ready-timeout', 300_000);
    const timeoutMs = parseNonZeroIntFlag(options.timeout, '--timeout', 180_000);
    let code: string;
    try {
      code = readCode(codeArg, options.file);
    } catch (err) {
      ui.error(err instanceof Error ? err.message : String(err));
      process.exit(1);
    }

    const projectPath = resolveAndValidateProjectPath(undefined, options);
    const { url, token, cloudAuthMissing, tokenFromCloudStore } = await resolveConnection(projectPath, options);
    if (cloudAuthMissing) {
      ui.error('Not logged in to ai-game.dev. Run `unity-mcp-cli login` to sign in, then retry — or pass --token / --url.');
      process.exit(1);
    }

    const spinner = options.raw ? null : ui.startSpinner('Connecting...');
    const result = await execCode({
      projectPath,
      code,
      usings: options.using,
      include: options.include,
      window: options.window === true,
      idleTimeoutSeconds,
      readyTimeoutMs,
      timeoutMs,
      probeUrls: resolveProbeUrls(projectPath, url, options.url),
      token,
      tokenFromCloudStore,
      overrideUrl: options.url,
      overrideToken: options.token,
      onStatus: message => {
        verbose(message);
        if (spinner) spinner.text = message;
      },
    });

    if (result.kind === 'failure') {
      if (result.toolFailure) {
        spinner?.stop();
        handleFailure(result.toolFailure, {
          toolName: SCRIPT_EXECUTE_TOOL,
          endpoint: result.toolFailure.endpoint,
          raw: options.raw,
          timeoutMs,
          errorNoun: 'tool',
        });
      }
      spinner?.error(result.reason === 'launch-failed' ? 'Could not start the Unity Editor' : 'The MCP server did not answer');
      ui.error(result.message);
      process.exit(1);
    }

    spinner?.stop();
    if (options.raw) {
      process.stdout.write(stringifyForRaw(result.data));
      return;
    }
    const response = readToolResponse(result.data);
    if (!response.ok) {
      ui.error(response.text);
      process.exit(1);
    }
    console.log(response.text);
  });
