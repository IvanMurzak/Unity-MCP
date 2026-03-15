import { Command } from 'commander';
import { createRequire } from 'module';
import { createProjectCommand } from './commands/create-project.js';
import { installUnityCommand } from './commands/install-unity.js';
import { openCommand } from './commands/open.js';
import { installPluginCommand } from './commands/install-plugin.js';
import { configureCommand } from './commands/configure.js';
import { removePluginCommand } from './commands/remove-plugin.js';
import { runToolCommand } from './commands/run-tool.js';
import { configureStyledHelp, error as uiError, setVerbose } from './utils/ui.js';

const require = createRequire(import.meta.url);
const pkg = require('../package.json') as { version: string };

const program = new Command();

program
  .name('unity-mcp-cli')
  .description('Cross-platform CLI tool for Unity-MCP operations')
  .version(pkg.version)
  .option('-v, --verbose', 'Enable verbose diagnostic output');

// Register all subcommands
const subcommands = [
  configureCommand,
  createProjectCommand,
  installPluginCommand,
  installUnityCommand,
  openCommand,
  removePluginCommand,
  runToolCommand,
];

for (const cmd of subcommands) {
  cmd.option('-v, --verbose', 'Enable verbose diagnostic output');
  configureStyledHelp(cmd);
  program.addCommand(cmd);
}

// Apply styled help to root (after subcommands so banner knows about them)
configureStyledHelp(program, pkg.version);

// Wire verbose flag before any command executes
program.hook('preAction', (_thisCommand, actionCommand) => {
  const opts = actionCommand.optsWithGlobals() as { verbose?: boolean };
  if (opts.verbose) {
    setVerbose(true);
  }
});

// Show help when no command provided
program.action(() => {
  program.outputHelp();
});

program.parseAsync().catch((err) => {
  uiError((err as Error).message || String(err));
  process.exit(1);
});
