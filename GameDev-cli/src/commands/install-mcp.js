import path from 'path';
import { fetchLatestVersion, installUnityMcp } from '../utils/manifest.js';

/**
 * Registers the `install-mcp` command with the CLI program.
 *
 * Usage:
 *   gamedev install-mcp <project-path> [options]
 *
 * Examples:
 *   gamedev install-mcp ./MyGame               # install latest version
 *   gamedev install-mcp ./MyGame --version 0.45.0   # install specific version
 *
 * @param {import('commander').Command} program
 */
export function registerInstallMcpCommand(program) {
  program
    .command('install-mcp <project-path>')
    .description('Install the Unity MCP package (com.ivanmurzak.unity.mcp) into a Unity project')
    .option('--version <version>', 'Specific package version to install (default: latest from OpenUPM)')
    .action(async (projectPath, options) => {
      const absPath = path.resolve(projectPath);
      console.log(`Target project: ${absPath}`);

      let version = options.version;
      if (!version) {
        console.log('Fetching latest version from OpenUPM...');
        version = await fetchLatestVersion();
      }

      console.log(`Installing com.ivanmurzak.unity.mcp@${version} ...`);

      try {
        installUnityMcp(absPath, version);
        console.log(`Successfully installed com.ivanmurzak.unity.mcp@${version}`);
        console.log('Re-open Unity Editor (or let Asset Database refresh) to complete the installation.');
      } catch (err) {
        console.error(`Error: ${err.message}`);
        process.exit(1);
      }
    });
}
