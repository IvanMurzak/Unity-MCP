// Unity editor sessions on cli-core's `EditorSession` (Code-Exec plan §4.6), shared by `exec`, `open` and `close`.
// A lease (`<project>/.ai-game-dev/session.json`) marks an editor this CLI started, a file lock lets only one call
// start an editor per project, and only a leased editor is ever closed. Library-safe: no console output, no exit.

import * as fs from 'fs';
import * as path from 'path';
import {
  EditorSession,
  EditorSessionError,
  DEFAULT_SESSION_READY_TIMEOUT_MS,
  defaultSessionProcessOps,
  isLoopbackHost,
  mcpReadinessProbe,
  unityAdapter,
  type EditorLaunchSpec,
  type SessionProcessOps,
} from '@baizor/gamedev-cli-core';
import { findEditorPath, getProjectEditorVersion } from '../utils/unity-editor.js';
import { clearCachedEditorPath } from '../utils/editor-cache.js';
import { resolveConnection, resolveProbeUrls } from '../utils/connection.js';
import { CLOUD_SERVER_URL } from '../utils/config.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { sendGracefulShutdown } from '../utils/unity-shutdown.js';
import { runSystemTool } from './run-tool.js';
import { readToolResponse } from './exec.js';

/**
 * Plugin system tool that quits an editor started by a CLI session: it checks the lease nonce, saves like the idle
 * shutdown and exits. Needed because the OS polite quit (`taskkill` without `/F`) never reaches a windowless editor.
 */
export const SESSION_QUIT_TOOL = 'editor-session-quit';

const QUIT_CALL_TIMEOUT_MS = 15_000;

/**
 * Default wait for an editor the CLI did not start (the user's GUI) to answer. Shorter than the start timeout: such an
 * editor may never answer here (plugin disconnected, or connected to the cloud), so the call fails with a clear hint.
 */
export const DEFAULT_FOREIGN_READY_TIMEOUT_MS = 60_000;

export interface UnitySessionDeps {
  /** The PID of a Unity Editor process that has the project open; undefined when none. */
  findEditorPid: (projectPath: string) => number | undefined;
  /** Ask the plugin at `url` to quit (`SESSION_QUIT_TOOL`); true when it accepted. */
  quitViaPlugin: (url: string, token: string | undefined, nonce: string) => Promise<boolean>;
  /** The OS polite quit; true when delivered. */
  requestOsClose: (pid: number) => boolean;
  findEditor: (version: string | undefined) => Promise<string | null>;
  /** Drop the cached editor path of `version` (it failed to start), so the next call resolves it again. */
  forgetEditor: (version: string | undefined) => void;
  processOps?: Partial<SessionProcessOps>;
  fetchImpl?: typeof fetch;
}

export interface UnitySessionOptions {
  projectPath: string;
  /**
   * The project's server URLs. A loopback one answering means the editor is up; a remote (cloud) one answers whether
   * or not this editor runs, so it counts only while a Unity process has the project open.
   */
  urls: string[];
  token?: string;
  /**
   * The server URL a started editor connects to; recorded in the session lease, so `close` reaches the editor's plugin
   * there without resolving the connection again.
   */
  serverUrl?: string;
  /** Extra environment for a started editor (`UNITY_MCP_*` connection settings). */
  env?: Record<string, string>;
  /** Explicit editor executable; default: the installed editor of the project's (or `unityVersion`) version. */
  editorPath?: string;
  unityVersion?: string;
  /** A started editor quits after this many seconds without tool calls; 0 = never. Default 300. */
  idleSeconds?: number;
  readyTimeoutMs?: number;
  /** Wait for an editor the CLI did not start. Default `DEFAULT_FOREIGN_READY_TIMEOUT_MS` (at most `readyTimeoutMs`). */
  foreignReadyTimeoutMs?: number;
  onStatus?: (message: string) => void;
  /** Called once this session has started an editor process (not when it reuses one). */
  onLaunched?: (pid: number) => void;
  deps?: Partial<UnitySessionDeps>;
}

const defaultDeps: UnitySessionDeps = {
  findEditorPid: projectPath => findUnityProcess(projectPath)?.pid,
  quitViaPlugin: async (url, token, nonce) => {
    const result = await runSystemTool({
      toolName: SESSION_QUIT_TOOL,
      url,
      ...(token ? { token } : {}),
      input: { nonce },
      timeoutMs: QUIT_CALL_TIMEOUT_MS,
    });
    return result.kind === 'success' && readToolResponse(result.data).ok;
  },
  requestOsClose: pid => sendGracefulShutdown(pid),
  findEditor: version => findEditorPath(version),
  forgetEditor: version => clearCachedEditorPath(version),
};

export function isLoopbackUrl(url: string): boolean {
  try {
    const host = new URL(url).hostname;
    return host === 'localhost' || isLoopbackHost(host);
  } catch {
    return false;
  }
}

/** The project's Unity editor session. The readiness value is the server URL that answered. */
export function createUnitySession(options: UnitySessionOptions): EditorSession<string> {
  const deps: UnitySessionDeps = { ...defaultDeps, ...options.deps };
  const projectPath = path.resolve(options.projectPath);
  const probe = (urls: string[]) => mcpReadinessProbe({
    urls, token: options.token, ...(deps.fetchImpl ? { fetchImpl: deps.fetchImpl } : {}),
  });
  const probeLocal = probe(options.urls.filter(isLoopbackUrl));
  const remoteUrls = options.urls.filter(url => !isLoopbackUrl(url));
  const probeRemote = probe(remoteUrls);
  // The process scan is slow (PowerShell on Windows): once an editor was seen, polling does not rescan.
  let editorSeen = false;
  const editorRunning = () => (editorSeen ||= deps.findEditorPid(projectPath) !== undefined);
  const probeReady = async (): Promise<string | undefined> =>
    (await probeLocal()) ?? (remoteUrls.length > 0 && editorRunning() ? probeRemote() : undefined);

  const editor: EditorLaunchSpec = {
    ...unityAdapter.editor!,
    // The PID lets the session notice cheaply when the user's editor exits while it waits for it.
    findRunningEditor: async root => deps.findEditorPid(root) ?? false,
    // `lease` is the verified lease naming `pid`: the plugin is asked at the server it was started against.
    requestClose: async (pid, _root, lease) => {
      const url = lease ? lease.serverUrl ?? await probeReady() : undefined;
      if (url !== undefined && await deps.quitViaPlugin(url, options.token, lease!.nonce)) return true;
      return deps.requestOsClose(pid);
    },
  };

  // Set when the binary came from discovery (and so maybe from the editor-path cache), not from `editorPath`.
  let discovered: { version: string | undefined } | undefined;
  const spawnDetached = deps.processOps?.spawnDetached ?? defaultSessionProcessOps.spawnDetached;
  const processOps: Partial<SessionProcessOps> = {
    ...deps.processOps,
    // A discovered binary that fails to start (corrupt, wrong arch, permissions) leaves the cache, as `open` does.
    spawnDetached: async (command, args, spawnOptions) => {
      try {
        const pid = await spawnDetached(command, args, spawnOptions);
        options.onLaunched?.(pid);
        return pid;
      } catch (err) {
        if (discovered) deps.forgetEditor(discovered.version);
        throw err;
      }
    },
  };

  const readyTimeoutMs = options.readyTimeoutMs ?? DEFAULT_SESSION_READY_TIMEOUT_MS;
  return new EditorSession<string>({
    adapter: { ...unityAdapter, editor },
    projectRoot: projectPath,
    probeReady,
    editorBinary: async () => {
      if (options.editorPath !== undefined) return validateEditorPath(options.editorPath);
      const version = options.unityVersion ?? getProjectEditorVersion(projectPath) ?? undefined;
      const found = await deps.findEditor(version);
      if (!found) throw new Error(version ? `Unity Editor ${version} is not installed.` : 'No Unity Editor found.');
      discovered = { version };
      return found;
    },
    ...(options.env ? { env: options.env } : {}),
    ...(options.serverUrl !== undefined
      ? { serverUrl: options.serverUrl, connectionMode: options.serverUrl === CLOUD_SERVER_URL ? 'Cloud' : 'Custom' }
      : {}),
    ...(options.idleSeconds !== undefined ? { idleSeconds: options.idleSeconds } : {}),
    readyTimeoutMs,
    foreignReadyTimeoutMs: options.foreignReadyTimeoutMs ?? Math.min(DEFAULT_FOREIGN_READY_TIMEOUT_MS, readyTimeoutMs),
    ...(options.onStatus ? { onStatus: options.onStatus } : {}),
    processOps,
  });
}

/**
 * The message for a session failure. An editor the CLI did not start that never answered gets a hint: its plugin is
 * not connected, or (Cloud mode, `serverUrl` is the cloud) it is reachable only through the cloud.
 */
export function sessionErrorMessage(err: unknown, serverUrl: string | undefined): string {
  const message = err instanceof Error ? err.message : String(err);
  if (!(err instanceof EditorSessionError) || err.source !== 'foreign' || err.reason !== 'not-ready') return message;
  const hint = serverUrl === CLOUD_SERVER_URL
    ? 'The project is in Cloud mode, which the CLI cannot reach the editor through: switch the plugin connection to ' +
      'Custom (a local server) in Window > AI Game Developer, or pass --url <server url>.'
    : 'Check that its AI Game Developer plugin is connected (or close that editor so the CLI can start its own).';
  return `The project is open in a Unity Editor that unity-mcp-cli did not start, and it did not answer: ${message} ${hint}`;
}

/**
 * The project's editor session, reaching its server the way the other commands resolve it (explicit url / token,
 * then the project config, then the project's local port).
 */
export async function createProjectSession(
  projectPath: string,
  connection: { url?: string; token?: string },
  extra: Partial<UnitySessionOptions> = {},
  factory: (options: UnitySessionOptions) => EditorSession<string> = createUnitySession,
): Promise<EditorSession<string>> {
  const { url, token } = await resolveConnection(projectPath, connection);
  return factory({ projectPath, urls: resolveProbeUrls(projectPath, url, connection.url), token, serverUrl: url, ...extra });
}

/**
 * An explicit editor path, resolved and checked to be a file. Checked before `path.resolve`: an empty string would
 * resolve to the working directory.
 */
export function validateEditorPath(editorPath: string): string {
  const trimmed = typeof editorPath === 'string' ? editorPath.trim() : '';
  if (trimmed.length === 0) throw new Error('Explicit Unity Editor path is empty or whitespace-only.');
  const resolved = path.resolve(trimmed);
  let stat: fs.Stats;
  try {
    stat = fs.statSync(resolved);
  } catch {
    throw new Error(`Unity Editor path does not exist: ${resolved}`);
  }
  if (!stat.isFile()) throw new Error(`Unity Editor path is not a file: ${resolved}`);
  return resolved;
}
