import * as fs from 'fs';
import * as path from 'path';
import { verbose } from './ui.js';
import { generatePortFromDirectory } from './port.js';
import { readConfig, resolveConnectionFromConfig, isCloudMode } from './config.js';
import { readCloudAccessToken } from './cloud-credentials.js';
import * as ui from './ui.js';

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
