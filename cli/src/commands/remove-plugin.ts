import { Command } from 'commander';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { removePlugin } from '../lib/remove-plugin.js';

export const removePluginCommand = new Command('remove-plugin')
  .description('Remove Unity-MCP plugin from a Unity project')
  .argument('[path]', 'Path to the Unity project')
  .option('--path <path>', 'Path to the Unity project')
  .action(async (positionalPath: string | undefined, options: { path?: string }) => {
    const resolvedPath = positionalPath ?? options.path;
    if (!resolvedPath) {
      ui.error('Path is required. Usage: unity-mcp-cli remove-plugin <path> or --path <path>');
      process.exit(1);
    }

    const projectPath = path.resolve(resolvedPath);
    ui.info(`Removing Unity-MCP plugin from: ${projectPath}`);

    const result = await removePlugin({ unityProjectPath: projectPath });

    if (result.kind === 'failure') {
      ui.error(result.error.message);
      process.exit(1);
    }

    // Narrowed: result.kind === 'success' below — `manifestPath` is
    // non-optional and `removed` is a definite boolean.
    verbose(`Manifest path: ${result.manifestPath}`);

    if (result.removed) {
      ui.success(`Removed com.ivanmurzak.unity.mcp from ${result.manifestPath}`);
    } else {
      ui.info('Unity-MCP plugin is not installed. Nothing to remove.');
    }

    for (const warning of result.warnings) {
      // `removed === false` is already surfaced via the info line above,
      // so skip the duplicate "not installed" warning to avoid noise.
      if (warning.startsWith('Unity-MCP plugin was not installed')) continue;
      ui.warn(warning);
    }

    ui.success('Done!');
  });
