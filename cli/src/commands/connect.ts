import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { findEditorPath, getProjectEditorVersion, launchEditor } from '../utils/unity-editor.js';
import * as ui from '../utils/ui.js';

export const connectCommand = new Command('connect')
  .description('Open Unity and enforce MCP connection to a specified server URL via environment variables')
  .requiredOption('--path <path>', 'Path to the Unity project')
  .requiredOption('--url <url>', 'MCP server URL to connect to')
  .option('--tools <names>', 'Comma-separated list of tools to enable (sets UNITY_MCP_TOOLS)')
  .option('--token <token>', 'Auth token (sets UNITY_MCP_TOKEN)')
  .option('--auth <option>', 'Auth option: none or required (sets UNITY_MCP_AUTH_OPTION)')
  .option('--keep-connected', 'Force keep connected (sets UNITY_MCP_KEEP_CONNECTED=true)')
  .option('--transport <method>', 'Transport method: streamableHttp or stdio (sets UNITY_MCP_TRANSPORT)')
  .option('--start-server <value>', 'Set to true/false to control server auto-start (sets UNITY_MCP_START_SERVER)', undefined)
  .option('--unity <version>', 'Specific Unity Editor version to use')
  .action(async (options: {
    path: string;
    url: string;
    tools?: string;
    token?: string;
    auth?: string;
    keepConnected?: boolean;
    transport?: string;
    startServer?: string;
    unity?: string;
  }) => {
    const projectPath = path.resolve(options.path);

    if (!fs.existsSync(projectPath)) {
      ui.error(`Project path does not exist: ${projectPath}`);
      process.exit(1);
    }

    // Determine editor version
    let version = options.unity;
    if (!version) {
      version = getProjectEditorVersion(projectPath) ?? undefined;
      if (version) {
        ui.info(`Detected editor version from project: ${version}`);
      }
    }

    const spinner = ui.startSpinner('Locating Unity Editor...');
    const editorPath = await findEditorPath(version);
    if (!editorPath) {
      spinner.error('Unity Editor not found');
      const versionMsg = version ? ` (version ${version})` : '';
      ui.error(`Unity Editor not found${versionMsg}. Install it with: unity-mcp-cli install-unity <version>`);
      process.exit(1);
    }
    spinner.success('Unity Editor located');

    // Build environment variables for MCP connection
    const env: Record<string, string> = {
      UNITY_MCP_HOST: options.url,
    };

    if (options.keepConnected) {
      env['UNITY_MCP_KEEP_CONNECTED'] = 'true';
    }

    if (options.tools) {
      env['UNITY_MCP_TOOLS'] = options.tools;
    }

    if (options.token) {
      env['UNITY_MCP_TOKEN'] = options.token;
    }

    if (options.auth) {
      if (options.auth !== 'none' && options.auth !== 'required') {
        ui.error('--auth must be "none" or "required"');
        process.exit(1);
      }
      env['UNITY_MCP_AUTH_OPTION'] = options.auth;
    }

    if (options.transport) {
      if (options.transport !== 'streamableHttp' && options.transport !== 'stdio') {
        ui.error('--transport must be "streamableHttp" or "stdio"');
        process.exit(1);
      }
      env['UNITY_MCP_TRANSPORT'] = options.transport;
    }

    if (options.startServer !== undefined) {
      const val = options.startServer.toLowerCase();
      if (val !== 'true' && val !== 'false') {
        ui.error('--start-server must be "true" or "false"');
        process.exit(1);
      }
      env['UNITY_MCP_START_SERVER'] = val;
    }

    ui.heading('Connection Details');
    ui.label('MCP Server', options.url);
    ui.label('Project', projectPath);
    ui.label('Editor', editorPath);

    ui.heading('Environment Variables');
    for (const [key, value] of Object.entries(env)) {
      const display = key === 'UNITY_MCP_TOKEN' ? '***' : value;
      ui.label(key, display);
    }

    ui.divider();
    launchEditor(editorPath, projectPath, env);
  });
