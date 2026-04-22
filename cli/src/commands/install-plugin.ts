import { Command } from 'commander';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { installPlugin } from '../lib/install-plugin.js';

export const installPluginCommand = new Command('install-plugin')
  .description('Install Unity-MCP plugin into a Unity project')
  .argument('[path]', 'Path to the Unity project')
  .option('--path <path>', 'Path to the Unity project')
  .option('--plugin-version <version>', 'Plugin version to install (default: latest)')
  .action(async (positionalPath: string | undefined, options: { path?: string; pluginVersion?: string }) => {
    const resolvedPath = positionalPath ?? options.path;
    if (!resolvedPath) {
      ui.error('Path is required. Usage: unity-mcp-cli install-plugin <path> or --path <path>');
      process.exit(1);
    }

    const projectPath = path.resolve(resolvedPath);

    // Wire the library's progress events back into the CLI's chalk-
    // styled ui so the terminal experience stays identical.
    let spinner: ReturnType<typeof ui.startSpinner> | undefined;
    if (!options.pluginVersion) {
      spinner = ui.startSpinner('Resolving latest plugin version...');
    }

    const result = await installPlugin({
      unityProjectPath: projectPath,
      version: options.pluginVersion,
      onProgress: (event) => {
        if (event.phase === 'dependencies-resolved' && spinner) {
          spinner.success(`Resolved plugin version: ${event.version}`);
          spinner = undefined;
        }
      },
    });

    if (!result.success) {
      if (spinner) {
        spinner.error('Failed to resolve plugin version');
        spinner = undefined;
      }
      ui.error(result.error?.message ?? 'Unknown error');
      process.exit(1);
    }

    verbose(`Plugin version: ${result.installedVersion} (explicit: ${!!options.pluginVersion})`);
    if (result.manifestPath) {
      verbose(`Manifest path: ${result.manifestPath}`);
    }
    ui.info(`Installing Unity-MCP plugin v${result.installedVersion} into: ${projectPath}`);

    for (const warning of result.warnings) {
      ui.warn(warning);
    }

    ui.success('Done! Open the project in Unity Editor to complete installation.');
  });
