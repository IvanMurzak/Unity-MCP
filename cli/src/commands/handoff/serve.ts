import { Command } from 'commander';
import * as ui from '../../utils/ui.js';
import { loadHandoffBridgeConfig } from '../../utils/discord-approval.js';
import { startHandoffServer } from '../../utils/handoff-server.js';
import { resolveHandoffProjectPath } from './helpers.js';

interface HandoffServeOptions {
  path?: string;
  envFile?: string;
  host?: string;
  port?: string;
}

export function createHandoffServeCommand(): Command {
  return new Command('serve')
    .description('Run the leader-hosted Discord handoff bridge server')
    .argument('[project-path]', 'Unity project path (defaults to cwd)')
    .option('--path <path>', 'Unity project path when omitted as a positional argument')
    .option('--env-file <path>', 'Optional env file containing UNITY_MCP_HANDOFF_* secrets')
    .option('--host <host>', 'Host interface to bind (default: 127.0.0.1)', '127.0.0.1')
    .option('--port <port>', 'Port to bind (default: UNITY_MCP_HANDOFF_PORT or 8787)')
    .action(async (positionalPath: string | undefined, options: HandoffServeOptions) => {
      try {
        const projectPath = resolveHandoffProjectPath(positionalPath, options);
        const config = loadHandoffBridgeConfig({ envFilePath: options.envFile });
        const port = parsePort(options.port ?? process.env['UNITY_MCP_HANDOFF_PORT'] ?? '8787');
        const host = options.host ?? '127.0.0.1';
        const server = await startHandoffServer({
          projectPath,
          config,
          port,
          host,
        });

        ui.heading('Unity-MCP Handoff Bridge Server');
        ui.label('Project', projectPath);
        ui.label('Host', host);
        ui.label('Port', String(port));
        ui.label('Health', `http://${host}:${port}/healthz`);
        ui.label('Discord endpoint', `http://${host}:${port}/discord/interactions`);
        if (config.envFilePath) {
          ui.label('Env file', config.envFilePath);
        }
        ui.divider();
        ui.success('Leader-hosted handoff bridge is listening. Press Ctrl+C to stop.');

        const shutdown = () => {
          server.close(() => process.exit(0));
        };
        process.once('SIGINT', shutdown);
        process.once('SIGTERM', shutdown);
        await new Promise<void>(() => {
          // keep process alive until signal shutdown
        });
      } catch (err) {
        ui.error((err as Error).message || String(err));
        process.exit(1);
      }
    });
}

function parsePort(rawPort: string): number {
  const value = Number.parseInt(rawPort, 10);
  if (!Number.isFinite(value) || value < 1 || value > 65535) {
    throw new Error(`Invalid handoff bridge port: ${rawPort}`);
  }
  return value;
}
