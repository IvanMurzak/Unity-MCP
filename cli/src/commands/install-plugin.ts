import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { addPluginToManifest, resolveLatestVersion } from '../utils/manifest.js';

export const installPluginCommand = new Command('install-plugin')
  .description('Install Unity-MCP plugin into a Unity project')
  .requiredOption('--project-path <path>', 'Path to the Unity project')
  .option('--plugin-version <version>', 'Plugin version to install (default: latest)')
  .action(async (options: { projectPath: string; pluginVersion?: string }) => {
    const projectPath = path.resolve(options.projectPath);

    // Validate project exists
    const manifestPath = path.join(projectPath, 'Packages', 'manifest.json');
    if (!fs.existsSync(manifestPath)) {
      console.error(`Error: Not a valid Unity project (missing Packages/manifest.json): ${projectPath}`);
      process.exit(1);
    }

    // Resolve version
    let version = options.pluginVersion;
    if (!version) {
      version = await resolveLatestVersion();
    }

    console.log(`Installing Unity-MCP plugin v${version} into: ${projectPath}`);
    addPluginToManifest(projectPath, version);
    console.log('Done! Open the project in Unity Editor to complete installation.');
  });
