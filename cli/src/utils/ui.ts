import chalk from 'chalk';
import ora, { type Ora } from 'ora';
import boxen from 'boxen';
import type { Command, Help } from 'commander';

/**
 * Apply styled help formatting to a Commander command.
 * Call this on every Command instance (root and subcommands)
 * so that --help / -h always renders the fancy UI.
 *
 * @param appVersion - The CLI version string, shown in the root banner.
 */
export function configureStyledHelp(cmd: Command, appVersion?: string): Command {
  cmd.configureHelp({
    formatHelp(target: Command, helper: Help): string {
      const isRoot = !target.parent;
      const lines: string[] = [];

      // Banner for root, styled title for subcommands
      if (isRoot && appVersion) {
        lines.push(
          boxen(
            `${chalk.bold.cyan('Unity-MCP CLI')}  ${chalk.dim(`v${appVersion}`)}\n${chalk.dim('Bridge LLMs with Unity via Model Context Protocol')}`,
            {
              padding: { top: 0, bottom: 0, left: 2, right: 2 },
              borderColor: 'cyan',
              borderStyle: 'round',
            }
          )
        );
      } else {
        lines.push(
          boxen(
            `${chalk.bold.cyan(target.name())} ${chalk.dim('\u2014')} ${target.description()}`,
            {
              padding: { top: 0, bottom: 0, left: 2, right: 2 },
              borderColor: 'cyan',
              borderStyle: 'round',
            }
          )
        );
      }
      lines.push('');

      // Usage
      lines.push(`${chalk.bold('Usage:')} ${chalk.yellow(helper.commandUsage(target))}`);
      lines.push('');

      // Subcommands
      const subcommands = helper.visibleCommands(target)
        .sort((a, b) => a.name().localeCompare(b.name()));
      if (subcommands.length > 0) {
        lines.push(chalk.bold('Commands:'));
        for (const sub of subcommands) {
          lines.push(`  ${chalk.yellow(sub.name().padEnd(20))} ${chalk.dim(sub.description() || '')}`);
        }
        lines.push('');
      }

      // Arguments
      const args = helper.visibleArguments(target);
      if (args.length > 0) {
        lines.push(chalk.bold('Arguments:'));
        for (const arg of args) {
          lines.push(`  ${chalk.green(helper.argumentTerm(arg).padEnd(30))} ${chalk.dim(helper.argumentDescription(arg))}`);
        }
        lines.push('');
      }

      // Options
      const options = helper.visibleOptions(target);
      if (options.length > 0) {
        lines.push(chalk.bold('Options:'));
        for (const opt of options) {
          lines.push(`  ${chalk.green(helper.optionTerm(opt).padEnd(30))} ${chalk.dim(helper.optionDescription(opt))}`);
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
  return cmd;
}

/**
 * Display a styled banner with the app name and optional subtitle.
 */
export function banner(title: string, subtitle?: string): void {
  const lines = [chalk.bold.cyan(title)];
  if (subtitle) {
    lines.push(chalk.dim(subtitle));
  }
  console.log(
    boxen(lines.join('\n'), {
      padding: { top: 0, bottom: 0, left: 2, right: 2 },
      borderColor: 'cyan',
      borderStyle: 'round',
    })
  );
}

/**
 * Print a success message with a green checkmark.
 */
export function success(msg: string): void {
  console.log(`${chalk.green('\u2714')} ${msg}`);
}

/**
 * Print an error message with a red cross to stderr.
 */
export function error(msg: string): void {
  console.error(`${chalk.red('\u2716')} ${chalk.red(msg)}`);
}

/**
 * Print an info message with a blue info symbol.
 */
export function info(msg: string): void {
  console.log(`${chalk.blue('\u2139')} ${msg}`);
}

/**
 * Print a warning message with a yellow warning symbol.
 */
export function warn(msg: string): void {
  console.log(`${chalk.yellow('\u26A0')} ${chalk.yellow(msg)}`);
}

/**
 * Print a bold cyan section heading.
 */
export function heading(msg: string): void {
  console.log(`\n${chalk.bold.cyan(msg)}`);
}

/**
 * Print a formatted key-value label.
 */
export function label(key: string, value: string): void {
  console.log(`  ${chalk.bold(key + ':')} ${value}`);
}

/**
 * Start an ora spinner with the given text. Returns the spinner instance.
 */
export function startSpinner(text: string): Ora {
  return ora({ text, color: 'cyan' }).start();
}

/**
 * Format a command name and description for help output.
 */
export function formatCommand(name: string, desc: string): string {
  return `  ${chalk.yellow(name.padEnd(20))} ${chalk.dim(desc)}`;
}

/**
 * Format an option flag and description for help output.
 */
export function formatOption(flags: string, desc: string): string {
  return `  ${chalk.green(flags.padEnd(30))} ${chalk.dim(desc)}`;
}

/**
 * Format a status badge (enabled/disabled).
 */
export function badge(enabled: boolean): string {
  return enabled
    ? chalk.bgGreen.black('[enabled]')
    : chalk.bgRed.white('[disabled]');
}

/**
 * Print a feature row with a styled badge and name.
 */
export function featureRow(name: string, enabled: boolean): void {
  console.log(`    ${badge(enabled)} ${name}`);
}

/**
 * Print a divider line.
 */
export function divider(): void {
  console.log(chalk.dim('\u2500'.repeat(50)));
}
