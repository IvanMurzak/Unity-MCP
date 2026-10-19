// Unity editor sessions on cli-core's `EditorSession` (Code-Exec plan §4.6), shared by `exec`, `open` and `close`.
// A lease (`<project>/.ai-game-dev/session.json`) marks an editor this CLI started, a file lock lets only one call
// start an editor per project, and only a leased editor is ever closed. Library-safe: no console output, no exit.

import * as fs from 'fs';
import * as path from 'path';
import {
  EditorSession,
  SESSION_DIR_NAME,
  SESSION_LEASE_FILE_NAME,
  isLoopbackHost,
  mcpReadinessProbe,
  unityAdapter,
  type EditorLaunchSpec,
  type SessionProcessOps,
} from '@baizor/gamedev-cli-core';
import { findEditorPath, getProjectEditorVersion } from '../utils/unity-editor.js';
import { resolveConnection, resolveProbeUrls } from '../utils/connection.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { sendGracefulShutdown } from '../utils/unity-shutdown.js';
import { runSystemTool } from './run-tool.js';

/**
 * Plugin system tool that quits an editor started by a CLI session: it checks the lease nonce, saves like the idle
 * shutdown and exits. Needed because the OS polite quit (`taskkill` without `/F`) never reaches a windowless editor.
 */
export const SESSION_QUIT_TOOL = 'editor-session-quit';

const QUIT_CALL_TIMEOUT_MS = 15_000;

export interface UnitySessionDeps {
  /** Whether a Unity Editor process has the project open. */
  isEditorRunning: (projectPath: string) => boolean;
  /** Ask the plugin at `url` to quit (`SESSION_QUIT_TOOL`); true when it accepted. */
  quitViaPlugin: (url: string, token: string | undefined, nonce: string) => Promise<boolean>;
  /** The OS polite quit; true when delivered. */
  requestOsClose: (pid: number) => boolean;
  findEditor: (version: string | undefined) => Promise<string | null>;
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
  /** Extra environment for a started editor (`UNITY_MCP_*` connection settings). */
  env?: Record<string, string>;
  /** Explicit editor executable; default: the installed editor of the project's (or `unityVersion`) version. */
  editorPath?: string;
  unityVersion?: string;
  /** A started editor quits after this many seconds without tool calls; 0 = never. Default 300. */
  idleSeconds?: number;
  readyTimeoutMs?: number;
  onStatus?: (message: string) => void;
  deps?: Partial<UnitySessionDeps>;
}

const defaultDeps: UnitySessionDeps = {
  isEditorRunning: projectPath => findUnityProcess(projectPath) !== null,
  quitViaPlugin: async (url, token, nonce) => {
    const result = await runSystemTool({
      toolName: SESSION_QUIT_TOOL,
      url,
      ...(token ? { token } : {}),
      input: { nonce },
      timeoutMs: QUIT_CALL_TIMEOUT_MS,
    });
    return result.kind === 'success' && (result.data as { status?: string } | null)?.status !== 'error';
  },
  requestOsClose: pid => sendGracefulShutdown(pid),
  findEditor: version => findEditorPath(version),
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
  const probeReady = async (): Promise<string | undefined> =>
    (await probeLocal()) ?? (remoteUrls.length > 0 && deps.isEditorRunning(projectPath) ? probeRemote() : undefined);

  const editor: EditorLaunchSpec = {
    ...unityAdapter.editor!,
    findRunningEditor: async root => deps.isEditorRunning(root),
    requestClose: async pid => {
      const lease = session.readLease();
      const url = lease?.pid === pid ? await probeReady() : undefined;
      if (url !== undefined && await deps.quitViaPlugin(url, options.token, lease!.nonce)) return true;
      return deps.requestOsClose(pid);
    },
  };

  const session: EditorSession<string> = new EditorSession<string>({
    adapter: { ...unityAdapter, editor },
    projectRoot: projectPath,
    probeReady,
    editorBinary: async () => {
      if (options.editorPath !== undefined) return validateEditorPath(options.editorPath);
      const version = options.unityVersion ?? getProjectEditorVersion(projectPath) ?? undefined;
      const found = await deps.findEditor(version);
      if (!found) throw new Error(version ? `Unity Editor ${version} is not installed.` : 'No Unity Editor found.');
      return found;
    },
    ...(options.env ? { env: options.env } : {}),
    ...(options.idleSeconds !== undefined ? { idleSeconds: options.idleSeconds } : {}),
    ...(options.readyTimeoutMs !== undefined ? { readyTimeoutMs: options.readyTimeoutMs } : {}),
    ...(options.onStatus ? { onStatus: options.onStatus } : {}),
    ...(deps.processOps ? { processOps: deps.processOps } : {}),
  });
  return session;
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
  return factory({ projectPath, urls: resolveProbeUrls(projectPath, url, connection.url), token, ...extra });
}

/** Whether the project has a session lease file at all (valid or not): cheap, no process checks. */
export function hasSessionLease(projectPath: string): boolean {
  return fs.existsSync(path.join(projectPath, SESSION_DIR_NAME, SESSION_LEASE_FILE_NAME));
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
