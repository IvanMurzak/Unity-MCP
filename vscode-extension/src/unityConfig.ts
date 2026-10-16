import * as path from 'node:path';
import { promises as fs } from 'node:fs';
import { pathExists, toErrorMessage } from './utils';

const CLOUD_SERVER_URL = 'https://ai-game.dev/mcp';

export interface UnityMcpProjectConfig {
  exists: boolean;
  ready: boolean;
  host?: string;
  token?: string;
  authOption?: 'none' | 'required';
  transport?: 'streamableHttp' | 'stdio';
  keepConnected?: boolean;
  connectionMode: 'custom' | 'cloud';
  warnings: string[];
}

export async function readUnityMcpProjectConfig(
  workspacePath: string,
): Promise<UnityMcpProjectConfig> {
  const configPath = path.join(
    workspacePath,
    'UserSettings',
    'AI-Game-Developer-Config.json',
  );

  if (!(await pathExists(configPath))) {
    return {
      exists: false,
      ready: false,
      connectionMode: 'custom',
      warnings: [],
    };
  }

  try {
    const raw = await fs.readFile(configPath, 'utf8');
    const parsed = JSON.parse(raw) as Record<string, unknown>;
    const parsedConnectionMode = parseConnectionMode(parsed['connectionMode']);
    const connectionMode = parsedConnectionMode ?? 'custom';
    const authOption = parseAuthOption(parsed['authOption']);
    const transport = parseTransport(parsed['transportMethod']);
    const host = resolveHost(connectionMode, parsed);
    const token = resolveToken(connectionMode, parsed);
    const warnings = buildConfigWarnings({
      hasConnectionModeField: 'connectionMode' in parsed,
      connectionModeIsValid: parsedConnectionMode !== undefined,
      connectionMode,
      host,
      token,
      authOption,
      transport,
    });

    return {
      exists: true,
      ready: warnings.length === 0,
      host,
      token,
      authOption,
      transport,
      keepConnected: typeof parsed['keepConnected'] === 'boolean'
        ? parsed['keepConnected']
        : undefined,
      connectionMode,
      warnings,
    };
  } catch (error) {
    return {
      exists: true,
      ready: false,
      connectionMode: 'custom',
      warnings: [
        `Could not parse UserSettings/AI-Game-Developer-Config.json: ${toErrorMessage(error)}`,
      ],
    };
  }
}

function parseAuthOption(value: unknown): 'none' | 'required' | undefined {
  return value === 'none' || value === 'required' ? value : undefined;
}

function parseTransport(value: unknown): 'streamableHttp' | 'stdio' | undefined {
  return value === 'stdio' || value === 'streamableHttp' ? value : undefined;
}

function parseConnectionMode(value: unknown): 'custom' | 'cloud' | undefined {
  if (value === undefined) {
    return undefined;
  }

  if (value === 'Cloud' || value === 1) {
    return 'cloud';
  }

  if (value === 'Custom' || value === 0) {
    return 'custom';
  }

  return undefined;
}

function resolveHost(
  connectionMode: 'custom' | 'cloud',
  parsed: Record<string, unknown>,
): string | undefined {
  if (connectionMode === 'cloud') {
    return CLOUD_SERVER_URL;
  }

  return readNonEmptyString(parsed['host']);
}

function resolveToken(
  connectionMode: 'custom' | 'cloud',
  parsed: Record<string, unknown>,
): string | undefined {
  const tokenKey = connectionMode === 'cloud' ? 'cloudToken' : 'token';
  return readNonEmptyString(parsed[tokenKey]);
}

function buildConfigWarnings(input: {
  hasConnectionModeField: boolean;
  connectionModeIsValid: boolean;
  connectionMode: 'custom' | 'cloud';
  host?: string;
  token?: string;
  authOption?: 'none' | 'required';
  transport?: 'streamableHttp' | 'stdio';
}): string[] {
  const warnings: string[] = [];

  if (input.hasConnectionModeField && !input.connectionModeIsValid) {
    warnings.push(
      'UserSettings/AI-Game-Developer-Config.json contains an unsupported connectionMode. Expected "Custom", "Cloud", 0, or 1.',
    );
  }

  if (input.authOption === undefined) {
    warnings.push(
      'UserSettings/AI-Game-Developer-Config.json is missing a supported authOption. Expected "none" or "required".',
    );
  }

  if (input.transport === undefined) {
    warnings.push(
      'UserSettings/AI-Game-Developer-Config.json is missing a supported transportMethod. Expected "streamableHttp" or "stdio".',
    );
  }

  if (input.host === undefined) {
    warnings.push(
      input.connectionMode === 'cloud'
        ? 'UserSettings/AI-Game-Developer-Config.json is missing a cloud connection URL.'
        : 'UserSettings/AI-Game-Developer-Config.json is missing a host URL.',
    );
  }

  if (input.authOption === 'required' && input.token === undefined) {
    warnings.push(
      input.connectionMode === 'cloud'
        ? 'UserSettings/AI-Game-Developer-Config.json requires a cloudToken for authenticated MCP launch.'
        : 'UserSettings/AI-Game-Developer-Config.json requires a token for authenticated MCP launch.',
    );
  }

  return warnings;
}

function readNonEmptyString(value: unknown): string | undefined {
  return typeof value === 'string' && value.trim().length > 0 ? value : undefined;
}
