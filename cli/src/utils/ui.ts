import chalk from 'chalk';
import ora, { type Ora } from 'ora';
import boxen from 'boxen';

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
 * Print an error message with a red cross and exit.
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
 * Print a divider line.
 */
export function divider(): void {
  console.log(chalk.dim('\u2500'.repeat(50)));
}
