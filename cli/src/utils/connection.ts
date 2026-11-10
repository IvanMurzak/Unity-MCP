import * as fs from 'fs';
import * as path from 'path';
import { verbose } from './ui.js';
import { generatePortFromDirectory } from './port.js';
import { readConfig, resolveConnectionFromConfig, isCloudMode } from './config.js';
import { readCloudAccessToken } from './cloud-credentials.js';
import * as ui from './ui.js';
import { findUnityProcess } from './unity-process.js';
import { hasLiveSessionLease } from '../lib/editor-session.js';

export interface ConnectionOptions {
  path?: string;
  url?: string;
  token?: string;
}

/**
 * Returns true if the given directory looks like a Unity project (has an Assets subfolder).
 */
export function isUnityProject(dir: string): boolean {
  return fs.existsSync(path.join(dir, 'Assets'));
}

/**
 * Resolve the project path from positional arg, --path option, or cwd.
 */
export function resolveProjectPath(positionalPath: string | undefined, options: ConnectionOptions): string {
  const resolved = path.resolve(positionalPath ?? options.path ?? process.cwd());
  if ((positionalPath !== undefined || options.path !== undefined) && !fs.existsSync(resolved)) {
    ui.error(`Project path does not exist: ${resolved}`);
    process.exit(1);
  }
  return resolved;
}

/**
 * Resolve the project path and validate it is a Unity project.
 * Skips validation when --url is provided (explicit server override).
 */
export function resolveAndValidateProjectPath(positionalPath: string | undefined, options: ConnectionOptions): string {
  const resolved = resolveProjectPath(positionalPath, options);

  // Skip Unity project validation when --url is explicitly provided
  if (options.url) {
    return resolved;
  }

  if (!isUnityProject(resolved)) {
    ui.error(`Not a Unity project (missing Assets folder): ${resolved}`);
    ui.info('Provide a Unity project path as an argument, or use --url to connect to a server directly.');
    process.exit(1);
  }

  return resolved;
}

/**
 * Resolve the server URL and auth token.
 *
 * URL priority:
 *   1. --url flag (explicit override)
 *   2. Config file connectionMode → Custom: host, Cloud: hardcoded cloud URL
 *   3. Deterministic port from project path
 *
 * Token priority:
 *   1. --token flag (explicit override)
 *   2. Config file token (Custom mode) / shared machine credential store (Cloud mode)
 */
export interface ResolveConnectionDeps {
  /**
   * Injection point for the Cloud-mode machine-store credential read. Forwarded to
   * `resolveConnectionFromConfig`; defaults to `readCloudAccessToken` — cli-core's
   * `MachineCredentialProvider` (proactive refresh under the cross-process lock, never a raw
   * on-disk read). Tests inject a deterministic value.
   */
  readCloudToken?: () => Promise<string | undefined> | string | undefined;
  /** Whether an editor the CLI did not start has the project open (`resolveEditorConnection`). */
  userEditorOpen?: (projectPath: string) => boolean;
}

/** An editor has the project open and no live session lease says the CLI started it. */
function userEditorOpen(projectPath: string): boolean {
  return findUnityProcess(projectPath) !== null && !hasLiveSessionLease(projectPath);
}

export async function resolveConnection(
  projectPath: string,
  options: ConnectionOptions,
  deps: ResolveConnectionDeps = {},
): Promise<{
  url: string;
  token: string | undefined;
  cloudAuthMissing: boolean;
  /** True when the Bearer came from the shared machine store (Cloud mode, no --token override). */
  tokenFromCloudStore: boolean;
}> {
  const config = readConfig(projectPath);
  const fromConfig = config
    ? await resolveConnectionFromConfig(config, {
        readCloudToken: deps.readCloudToken ?? readCloudAccessToken,
      })
    : { url: undefined, token: undefined };

  verbose(`Config loaded: connectionMode=${config?.connectionMode ?? 'N/A'}, configUrl=${fromConfig.url ?? 'N/A'}, hasToken=${!!fromConfig.token}`);

  let url: string;
  let usingCloudUrl = false;
  if (options.url) {
    url = options.url.replace(/\/$/, '');
    verbose(`Using explicit --url: ${url}`);
  } else if (fromConfig.url) {
    url = fromConfig.url.replace(/\/$/, '');
    usingCloudUrl = !!config && isCloudMode(config);
    verbose(`Using URL from config (${config?.connectionMode} mode): ${url}`);
  } else {
    const port = generatePortFromDirectory(projectPath);
    url = `http://localhost:${port}`;
    verbose(`Using deterministic port URL: ${url}`);
  }

  const token = options.token ?? fromConfig.token;
  if (options.token) {
    verbose('Using explicit --token');
  }

  // Cloud mode resolves its Bearer from the shared machine credential store (see
  // resolveConnectionFromConfig). When the store holds no credential AND the caller overrode
  // neither the endpoint (--url) nor the token (--token), the request would go out unauthenticated —
  // signal this so the run-tool command surfaces an actionable "not logged in" error instead of a
  // silent unauthenticated cloud call (defect E / D11).
  const cloudAuthMissing = usingCloudUrl && !token;
  const tokenFromCloudStore = usingCloudUrl && !options.token && !!token;

  return { url, token, cloudAuthMissing, tokenFromCloudStore };
}

/**
 * Base URLs to probe for readiness. With an explicit --url only that URL; otherwise the config-resolved URL and the
 * deterministic local port (when they differ), so whichever answers first wins.
 */
export function resolveProbeUrls(projectPath: string, resolvedUrl: string, explicitUrl: string | undefined): string[] {
  const urls = [resolvedUrl];
  if (!explicitUrl) {
    const localUrl = `http://localhost:${generatePortFromDirectory(projectPath)}`;
    if (localUrl !== resolvedUrl) {
      urls.push(localUrl);
      verbose(`Will probe both config URL (${resolvedUrl}) and local URL (${localUrl})`);
    }
  }
  return urls;
}

export interface EditorConnection {
  url: string;
  token: string | undefined;
  tokenFromCloudStore: boolean;
  /**
   * The server URL a started editor must serve. Set to the project's local server when the project is in Cloud mode
   * but nobody is signed in (`localFallback`); otherwise the explicit `--url`.
   */
  overrideUrl: string | undefined;
  /** Cloud mode without a sign-in: the command uses the project's local server instead of refusing. */
  localFallback: boolean;
  /**
   * Cloud mode without a sign-in while an editor the CLI did not start has the project open: that editor talks to the
   * cloud, so only a sign-in reaches it. The command refuses with NOT_AUTHENTICATED.
   */
  signInRequired: boolean;
}

/** The error of `signInRequired`. */
export const SIGN_IN_REQUIRED_MESSAGE = 'The project is open in a Unity Editor connected to ai-game.dev (Cloud mode), and you are not signed in. '
  + 'Run `unity-mcp-cli login`, or switch the editor\'s connection to Custom (a local server), or pass --url.';

/**
 * The connection of a command that can start the project's editor itself (`exec`, `run-tool`, `compile`, `tools`).
 * In Cloud mode without an ai-game.dev sign-in the cloud cannot be reached, but the editor can serve its own local MCP
 * server: an editor the CLI starts is told to (in memory — the project config is not changed), so the command works
 * on a fresh project instead of failing with "not logged in". A user's editor connected to the cloud is still found
 * only through the cloud.
 */
export async function resolveEditorConnection(
  projectPath: string,
  options: ConnectionOptions,
  deps: ResolveConnectionDeps = {},
): Promise<EditorConnection> {
  const resolved = await resolveConnection(projectPath, options, deps);
  if (!resolved.cloudAuthMissing) {
    return {
      url: resolved.url, token: resolved.token, tokenFromCloudStore: resolved.tokenFromCloudStore, overrideUrl: options.url,
      localFallback: false, signInRequired: false,
    };
  }
  const localUrl = `http://localhost:${generatePortFromDirectory(projectPath)}`;
  if ((deps.userEditorOpen ?? userEditorOpen)(projectPath)) {
    return { url: resolved.url, token: undefined, tokenFromCloudStore: false, overrideUrl: undefined, localFallback: false, signInRequired: true };
  }
  verbose(`Not signed in to ai-game.dev (Cloud mode): using the project's local server ${localUrl}`);
  return { url: localUrl, token: readConfig(projectPath)?.token, tokenFromCloudStore: false, overrideUrl: localUrl, localFallback: true, signInRequired: false };
}

/** The note a command shows when it fell back to the local server. */
export function localFallbackNote(url: string): string {
  return `Not signed in to ai-game.dev, so the project's local MCP server (${url}) is used: an editor started now serves it. ` +
    'Run `unity-mcp-cli login` to use Cloud mode.';
}
