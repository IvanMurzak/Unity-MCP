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
import { runToolCommand } from './commands/run-tool.js';
import { runSystemToolCommand } from './commands/run-system-tool.js';
import { setupMcpCommand } from './commands/setup-mcp.js';
import { setupSkillsCommand } from './commands/setup-skills.js';
import { statusCommand } from './commands/status.js';
import { testCommand } from './commands/test.js';
import { createUpdateCommand } from './commands/update.js';
import { waitForReadyCommand } from './commands/wait-for-ready.js';
import { configureStyledHelp, error as uiError, setVerbose } from './utils/ui.js';
import { ExitCode } from './utils/exit-codes.js';
import { formatEnvelope } from './utils/output.js';
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
  runToolCommand,
  runSystemToolCommand,
  setupMcpCommand,
  setupSkillsCommand,
  statusCommand,
  testCommand,
  createUpdateCommand(pkg.version),
  waitForReadyCommand,
];

/**
 * Commander's parse errors (unknown option, missing argument, ...) are usage errors: exit 2, as in the shared exit-code
 * table. Help and version output keep exit 0.
 */
function exitOnParseError(err: { exitCode: number; code: string; message: string }): never {
  const informational = err.code === 'commander.helpDisplayed' || err.code === 'commander.help' || err.code === 'commander.version';
  if (informational || err.exitCode === 0) process.exit(err.exitCode);
  // Commander already printed the message to stderr; a machine format still gets its envelope on stdout.
  writeErrorEnvelope('USAGE', err.message.replace(/^error:\s*/, ''));
  process.exit(ExitCode.Usage);
}

/**
 * `--format json|ndjson` from the raw arguments: a failure before (or outside) a command's own handling — a parse
 * error, an unexpected exception — must still leave one envelope on stdout.
 */
function machineFormatFromArgv(argv: readonly string[]): 'json' | 'ndjson' | undefined {
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--') break;
    const value = arg === '--format' ? argv[i + 1] : arg.startsWith('--format=') ? arg.slice('--format='.length) : undefined;
    if (value === undefined) continue;
    const format = value.trim().toLowerCase();
    return format === 'json' || format === 'ndjson' ? format : undefined;
  }
  return undefined;
}

function writeErrorEnvelope(code: string, message: string): void {
  const format = machineFormatFromArgv(process.argv.slice(2));
  if (!format) return;
  const command = process.argv.slice(2).find(a => !a.startsWith('-')) ?? '';
  process.stdout.write(formatEnvelope({ success: false, command, data: null, errors: [{ code, message }], warnings: [] }, format));
}

for (const cmd of subcommands) {
  cmd.option('-v, --verbose', 'Enable verbose diagnostic output');
  cmd.exitOverride(exitOnParseError);
  configureStyledHelp(cmd);
  program.addCommand(cmd);
}
program.exitOverride(exitOnParseError);

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
    const message = (err as Error).message || String(err);
    uiError(message);
    writeErrorEnvelope('ERROR', message);
    process.exit(ExitCode.Error);
  });
