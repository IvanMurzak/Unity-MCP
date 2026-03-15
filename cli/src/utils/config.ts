import * as fs from 'fs';
import * as path from 'path';
import { generatePortFromDirectory } from './port.js';

const CONFIG_RELATIVE_PATH = 'UserSettings/AI-Game-Developer-Config.json';

export interface McpFeature {
  name: string;
  enabled: boolean;
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
  connectionMode?: string;
  cloudServerUrl?: string;
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
    cloudServerUrl: 'https://ai-game.dev',
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
 * - enableNames: set these to enabled=true
 * - disableNames: set these to enabled=false
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
  const rawFeatures = config[featureType];
  const features: McpFeature[] = Array.isArray(rawFeatures)
    ? rawFeatures.filter(
        (f): f is McpFeature =>
          typeof f === 'object' && f !== null && typeof f.name === 'string' && typeof f.enabled === 'boolean'
      )
    : [];

  if (options.enableAll) {
    for (const f of features) f.enabled = true;
    config[featureType] = features;
    return;
  }

  if (options.disableAll) {
    for (const f of features) f.enabled = false;
    config[featureType] = features;
    return;
  }

  if (options.enableNames) {
    for (const name of options.enableNames) {
      const existing = features.find((f) => f.name === name);
      if (existing) {
        existing.enabled = true;
      } else {
        features.push({ name, enabled: true });
      }
    }
  }

  if (options.disableNames) {
    for (const name of options.disableNames) {
      const existing = features.find((f) => f.name === name);
      if (existing) {
        existing.enabled = false;
      } else {
        features.push({ name, enabled: false });
      }
    }
  }

  config[featureType] = features;
}

/**
 * Resolve the server URL and auth token from a project config based on connectionMode.
 * - Custom mode: uses `host` and `token`
 * - Cloud mode: uses `cloudServerUrl` and `cloudToken`
 * Returns null values when the config or relevant fields are not set.
 */
export function resolveConnectionFromConfig(config: UnityConnectionConfig): {
  url: string | undefined;
  token: string | undefined;
} {
  const isCloud = config.connectionMode === 'Cloud';

  return {
    url: isCloud ? config.cloudServerUrl : config.host,
    token: isCloud ? config.cloudToken : config.token,
  };
}
