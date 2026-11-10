import { Command } from 'commander';
import { DEFAULT_SESSION_READY_TIMEOUT_MS } from '@baizor/gamedev-cli-core';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { localFallbackNote, resolveAndValidateProjectPath, resolveEditorConnection, resolveProbeUrls } from '../utils/connection.js';
import { parseInput } from '../utils/input.js';
import { getProjectEditorVersion } from '../utils/unity-editor.js';
import { failIfAwaitingInput } from '../utils/awaiting-input.js';
import { ExitCode, type ExitCodeValue } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';
import { ensureEditor, type EnsureEditorFailure } from '../lib/ensure-editor.js';
import { isLoopbackUrl } from '../lib/editor-session.js';
import { readToolResponse } from '../lib/exec.js';
import type { RunToolFailure, RunToolOptions, RunToolResult } from '../lib/types.js';
import { parsePositiveIntFlag } from './open.js';

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
  arg?: string[];
  raw?: boolean;
  timeout?: string;
  format?: string;
  start?: boolean;
  once?: boolean;
  readyTimeout?: string;
}

interface FailureContext {
  toolName: string;
  endpoint: string;
  raw: boolean | undefined;
  timeoutMs: number;
  errorNoun: string;
}

/** Exit code and stable error token of a failed tool call. */
export function classifyToolFailure(failure: RunToolFailure): { exitCode: ExitCodeValue; code: string } {
  switch (failure.reason) {
    case 'timeout':
      return { exitCode: ExitCode.Timeout, code: 'TIMEOUT' };
    case 'connection-refused':
    case 'connection-reset':
    case 'network-error':
      return { exitCode: ExitCode.Unreachable, code: 'UNREACHABLE' };
    case 'invalid-input':
      return { exitCode: ExitCode.Usage, code: 'USAGE' };
    case 'not-authenticated':
      return { exitCode: ExitCode.CommandFailed, code: 'NOT_AUTHENTICATED' };
    case 'http-error':
      if (failure.httpStatus === 503) return { exitCode: ExitCode.Busy, code: 'BUSY' };
      // The server's own tool timeout, and a plugin connection lost after the call was sent.
      if (failure.httpStatus === 504) return { exitCode: ExitCode.Timeout, code: 'TIMEOUT' };
      if (failure.httpStatus === 502) return { exitCode: ExitCode.Unreachable, code: 'UNREACHABLE' };
      if (failure.httpStatus === 404) return { exitCode: ExitCode.Usage, code: 'NOT_FOUND' };
      if (failure.httpStatus === 400) return { exitCode: ExitCode.Usage, code: 'USAGE' };
      // 422: the tool ran and failed, unless the server rejected the arguments before running it.
      if (failure.httpStatus === 422) {
        return serverError(failure.data)?.startsWith('[invalid_params]')
          ? { exitCode: ExitCode.Usage, code: 'USAGE' }
          : { exitCode: ExitCode.CommandFailed, code: 'TOOL_FAILED' };
      }
      if (failure.httpStatus === 401 || failure.httpStatus === 403) return { exitCode: ExitCode.CommandFailed, code: 'NOT_AUTHENTICATED' };
      return { exitCode: ExitCode.CommandFailed, code: 'TOOL_FAILED' };
    default:
      return { exitCode: ExitCode.CommandFailed, code: 'TOOL_FAILED' };
  }
}

/** The server's own error text of a failed call (`{ "error": "..." }`), when it sent one. */
export function serverError(data: unknown): string | undefined {
  if (typeof data === 'string') return data.trim() || undefined;
  const error = (data as { error?: unknown } | null | undefined)?.error;
  return typeof error === 'string' && error.trim() !== '' ? error : undefined;
}

/** Exit code and error token of an editor that could not be started or reached. */
export function classifyEditorFailure(failure: Pick<EnsureEditorFailure, 'reason' | 'sessionReason'>): { exitCode: ExitCodeValue; code: string } {
  // Another CLI call holds the project's start lock for the whole wait: retry later.
  return failure.sessionReason === 'lock-busy'
    ? { exitCode: ExitCode.Busy, code: 'BUSY' }
    : { exitCode: ExitCode.Unreachable, code: 'UNREACHABLE' };
}

/** Hint for an editor the CLI did not start that never answered. */
export const PLUGIN_NOT_CONNECTED_HINT = 'If the project is open in a Unity Editor, check that its AI Game Developer plugin is connected.';

/**
 * Whether a missing editor may be started for `projectPath`: only for a project that names its Unity version, so the
 * CLI never guesses an editor for a folder that merely looks like a project.
 */
export function canStartEditor(projectPath: string, explicitUrl?: string): boolean {
  // An explicit remote server is called as given: the editor that serves it may run on another machine.
  if (explicitUrl !== undefined && !isLoopbackUrl(explicitUrl)) return false;
  return getProjectEditorVersion(projectPath) !== null;
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
    .option('--arg <key=value>', 'One tool argument, repeatable, merged over --input; JSON values are parsed (count=3, flag=true), anything else is text (query=objects | select name)', (v: string, prev: string[] = []) => [...prev, v])
    .option('--raw', 'Output raw JSON (no formatting)')
    .option('--format <format>', FORMAT_OPTION_DESCRIPTION, 'text')
    .option('--timeout <ms>', 'Request timeout in milliseconds (default: 60000)', '60000')
    .option('--no-start', 'Never start an editor: fail when none answers (default: start a headless one for a project with ProjectSettings/ProjectVersion.txt, unless --url names a remote server)')
    .option('--once', 'Close the editor after the call if this call started it (one-shot CI use)')
    .option('--ready-timeout <ms>', 'How long to wait for a started editor to become ready', String(DEFAULT_SESSION_READY_TIMEOUT_MS))
    .action(async (toolName: string, positionalPath: string | undefined, options: CliOptions) => {
      const format = parseFormat(options.format);
      // Explicitly typed: `out.fail` returns `never`, which narrows only through a declared type.
      const out: CommandOutput = new CommandOutput(cfg.name, format);
      // Validate --timeout first so a bad value short-circuits before
      // we read --input-file or hit the network.
      const timeoutMs = parseInt(options.timeout!, 10);
      if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
        out.fail(ExitCode.Usage, { code: 'USAGE', message: `Invalid --timeout value: "${options.timeout}". Must be a positive integer (milliseconds).` });
      }
      const readyTimeoutMs = parsePositiveIntFlag(options.readyTimeout, '--ready-timeout', DEFAULT_SESSION_READY_TIMEOUT_MS);
      const human = !options.raw && !out.machine;

      // Resolve path + connection up front so the heading and verbose
      // output reflect the final endpoint before the HTTP call fires.
      const projectPath = resolveAndValidateProjectPath(positionalPath, options);
      const conn = await resolveEditorConnection(projectPath, options);
      const { url: resolvedUrl, token, tokenFromCloudStore } = conn;
      if (conn.signInRequired) out.fail(ExitCode.CommandFailed, { code: 'NOT_AUTHENTICATED', message: conn.signInRequired });
      if (conn.localFallback) out.warn(localFallbackNote(conn.url));
      const body = parseInput(options);
      const authSource = options.token ? '--token flag' : 'config or machine store';

      verbose(`${cfg.verboseLabel}: ${toolName}`);
      verbose(`Body: ${body}`);
      if (token) verbose(`Authorization header set (source: ${authSource})`);

      const spinner = human ? ui.startSpinner('Connecting...') : null;
      let baseUrl = resolvedUrl;
      let editor: { source?: string; closeIfStarted: () => Promise<number | undefined> } = { closeIfStarted: async () => undefined };
      if (options.start !== false && canStartEditor(projectPath, options.url)) {
        const ensured = await ensureEditor({
          projectPath,
          probeUrls: resolveProbeUrls(projectPath, resolvedUrl, conn.overrideUrl),
          token,
          overrideUrl: conn.overrideUrl,
          overrideToken: options.token,
          readyTimeoutMs,
          closeOnFailure: options.once === true,
          onStatus: message => {
            verbose(message);
            out.progress(message);
            if (spinner) spinner.text = message;
          },
        });
        if (ensured.kind === 'failure') {
          spinner?.error(ensured.reason === 'launch-failed' ? 'Could not start the Unity Editor' : 'The MCP server did not answer');
          const { exitCode, code } = classifyEditorFailure(ensured);
          await failIfAwaitingInput(out, projectPath, exitCode);
          out.fail(exitCode, { code, message: ensured.message }, null,
            ensured.reason === 'not-ready' && ensured.editorSource !== 'foreign' ? PLUGIN_NOT_CONNECTED_HINT : undefined);
        }
        baseUrl = ensured.baseUrl;
        editor = ensured;
      }
      const endpoint = `${baseUrl}${cfg.routePrefix}/${encodeURIComponent(toolName)}`;
      verbose(`Endpoint: ${endpoint}`);

      spinner?.stop();
      verbose(`${cfg.headingLabel}: ${toolName} at ${endpoint}${token ? ` (auth from ${authSource})` : ''}`);
      out.progress(`Calling ${toolName}...`);
      const callSpinner = human ? ui.startSpinner(`Calling ${toolName}...`) : null;

      // The CLI already resolved url/token, so passing them explicitly makes the lib's resolver a no-op; the lib
      // still refreshes a machine-store Bearer once on 401.
      const result = await cfg.invoke({
        toolName, url: baseUrl, ...(token ? { token } : {}), input: body, timeoutMs, tokenFromCloudStore,
      });

      const closedPid = options.once ? await editor.closeIfStarted() : undefined;
      if (closedPid !== undefined) verbose(`Closed the editor this call started (PID ${closedPid})`);
      const editorData = { ...(editor.source ? { source: editor.source } : {}), ...(closedPid !== undefined ? { closedPid } : {}) };

      if (result.kind === 'success') {
        const response = readToolResponse(result.data);
        if (out.machine) {
          if (!response.ok) {
            out.fail(ExitCode.CommandFailed, { code: 'TOOL_FAILED', message: response.text }, { tool: toolName, result: result.data, editor: editorData });
          }
          out.succeed({ tool: toolName, result: result.data, editor: editorData });
          return;
        }
        if (options.raw) {
          process.stdout.write(stringifyForRaw(result.data));
        } else {
          // The tool's own answer as it wrote it (the JSON envelope is `--raw` / `--format json`).
          callSpinner?.stop();
          if (response.ok) console.log(response.text);
          else ui.error(`${toolName} reported an error: ${response.text}`);
        }
        if (!response.ok) process.exit(ExitCode.CommandFailed);
        return;
      }

      callSpinner?.stop();
      const classified = classifyToolFailure(result);
      await failIfAwaitingInput(out, projectPath, classified.exitCode, { tool: toolName, editor: editorData }, options.raw);
      if (out.machine) {
        out.fail(classified.exitCode, { code: classified.code, message: buildFailureMessage(result, { toolName, endpoint, raw: false, timeoutMs, errorNoun: cfg.errorNoun }) },
          { tool: toolName, ...(result.data !== undefined ? { result: result.data } : {}), ...(result.httpStatus ? { httpStatus: result.httpStatus } : {}), editor: editorData });
      }
      handleFailure(result, {
        toolName,
        endpoint,
        raw: options.raw,
        timeoutMs,
        errorNoun: cfg.errorNoun,
      });
    });
}

export function stringifyForRaw(data: unknown): string {
  if (typeof data === 'string') return data;
  if (data === undefined) return '';
  return JSON.stringify(data);
}

/** Print a failed call (text or `--raw`) and exit with its classified code. */
export function handleFailure(failure: RunToolFailure, ctx: FailureContext): never {
  const { exitCode } = classifyToolFailure(failure);
  if (failure.reason === 'http-error') {
    if (ctx.raw) {
      process.stdout.write(stringifyForRaw(failure.data));
    } else {
      const text = serverError(failure.data);
      ui.error(text ?? `HTTP ${failure.httpStatus}: ${failure.message}`);
      if (text === undefined && failure.data !== undefined) {
        console.error(typeof failure.data === 'string' ? failure.data : JSON.stringify(failure.data, null, 2));
      }
      const hint = failureHint(failure, ctx.toolName);
      if (hint) console.error(hint);
    }
    process.exit(exitCode);
  }

  const message = buildFailureMessage(failure, ctx);
  if (ctx.raw) process.stderr.write(message + '\n');
  else ui.error(`Failed to call ${ctx.errorNoun}: ${message}`);
  process.exit(exitCode);
}

/** What to do next after a failed call, when there is a clear next step. */
export function failureHint(failure: RunToolFailure, toolName: string): string | undefined {
  if (failure.reason === 'http-error' && failure.httpStatus === 404) {
    return `No tool named "${toolName}". List the available tools with: unity-mcp-cli tools`;
  }
  if (failure.reason === 'http-error' && serverError(failure.data)?.startsWith('[invalid_params]')) {
    return `See the inputs of ${toolName} with: unity-mcp-cli tools ${toolName}`;
  }
  return undefined;
}

export function buildFailureMessage(failure: RunToolFailure, ctx: FailureContext): string {
  switch (failure.reason) {
    case 'timeout':
      return `${capitalize(ctx.errorNoun)} call timed out after ${ctx.timeoutMs / 1000} seconds: ${ctx.toolName}`;
    case 'connection-refused':
      return `Connection refused at ${ctx.endpoint}. Is the MCP server running? Start Unity Editor with the MCP plugin first.`;
    case 'connection-reset':
      return `Connection was reset by the server at ${ctx.endpoint}. The server may have crashed or restarted.`;
    case 'network-error':
      return `Cannot reach ${ctx.endpoint}. Check your network connection and server URL.`;
    case 'http-error': {
      const text = serverError(failure.data) ?? `HTTP ${failure.httpStatus}: ${failure.message}`;
      const hint = failureHint(failure, ctx.toolName);
      return hint ? `${text} ${hint}` : text;
    }
    default:
      return failure.message;
  }
}

function capitalize(s: string): string {
  return s.length === 0 ? s : s.charAt(0).toUpperCase() + s.slice(1);
}
