// Library-safe `ensureEditor`: make sure the project's Unity Editor answers, starting a warm headless one when none
// runs (the `exec` policy, shared by `exec`, `run-tool` and `compile`). No console output, no `process.exit`.

import {
  DEFAULT_SESSION_IDLE_SECONDS,
  EditorSessionError,
  type EditorSession,
  type EditorSessionErrorReason,
  type EnsureSource,
  type WaitedEditorSource,
} from '@baizor/gamedev-cli-core';
import { createUnitySession, isLoopbackUrl, sessionErrorMessage, type UnitySessionOptions } from './editor-session.js';
import * as fs from 'fs';
import * as path from 'path';
import { buildOpenEnv, dismissLaunchErrorsUntil } from './open.js';
import { formatCompilerMessage, parseCompilerMessages } from '../utils/compiler-messages.js';

/** The log of a headless editor the CLI starts: in the project's Logs/ (never committed), where an agent can read it. */
export function headlessLogPath(projectPath: string): string {
  return path.join(path.resolve(projectPath), 'Logs', 'unity-mcp-cli-editor.log');
}

/**
 * Arguments for a headless editor the CLI starts. In batch mode Unity aborts at startup when the project's scripts do
 * not compile ("Scripts have compiler errors"); with `-ignorecompilererrors` it keeps running (the plugin's own
 * assemblies compile separately), so `compile` and `exec` can still report and fix the errors.
 */
export function headlessEditorArgs(projectPath: string): string[] {
  return ['-ignorecompilererrors', '-logFile', headlessLogPath(projectPath)];
}

/** Why a started editor exited, read from its log: the compile errors when there are any, else the log's path. */
export function explainEditorExit(projectPath: string): string {
  const logFile = headlessLogPath(projectPath);
  let log = '';
  try {
    log = fs.readFileSync(logFile, 'utf-8');
  } catch {
    return '';
  }
  const errors = parseCompilerMessages(log).filter(m => m.severity === 'error');
  const lines = errors.slice(0, 10).map(formatCompilerMessage);
  return lines.length > 0
    ? ` The project's scripts do not compile:\n${lines.join('\n')}\nEditor log: ${logFile}`
    : ` Editor log: ${logFile}`;
}

/** How long `--once` waits for an editor it started to save and quit before killing it. */
export const ONCE_CLOSE_TIMEOUT_MS = 60_000;

/** The part of an `EditorSession` the callers use; tests pass a fake. `closeOwned` is optional for fakes. */
export type EnsureEditorSession = Pick<EditorSession<string>, 'ensure'> & Partial<Pick<EditorSession<string>, 'closeOwned' | 'readLease'>>;

export type EnsureEditorSessionFactory = (options: UnitySessionOptions) => EnsureEditorSession;

export interface EnsureEditorOptions {
  projectPath: string;
  /** Server URLs to try, first answer wins (the CLI passes the config URL plus the project's local port). */
  probeUrls: string[];
  /** Bearer for the server. */
  token?: string;
  /** Explicit server URL / token: handed to a newly started editor so its plugin connects there. */
  overrideUrl?: string;
  overrideToken?: string;
  /** Start the editor with a window instead of headless (only when it is not running yet). */
  window?: boolean;
  /** An editor started here quits after this many seconds without tool calls (0 = never). Default 300. */
  idleTimeoutSeconds?: number;
  readyTimeoutMs?: number;
  /** Wait for an editor this call did not start (the user's GUI) to answer; see `UnitySessionOptions.foreignReadyTimeoutMs`. */
  foreignReadyTimeoutMs?: number;
  onStatus?: (message: string) => void;
  /**
   * Close an editor this call started when it never became ready (`--once`), instead of leaving it to its idle
   * timeout. Only an editor whose lease this call wrote is closed.
   */
  closeOnFailure?: boolean;
  /** Injection point for tests. */
  session?: EnsureEditorSessionFactory;
}

export interface EnsureEditorReady {
  kind: 'ready';
  /** The server URL that answered. */
  baseUrl: string;
  /** How the editor was obtained: `started` when this call launched it. */
  source: EnsureSource | undefined;
  /**
   * Close the editor when this call started it (`--once`); a reused editor is left alone. Resolves to the pid closed,
   * or undefined when nothing was closed.
   */
  closeIfStarted: () => Promise<number | undefined>;
}

export interface EnsureEditorFailure {
  kind: 'failure';
  /** `launch-failed`: no editor could be started; `not-ready`: its server never answered. */
  reason: 'launch-failed' | 'not-ready';
  sessionReason?: EditorSessionErrorReason;
  /** `not-ready`: the editor that never answered — `foreign` is one the CLI did not start (e.g. the user's GUI). */
  editorSource?: WaitedEditorSource;
  message: string;
  error: Error;
}

export type EnsureEditorResult = EnsureEditorReady | EnsureEditorFailure;

/**
 * Reuse the project's running editor (GUI or headless, never closed), or start one under the project's session lease
 * and start lock so parallel calls share one editor. A started editor quits by itself after `idleTimeoutSeconds`
 * without tool calls, so the next call stays warm.
 */
export async function ensureEditor(options: EnsureEditorOptions): Promise<EnsureEditorResult> {
  const factory = options.session ?? createUnitySession;
  // The server a started editor connects to (recorded in its lease): the explicit URL, normalized as the CLI resolves
  // it (no trailing slash), else the resolved one.
  const serverUrl = options.overrideUrl?.replace(/\/$/, '') ?? options.probeUrls[0];
  // A windowed editor we start may stop on the "compile errors at launch" dialog; click it away until it is ready.
  const ready = new AbortController();
  let dismissing: Promise<void> | undefined;
  let session: EnsureEditorSession | undefined;
  try {
    session = factory({
      projectPath: options.projectPath,
      urls: options.probeUrls,
      token: options.token,
      ...(serverUrl !== undefined ? { serverUrl } : {}),
      env: buildOpenEnv({
        keepConnected: true,
        url: options.overrideUrl,
        token: options.overrideToken,
        // A local server is only reachable if someone starts it; the plugin does when asked. The CLI talks HTTP to
        // it, so it also asks for the http transport (in memory only): with a project saved on stdio the plugin
        // would otherwise leave the port to the AI agent and start nothing.
        ...(options.overrideUrl && isLoopbackUrl(options.overrideUrl)
          ? { startServer: true, transport: 'streamableHttp' as const }
          : {}),
      }),
      idleSeconds: options.idleTimeoutSeconds ?? DEFAULT_SESSION_IDLE_SECONDS,
      ...(options.window ? {} : { extraArgs: headlessEditorArgs(options.projectPath) }),
      ...(options.readyTimeoutMs !== undefined ? { readyTimeoutMs: options.readyTimeoutMs } : {}),
      ...(options.foreignReadyTimeoutMs !== undefined ? { foreignReadyTimeoutMs: options.foreignReadyTimeoutMs } : {}),
      ...(options.onStatus ? { onStatus: options.onStatus } : {}),
      ...(options.window ? { onLaunched: (pid: number) => { dismissing = dismissLaunchErrorsUntil(ready.signal, [], pid); } } : {}),
    });
    const started = session;
    const ensured = await started.ensure({ mode: options.window ? 'window' : 'headless' });
    return {
      kind: 'ready',
      baseUrl: ensured.ready,
      source: ensured.source,
      closeIfStarted: async () => {
        if (ensured.source !== 'started' || !started.closeOwned) return undefined;
        // Only while the lease is still the one this call wrote: after a crash another call may have started (and
        // leased) a new editor, which is not ours to close.
        const nonce = ensured.lease?.nonce;
        if (nonce !== undefined && started.readLease && started.readLease()?.nonce !== nonce) return undefined;
        const closed = await started.closeOwned({ timeoutMs: ONCE_CLOSE_TIMEOUT_MS, force: true });
        return closed.kind === 'closed' ? closed.pid : undefined;
      },
    };
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    const sessionError = err instanceof EditorSessionError ? err : undefined;
    const sessionReason = sessionError?.reason;
    if (options.closeOnFailure && sessionError?.source === 'started' && session?.closeOwned) {
      await session.closeOwned({ timeoutMs: ONCE_CLOSE_TIMEOUT_MS, force: true }).catch(() => undefined);
    }
    return {
      kind: 'failure',
      reason: sessionReason === 'exited' || sessionReason === 'not-ready' ? 'not-ready' : 'launch-failed',
      ...(sessionReason ? { sessionReason } : {}),
      ...(sessionError?.source ? { editorSource: sessionError.source } : {}),
      message: sessionErrorMessage(error, serverUrl) + (sessionReason === 'exited' ? explainEditorExit(options.projectPath) : ''),
      error,
    };
  } finally {
    ready.abort();
    await dismissing;
  }
}
