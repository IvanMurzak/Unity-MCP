import { Command } from 'commander';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { getAgentById, getAgentIds, listAgentTable } from '../utils/agents.js';
import { readConfig, isCloudMode, writeConfig, CLOUD_SERVER_BASE_URL } from '../utils/config.js';
import { deviceAuthFlow } from '../utils/auth.js';
import { openBrowser } from '../utils/browser.js';

interface SetupSkillsOptions {
  url?: string;
  token?: string;
  list?: boolean;
  timeout?: string;
}

function listAgentsWithSkills(): void {
  listAgentTable('AI Agents \u2014 Skills Support', 'Skills Path', (a) => a.skillsPath ?? '\u2014');
}

/**
 * POST to the skill-generate endpoint and return the response.
 */
async function callGenerateSkills(
  endpoint: string,
  token: string | undefined,
  body: string,
  timeoutMs: number,
): Promise<Response> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const controller = new AbortController();
  const fetchTimeout = setTimeout(() => controller.abort(), timeoutMs);

  try {
    return await fetch(endpoint, {
      method: 'POST',
      headers,
      body,
      signal: controller.signal,
    });
  } finally {
    clearTimeout(fetchTimeout);
  }
}

/**
 * Run the inline cloud login flow. Returns the new token on success, or null on failure.
 */
async function inlineCloudLogin(projectPath: string): Promise<string | null> {
  ui.info('Cloud authentication required. Starting login...');
  console.log();

  let spinner: ReturnType<typeof ui.startSpinner> | undefined;

  try {
    const result = await deviceAuthFlow(
      CLOUD_SERVER_BASE_URL,
      'Unity-MCP CLI',
      {
        onUserCode: (userCode, verificationUrl) => {
          ui.info('Open this URL to authorize:');
          console.log();
          console.log(`  ${verificationUrl}`);
          console.log();
          ui.label('Code', userCode);
          openBrowser(verificationUrl);
        },
        onPolling: () => {
          spinner = ui.startSpinner('Waiting for authorization...');
        },
      },
    );

    if (result.success) {
      spinner?.success('Authorized');

      const config = readConfig(projectPath) ?? {};
      const updatedConfig = {
        ...config,
        cloudToken: result.accessToken,
        connectionMode: 'Cloud' as const,
      };
      writeConfig(projectPath, updatedConfig);
      ui.success('Authentication complete. Cloud token saved.');
      console.log();

      return result.accessToken;
    }

    spinner?.stop();
    ui.error(result.message);
    return null;
  } catch (err) {
    spinner?.stop();
    const message = err instanceof Error ? err.message : String(err);
    ui.error(`Authentication failed: ${message}`);
    return null;
  }
}

export const setupSkillsCommand = new Command('setup-skills')
  .description('Generate skill files for an AI agent')
  .argument('[agent-id]', 'Agent to generate skills for (use --list to see all)')
  .argument('[path]', 'Unity project path (defaults to cwd)')
  .option('--url <url>', 'Server URL override')
  .option('--token <token>', 'Auth token override')
  .option('--list', 'List all agents with skills support status')
  .option('--timeout <ms>', 'Request timeout in milliseconds (default: 60000)', '60000')
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

      // Resolve project path and validate it is a Unity project
      const projectPath = resolveAndValidateProjectPath(positionalPath, options);
      verbose(`Project path: ${projectPath}`);

      // Resolve skills path (absolute)
      const skillsPath = path.join(projectPath, agent.skillsPath);
      verbose(`Skills path: ${skillsPath}`);

      // Resolve server connection
      let { url: serverUrl, token } = resolveConnection(projectPath, options);

      // Call the MCP server to generate skills
      const endpoint = `${serverUrl}/api/system-tools/unity-skill-generate`;
      verbose(`Endpoint: ${endpoint}`);

      const body = JSON.stringify({ path: agent.skillsPath });

      ui.heading('Generate Skills');
      ui.label('Agent', agent.name);
      ui.label('Skills path', skillsPath);
      ui.label('Server', serverUrl);
      ui.divider();

      const timeoutMs = parseInt(options.timeout ?? '60000', 10);
      if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
        ui.error(`Invalid timeout value: "${options.timeout}". Must be a positive integer (milliseconds).`);
        process.exit(1);
      }

      const spinner = ui.startSpinner(
        `Generating skills for ${agent.name}...`,
      );

      try {
        let response = await callGenerateSkills(endpoint, token, body, timeoutMs);

        // Handle 401 in Cloud mode
        if (response.status === 401) {
          await response.text();
          spinner.stop();

          const config = readConfig(projectPath);
          const cloud = config != null && isCloudMode(config);
          const hadToken = !!token;

          if (cloud && !options.token && !hadToken) {
            // No token at all — run inline device auth to get one
            const newToken = await inlineCloudLogin(projectPath);
            if (!newToken) {
              process.exit(1);
            }

            // Token saved. Unity Editor needs to connect with it.
            ui.info('Unity Editor must connect to the cloud server with this token.');
            ui.info('If Unity was already running, restart it to pick up the new token:');
            console.log();
            console.log(`  unity-mcp-cli open ${positionalPath ?? '.'}`);
            console.log(`  unity-mcp-cli wait-for-ready ${positionalPath ?? '.'}`);
            console.log(`  unity-mcp-cli setup-skills ${agentId} ${positionalPath ?? '.'}`);
            console.log();
            process.exit(0);
          } else if (cloud && hadToken) {
            // Token exists but was rejected — Unity Editor isn't connected
            ui.error('Cloud server rejected the token. Unity Editor may not be connected yet.');
            ui.info('Make sure Unity Editor is running and connected to the cloud server.');
            ui.info('Try the following:');
            console.log();
            console.log(`  unity-mcp-cli open ${positionalPath ?? '.'}`);
            console.log(`  unity-mcp-cli wait-for-ready ${positionalPath ?? '.'}`);
            console.log(`  unity-mcp-cli setup-skills ${agentId} ${positionalPath ?? '.'}`);
            console.log();
            process.exit(1);
          } else {
            ui.error('HTTP 401: Unauthorized. Check your --token value.');
            process.exit(1);
          }
        }

        if (!response.ok) {
          spinner.stop();
          const text = await response.text();

          if (response.status === 404) {
            ui.error(
              'The unity-skill-generate tool is not yet available. Please update the Unity-MCP plugin to the latest version.',
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
        handleFetchError(err, timeoutMs);
        process.exit(1);
      }
    },
  );

function handleFetchError(err: unknown, timeoutMs: number): void {
  const message = err instanceof Error ? err.message : String(err);
  const isTimeout = err instanceof Error && err.name === 'AbortError';
  const isConnectionRefused =
    message.includes('ECONNREFUSED') || message.includes('fetch failed');

  if (isTimeout) {
    ui.error(`Request timed out after ${timeoutMs / 1000} seconds.`);
  } else if (isConnectionRefused) {
    ui.error(
      'MCP Server is not running. Please start Unity Editor with the Unity-MCP plugin installed, then retry.',
    );
  } else {
    ui.error(`Failed to generate skills: ${message}`);
  }
}
