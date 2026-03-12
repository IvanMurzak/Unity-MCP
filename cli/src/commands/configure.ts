import { Command } from 'commander';
import * as path from 'path';
import * as fs from 'fs';
import { getOrCreateConfig, writeConfig, updateFeatures } from '../utils/config.js';

function parseCommaSeparated(value: string): string[] {
  return value.split(',').map((s) => s.trim()).filter(Boolean);
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
      console.error('Error: Path is required. Usage: unity-mcp configure <path> or --path <path>');
      process.exit(1);
    }
    const projectPath = path.resolve(resolvedPath);

    if (!fs.existsSync(projectPath)) {
      console.error(`Error: Project path does not exist: ${projectPath}`);
      process.exit(1);
    }

    const config = getOrCreateConfig(projectPath);

    if (options.list) {
      console.log('\nCurrent configuration:');
      console.log(`  Host: ${config.host ?? 'not set'}`);
      console.log(`  Keep Connected: ${config.keepConnected ?? false}`);
      console.log(`  Transport: ${config.transportMethod ?? 'streamableHttp'}`);
      console.log(`  Auth: ${config.authOption ?? 'none'}`);

      const printFeatures = (label: string, features: { name: string; enabled: boolean }[] | undefined) => {
        if (!features || features.length === 0) {
          console.log(`\n  ${label}: (none configured - all enabled by default)`);
          return;
        }
        console.log(`\n  ${label}:`);
        for (const f of features) {
          const status = f.enabled ? '[enabled]' : '[disabled]';
          console.log(`    ${status} ${f.name}`);
        }
      };

      printFeatures('Tools', config.tools);
      printFeatures('Prompts', config.prompts);
      printFeatures('Resources', config.resources);
      return;
    }

    // Apply tool changes
    if (options.enableTools || options.disableTools || options.enableAllTools || options.disableAllTools) {
      updateFeatures(config, 'tools', {
        enableNames: options.enableTools,
        disableNames: options.disableTools,
        enableAll: options.enableAllTools,
        disableAll: options.disableAllTools,
      });
    }

    // Apply prompt changes
    if (options.enablePrompts || options.disablePrompts || options.enableAllPrompts || options.disableAllPrompts) {
      updateFeatures(config, 'prompts', {
        enableNames: options.enablePrompts,
        disableNames: options.disablePrompts,
        enableAll: options.enableAllPrompts,
        disableAll: options.disableAllPrompts,
      });
    }

    // Apply resource changes
    if (options.enableResources || options.disableResources || options.enableAllResources || options.disableAllResources) {
      updateFeatures(config, 'resources', {
        enableNames: options.enableResources,
        disableNames: options.disableResources,
        enableAll: options.enableAllResources,
        disableAll: options.disableAllResources,
      });
    }

    writeConfig(projectPath, config);
    console.log('Configuration updated successfully.');
  });
