import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { generatePortFromDirectory } from '../utils/port.js';
import { findUnityProcess } from '../utils/unity-process.js';

const PING_ENDPOINT = '/api/system-tools/ping';

interface StatusOptions {
  path?: string;
  url?: string;
  token?: string;
  timeout?: string;
}

/**
 * Probe a server endpoint, return parsed JSON on success or null on failure.
 */
async function probeServer(
  baseUrl: string,
  headers: Record<string, string>,
  timeoutMs: number,
): Promise<{ ok: true; data: unknown } | { ok: false; reason: string }> {
  const endpoint = `${baseUrl}${PING_ENDPOINT}`;
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(endpoint, {
      method: 'POST',
      headers,
      body: '{}',
      signal: controller.signal,
    });

    const text = await response.text();
    if (response.ok) {
      try {
        return { ok: true, data: JSON.parse(text) };
      } catch {
        return { ok: true, data: text };
      }
    }
    return { ok: false, reason: `HTTP ${response.status}` };
  } catch (err) {
    const cause = err instanceof Error && 'cause' in err ? (err.cause as Error & { code?: string }) : null;
    const code = cause?.code ?? '';
    const isAbort = err instanceof Error && err.name === 'AbortError';

    if (isAbort) return { ok: false, reason: 'timed out' };
    if (code === 'ECONNREFUSED') return { ok: false, reason: 'connection refused' };
    if (code === 'ECONNRESET') return { ok: false, reason: 'connection reset' };
    return { ok: false, reason: err instanceof Error ? err.message : String(err) };
  } finally {
    clearTimeout(timer);
  }
}

export const statusCommand = new Command('status')
  .description('Check Unity Editor and MCP server connection status')
  .argument('[path]', 'Unity project path (used for config and auto port detection)')
  .option('--path <path>', 'Unity project path (config and auto port detection)')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .option('--timeout <ms>', 'Probe timeout in milliseconds (default: 5000)', '5000')
  .action(async (positionalPath: string | undefined, options: StatusOptions) => {
    const projectPath = resolveAndValidateProjectPath(positionalPath, options);
    const { url: configUrl, token } = resolveConnection(projectPath, options);
    const localPort = generatePortFromDirectory(projectPath);
    const localUrl = `http://localhost:${localPort}`;

    const timeoutMs = parseInt(options.timeout ?? '5000', 10);

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    ui.heading('Unity-MCP Status');
    ui.label('Project', projectPath);
    ui.divider();

    // 1. Unity process detection
    ui.heading('Unity Editor Process');
    const proc = findUnityProcess(projectPath);
    if (proc) {
      ui.success(`Unity is running (PID: ${proc.pid})`);
    } else {
      ui.warn('Unity is not running with this project');
    }

    // 2. Local server check
    ui.heading('Local MCP Server');
    ui.label('URL', localUrl);

    const localSpinner = ui.startSpinner(`Probing ${localUrl}...`);
    const localResult = await probeServer(localUrl, headers, timeoutMs);
    if (localResult.ok) {
      localSpinner.success('Connected');
      verbose(`Local server response: ${JSON.stringify(localResult.data)}`);
    } else {
      localSpinner.error(`Not available (${localResult.reason})`);
    }

    // 3. Config-resolved server check (if different from local)
    if (configUrl !== localUrl) {
      ui.heading('Config Server');
      ui.label('URL', configUrl);

      const configSpinner = ui.startSpinner(`Probing ${configUrl}...`);
      const configResult = await probeServer(configUrl, headers, timeoutMs);
      if (configResult.ok) {
        configSpinner.success('Connected');
        verbose(`Config server response: ${JSON.stringify(configResult.data)}`);
      } else {
        configSpinner.error(`Not available (${configResult.reason})`);
      }
    }

    ui.divider();

    // Summary
    const localOk = localResult.ok;
    const configOk = configUrl !== localUrl ? (await probeServer(configUrl, headers, timeoutMs)).ok : localOk;

    if (localOk || configOk) {
      ui.success('MCP server is reachable — ready for tool calls');
      process.exit(0);
    } else if (proc) {
      ui.warn('Unity is running but MCP server is not responding yet');
      process.exit(1);
    } else {
      ui.error('Unity is not running and MCP server is not reachable');
      process.exit(1);
    }
  });
