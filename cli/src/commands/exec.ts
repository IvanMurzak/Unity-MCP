import * as fs from 'fs';
import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection, resolveProbeUrls } from '../utils/connection.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { openProject } from '../lib/open.js';
import { runTool } from '../lib/run-tool.js';
import { waitForReady } from '../lib/wait-for-ready.js';
import { handleFailure, invokeToolWithRefresh, stringifyForRaw } from './run-tool-builder.js';
import type { OpenProjectOptions, OpenProjectResult, RunToolOptions, RunToolResult } from '../lib/types.js';
import type { WaitForReadyOptions, WaitForReadyResult } from '../lib/wait-for-ready.js';

export const SCRIPT_EXECUTE_TOOL = 'script-execute';
export const DEFAULT_IDLE_SECONDS = 300;

interface ExecCliOptions {
  path?: string;
  file?: string;
  using?: string[];
  include?: string[];
  window?: boolean;
  idle?: string;
  readyTimeout?: string;
  timeout?: string;
  raw?: boolean;
  url?: string;
  token?: string;
}

/** Everything `runExec` touches outside its arguments; tests swap these out. */
export interface ExecDeps {
  findProcess: (projectPath: string) => { pid: number } | null;
  open: (options: OpenProjectOptions) => Promise<OpenProjectResult>;
  wait: (options: WaitForReadyOptions) => Promise<WaitForReadyResult>;
  invoke: (options: RunToolOptions) => Promise<RunToolResult>;
}

export interface ExecInput {
  projectPath: string;
  code: string;
  usings: string[];
  include: string[];
  /** Launch with a window instead of headless (only when no editor is running yet). */
  window: boolean;
  idleTimeoutSeconds: number;
  /** Explicit --url / --token: handed to a newly started editor so its plugin connects there. */
  overrideUrl?: string;
  overrideToken?: string;
  readyTimeoutMs: number;
  timeoutMs: number;
  probeUrls: string[];
  baseUrl: string;
  token: string | undefined;
  tokenFromCloudStore: boolean;
  onStatus?: (message: string) => void;
}

export type ExecOutcome =
  | { kind: 'launch-failed'; message: string }
  | { kind: 'not-ready'; elapsedMs: number }
  | { kind: 'called'; result: RunToolResult; launched: boolean };

/**
 * Run C# in the project's editor: reuse a running editor (GUI or headless, never closed), otherwise start one headless
 * (or windowed) that quits by itself after `idleTimeoutSeconds` without tool calls, so the next `exec` stays warm.
 */
export async function runExec(input: ExecInput, deps: ExecDeps): Promise<ExecOutcome> {
  let launched = false;
  if (!deps.findProcess(input.projectPath)) {
    input.onStatus?.(input.window ? 'Starting Unity Editor...' : 'Starting Unity Editor (headless)...');
    const opened = await deps.open({
      projectPath: input.projectPath,
      headless: !input.window,
      keepConnected: true,
      idleTimeoutSeconds: input.idleTimeoutSeconds,
      url: input.overrideUrl,
      token: input.overrideToken,
      // A local server is only reachable if someone starts it; the plugin does when asked.
      ...(input.overrideUrl && isLoopbackUrl(input.overrideUrl) ? { startServer: true } : {}),
    });
    if (opened.kind === 'failure') return { kind: 'launch-failed', message: opened.errorMessage };
    launched = true;
  }

  input.onStatus?.('Waiting for the MCP server...');
  const ready = await deps.wait({
    urls: input.probeUrls,
    headers: input.token ? { Authorization: `Bearer ${input.token}` } : undefined,
    timeoutMs: input.readyTimeoutMs,
    intervalMs: 2000,
  });
  if (ready.kind === 'failure') return { kind: 'not-ready', elapsedMs: ready.elapsedMs };

  input.onStatus?.('Running...');
  const result = await invokeToolWithRefresh(deps.invoke, {
    toolName: SCRIPT_EXECUTE_TOOL,
    baseUrl: ready.baseUrl,
    token: input.token,
    input: buildScriptExecuteInput(input.code, input.usings, input.include),
    timeoutMs: input.timeoutMs,
    tokenFromCloudStore: input.tokenFromCloudStore,
  });
  return { kind: 'called', result, launched };
}

export function isLoopbackUrl(url: string): boolean {
  try {
    const host = new URL(url).hostname;
    return host === 'localhost' || host === '[::1]' || /^127\./.test(host);
  } catch {
    return false;
  }
}

/** Request body for `script-execute`; empty lists are left out so older plugins without them still accept it. */
export function buildScriptExecuteInput(code: string, usings: string[], include: string[]): Record<string, unknown> {
  return {
    csharpCode: code,
    ...(usings.length > 0 ? { usings } : {}),
    ...(include.length > 0 ? { include } : {}),
  };
}

/** The tool's text output and whether it reported success. */
export function readToolResponse(data: unknown): { ok: boolean; text: string } {
  if (typeof data === 'string') return { ok: true, text: data };
  const response = data as { status?: string; content?: Array<{ type?: string; text?: string }> } | null;
  const texts = (response?.content ?? []).filter(c => c?.type === 'text' && typeof c.text === 'string').map(c => c.text!);
  return {
    ok: response?.status === undefined || response.status === 'success',
    text: texts.length > 0 ? texts.join('\n') : stringifyForRaw(data),
  };
}

function collect(value: string, previous: string[]): string[] {
  return [...previous, value];
}

function readCode(code: string | undefined, file: string | undefined): string {
  if (code !== undefined && file !== undefined) throw new Error('Pass the code either as an argument or with --file, not both.');
  if (file === '-' || (code === undefined && file === undefined && !process.stdin.isTTY)) return fs.readFileSync(0, 'utf-8');
  if (file !== undefined) return fs.readFileSync(file, 'utf-8');
  if (code === undefined) throw new Error('No code given: pass it as an argument, with --file <file>, or on stdin.');
  return code;
}

function positiveInt(value: string | undefined, flag: string): number {
  const parsed = Number(value);
  if (!Number.isInteger(parsed) || parsed <= 0) {
    ui.error(`Invalid ${flag} value: "${value}". Must be a positive integer.`);
    process.exit(1);
  }
  return parsed;
}

export const execCommand = new Command('exec')
  .description('Run C# in the Unity Editor; starts a headless editor when none is open and keeps it warm')
  .argument('[code]', 'C# snippet: statements, the last expression is the result')
  .option('--path <path>', 'Unity project path (default: current directory)')
  .option('-f, --file <file>', 'Read the code from a file ("-" for stdin)')
  .option('--using <namespace>', 'Extra namespace (repeatable)', collect, [])
  .option('--include <path>', 'Helper .cs file or folder, relative to the project snippets folder (repeatable)', collect, [])
  .option('--window', 'Start the editor with a window instead of headless (only when it is not running yet)')
  .option('--idle <seconds>', 'Quit an editor started by exec after this many idle seconds', String(DEFAULT_IDLE_SECONDS))
  .option('--ready-timeout <ms>', 'How long to wait for the editor to become ready', '300000')
  .option('--timeout <ms>', 'Tool call timeout in milliseconds', '180000')
  .option('--raw', 'Print the raw tool response JSON')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .action(async (codeArg: string | undefined, options: ExecCliOptions) => {
    const idleTimeoutSeconds = positiveInt(options.idle, '--idle');
    const readyTimeoutMs = positiveInt(options.readyTimeout, '--ready-timeout');
    const timeoutMs = positiveInt(options.timeout, '--timeout');
    let code: string;
    try {
      code = readCode(codeArg, options.file);
    } catch (err) {
      ui.error(err instanceof Error ? err.message : String(err));
      process.exit(1);
    }

    const projectPath = resolveAndValidateProjectPath(undefined, options);
    const { url: baseUrl, token, cloudAuthMissing, tokenFromCloudStore } = await resolveConnection(projectPath, options);
    if (cloudAuthMissing) {
      ui.error('Not logged in to ai-game.dev. Run `unity-mcp-cli login` to sign in, then retry — or pass --token / --url.');
      process.exit(1);
    }
    const probeUrls = await resolveProbeUrls(projectPath, options);

    const spinner = options.raw ? null : ui.startSpinner('Connecting...');
    const outcome = await runExec({
      projectPath,
      code,
      usings: options.using ?? [],
      include: options.include ?? [],
      window: options.window === true,
      idleTimeoutSeconds,
      overrideUrl: options.url,
      overrideToken: options.token,
      readyTimeoutMs,
      timeoutMs,
      probeUrls,
      baseUrl,
      token,
      tokenFromCloudStore,
      onStatus: message => {
        verbose(message);
        if (spinner) spinner.text = message;
      },
    }, { findProcess: findUnityProcess, open: openProject, wait: waitForReady, invoke: runTool });

    if (outcome.kind === 'launch-failed') {
      spinner?.error('Could not start the Unity Editor');
      ui.error(outcome.message);
      process.exit(1);
    }
    if (outcome.kind === 'not-ready') {
      spinner?.error(`The MCP server did not answer within ${(readyTimeoutMs / 1000).toFixed(0)}s`);
      process.exit(1);
    }

    const { result, launched } = outcome;
    if (result.kind === 'failure') {
      spinner?.stop();
      handleFailure(result, {
        toolName: SCRIPT_EXECUTE_TOOL,
        endpoint: `${baseUrl}/api/tools/${SCRIPT_EXECUTE_TOOL}`,
        raw: options.raw,
        timeoutMs,
        errorNoun: 'tool',
      });
    }
    if (options.raw) {
      process.stdout.write(stringifyForRaw(result.data));
      return;
    }
    const response = readToolResponse(result.data);
    spinner?.stop();
    if (launched) verbose(`Editor started by exec; it quits after ${idleTimeoutSeconds}s without tool calls.`);
    if (!response.ok) {
      ui.error(response.text);
      process.exit(1);
    }
    console.log(response.text);
  });
