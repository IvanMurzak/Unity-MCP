import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { isUnityProject, resolveAndValidateProjectPath, resolveConnection } from '../utils/connection.js';
import { getAgentById, getAgentIds, listAgentTable } from '../utils/agents.js';
import { readConfig, isCloudMode } from '../utils/config.js';
import { writeCliSkills } from '../lib/setup-skills.js';
import { CLI_NAME } from '../lib/cli-guide.js';

interface SetupSkillsOptions {
  url?: string;
  token?: string;
  list?: boolean;
  timeout?: string;
  offline?: boolean;
}

function listAgentsWithSkills(): void {
  listAgentTable('AI Agents — Skills Support', 'Skills Path', (a) => a.skillsPath ?? '—');
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

export const setupSkillsCommand = new Command('setup-skills')
  .description(`Write skill files for an AI agent: the ${CLI_NAME} skill (offline, no editor needed) and, when the project's editor answers, the plugin's per-tool skills`)
  .argument('[agent-id]', 'Agent to generate skills for (use --list to see all)')
  .argument('[path]', 'Unity project path (defaults to cwd)')
  .option('--url <url>', 'Server URL override')
  .option('--token <token>', 'Auth token override')
  .option('--list', 'List all agents with skills support status')
  .option('--offline', `Only write the ${CLI_NAME} skill; do not ask the editor for the plugin's skills`)
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

      const timeoutMs = parseInt(options.timeout ?? '60000', 10);
      if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) {
        ui.error(`Invalid timeout value: "${options.timeout}". Must be a positive integer (milliseconds).`);
        process.exit(1);
      }

      // Resolve project path and validate it is a Unity project
      const projectPath = resolveAndValidateProjectPath(positionalPath, options);
      verbose(`Project path: ${projectPath}`);

      // The CLI's own skill needs nothing but the CLI: written first, whatever the editor's state. Only into a Unity
      // project: with --url the folder is not checked above, and a home folder's .claude/skills is every project's.
      ui.heading('Generate Skills');
      ui.label('Agent', agent.name);
      if (isUnityProject(projectPath)) {
        const offline = writeCliSkills({ projectPath, agentId });
        if (offline.kind === 'failure') {
          ui.error(offline.error.message);
          process.exit(1);
        }
        ui.label('Skills path', offline.skillsDir);
        if (offline.filesWritten.length > 0) ui.success(`Wrote the ${CLI_NAME} skill (${offline.filesWritten.length} file(s) changed)`);
        else ui.info(`The ${CLI_NAME} skill is up to date`);
      } else {
        ui.warn(`Not a Unity project (missing Assets folder): ${projectPath}; the ${CLI_NAME} skill was not written.`);
        if (options.offline) process.exit(1);
      }
      if (options.offline) return;

      // The plugin's per-tool skills: only an editor can generate them. A closed editor is not an error.
      const { url: serverUrl, token } = await resolveConnection(projectPath, options);
      // Never a login from here: it opens a browser and signs the machine in, a side effect an agent's setup step
      // must not have. Without a token, a Cloud project's editor cannot be asked: say how, and stop.
      const config = readConfig(projectPath);
      const cloud = config != null && isCloudMode(config);
      if (cloud && !token) {
        ui.info('Cloud mode, not signed in: only the CLI skill was written. For the plugin\'s per-tool skills too:');
        console.log();
        console.log('  unity-mcp-cli login');
        printReconnectSteps(agentId, positionalPath);
        return;
      }
      const endpoint = `${serverUrl}/api/system-tools/unity-skill-generate`;
      verbose(`Endpoint: ${endpoint}`);
      ui.label('Server', serverUrl);
      ui.divider();

      const spinner = ui.startSpinner(`Asking the editor for the plugin's skills...`);

      try {
        const response = await callGenerateSkills(endpoint, token, JSON.stringify({ path: agent.skillsPath }), timeoutMs);

        // 401 is a note: a rejected Cloud token (the editor is not connected yet) or a wrong --token.
        if (response.status === 401) {
          await response.text();
          spinner.stop();

          if (cloud) {
            // Token exists but was rejected — Unity Editor isn't connected
            ui.warn('Cloud server rejected the token: the Unity Editor may not be connected yet, so only the CLI skill was written.');
            ui.info('For the plugin\'s skills too, connect the editor and run setup-skills again:');
            printReconnectSteps(agentId, positionalPath);
          } else {
            ui.warn('HTTP 401 from the server: check your --token value. Only the CLI skill was written.');
          }
          return;
        }

        if (!response.ok) {
          spinner.stop();
          const text = await response.text();

          if (response.status === 404) {
            ui.warn(
              'The editor\'s plugin has no unity-skill-generate tool: update the Unity-MCP plugin for its per-tool skills.',
            );
          } else {
            ui.warn(`The plugin's skills were not generated: HTTP ${response.status}: ${response.statusText}`);
            if (text) {
              ui.info(text);
            }
          }
          return;
        }

        spinner.success(`The plugin's skills were generated for ${agent.name}`);
      } catch (err) {
        spinner.stop();
        reportFetchError(err, timeoutMs);
      }
    },
  );

/** The commands that connect the editor and rerun setup-skills for the plugin's skills too. */
function printReconnectSteps(agentId: string, positionalPath: string | undefined): void {
  const at = positionalPath ?? '.';
  console.log();
  for (const step of [`open ${at}`, `wait-for-ready ${at}`, `setup-skills ${agentId} ${at}`]) console.log(`  unity-mcp-cli ${step}`);
  console.log();
}

/** The editor did not answer: the CLI skill is written, so this is information, not a failure. */
function reportFetchError(err: unknown, timeoutMs: number): void {
  const message = err instanceof Error ? err.message : String(err);
  const isTimeout = err instanceof Error && err.name === 'AbortError';
  // Only a refused connection means "no editor there"; DNS, TLS or reset errors (a wrong --url) are worth a warning.
  const cause = (err as { cause?: { code?: string } } | undefined)?.cause;
  const isConnectionRefused = cause?.code === 'ECONNREFUSED' || message.includes('ECONNREFUSED');

  if (isTimeout) {
    ui.warn(`The editor did not answer within ${timeoutMs / 1000} seconds: only the CLI skill was written.`);
  } else if (isConnectionRefused) {
    ui.info(
      'No Unity Editor answered, so only the CLI skill was written. The plugin\'s per-tool skills come from the editor: it writes them when it runs, or run setup-skills again while it is open.',
    );
  } else {
    ui.warn(`The plugin's skills were not generated: ${message}`);
  }
}
