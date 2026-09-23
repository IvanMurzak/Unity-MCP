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
import type { McpTransport, SetupMcpSuccess } from '../lib/types.js';

interface SetupMcpCliOptions {
  transport?: string;
  url?: string;
  token?: string;
  oauth?: boolean;
  regenerateKey?: boolean;
  list?: boolean;
  /** commander sets this to `false` when `--no-pin` is passed (defaults to `true`). */
  pin?: boolean;
}

/** Which credential the written config carries — never the key itself, only its server-side id. */
function describeCredential(
  result: Pick<SetupMcpSuccess, 'credential' | 'projectKeyId' | 'projectKeySource'>,
): string {
  switch (result.credential) {
    case 'project-key': {
      const id = result.projectKeyId ? ` ${result.projectKeyId}` : '';
      const source = result.projectKeySource ? ` (${result.projectKeySource})` : '';
      return `project key${id}${source}`;
    }
    case 'token':
      return 'access token (--token)';
    default:
      return 'none (URL-only — the agent signs in itself)';
  }
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
  .option('--token <token>', 'Explicit PAT — written as the Authorization header instead of the project key')
  .option('--oauth', 'Write a URL-only Cloud config (the agent signs in with its own OAuth) instead of the project key')
  .option('--regenerate-key', 'Mint a fresh project key, rewrite the config, and revoke the previous key (Cloud http only)')
  .option('--no-pin', 'Write an unpinned URL / omit the project= arg (default: pin to this project via /mcp/p/<pin>)')
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
        oauth: options.oauth,
        regenerateKey: options.regenerateKey,
        // commander sets `options.pin === false` when `--no-pin` was passed.
        noPin: options.pin === false,
      });

      if (result.kind === 'failure') {
        spinner.error('Failed to write config');
        ui.error(result.error.message);
        process.exit(1);
      }

      // Narrowed: result.kind === 'success' below — `configPath` and
      // `transport` are non-optional.
      if (positionalPath) {
        verbose(`Project path: ${positionalPath}`);
      }
      verbose(`Config file: ${result.configPath}`);

      spinner.success(`${agent.name} configured successfully`);

      console.log('');
      ui.label('Config file', result.configPath);
      ui.label('Transport', result.transport);
      ui.label('Server name', MCP_SERVER_NAME);
      ui.label('Credential', describeCredential(result));

      for (const warning of result.warnings) {
        console.log('');
        ui.warn(warning);
      }
    },
  );
