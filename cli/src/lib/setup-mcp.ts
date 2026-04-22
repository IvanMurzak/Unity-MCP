import * as fs from 'fs';
import * as path from 'path';
import { generatePortFromDirectory } from '../utils/port.js';
import { readConfig, resolveConnectionFromConfig } from '../utils/config.js';
import {
  getAgentById,
  getAgentIds,
  resolveServerBinaryPath,
  writeJsonAgentConfig,
  writeTomlAgentConfig,
  MCP_SERVER_NAME,
} from '../utils/agents.js';
import { emitProgress } from './progress.js';
import type {
  SetupMcpOptions,
  SetupMcpResult,
  McpTransport,
} from './types.js';

function isValidTransport(t: string | undefined): t is McpTransport {
  return t === 'stdio' || t === 'http';
}

/**
 * Write MCP configuration for the given AI agent, so it can talk to
 * Unity-MCP. Library-safe: no stdout noise, no process.exit, no throws
 * past the public boundary.
 */
export async function setupMcp(opts: SetupMcpOptions): Promise<SetupMcpResult> {
  const warnings: string[] = [];
  const nextSteps: string[] = [];

  try {
    if (!opts || typeof opts.agentId !== 'string' || opts.agentId.length === 0) {
      return {
        success: false,
        warnings,
        nextSteps,
        error: new Error(
          `agentId is required. Available agent IDs: ${getAgentIds().join(', ')}`,
        ),
      };
    }

    const agent = getAgentById(opts.agentId);
    if (!agent) {
      return {
        success: false,
        warnings,
        nextSteps,
        error: new Error(
          `Unknown agent: "${opts.agentId}". Available agent IDs: ${getAgentIds().join(', ')}`,
        ),
      };
    }

    const transport: McpTransport = opts.transport ?? 'http';
    if (!isValidTransport(transport)) {
      return {
        success: false,
        warnings,
        nextSteps,
        error: new Error(`Invalid transport: "${transport}". Must be "stdio" or "http".`),
      };
    }

    // Resolve project path (defaults to cwd, matching CLI behaviour)
    const projectPath = path.resolve(opts.unityProjectPath ?? process.cwd());
    if (opts.unityProjectPath && !fs.existsSync(projectPath)) {
      return {
        success: false,
        warnings,
        nextSteps,
        error: new Error(`Project path does not exist: ${projectPath}`),
      };
    }

    emitProgress(opts.onProgress, {
      phase: 'start',
      message: `Configuring ${agent.name} (${transport}) for ${projectPath}`,
    });

    const config = readConfig(projectPath);
    const fromConfig = config
      ? resolveConnectionFromConfig(config)
      : { url: undefined, token: undefined };

    const port = (() => {
      if (!config?.host) return generatePortFromDirectory(projectPath);
      try {
        return parseInt(new URL(config.host).port, 10) || generatePortFromDirectory(projectPath);
      } catch {
        return generatePortFromDirectory(projectPath);
      }
    })();

    const timeout = (config?.timeoutMs as number) ?? 10000;
    const auth = (config?.authOption as string) ?? 'none';
    const token = opts.token ?? fromConfig.token ?? '';
    const authRequired = auth === 'required';

    const serverPath = resolveServerBinaryPath(projectPath).replace(/\\/g, '/');

    // Resolve URL for HTTP
    let serverUrl: string;
    if (opts.url) {
      serverUrl = opts.url.replace(/\/$/, '');
    } else if (fromConfig.url) {
      serverUrl = fromConfig.url.replace(/\/$/, '');
    } else {
      serverUrl = `http://localhost:${port}`;
    }

    const configPath = agent.getConfigPath(projectPath);

    let props: Record<string, unknown>;
    let removeKeys: string[];

    if (transport === 'stdio') {
      props = agent.getStdioProps(serverPath, port, timeout, auth, token);
      removeKeys = agent.stdioRemoveKeys;
    } else {
      props = agent.getHttpProps(serverUrl, token, authRequired);
      removeKeys = agent.httpRemoveKeys;
    }

    if (agent.configFormat === 'toml') {
      writeTomlAgentConfig(configPath, agent.bodyPath, MCP_SERVER_NAME, props, removeKeys);
    } else {
      writeJsonAgentConfig(configPath, agent.bodyPath, MCP_SERVER_NAME, props, removeKeys);
    }

    emitProgress(opts.onProgress, {
      phase: 'manifest-patched',
      message: `Wrote ${configPath}`,
      manifestPath: configPath,
    });

    if (transport === 'stdio' && !fs.existsSync(serverPath.replace(/\//g, path.sep))) {
      warnings.push(
        'Server binary not found. Open Unity with the MCP plugin to download it automatically.',
      );
    }

    emitProgress(opts.onProgress, { phase: 'done', message: `${agent.name} configured successfully.` });

    return {
      success: true,
      agentId: agent.id,
      configPath,
      transport,
      warnings,
      nextSteps,
    };
  } catch (err: unknown) {
    return {
      success: false,
      warnings,
      nextSteps,
      error: err instanceof Error ? err : new Error(String(err)),
    };
  }
}

/**
 * List every agent id known to the `setupMcp` function. Useful for
 * consumer UIs that want to render a picker.
 */
export function listAgentIds(): string[] {
  return getAgentIds();
}
