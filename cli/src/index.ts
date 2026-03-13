import { Command, Help } from 'commander';
import chalk from 'chalk';
import boxen from 'boxen';
import { createRequire } from 'module';
import { createProjectCommand } from './commands/create-project.js';
import { installEditorCommand } from './commands/install-editor.js';
import { openCommand } from './commands/open.js';
import { installPluginCommand } from './commands/install-plugin.js';
import { configureCommand } from './commands/configure.js';
import { connectCommand } from './commands/connect.js';
import { removePluginCommand } from './commands/remove-plugin.js';

const require = createRequire(import.meta.url);
const pkg = require('../package.json') as { version: string };

const program = new Command();

program
  .name('unity-mcp-cli')
  .description('Cross-platform CLI tool for Unity-MCP operations')
  .version(pkg.version);

program.addCommand(createProjectCommand);
program.addCommand(installEditorCommand);
program.addCommand(openCommand);
program.addCommand(installPluginCommand);
program.addCommand(configureCommand);
program.addCommand(connectCommand);
program.addCommand(removePluginCommand);

// Custom help formatting
program.configureHelp({
  formatHelp(cmd: Command, helper: Help): string {
    const isRoot = !cmd.parent;
    const lines: string[] = [];

    // Banner
    if (isRoot) {
      lines.push(
        boxen(
          `${chalk.bold.cyan('Unity-MCP CLI')}  ${chalk.dim(`v${pkg.version}`)}\n${chalk.dim('Bridge LLMs with Unity via Model Context Protocol')}`,
          {
            padding: { top: 0, bottom: 0, left: 2, right: 2 },
            borderColor: 'cyan',
            borderStyle: 'round',
          }
        )
      );
      lines.push('');
    } else {
      lines.push(
        `${chalk.bold.cyan(cmd.name())} ${chalk.dim('\u2014')} ${cmd.description()}`
      );
      lines.push('');
    }

    // Usage
    lines.push(`${chalk.bold('Usage:')} ${chalk.yellow(helper.commandUsage(cmd))}`);
    lines.push('');

    // Commands (root only)
    const subcommands = helper.visibleCommands(cmd);
    if (subcommands.length > 0) {
      lines.push(chalk.bold('Commands:'));
      for (const sub of subcommands) {
        const name = sub.name();
        const desc = sub.description() || '';
        lines.push(`  ${chalk.yellow(name.padEnd(20))} ${chalk.dim(desc)}`);
      }
      lines.push('');
    }

    // Arguments
    const args = helper.visibleArguments(cmd);
    if (args.length > 0) {
      lines.push(chalk.bold('Arguments:'));
      for (const arg of args) {
        const term = helper.argumentTerm(arg);
        const desc = helper.argumentDescription(arg);
        lines.push(`  ${chalk.green(term.padEnd(30))} ${chalk.dim(desc)}`);
      }
      lines.push('');
    }

    // Options
    const options = helper.visibleOptions(cmd);
    if (options.length > 0) {
      lines.push(chalk.bold('Options:'));
      for (const opt of options) {
        const flags = helper.optionTerm(opt);
        const desc = helper.optionDescription(opt);
        lines.push(`  ${chalk.green(flags.padEnd(30))} ${chalk.dim(desc)}`);
      }
      lines.push('');
    }

    // Footer
    if (isRoot) {
      lines.push(
        chalk.dim('Run ') +
        chalk.yellow('unity-mcp-cli <command> --help') +
        chalk.dim(' for detailed usage of each command.')
      );
      lines.push('');
    }

    return lines.join('\n');
  },
});

// Show help when no command provided
program.action(() => {
  program.outputHelp();
});

program.parseAsync().catch((err) => {
  console.error(chalk.red(`\u2716 ${(err as Error).message || err}`));
  process.exit(1);
});
