import { Command } from 'commander';
import chalk from 'chalk';
import { createRequire } from 'module';
import { createProjectCommand } from './commands/create-project.js';
import { installUnityCommand } from './commands/install-unity.js';
import { openCommand } from './commands/open.js';
import { installPluginCommand } from './commands/install-plugin.js';
import { configureCommand } from './commands/configure.js';
import { connectCommand } from './commands/connect.js';
import { removePluginCommand } from './commands/remove-plugin.js';
import { configureStyledHelp } from './utils/ui.js';

const require = createRequire(import.meta.url);
const pkg = require('../package.json') as { version: string };

const program = new Command();

program
  .name('unity-mcp-cli')
  .description('Cross-platform CLI tool for Unity-MCP operations')
  .version(pkg.version);

// Register all subcommands
const subcommands = [
  createProjectCommand,
  installUnityCommand,
  openCommand,
  installPluginCommand,
  configureCommand,
  connectCommand,
  removePluginCommand,
];

for (const cmd of subcommands) {
  configureStyledHelp(cmd);
  program.addCommand(cmd);
}

// Apply styled help to root (after subcommands so banner knows about them)
configureStyledHelp(program, pkg.version);

// Show help when no command provided
program.action(() => {
  program.outputHelp();
});

program.parseAsync().catch((err) => {
  console.error(chalk.red(`\u2716 ${(err as Error).message || err}`));
  process.exit(1);
});
