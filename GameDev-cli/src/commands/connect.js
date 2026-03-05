import path from 'path';
import { readConfig, writeConfig } from '../utils/config.js';
import { openUnityProject } from '../utils/unity.js';

/**
 * Registers the `connect` command with the CLI program.
 *
 * This command:
 *  1. Updates AI-Game-Developer-Config.json with the target MCP server URL.
 *  2. Opens Unity with the `-mcpServerUrl <url>` argument.
 *  3. The Unity plugin's [InitializeOnLoad] CommandLineArgs class reads this
 *     argument and calls EnforceConnect(url) to update the runtime config and
 *     initiate the connection immediately, without requiring a manual reconnect.
 *
 * Usage:
 *   gamedev connect <project-path> --url http://localhost:8080
 *
 * @param {import('commander').Command} program
 */
export function registerConnectCommand(program) {
  program
    .command('connect <project-path>')
    .description(
      'Open Unity and enforce an MCP connection to the specified server URL.\n\n' +
      'Updates AI-Game-Developer-Config.json and launches Unity with the\n' +
      '-mcpServerUrl argument so the plugin connects immediately on startup.'
    )
    .requiredOption('--url <url>', 'MCP server URL to connect to (e.g. http://localhost:8080)')
    .option('--unity-path <path>', 'Explicit path to the Unity executable (auto-detected if omitted)')
    .option('--unity-version <version>', 'Unity version to use when multiple versions are installed')
    .action(async (projectPath, options) => {
      const absPath = path.resolve(projectPath);

      // Persist the URL in the config so it survives editor restarts
      console.log(`Updating config: host = ${options.url}`);
      const config = readConfig(absPath);
      config.host = options.url;
      config.keepConnected = true;
      writeConfig(absPath, config);

      console.log(`Opening Unity with MCP connection to: ${options.url}`);

      try {
        await openUnityProject(absPath, {
          unityPath: options.unityPath,
          unityVersion: options.unityVersion,
          // Passes -mcpServerUrl to Unity so CommandLineArgs.EnforceConnect() is
          // called automatically by the [InitializeOnLoad] static constructor.
          extraArgs: ['-mcpServerUrl', options.url],
        });
      } catch (err) {
        console.error(`Error: ${err.message}`);
        process.exit(1);
      }
    });
}
