import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { generatePortFromDirectory } from '../utils/port.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { probe, type ProbeResult } from '../utils/probe.js';

interface StatusOptions {
  path?: string;
  url?: string;
  token?: string;
  timeout?: string;
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

    if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
      ui.error(`Invalid --timeout value: "${options.timeout}". Must be a positive integer (milliseconds).`);
      process.exit(1);
    }

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
    const localResult = await probe(localUrl, headers, timeoutMs);
    if (localResult.ok) {
      localSpinner.success('Connected');
      verbose(`Local server response: ${JSON.stringify(localResult.data)}`);
    } else {
      localSpinner.error(`Not available (${localResult.reason})`);
    }

    // 3. Config-resolved server check (if different from local)
    let configResult: ProbeResult | null = null;
    if (configUrl !== localUrl) {
      ui.heading('Config Server');
      ui.label('URL', configUrl);

      const configSpinner = ui.startSpinner(`Probing ${configUrl}...`);
      configResult = await probe(configUrl, headers, timeoutMs);
      if (configResult.ok) {
        configSpinner.success('Connected');
        verbose(`Config server response: ${JSON.stringify(configResult.data)}`);
      } else {
        configSpinner.error(`Not available (${configResult.reason})`);
      }
    }

    ui.divider();

    // Summary — reuse probe results, no duplicate calls
    const localOk = localResult.ok;
    const configOk = configResult ? configResult.ok : localOk;

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
