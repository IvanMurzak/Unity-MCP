import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { removePluginFromManifest } from '../utils/manifest.js';

export const removePluginCommand = new Command('remove-plugin')
  .description('Remove Unity-MCP plugin from a Unity project')
  .argument('[path]', 'Path to the Unity project')
  .option('--path <path>', 'Path to the Unity project')
  .action(async (positionalPath: string | undefined, options: { path?: string }) => {
    const resolvedPath = positionalPath ?? options.path;
    if (!resolvedPath) {
      console.error('Error: Path is required. Usage: unity-mcp-cli remove-plugin <path> or --path <path>');
      process.exit(1);
    }
    const projectPath = path.resolve(resolvedPath);

    // Validate project exists
    const manifestPath = path.join(projectPath, 'Packages', 'manifest.json');
    if (!fs.existsSync(manifestPath)) {
      console.error(`Error: Not a valid Unity project (missing Packages/manifest.json): ${projectPath}`);
      process.exit(1);
    }

    console.log(`Removing Unity-MCP plugin from: ${projectPath}`);
    removePluginFromManifest(projectPath);
    console.log('Done!');
  });
