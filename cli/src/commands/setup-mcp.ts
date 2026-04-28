import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import {
  getAgentById,
  getAgentIds,
  listAgentTable,
  MCP_SERVER_NAME,
} from '../utils/agents.js';
import { setupMcp } from '../lib/setup-mcp.js';
import type { McpTransport } from '../lib/types.js';

interface SetupMcpCliOptions {
  transport?: string;
  url?: string;
  token?: string;
  list?: boolean;
}

function listAgents(): void {
  listAgentTable('Available AI Agents', 'Config Path', (a) => a.configPathDisplay);
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
      options: SetupMcpCliOptions,
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

      // Resolve the agent up-front so we can:
      //   1. Use the display name in user-facing strings (matches the
      //      historical `Configuring <Name> ...` phrasing).
      //   2. Preserve the prior `ui.error(...)` + `ui.info("Available
      //      agent IDs: ...")` split for the unknown-agent error path.
      const agent = getAgentById(agentId);
      if (!agent) {
        ui.error(`Unknown agent: "${agentId}"`);
        ui.info(`Available agent IDs: ${getAgentIds().join(', ')}`);
        process.exit(1);
      }

      const transport = (options.transport ?? 'http') as McpTransport;

      const spinner = ui.startSpinner(
        `Configuring ${agent.name} (${transport})...`,
      );

      const result = await setupMcp({
        agentId,
        unityProjectPath: positionalPath,
        transport,
        url: options.url,
        token: options.token,
      });

      if (!result.success) {
        spinner.error('Failed to write config');
        ui.error(result.error?.message ?? 'Unknown error');
        process.exit(1);
      }

      if (positionalPath) {
        verbose(`Project path: ${positionalPath}`);
      }
      if (result.configPath) {
        verbose(`Config file: ${result.configPath}`);
      }

      spinner.success(`${agent.name} configured successfully`);

      console.log('');
      if (result.configPath) ui.label('Config file', result.configPath);
      if (result.transport) ui.label('Transport', result.transport);
      ui.label('Server name', MCP_SERVER_NAME);

      for (const warning of result.warnings) {
        console.log('');
        ui.warn(warning);
      }
    },
  );
