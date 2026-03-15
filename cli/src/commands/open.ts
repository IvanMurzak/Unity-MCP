import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { findEditorPath, getProjectEditorVersion, launchEditor, printEditorNotFoundHelp } from '../utils/unity-editor.js';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';

export const openCommand = new Command('open')
  .description('Open a Unity project in Unity Editor (with MCP connection by default)')
  .argument('[path]', 'Path to the Unity project')
  .option('--path <path>', 'Path to the Unity project')
  .option('--unity <version>', 'Specific Unity Editor version to use')
  .option('--no-connect', 'Open without MCP connection environment variables')
  .option('--url <url>', 'MCP server URL to connect to (sets UNITY_MCP_HOST)')
  .option('--tools <names>', 'Comma-separated list of tools to enable (sets UNITY_MCP_TOOLS)')
  .option('--token <token>', 'Auth token (sets UNITY_MCP_TOKEN)')
  .option('--auth <option>', 'Auth option: none or required (sets UNITY_MCP_AUTH_OPTION)')
  .option('--keep-connected', 'Force keep connected (sets UNITY_MCP_KEEP_CONNECTED=true)')
  .option('--transport <method>', 'Transport method: streamableHttp or stdio (sets UNITY_MCP_TRANSPORT)')
  .option('--start-server <value>', 'Set to true/false to control server auto-start (sets UNITY_MCP_START_SERVER)', undefined)
  .action(async (positionalPath: string | undefined, options: {
    path?: string;
    unity?: string;
    connect?: boolean;
    url?: string;
    tools?: string;
    token?: string;
    auth?: string;
    keepConnected?: boolean;
    transport?: string;
    startServer?: string;
  }) => {
    const resolvedPath = positionalPath ?? options.path;
    if (!resolvedPath) {
      ui.error('Path is required. Usage: unity-mcp-cli open <path> or --path <path>');
      process.exit(1);
    }
    const projectPath = path.resolve(resolvedPath);

    if (!fs.existsSync(projectPath)) {
      ui.error(`Project path does not exist: ${projectPath}`);
      process.exit(1);
    }

    verbose(`open invoked for project: ${projectPath}`);
    verbose(`--no-connect: ${options.connect === false}`);

    // Determine editor version
    let version = options.unity;
    if (!version) {
      version = getProjectEditorVersion(projectPath) ?? undefined;
      if (version) {
        ui.info(`Detected editor version from project: ${version}`);
        verbose(`Resolved editor version from ProjectVersion.txt: ${version}`);
      }
    }

    const spinner = ui.startSpinner('Locating Unity Editor...');
    const editorPath = await findEditorPath(version);
    if (!editorPath) {
      spinner.stop();
      printEditorNotFoundHelp(version, 'open');
      process.exit(1);
    }
    spinner.success('Unity Editor located');
    verbose(`Editor path: ${editorPath}`);

    // Build environment variables for MCP connection (unless --no-connect)
    const useConnect = options.connect !== false;
    let env: Record<string, string> | undefined;

    if (useConnect) {
      const envVars: Record<string, string> = {};

      if (options.url) {
        envVars['UNITY_MCP_HOST'] = options.url;
      }

      if (options.keepConnected) {
        envVars['UNITY_MCP_KEEP_CONNECTED'] = 'true';
      }

      if (options.tools) {
        envVars['UNITY_MCP_TOOLS'] = options.tools;
      }

      if (options.token) {
        envVars['UNITY_MCP_TOKEN'] = options.token;
      }

      if (options.auth) {
        if (options.auth !== 'none' && options.auth !== 'required') {
          ui.error('--auth must be "none" or "required"');
          process.exit(1);
        }
        envVars['UNITY_MCP_AUTH_OPTION'] = options.auth;
      }

      if (options.transport) {
        if (options.transport !== 'streamableHttp' && options.transport !== 'stdio') {
          ui.error('--transport must be "streamableHttp" or "stdio"');
          process.exit(1);
        }
        envVars['UNITY_MCP_TRANSPORT'] = options.transport;
      }

      if (options.startServer !== undefined) {
        const val = options.startServer.toLowerCase();
        if (val !== 'true' && val !== 'false') {
          ui.error('--start-server must be "true" or "false"');
          process.exit(1);
        }
        envVars['UNITY_MCP_START_SERVER'] = val;
      }

      if (Object.keys(envVars).length > 0) {
        env = envVars;
        ui.heading('Connection Details');
        ui.label('Project', projectPath);
        ui.label('Editor', editorPath);

        ui.heading('Environment Variables');
        for (const [key, value] of Object.entries(envVars)) {
          const display = key === 'UNITY_MCP_TOKEN' ? '***' : value;
          ui.label(key, display);
          verbose(`Setting ${key}=${display}`);
        }
        ui.divider();
      }
    } else {
      verbose('MCP connection disabled via --no-connect');
    }

    ui.label('Project', projectPath);
    ui.label('Editor', editorPath);
    launchEditor(editorPath, projectPath, env);
  });
