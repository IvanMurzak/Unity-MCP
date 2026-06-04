import * as path from 'node:path';
import { promises as fs } from 'node:fs';
import { pathExists, toErrorMessage } from './utils';

export interface UnityMcpProjectConfig {
  exists: boolean;
  host?: string;
  token?: string;
  authOption?: 'none' | 'required';
  transport?: 'streamableHttp' | 'stdio';
  keepConnected?: boolean;
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
      warnings: [],
    };
  }

  try {
    const raw = await fs.readFile(configPath, 'utf8');
    const parsed = JSON.parse(raw) as Record<string, unknown>;

    return {
      exists: true,
      host: typeof parsed['host'] === 'string' ? parsed['host'] : undefined,
      token: typeof parsed['token'] === 'string' ? parsed['token'] : undefined,
      authOption: parseAuthOption(parsed['authOption']),
      transport: parseTransport(parsed['transportMethod']),
      keepConnected: typeof parsed['keepConnected'] === 'boolean'
        ? parsed['keepConnected']
        : undefined,
      warnings: [],
    };
  } catch (error) {
    return {
      exists: true,
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
