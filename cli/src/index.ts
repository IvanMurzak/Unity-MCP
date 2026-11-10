import { Command } from 'commander';
import { createRequire } from 'module';
import { bootstrapLocalCommand } from './commands/bootstrap-local.js';
import { closeCommand } from './commands/close.js';
import { createCommandsCommand } from './commands/commands.js';
import { compileCommand } from './commands/compile.js';
import { dialogCommand } from './commands/dialog.js';
import { createProjectCommand } from './commands/create-project.js';
import { execCommand } from './commands/exec.js';
import { installUnityCommand } from './commands/install-unity.js';
import { openCommand } from './commands/open.js';
import { installExtensionCommand } from './commands/install-extension.js';
import { installPluginCommand } from './commands/install-plugin.js';
import { loginCommand } from './commands/login.js';
import { configureCommand } from './commands/configure.js';
import { removePluginCommand } from './commands/remove-plugin.js';
import { RQL_RECIPES, rqlCommand } from './commands/rql.js';
import { runToolCommand } from './commands/run-tool.js';
import { runSystemToolCommand } from './commands/run-system-tool.js';
import { setupMcpCommand } from './commands/setup-mcp.js';
import { setupSkillsCommand } from './commands/setup-skills.js';
import { statusCommand } from './commands/status.js';
import { testCommand } from './commands/test.js';
import { toolsCommand } from './commands/tools.js';
import { createUpdateCommand } from './commands/update.js';
import { waitForReadyCommand } from './commands/wait-for-ready.js';
import { configureStyledHelp, error as uiError, setRootRecipes, setVerbose } from './utils/ui.js';
import { classifyCliError, writeFailureEnvelope } from '@baizor/gamedev-cli-core';
import { checkForUpdate, isRunningViaNpx, printUpdateNotification } from './utils/update-check.js';

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
  bootstrapLocalCommand,
  closeCommand,
  createCommandsCommand(() => program, pkg.version),
  compileCommand,
  configureCommand,
  createProjectCommand,
  dialogCommand,
  execCommand,
  installExtensionCommand,
  installPluginCommand,
  installUnityCommand,
  loginCommand,
  openCommand,
  removePluginCommand,
  rqlCommand,
  runToolCommand,
  runSystemToolCommand,
  setupMcpCommand,
  setupSkillsCommand,
  statusCommand,
  testCommand,
  toolsCommand,
  createUpdateCommand(pkg.version),
  waitForReadyCommand,
];

/**
 * Commander's parse errors (unknown option, missing argument, ...) are usage errors: exit 2, as in the shared exit-code
 * table. Help and version output keep exit 0. Commander already printed the message to stderr; a machine format still
 * gets its envelope on stdout.
 */
function exitOnParseError(err: { exitCode: number; code: string; message: string }): never {
  const failure = classifyCliError(err);
  if (!failure.informational) writeFailureEnvelope(program, failure);
  process.exit(failure.exitCode);
}

for (const cmd of subcommands) {
  cmd.option('-v, --verbose', 'Enable verbose diagnostic output');
  cmd.exitOverride(exitOnParseError);
  configureStyledHelp(cmd);
  program.addCommand(cmd);
}
program.exitOverride(exitOnParseError);

// Apply styled help to root (after subcommands so banner knows about them)
setRootRecipes(RQL_RECIPES);
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

// Start update check in background (non-blocking, parallel with command execution)
const updateCheckPromise = isRunningViaNpx() ? Promise.resolve(null) : checkForUpdate(pkg.version);

program.parseAsync()
  .then(async () => {
    const update = await updateCheckPromise;
    if (update) {
      printUpdateNotification(update.current, update.latest);
    }
  })
  .catch((err) => {
    // A failure no command handled still leaves one envelope on stdout in a machine format.
    const failure = classifyCliError(err);
    uiError(failure.message);
    writeFailureEnvelope(program, failure);
    process.exit(failure.exitCode);
  });
