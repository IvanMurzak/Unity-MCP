import * as fs from 'fs';
import * as path from 'path';
import {
  setupMcp as coreSetupMcp,
  unityAdapter,
  getAgentIds,
} from '@baizor/gamedev-cli-core';
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
 * Write MCP configuration for the given AI agent, so it can talk to the Unity MCP hub.
 *
 * This is now a thin adapter over `@baizor/gamedev-cli-core`'s `setupMcp` (auth-fixes T4/M7/M8):
 *   - the http URL is **pinned by default** to `<base>/mcp/p/<pin-v2>` and the stdio config carries a
 *     `project=<pin>` arg, so the config routes strictly to this project's engine instance even when
 *     the account has several (`--no-pin` is the escape hatch);
 *   - the config is **credential-free by default** — a static `Authorization: Bearer` header (http)
 *     or `token=` arg (stdio) is written ONLY on an explicit `--token` PAT opt-in, so an OAuth-capable
 *     client authorizes natively (RFC 9728) instead of being suppressed by a static header;
 *   - the config bytes are written by cli-core's golden-vector-gated JSON/TOML writers, so `setup-mcp`
 *     output matches the Unity Editor's Configure output byte-for-byte.
 *
 * Library-safe: no stdout noise, no `process.exit`, no throws past the public boundary. Narrow the
 * returned `SetupMcpResult` with `result.kind === 'success'` / `=== 'failure'`.
 */
export async function setupMcp(opts: SetupMcpOptions): Promise<SetupMcpResult> {
  // This wrapper never accumulates its own warnings/next-steps — cli-core owns the warning channel
  // (`result.warnings`), and `nextSteps` is always empty here. Both fields are written as literals
  // on each return to satisfy the `SetupMcpResult` shape.
  try {
    // Resolve project path. If the caller supplied one explicitly it must exist; otherwise cli-core
    // falls back to cwd (matching the CLI behaviour and closing the "path required" half of B1).
    let projectPath: string | undefined;
    if (opts.unityProjectPath) {
      const validated = requireExistingPath(opts.unityProjectPath);
      if (!validated.ok) {
        return { kind: 'failure', success: false, warnings: [], nextSteps: [], error: validated.error };
      }
      projectPath = validated.projectPath;
    }

    const transport = opts.transport ?? 'http';
    if (!isValidTransport(transport)) {
      return {
        kind: 'failure',
        success: false,
        warnings: [],
        nextSteps: [],
        error: new Error(`Invalid transport: "${transport}". Must be "stdio" or "http".`),
      };
    }

    const result = coreSetupMcp({
      adapter: unityAdapter,
      agentId: opts.agentId,
      transport,
      projectPath,
      url: opts.url,
      token: opts.token,
      noPin: opts.noPin === true,
    });

    if (result.kind === 'failure') {
      return {
        kind: 'failure',
        success: false,
        warnings: result.warnings,
        nextSteps: [],
        error: result.error,
      };
    }

    // Preserve the historical stdio-server-missing warning (cli-core does not emit it): the plugin
    // downloads the server binary when Unity opens, so a stdio config written before that is valid
    // but not yet runnable.
    if (transport === 'stdio') {
      const serverPath = unityAdapter.serverBinaryPath(projectPath ?? path.resolve(process.cwd()));
      if (!fs.existsSync(serverPath)) {
        result.warnings.push(
          'Server binary not found. Open Unity with the MCP plugin to download it automatically.',
        );
      }
    }

    return {
      kind: 'success',
      success: true,
      agentId: result.agentId,
      configPath: result.configPath,
      transport: result.transport,
      warnings: result.warnings,
      nextSteps: [],
    };
  } catch (err: unknown) {
    return {
      kind: 'failure',
      success: false,
      warnings: [],
      nextSteps: [],
      error: err instanceof Error ? err : new Error(String(err)),
    };
  }
}

/**
 * List every agent id known to `setupMcp`. Useful for consumer UIs that want to render a picker.
 */
export function listAgentIds(): string[] {
  return getAgentIds();
}
