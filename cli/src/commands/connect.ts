import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { findEditorPath, getProjectEditorVersion, launchEditor } from '../utils/unity-editor.js';

export const connectCommand = new Command('connect')
  .description('Open Unity and enforce MCP connection to a specified server URL via environment variables')
  .requiredOption('--path <path>', 'Path to the Unity project')
  .requiredOption('--url <url>', 'MCP server URL to connect to')
  .option('--tools <names>', 'Comma-separated list of tools to enable (sets UNITY_MCP_TOOLS)')
  .option('--token <token>', 'Auth token (sets UNITY_MCP_TOKEN)')
  .option('--auth <option>', 'Auth option: none or required (sets UNITY_MCP_AUTH_OPTION)')
  .option('--keep-connected', 'Force keep connected (sets UNITY_MCP_KEEP_CONNECTED=true)')
  .option('--unity <version>', 'Specific Unity Editor version to use')
  .action(async (options: {
    path: string;
    url: string;
    tools?: string;
    token?: string;
    auth?: string;
    keepConnected?: boolean;
    unity?: string;
  }) => {
    const projectPath = path.resolve(options.path);

    if (!fs.existsSync(projectPath)) {
      console.error(`Error: Project path does not exist: ${projectPath}`);
      process.exit(1);
    }

    // Determine editor version
    let version = options.unity;
    if (!version) {
      version = getProjectEditorVersion(projectPath) ?? undefined;
      if (version) {
        console.log(`Detected editor version from project: ${version}`);
      }
    }

    const editorPath = await findEditorPath(version);
    if (!editorPath) {
      const versionMsg = version ? ` (version ${version})` : '';
      console.error(`Error: Unity Editor not found${versionMsg}. Install it with: unity-mcp-cli install-editor --version <version>`);
      process.exit(1);
    }

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
        console.error('Error: --auth must be "none" or "required"');
        process.exit(1);
      }
      env['UNITY_MCP_AUTH_OPTION'] = options.auth;
    }

    console.log(`Connecting to MCP server: ${options.url}`);
    console.log('Environment variables:');
    for (const [key, value] of Object.entries(env)) {
      const display = key === 'UNITY_MCP_TOKEN' ? '***' : value;
      console.log(`  ${key}=${display}`);
    }

    console.log(`\nOpening project: ${projectPath}`);
    console.log(`Using editor: ${editorPath}`);
    launchEditor(editorPath, projectPath, env);
  });
