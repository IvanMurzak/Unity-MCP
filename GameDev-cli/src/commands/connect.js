import fs from 'fs';
import path from 'path';
import { readConfig, writeConfig } from '../utils/config.js';
import { openUnityProject, getConnectResultPath, pollForFile } from '../utils/unity.js';

/**
 * Registers the `connect` command with the CLI program.
 *
 * This command:
 *  1. Updates AI-Game-Developer-Config.json with the target MCP server URL.
 *  2. Deletes any stale mcp-connect-result.json from a previous run.
 *  3. Opens Unity with the `-mcpServerUrl <url>` argument.
 *  4. When --wait is used, polls Library/mcp-connect-result.json for the outcome
 *     written by the Unity plugin's [InitializeOnLoad] CommandLineArgs class.
 *
 * Usage:
 *   gamedev connect <project-path> --url http://localhost:8080
 *   gamedev connect <project-path> --url http://localhost:8080 --wait
 *
 * @param {import('commander').Command} program
 */
export function registerConnectCommand(program) {
  program
    .command('connect <project-path>')
    .description(
      'Open Unity and enforce an MCP connection to the specified server URL.\n\n' +
      'Updates AI-Game-Developer-Config.json and launches Unity with the\n' +
      '-mcpServerUrl argument so the plugin connects immediately on startup.\n\n' +
      'Use --wait to block until Unity reports a result (success or detailed error).'
    )
    .requiredOption('--url <url>', 'MCP server URL to connect to (e.g. http://localhost:8080)')
    .option('--unity-path <path>', 'Explicit path to the Unity executable (auto-detected if omitted)')
    .option('--unity-version <version>', 'Unity version to use when multiple versions are installed')
    .option('--wait', 'Wait for Unity to report the connection result (polls Library/mcp-connect-result.json)')
    .option('--wait-timeout <seconds>', 'Seconds to wait for a result when --wait is set (default: 60)', parseInt)
    .action(async (projectPath, options) => {
      const absPath = path.resolve(projectPath);
      const resultFilePath = getConnectResultPath(absPath);

      // Persist the URL in the config so it survives editor restarts
      console.log(`Updating config: host = ${options.url}`);
      const config = readConfig(absPath);
      config.host = options.url;
      config.keepConnected = true;
      writeConfig(absPath, config);

      // Remove stale result file so we can detect when Unity writes a fresh one
      if (fs.existsSync(resultFilePath)) {
        fs.unlinkSync(resultFilePath);
      }

      console.log(`Opening Unity with MCP connection to: ${options.url}`);

      try {
        await openUnityProject(absPath, {
          unityPath: options.unityPath,
          unityVersion: options.unityVersion,
          // Passes -mcpServerUrl to Unity so CommandLineArgs.EnforceConnect() is
          // called automatically by the [InitializeOnLoad] static constructor.
          extraArgs: ['-mcpServerUrl', options.url],
          detached: !options.wait,
        });
      } catch (err) {
        console.error(`Error launching Unity: ${err.message}`);
        process.exit(1);
      }

      if (!options.wait) {
        console.log('Unity launched. Use --wait to block until the connection result is available.');
        return;
      }

      // Wait for the result file written by CommandLineArgs.cs
      const timeoutMs = ((options.waitTimeout ?? 60)) * 1000;
      console.log(`Waiting up to ${timeoutMs / 1000}s for Unity to report the connection result...`);
      const found = await pollForFile(resultFilePath, timeoutMs);

      if (!found) {
        console.error(
          `Timed out waiting for ${resultFilePath}.\n` +
          'Check the Unity Editor console or log file for details.\n' +
          'Ensure the Unity MCP plugin is installed and the project compiled without errors.'
        );
        process.exit(1);
      }

      let result;
      try {
        result = JSON.parse(fs.readFileSync(resultFilePath, 'utf-8'));
      } catch (err) {
        console.error(`Failed to read result file: ${err.message}`);
        process.exit(1);
      }

      if (result.success) {
        console.log(`[Success] MCP connection established to ${options.url}`);
      } else {
        console.error('[Error] MCP connection failed:');
        if (result.errorType)    console.error(`  Type:    ${result.errorType}`);
        if (result.errorMessage) console.error(`  Message: ${result.errorMessage}`);
        if (result.stackTrace)   console.error(`  Stack:\n${result.stackTrace.split('\n').map(l => '    ' + l).join('\n')}`);
        process.exit(1);
      }
    });
}
