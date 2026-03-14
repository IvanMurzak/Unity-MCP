import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { addPluginToManifest, resolveLatestVersion } from '../utils/manifest.js';
import * as ui from '../utils/ui.js';

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

    // Validate project exists
    const manifestPath = path.join(projectPath, 'Packages', 'manifest.json');
    if (!fs.existsSync(manifestPath)) {
      ui.error(`Not a valid Unity project (missing Packages/manifest.json): ${projectPath}`);
      process.exit(1);
    }

    // Resolve version
    let version = options.pluginVersion;
    if (!version) {
      const spinner = ui.startSpinner('Resolving latest plugin version...');
      version = await resolveLatestVersion();
      spinner.success(`Resolved plugin version: ${version}`);
    }

    ui.info(`Installing Unity-MCP plugin v${version} into: ${projectPath}`);
    addPluginToManifest(projectPath, version);
    ui.success('Done! Open the project in Unity Editor to complete installation.');
  });
