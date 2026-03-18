import { Command } from 'commander';
import * as fs from 'fs';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { generatePortFromDirectory } from '../utils/port.js';
import { readConfig, resolveConnectionFromConfig } from '../utils/config.js';
import { agentRegistry, getAgentById, getAgentIds } from '../utils/agents.js';

interface SetupSkillsOptions {
  url?: string;
  token?: string;
  list?: boolean;
}

function listAgentsWithSkills(): void {
  ui.heading('AI Agents — Skills Support');
  console.log('');
  const maxId = Math.max(...agentRegistry.map((a) => a.id.length));
  for (const agent of agentRegistry) {
    const skills = agent.skillsPath ? `skills: ${agent.skillsPath}` : 'no skills support';
    const badge = agent.skillsPath ? '\u2714' : '\u2716';
    console.log(`  ${badge} ${agent.id.padEnd(maxId + 2)} ${skills}`);
  }
  console.log('');
}

export const setupSkillsCommand = new Command('setup-skills')
  .description('Generate skill files for an AI agent')
  .argument('[agent-id]', 'Agent to generate skills for (use --list to see all)')
  .argument('[path]', 'Unity project path (defaults to cwd)')
  .option('--url <url>', 'Server URL override')
  .option('--token <token>', 'Auth token override')
  .option('--list', 'List all agents with skills support status')
  .action(
    async (
      agentId: string | undefined,
      positionalPath: string | undefined,
      options: SetupSkillsOptions,
    ) => {
      if (options.list) {
        listAgentsWithSkills();
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

      if (!agent.skillsPath) {
        ui.error(`Agent "${agent.name}" does not support skills.`);
        process.exit(1);
      }

      // Resolve project path
      const projectPath = path.resolve(positionalPath ?? process.cwd());
      if (positionalPath && !fs.existsSync(projectPath)) {
        ui.error(`Project path does not exist: ${projectPath}`);
        process.exit(1);
      }
      verbose(`Project path: ${projectPath}`);

      // Resolve skills path (absolute)
      const skillsPath = path.join(projectPath, agent.skillsPath);
      verbose(`Skills path: ${skillsPath}`);

      // Resolve server connection
      const config = readConfig(projectPath);
      const fromConfig = config
        ? resolveConnectionFromConfig(config)
        : { url: undefined, token: undefined };

      let serverUrl: string;
      if (options.url) {
        serverUrl = options.url.replace(/\/$/, '');
      } else if (fromConfig.url) {
        serverUrl = fromConfig.url.replace(/\/$/, '');
      } else {
        const port = generatePortFromDirectory(projectPath);
        serverUrl = `http://localhost:${port}`;
      }

      const token = options.token ?? fromConfig.token;

      // Call the MCP server to generate skills
      const endpoint = `${serverUrl}/api/tools/skills-generate`;
      verbose(`Endpoint: ${endpoint}`);

      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
      };
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const body = JSON.stringify({ skillsPath });

      ui.heading('Generate Skills');
      ui.label('Agent', agent.name);
      ui.label('Skills path', skillsPath);
      ui.label('Server', serverUrl);
      ui.divider();

      const spinner = ui.startSpinner(
        `Generating skills for ${agent.name}...`,
      );

      const controller = new AbortController();
      const fetchTimeout = setTimeout(() => controller.abort(), 30000);

      try {
        const response = await fetch(endpoint, {
          method: 'POST',
          headers,
          body,
          signal: controller.signal,
        });

        if (!response.ok) {
          spinner.stop();
          const text = await response.text();

          if (response.status === 404) {
            ui.error(
              'The skills-generate tool is not yet available. Please update the Unity-MCP plugin to the latest version.',
            );
          } else {
            ui.error(`HTTP ${response.status}: ${response.statusText}`);
            if (text) {
              ui.info(text);
            }
          }
          process.exit(1);
        }

        spinner.success(`Skills generated for ${agent.name}`);
        ui.label('Output path', skillsPath);
      } catch (err) {
        spinner.stop();
        const message =
          err instanceof Error ? err.message : String(err);
        const isTimeout =
          err instanceof Error && err.name === 'AbortError';
        const isConnectionRefused =
          message.includes('ECONNREFUSED') || message.includes('fetch failed');

        if (isTimeout) {
          ui.error('Request timed out after 30 seconds.');
        } else if (isConnectionRefused) {
          ui.error(
            'MCP Server is not running. Please start Unity Editor with the Unity-MCP plugin installed, then retry.',
          );
        } else {
          ui.error(`Failed to generate skills: ${message}`);
        }
        process.exit(1);
      } finally {
        clearTimeout(fetchTimeout);
      }
    },
  );
