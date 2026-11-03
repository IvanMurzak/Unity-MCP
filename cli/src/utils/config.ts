import * as fs from 'fs';
import * as path from 'path';
import { generatePortFromDirectory } from './port.js';

const CONFIG_RELATIVE_PATH = 'UserSettings/AI-Game-Developer-Config.json';

/**
 * A tool / prompt / resource entry of the plugin's config file: whether MCP clients see it listed (tools/list).
 * Visibility is not an access control: a hidden tool still runs when called by name. The plugin writes `visible`;
 * a file saved before the rename carries `enabled`, which {@link readFeatures} still reads.
 */
export interface McpFeature {
  name: string;
  visible: boolean;
}

/**
 * The well-formed entries of a feature group, as `{ name, visible }`. Accepts the pre-rename key `enabled`;
 * `visible` wins when both are present. Anything else (non-objects, a non-string name, no boolean flag) is dropped.
 */
export function readFeatures(raw: unknown): McpFeature[] {
  if (!Array.isArray(raw)) return [];
  const features: McpFeature[] = [];
  for (const entry of raw as unknown[]) {
    if (typeof entry !== 'object' || entry === null) continue;
    const { name, visible, enabled } = entry as { name?: unknown; visible?: unknown; enabled?: unknown };
    if (typeof name !== 'string') continue;
    const flag = typeof visible === 'boolean' ? visible : typeof enabled === 'boolean' ? enabled : undefined;
    if (flag === undefined) continue;
    features.push({ name, visible: flag });
  }
  return features;
}

export interface UnityConnectionConfig {
  host?: string;
  token?: string;
  keepConnected?: boolean;
  logLevel?: number;
  timeoutMs?: number;
  keepServerRunning?: boolean;
  transportMethod?: string;
  authOption?: string;
  connectionMode?: string | number;
  cloudToken?: string;
  tools?: McpFeature[];
  prompts?: McpFeature[];
  resources?: McpFeature[];
  [key: string]: unknown;
}

function getConfigPath(projectPath: string): string {
  return path.join(projectPath, CONFIG_RELATIVE_PATH);
}

/**
 * Create a default config for a Unity project.
 */
export function createDefaultConfig(projectPath: string): UnityConnectionConfig {
  const port = generatePortFromDirectory(projectPath);
  return {
    host: `http://localhost:${port}`,
    keepConnected: false,
    logLevel: 3,
    timeoutMs: 10000,
    keepServerRunning: false,
    transportMethod: 'streamableHttp',
    authOption: 'none',
    connectionMode: 'Custom',
    tools: [],
    prompts: [],
    resources: [],
  };
}

/**
 * Read the AI-Game-Developer-Config.json from a Unity project.
 * Returns null if the file doesn't exist.
 */
export function readConfig(projectPath: string): UnityConnectionConfig | null {
  const configPath = getConfigPath(projectPath);
  if (!fs.existsSync(configPath)) {
    return null;
  }
  const json = fs.readFileSync(configPath, 'utf-8');
  try {
    return JSON.parse(json) as UnityConnectionConfig;
  } catch (err) {
    if (err instanceof SyntaxError) {
      throw new SyntaxError(`Malformed JSON in config file: ${configPath}\n${err.message}`);
    }
    throw err;
  }
}

/**
 * Write the AI-Game-Developer-Config.json to a Unity project.
 * Creates the UserSettings directory if needed.
 */
export function writeConfig(projectPath: string, config: UnityConnectionConfig): void {
  const configPath = getConfigPath(projectPath);
  const dir = path.dirname(configPath);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
  fs.writeFileSync(configPath, JSON.stringify(config, null, 2) + '\n');
}

/**
 * Read config or create with defaults if it doesn't exist.
 */
export function getOrCreateConfig(projectPath: string): UnityConnectionConfig {
  if (fs.existsSync(getConfigPath(projectPath))) {
    return readConfig(projectPath) as UnityConnectionConfig;
  }

  const config = createDefaultConfig(projectPath);
  writeConfig(projectPath, config);
  return config;
}

/**
 * Update features (tools, prompts, or resources) in the config.
 * - enableNames: set these to visible=true
 * - disableNames: set these to visible=false (hidden from tools/list; still callable by name)
 * - enableAll/disableAll: override all features
 */
export function updateFeatures(
  config: UnityConnectionConfig,
  featureType: 'tools' | 'prompts' | 'resources',
  options: {
    enableNames?: string[];
    disableNames?: string[];
    enableAll?: boolean;
    disableAll?: boolean;
  }
): void {
  const features = readFeatures(config[featureType]);

  if (options.enableAll) {
    for (const f of features) f.visible = true;
    config[featureType] = features;
    return;
  }

  if (options.disableAll) {
    for (const f of features) f.visible = false;
    config[featureType] = features;
    return;
  }

  if (options.enableNames) {
    for (const name of options.enableNames) {
      const existing = features.find((f) => f.name === name);
      if (existing) {
        existing.visible = true;
      } else {
        features.push({ name, visible: true });
      }
    }
  }

  if (options.disableNames) {
    for (const name of options.disableNames) {
      const existing = features.find((f) => f.name === name);
      if (existing) {
        existing.visible = false;
      } else {
        features.push({ name, visible: false });
      }
    }
  }

  config[featureType] = features;
}

/**
 * Determine whether the config is in Cloud mode.
 * Handles both string ("Cloud") and legacy integer (1) representations
 * of the ConnectionMode enum.
 */
export function isCloudMode(config: UnityConnectionConfig): boolean {
  const mode = config.connectionMode;
  return mode === 'Cloud' || mode === 1;
}

export const CLOUD_SERVER_BASE_URL = 'https://ai-game.dev';
export const CLOUD_SERVER_URL = 'https://ai-game.dev/mcp';

/** Options for {@link resolveConnectionFromConfig}. */
export interface ResolveConnectionFromConfigOptions {
  /**
   * Supplies the Cloud-mode Bearer credential. REQUIRED (no built-in default): the production
   * value is `readCloudAccessToken` from `cloud-credentials.ts` — cli-core's
   * `MachineCredentialProvider`, which proactively refreshes an expiring token under the
   * cross-process lock (unified-machine-auth 04 §3). A raw on-disk `accessToken` read must never
   * reappear here: it returns a token nobody refreshes (the pre-d2 defect). Tests inject a
   * deterministic value; sync or async both work.
   */
  readCloudToken: () => Promise<string | undefined> | string | undefined;
}

/**
 * Resolve the server URL and auth token from a project config based on connectionMode.
 * - Custom mode (string "Custom" or integer 0): uses `host` and `token` (self-host / derived-port).
 * - Cloud mode (string "Cloud" or integer 1): uses the hardcoded cloud URL and the Bearer credential
 *   supplied by `options.readCloudToken` (production: the shared machine credential store via
 *   cli-core's refreshing `MachineCredentialProvider`) — NOT the on-disk `cloudToken`, which the
 *   plugin stopped writing post-T9 (defect E / D11).
 * In Custom mode, `url` and `token` may be undefined if the corresponding config fields are not set.
 * In Cloud mode, `url` is always the hardcoded cloud URL, while `token` is the provided credential and
 * is `undefined` when the user is not logged in — the caller surfaces an actionable "not logged in"
 * error rather than issuing a silent unauthenticated request.
 */
export async function resolveConnectionFromConfig(
  config: UnityConnectionConfig,
  options: ResolveConnectionFromConfigOptions,
): Promise<{
  url: string | undefined;
  token: string | undefined;
}> {
  if (isCloudMode(config)) {
    return { url: CLOUD_SERVER_URL, token: await options.readCloudToken() };
  }

  return { url: config.host, token: config.token };
}
