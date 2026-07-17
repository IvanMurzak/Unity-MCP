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
import { requireExistingPath } from './validation.js';
import type {
  SetupMcpOptions,
  SetupMcpResult,
  McpTransport,
} from './types.js';

function isValidTransport(t: string | undefined): t is McpTransport {
  return t === 'stdio' || t === 'http';
}

/**
 * Extract a port from a `host` URL string, falling back to the
 * project-directory-derived deterministic port on any parse failure.
 */
function portFromHost(host: string | undefined, projectPath: string): number {
  if (!host) return generatePortFromDirectory(projectPath);
  try {
    return parseInt(new URL(host).port, 10) || generatePortFromDirectory(projectPath);
  } catch {
    return generatePortFromDirectory(projectPath);
  }
}

/**
 * True when `configPath` resolves to a location at or under `projectRoot`
 * (separator- and case-insensitive) — i.e. a VCS-visible project-scoped file.
 * Mirrors the b6 configurator's `IsProjectScopedPath`, used to decide whether a
 * written access token would land in a committable file (design 03 Flow C).
 */
function isProjectScoped(configPath: string, projectRoot: string): boolean {
  const norm = (p: string): string =>
    path.resolve(p).replace(/\\/g, '/').replace(/\/+$/, '').toLowerCase();
  const root = norm(projectRoot);
  const target = norm(configPath);
  return target === root || target.startsWith(root + '/');
}

/**
 * Write MCP configuration for the given AI agent, so it can talk to
 * Unity-MCP. Library-safe: no stdout noise, no process.exit, no throws
 * past the public boundary.
 *
 * The returned `SetupMcpResult` is a discriminated union — narrow with
 * `result.kind === 'success'` to access `agentId` / `configPath` /
 * `transport`, or `result.kind === 'failure'` to access `error`.
 */
export async function setupMcp(opts: SetupMcpOptions): Promise<SetupMcpResult> {
  const warnings: string[] = [];
  const nextSteps: string[] = [];

  try {
    if (!opts || typeof opts.agentId !== 'string' || opts.agentId.length === 0) {
      return {
        kind: 'failure',
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
        kind: 'failure',
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
        kind: 'failure',
        success: false,
        warnings,
        nextSteps,
        error: new Error(`Invalid transport: "${transport}". Must be "stdio" or "http".`),
      };
    }

    // Resolve project path. If the caller supplied one explicitly it
    // must exist; otherwise fall back to cwd (matches CLI behaviour).
    let projectPath: string;
    if (opts.unityProjectPath) {
      const validated = requireExistingPath(opts.unityProjectPath);
      if (!validated.ok) {
        return { kind: 'failure', success: false, warnings, nextSteps, error: validated.error };
      }
      projectPath = validated.projectPath;
    } else {
      projectPath = path.resolve(process.cwd());
    }

    emitProgress(opts.onProgress, {
      phase: 'start',
      message: `Configuring ${agent.name} (${transport}) for ${projectPath}`,
    });

    const config = readConfig(projectPath);
    const fromConfig = config
      ? resolveConnectionFromConfig(config)
      : { url: undefined, token: undefined };

    const port = portFromHost(config?.host, projectPath);

    const timeout = (config?.timeoutMs as number) ?? 10000;
    const auth = (config?.authOption as string) ?? 'none';

    // Explicit PAT opt-in (design 03 Flow C): a token the caller passed on the
    // command line (`--token`). Only an explicit opt-in — never a token merely
    // resolved from the project config — places a static credential in the file.
    const patOptIn = typeof opts.token === 'string' && opts.token.length > 0;
    const token = opts.token ?? fromConfig.token ?? '';

    // b6 credential policy (design decision D11): the DEFAULT http config is
    // credential-free. OAuth-capable clients perform native RFC 9728 OAuth
    // against the URL (Flow A), so a static `Authorization: Bearer` header both
    // FAILS (the hosted ai-game.dev/mcp endpoint 401s the token) AND suppresses
    // the client's own OAuth handshake ("OAuth fallback is disabled when
    // headers.Authorization is set"). A static header is written ONLY for an
    // explicit PAT opt-in (Flow C) or a client that cannot do MCP OAuth
    // (`supportsOAuth === false`) — never automatically for an OAuth-capable
    // client, regardless of the server's `authRequired` setting.
    const supportsOAuth = agent.supportsOAuth !== false;
    const emitAuthHeader = token.length > 0 && (patOptIn || !supportsOAuth);

    const serverPath = resolveServerBinaryPath(projectPath).replace(/\\/g, '/');

    // Resolve URL for HTTP — explicit override, then config, then
    // deterministic localhost fallback. Trailing slash stripped.
    const serverUrl = (opts.url ?? fromConfig.url ?? `http://localhost:${port}`).replace(/\/$/, '');

    const configPath = agent.getConfigPath(projectPath);

    let props: Record<string, unknown>;
    let removeKeys: string[];

    if (transport === 'stdio') {
      props = agent.getStdioProps(serverPath, port, timeout, auth, token);
      removeKeys = agent.stdioRemoveKeys;
    } else {
      props = agent.getHttpProps(serverUrl, token, emitAuthHeader);
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

    // Flow C credential-placement rule (design 03): a static access token
    // written into a project-scoped config file is VCS-visible. Warn so the
    // user prefers an env-var / user-scope placement for the credential.
    if (transport === 'http' && emitAuthHeader && isProjectScoped(configPath, projectPath)) {
      warnings.push(
        `Wrote an access token into project-scoped config file "${configPath}" — it is under the project root and may be committed to version control. Prefer an env-var or user-scope placement for the access token (design 03 Flow C).`,
      );
    }

    if (transport === 'stdio' && !fs.existsSync(serverPath.replace(/\//g, path.sep))) {
      warnings.push(
        'Server binary not found. Open Unity with the MCP plugin to download it automatically.',
      );
    }

    emitProgress(opts.onProgress, { phase: 'done', message: `${agent.name} configured successfully.` });

    return {
      kind: 'success',
      success: true,
      agentId: agent.id,
      configPath,
      transport,
      warnings,
      nextSteps,
    };
  } catch (err: unknown) {
    return {
      kind: 'failure',
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
