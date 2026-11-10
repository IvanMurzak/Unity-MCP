import { Command } from 'commander';
import { DEFAULT_SESSION_READY_TIMEOUT_MS } from '@baizor/gamedev-cli-core';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection, resolveProbeUrls } from '../utils/connection.js';
import { parseInput } from '../utils/input.js';
import { getProjectEditorVersion } from '../utils/unity-editor.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { listEditorDialogs, type EditorDialog } from '../utils/editor-dialogs.js';
import { ExitCode, type ExitCodeValue } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat, type EnvelopeError } from '../utils/output.js';
import { ensureEditor, type EnsureEditorFailure } from '../lib/ensure-editor.js';
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
      if (failure.httpStatus === 404) return { exitCode: ExitCode.Usage, code: 'NOT_FOUND' };
      if (failure.httpStatus === 400 || failure.httpStatus === 422) return { exitCode: ExitCode.Usage, code: 'USAGE' };
      if (failure.httpStatus === 401 || failure.httpStatus === 403) return { exitCode: ExitCode.CommandFailed, code: 'NOT_AUTHENTICATED' };
      return { exitCode: ExitCode.CommandFailed, code: 'TOOL_FAILED' };
    default:
      return { exitCode: ExitCode.CommandFailed, code: 'TOOL_FAILED' };
  }
}

/** Exit code and error token of an editor that could not be started or reached. */
export function classifyEditorFailure(failure: Pick<EnsureEditorFailure, 'reason' | 'sessionReason'>): { exitCode: ExitCodeValue; code: string } {
  // Another CLI call holds the project's start lock for the whole wait: retry later.
  return failure.sessionReason === 'lock-busy'
    ? { exitCode: ExitCode.Busy, code: 'BUSY' }
    : { exitCode: ExitCode.Unreachable, code: 'UNREACHABLE' };
}

/**
 * The modal dialogs of the project's editor, when it is running and shows any (Windows). A call that timed out or was
 * refused as busy is then waiting for a human, which `AWAITING_INPUT` (exit 18) says instead of a retryable code.
 */
export async function findBlockingDialogs(projectPath: string): Promise<EditorDialog[]> {
  const proc = findUnityProcess(projectPath);
  if (!proc) return [];
  const listed = await listEditorDialogs([proc.pid]);
  return listed.kind === 'ok' ? listed.dialogs : [];
}

export function describeDialogs(dialogs: EditorDialog[]): string {
  return dialogs.map(d => `"${d.title}"${d.message ? `: ${d.message.replace(/\s+/g, ' ')}` : ''} [${d.buttons.join(' | ')}]`).join('; ');
}

/** The awaiting-input error for `dialogs`, with the hint to answer them. */
export function awaitingInputError(dialogs: EditorDialog[]): EnvelopeError {
  return {
    code: 'AWAITING_INPUT',
    message: `The Unity Editor is blocked by a modal dialog: ${describeDialogs(dialogs)}. ` +
      'Answer it in the editor, or with `unity-mcp-cli dialog --press <button>`.',
  };
}

/**
 * Whether a missing editor may be started for `projectPath`: only for a project that names its Unity version, so the
 * CLI never guesses an editor for a folder that merely looks like a project.
 */
export function canStartEditor(projectPath: string): boolean {
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
    .option('--raw', 'Output raw JSON (no formatting)')
    .option('--format <format>', FORMAT_OPTION_DESCRIPTION, 'text')
    .option('--timeout <ms>', 'Request timeout in milliseconds (default: 60000)', '60000')
    .option('--no-start', 'Never start an editor: fail when none answers (default: start a headless one for a project with ProjectSettings/ProjectVersion.txt)')
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
      const { url: resolvedUrl, token, cloudAuthMissing, tokenFromCloudStore } =
        await resolveConnection(projectPath, options);
      if (cloudAuthMissing) {
        out.fail(ExitCode.CommandFailed, {
          code: 'NOT_AUTHENTICATED',
          message: 'Not logged in to ai-game.dev. Run `unity-mcp-cli login` to sign in, then retry — or pass --token / --url to target a specific server.',
        });
      }
      const body = parseInput(options);
      const authSource = options.token ? '--token flag' : 'config or machine store';

      verbose(`${cfg.verboseLabel}: ${toolName}`);
      verbose(`Body: ${body}`);
      if (token) verbose(`Authorization header set (source: ${authSource})`);

      const spinner = human ? ui.startSpinner('Connecting...') : null;
      let baseUrl = resolvedUrl;
      let editor: { source?: string; closeIfStarted: () => Promise<number | undefined> } = { closeIfStarted: async () => undefined };
      if (options.start !== false && canStartEditor(projectPath)) {
        const ensured = await ensureEditor({
          projectPath,
          probeUrls: resolveProbeUrls(projectPath, resolvedUrl, options.url),
          token,
          overrideUrl: options.url,
          overrideToken: options.token,
          readyTimeoutMs,
          onStatus: message => {
            verbose(message);
            out.progress(message);
            if (spinner) spinner.text = message;
          },
        });
        if (ensured.kind === 'failure') {
          spinner?.error(ensured.reason === 'launch-failed' ? 'Could not start the Unity Editor' : 'The MCP server did not answer');
          const dialogs = await findBlockingDialogs(projectPath);
          if (dialogs.length > 0) out.fail(ExitCode.AwaitingInput, awaitingInputError(dialogs), { dialogs });
          const { exitCode, code } = classifyEditorFailure(ensured);
          out.fail(exitCode, { code, message: ensured.message }, null,
            ensured.reason === 'not-ready' && ensured.editorSource !== 'foreign'
              ? 'If the project is open in a Unity Editor, check that its AI Game Developer plugin is connected.'
              : undefined);
        }
        baseUrl = ensured.baseUrl;
        editor = ensured;
      }
      const endpoint = `${baseUrl}${cfg.routePrefix}/${encodeURIComponent(toolName)}`;
      verbose(`Endpoint: ${endpoint}`);

      if (human) {
        spinner?.stop();
        ui.heading(cfg.headingLabel);
        ui.label('Tool', toolName);
        ui.label('URL', endpoint);
        if (token) ui.label('Auth', `from ${authSource}`);
        ui.divider();
      }
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
          if (response.ok) callSpinner?.success(`${toolName} completed`);
          else callSpinner?.error(`${toolName} reported an error`);
          ui.success('Response:');
          console.log(typeof result.data === 'string'
            ? result.data
            : JSON.stringify(result.data, null, 2));
        }
        if (!response.ok) process.exit(ExitCode.CommandFailed);
        return;
      }

      callSpinner?.stop();
      const classified = classifyToolFailure(result);
      if (classified.exitCode === ExitCode.Timeout || classified.exitCode === ExitCode.Busy) {
        const dialogs = await findBlockingDialogs(projectPath);
        if (dialogs.length > 0) {
          if (out.machine) out.fail(ExitCode.AwaitingInput, awaitingInputError(dialogs), { tool: toolName, dialogs, editor: editorData });
          if (options.raw) process.stderr.write(awaitingInputError(dialogs).message + '\n');
          else ui.error(awaitingInputError(dialogs).message);
          process.exit(ExitCode.AwaitingInput);
        }
      }
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
      ui.error(`HTTP ${failure.httpStatus}: ${failure.message}`);
      if (failure.data !== undefined) {
        ui.info(typeof failure.data === 'string'
          ? failure.data
          : JSON.stringify(failure.data, null, 2));
      }
    }
    process.exit(exitCode);
  }

  const message = buildFailureMessage(failure, ctx);
  if (ctx.raw) process.stderr.write(message + '\n');
  else ui.error(`Failed to call ${ctx.errorNoun}: ${message}`);
  process.exit(exitCode);
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
    case 'http-error':
      return `HTTP ${failure.httpStatus}: ${failure.message}`;
    default:
      return failure.message;
  }
}

function capitalize(s: string): string {
  return s.length === 0 ? s : s.charAt(0).toUpperCase() + s.slice(1);
}
