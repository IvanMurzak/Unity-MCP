import { Command } from 'commander';
import * as fs from 'fs';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { generatePortFromDirectory } from '../utils/port.js';
import { readConfig, resolveConnectionFromConfig } from '../utils/config.js';
import {
  agentRegistry,
  getAgentById,
  getAgentIds,
  resolveServerBinaryPath,
  writeJsonAgentConfig,
  writeTomlAgentConfig,
  MCP_SERVER_NAME,
} from '../utils/agents.js';

interface SetupMcpOptions {
  transport?: string;
  url?: string;
  token?: string;
  list?: boolean;
}

function listAgents(): void {
  ui.heading('Available AI Agents');
  console.log('');
  const maxId = Math.max(...agentRegistry.map((a) => a.id.length));
  for (const agent of agentRegistry) {
    const format = agent.configFormat === 'toml' ? ' (TOML)' : '';
    console.log(`  ${agent.id.padEnd(maxId + 2)} ${agent.name}${format}`);
  }
  console.log('');
}

export const setupMcpCommand = new Command('setup-mcp')
  .description('Write MCP config for an AI agent')
  .argument('[agent-id]', 'Agent to configure (use --list to see all)')
  .argument('[path]', 'Unity project path (defaults to cwd)')
  .option(
    '--transport <transport>',
    'Transport method: stdio or http (default: http)',
    'http',
  )
  .option('--url <url>', 'Server URL override (for http transport)')
  .option('--token <token>', 'Auth token override')
  .option('--list', 'List all available agent IDs')
  .action(
    async (
      agentId: string | undefined,
      positionalPath: string | undefined,
      options: SetupMcpOptions,
    ) => {
      if (options.list) {
        listAgents();
        return;
      }

      if (!agentId) {
        ui.error('Missing required argument: agent-id');
        ui.info(`Available agent IDs: ${getAgentIds().join(', ')}`);
        process.exit(1);
      }

      const agent = getAgentById(agentId);
      if (!agent) {
        ui.error(`Unknown agent: "${agentId}"`);
        ui.info(`Available agent IDs: ${getAgentIds().join(', ')}`);
        process.exit(1);
      }

      const transport = options.transport ?? 'http';
      if (transport !== 'stdio' && transport !== 'http') {
        ui.error(`Invalid transport: "${transport}". Must be "stdio" or "http".`);
        process.exit(1);
      }

      // Resolve project path
      const projectPath = path.resolve(positionalPath ?? process.cwd());
      if (positionalPath && !fs.existsSync(projectPath)) {
        ui.error(`Project path does not exist: ${projectPath}`);
        process.exit(1);
      }
      verbose(`Project path: ${projectPath}`);

      // Read project config for port, timeout, auth, token
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
      const token = options.token ?? fromConfig.token ?? '';
      const authRequired = auth === 'required';

      // Resolve server binary path
      const serverPath = resolveServerBinaryPath(projectPath).replace(
        /\\/g,
        '/',
      );
      verbose(`Server binary: ${serverPath}`);

      // Resolve URL for HTTP
      let serverUrl: string;
      if (options.url) {
        serverUrl = options.url.replace(/\/$/, '');
      } else if (fromConfig.url) {
        serverUrl = fromConfig.url.replace(/\/$/, '');
      } else {
        serverUrl = `http://localhost:${port}`;
      }

      // Get config path and build properties
      const configPath = agent.getConfigPath(projectPath);
      verbose(`Config file: ${configPath}`);

      let props: Record<string, unknown>;
      let removeKeys: string[];

      if (transport === 'stdio') {
        props = agent.getStdioProps(serverPath, port, timeout, auth, token);
        removeKeys = agent.stdioRemoveKeys;
      } else {
        props = agent.getHttpProps(serverUrl, token, authRequired);
        removeKeys = agent.httpRemoveKeys;
      }

      // Write config
      const spinner = ui.startSpinner(
        `Configuring ${agent.name} (${transport})...`,
      );

      try {
        if (agent.configFormat === 'toml') {
          writeTomlAgentConfig(
            configPath,
            agent.bodyPath,
            MCP_SERVER_NAME,
            props,
            removeKeys,
          );
        } else {
          writeJsonAgentConfig(
            configPath,
            agent.bodyPath,
            MCP_SERVER_NAME,
            props,
            removeKeys,
          );
        }

        spinner.success(`${agent.name} configured successfully`);

        console.log('');
        ui.label('Config file', configPath);
        ui.label('Transport', transport);
        ui.label('Server name', MCP_SERVER_NAME);

        if (transport === 'stdio' && !fs.existsSync(serverPath.replace(/\//g, path.sep))) {
          console.log('');
          ui.warn(
            'Server binary not found. Open Unity with the MCP plugin to download it automatically.',
          );
        }
      } catch (err) {
        spinner.error('Failed to write config');
        ui.error(
          err instanceof Error ? err.message : String(err),
        );
        process.exit(1);
      }
    },
  );
