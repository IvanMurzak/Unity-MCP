import { Command } from 'commander';
import * as path from 'path';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { configure } from '../lib/configure.js';
import type { FeatureAction } from '../lib/types.js';

function parseCommaSeparated(value: string): string[] {
  return value.split(',').map((s) => s.trim()).filter(Boolean);
}

function buildAction(
  enable: string[] | undefined,
  disable: string[] | undefined,
  enableAll: boolean | undefined,
  disableAll: boolean | undefined,
): FeatureAction | undefined {
  if (!enable && !disable && !enableAll && !disableAll) return undefined;
  return {
    enableNames: enable,
    disableNames: disable,
    enableAll,
    disableAll,
  };
}

export const configureCommand = new Command('configure')
  .description('Configure MCP tools, prompts, and resources in AI-Game-Developer-Config.json')
  .argument('[path]', 'Path to the Unity project')
  .option('--path <path>', 'Path to the Unity project')
  .option('--enable-tools <names>', 'Enable specific tools (comma-separated)', parseCommaSeparated)
  .option('--disable-tools <names>', 'Disable specific tools (comma-separated)', parseCommaSeparated)
  .option('--enable-all-tools', 'Enable all tools')
  .option('--disable-all-tools', 'Disable all tools')
  .option('--enable-prompts <names>', 'Enable specific prompts (comma-separated)', parseCommaSeparated)
  .option('--disable-prompts <names>', 'Disable specific prompts (comma-separated)', parseCommaSeparated)
  .option('--enable-all-prompts', 'Enable all prompts')
  .option('--disable-all-prompts', 'Disable all prompts')
  .option('--enable-resources <names>', 'Enable specific resources (comma-separated)', parseCommaSeparated)
  .option('--disable-resources <names>', 'Disable specific resources (comma-separated)', parseCommaSeparated)
  .option('--enable-all-resources', 'Enable all resources')
  .option('--disable-all-resources', 'Disable all resources')
  .option('--list', 'List current configuration')
  .action(async (positionalPath: string | undefined, options: {
    path?: string;
    enableTools?: string[];
    disableTools?: string[];
    enableAllTools?: boolean;
    disableAllTools?: boolean;
    enablePrompts?: string[];
    disablePrompts?: string[];
    enableAllPrompts?: boolean;
    disableAllPrompts?: boolean;
    enableResources?: string[];
    disableResources?: string[];
    enableAllResources?: boolean;
    disableAllResources?: boolean;
    list?: boolean;
  }) => {
    const resolvedPath = positionalPath ?? options.path;
    if (!resolvedPath) {
      ui.error('Path is required. Usage: unity-mcp-cli configure <path> or --path <path>');
      process.exit(1);
    }

    const projectPath = path.resolve(resolvedPath);

    verbose(`Loading config for project: ${projectPath}`);

    const result = await configure({
      unityProjectPath: projectPath,
      tools: buildAction(options.enableTools, options.disableTools, options.enableAllTools, options.disableAllTools),
      prompts: buildAction(options.enablePrompts, options.disablePrompts, options.enableAllPrompts, options.disableAllPrompts),
      resources: buildAction(options.enableResources, options.disableResources, options.enableAllResources, options.disableAllResources),
    });

    if (!result.success) {
      ui.error(result.error?.message ?? 'Unknown error');
      process.exit(1);
    }

    if (options.list && result.snapshot) {
      ui.heading('Current configuration');
      ui.label('Host', result.snapshot.host ?? 'not set');
      ui.label('Keep Connected', String(result.snapshot.keepConnected ?? false));
      ui.label('Transport', result.snapshot.transportMethod ?? 'streamableHttp');
      ui.label('Auth', result.snapshot.authOption ?? 'none');

      const printFeatures = (featureLabel: string, features: { name: string; enabled: boolean }[]) => {
        ui.heading(featureLabel);
        if (features.length === 0) {
          ui.info('(none configured - all enabled by default)');
          return;
        }
        for (const f of features) {
          ui.featureRow(f.name, f.enabled);
        }
      };

      printFeatures('Tools', result.snapshot.tools);
      printFeatures('Prompts', result.snapshot.prompts);
      printFeatures('Resources', result.snapshot.resources);
      return;
    }

    verbose('Writing updated configuration');
    ui.success('Configuration updated successfully.');
  });
