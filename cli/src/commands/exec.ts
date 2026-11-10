import * as fs from 'fs';
import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { localFallbackNote, SIGN_IN_REQUIRED_MESSAGE, resolveAndValidateProjectPath, resolveEditorConnection, resolveProbeUrls } from '../utils/connection.js';
import { DEFAULT_EXEC_TIMEOUT_MS, execCode, readToolResponse, SCRIPT_EXECUTE_TOOL } from '../lib/exec.js';
import { parsePositiveIntFlag } from './open.js';
import {
  buildFailureMessage, classifyEditorFailure, classifyToolFailure, handleFailure, PLUGIN_NOT_CONNECTED_HINT, stringifyForRaw,
} from './run-tool-builder.js';
import { failIfAwaitingInput } from '../utils/awaiting-input.js';
import { ExitCode } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';

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
  format?: string;
  once?: boolean;
  url?: string;
  token?: string;
}

/** `parsePositiveIntFlag` also accepts 0, which none of the exec timeouts can mean. */
function parseNonZeroIntFlag(raw: string | undefined, flagName: string, fallback: number): number {
  const value = parsePositiveIntFlag(raw, flagName, fallback);
  if (value === 0) {
    ui.error(`${flagName} must be greater than 0`);
    process.exit(ExitCode.Usage);
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
  .option('--idle <seconds>', 'Quit an editor started by exec after this many idle seconds (0: never)', '300')
  .option('--ready-timeout <ms>', 'How long to wait for the editor to become ready', '300000')
  .option('--timeout <ms>', 'Tool call timeout in milliseconds', String(DEFAULT_EXEC_TIMEOUT_MS))
  .option('--raw', 'Print the raw tool response JSON')
  .option('--format <format>', FORMAT_OPTION_DESCRIPTION, 'text')
  .option('--once', 'Close the editor after the call if this call started it (one-shot CI use)')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .action(async (codeArg: string | undefined, options: ExecCliOptions) => {
    const format = parseFormat(options.format);
    // Explicitly typed: `out.fail` returns `never`, which narrows only through a declared type.
    const out: CommandOutput = new CommandOutput('exec', format);
    const idleTimeoutSeconds = parsePositiveIntFlag(options.idle, '--idle', 300); // 0: never quit
    const readyTimeoutMs = parseNonZeroIntFlag(options.readyTimeout, '--ready-timeout', 300_000);
    const timeoutMs = parseNonZeroIntFlag(options.timeout, '--timeout', DEFAULT_EXEC_TIMEOUT_MS);
    let code: string;
    try {
      code = readCode(codeArg, options.file);
    } catch (err) {
      out.fail(ExitCode.Usage, { code: 'USAGE', message: err instanceof Error ? err.message : String(err) });
    }

    const projectPath = resolveAndValidateProjectPath(undefined, options);
    const conn = await resolveEditorConnection(projectPath, options);
    const { url, token, tokenFromCloudStore } = conn;
    if (conn.signInRequired) out.fail(ExitCode.CommandFailed, { code: 'NOT_AUTHENTICATED', message: SIGN_IN_REQUIRED_MESSAGE });
    if (conn.localFallback) out.warn(localFallbackNote(conn.url));

    const spinner = options.raw || out.machine ? null : ui.startSpinner('Connecting...');
    const result = await execCode({
      projectPath,
      code,
      usings: options.using,
      include: options.include,
      window: options.window === true,
      idleTimeoutSeconds,
      readyTimeoutMs,
      timeoutMs,
      once: options.once === true,
      probeUrls: resolveProbeUrls(projectPath, url, conn.overrideUrl),
      token,
      tokenFromCloudStore,
      overrideUrl: conn.overrideUrl,
      overrideToken: options.token,
      onStatus: message => {
        verbose(message);
        out.progress(message);
        if (spinner) spinner.text = message;
      },
    });
    const editor = {
      ...(result.kind === 'success' && result.editorSource ? { source: result.editorSource } : {}),
      ...(result.closedPid !== undefined ? { closedPid: result.closedPid } : {}),
    };

    if (result.kind === 'failure') {
      spinner?.stop();
      const classified = result.toolFailure
        ? classifyToolFailure(result.toolFailure)
        : classifyEditorFailure({ reason: result.reason === 'tool-failed' ? 'not-ready' : result.reason, sessionReason: result.sessionReason });
      await failIfAwaitingInput(out, projectPath, classified.exitCode, { editor }, options.raw);
      if (result.toolFailure) {
        if (out.machine) {
          out.fail(classified.exitCode, {
            code: classified.code,
            message: buildFailureMessage(result.toolFailure, { toolName: SCRIPT_EXECUTE_TOOL, endpoint: result.toolFailure.endpoint, raw: false, timeoutMs, errorNoun: 'tool' }),
          }, { editor });
        }
        handleFailure(result.toolFailure, {
          toolName: SCRIPT_EXECUTE_TOOL,
          endpoint: result.toolFailure.endpoint,
          raw: options.raw,
          timeoutMs,
          errorNoun: 'tool',
        });
      }
      if (!out.machine) ui.error(result.reason === 'launch-failed' ? 'Could not start the Unity Editor' : 'The MCP server did not answer');
      out.fail(classified.exitCode, { code: classified.code, message: result.message }, { editor },
        result.reason === 'not-ready' && result.editorSource !== 'foreign' ? PLUGIN_NOT_CONNECTED_HINT : undefined);
    }

    spinner?.stop();
    const response = readToolResponse(result.data);
    if (out.machine) {
      if (!response.ok) out.fail(ExitCode.CommandFailed, { code: 'TOOL_FAILED', message: response.text }, { result: result.data, editor });
      out.succeed({ output: response.text, result: result.data, editor });
      return;
    }
    if (options.raw) {
      process.stdout.write(stringifyForRaw(result.data));
      if (!response.ok) process.exit(ExitCode.CommandFailed);
      return;
    }
    if (!response.ok) {
      ui.error(response.text);
      process.exit(ExitCode.CommandFailed);
    }
    console.log(response.text);
  });
