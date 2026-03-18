import chalk from 'chalk';
import yoctoSpinner, { type Spinner } from 'yocto-spinner';
import type { Command, Help } from 'commander';

// --- Verbose mode ---
let verboseEnabled = false;

export function setVerbose(enabled: boolean): void {
  verboseEnabled = enabled;
}

export function verbose(msg: string): void {
  if (verboseEnabled) {
    if (isTTY()) {
      console.log(chalk.dim(`[verbose] ${msg}`));
    } else {
      console.log(`[verbose] ${msg}`);
    }
  }
}

// --- TTY detection ---
function isTTY(): boolean {
  return !!process.stdout.isTTY;
}

/**
 * Draw a Unicode rounded box around text with cyan borders.
 */
function drawBox(text: string): string {
  const lines = text.split('\n');
  const padding = 2;
  const maxLen = Math.max(...lines.map(l => stripAnsi(l).length));
  const innerWidth = maxLen + padding * 2;
  const pad = ' '.repeat(padding);

  const top = chalk.cyan('╭' + '─'.repeat(innerWidth) + '╮');
  const bottom = chalk.cyan('╰' + '─'.repeat(innerWidth) + '╯');
  const middle = lines.map(l => {
    const visible = stripAnsi(l).length;
    const right = ' '.repeat(maxLen - visible);
    return chalk.cyan('│') + pad + l + right + pad + chalk.cyan('│');
  });

  return [top, ...middle, bottom].join('\n');
}

/**
 * Strip ANSI escape codes from a string.
 */
function stripAnsi(str: string): string {
  // eslint-disable-next-line no-control-regex
  return str.replace(/\x1B\[[0-9;]*m/g, '');
}

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
          drawBox(
            `${chalk.bold.cyan('Unity-MCP CLI')}  ${chalk.dim(`v${appVersion}`)}\n${chalk.dim('Bridge LLMs with Unity via Model Context Protocol')}`
          )
        );
      } else {
        lines.push(
          drawBox(
            `${chalk.bold.cyan(target.name())} ${chalk.dim('\u2014')} ${target.description()}`
          )
        );
      }
      lines.push('');

      // Usage
      lines.push(`${chalk.bold('Usage:')} ${chalk.yellow(helper.commandUsage(target))}`);
      lines.push('');

      // Subcommands
      const subcommands = helper.visibleCommands(target);
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
 * Print a success message with a green checkmark.
 * In non-TTY mode, uses plain text prefix.
 */
export function success(msg: string): void {
  if (isTTY()) {
    console.log(`${chalk.green('\u2714')}  ${msg}`);
  } else {
    console.log(`SUCCESS: ${msg}`);
  }
}

/**
 * Print an error message with a red cross to stderr.
 * In non-TTY mode, uses plain text prefix.
 */
export function error(msg: string): void {
  if (isTTY()) {
    console.error(`${chalk.red('\u2716')}  ${chalk.red(msg)}`);
  } else {
    console.error(`ERROR: ${msg}`);
  }
}

/**
 * Print an info message with a blue info symbol.
 * In non-TTY mode, uses plain text prefix.
 */
export function info(msg: string): void {
  if (isTTY()) {
    console.log(`${chalk.blue('\u2139')}  ${msg}`);
  } else {
    console.log(`INFO: ${msg}`);
  }
}

/**
 * Print a warning message with a yellow warning symbol.
 * In non-TTY mode, uses plain text prefix.
 */
export function warn(msg: string): void {
  if (isTTY()) {
    console.log(`${chalk.yellow('\u26A0')}  ${chalk.yellow(msg)}`);
  } else {
    console.log(`WARN: ${msg}`);
  }
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
 * Create a no-op spinner for non-TTY environments.
 * Methods are safe to call but produce no ANSI output.
 */
function createNoOpSpinner(text: string): Spinner {
  console.log(text);
  const self: Spinner = {
    start: () => self,
    stop: () => self,
    success: (msg?: string) => { if (msg) console.log(`SUCCESS: ${msg}`); return self; },
    error: (msg?: string) => { if (msg) console.error(`ERROR: ${msg}`); return self; },
    warning: (msg?: string) => { if (msg) console.log(`WARN: ${msg}`); return self; },
    info: (msg?: string) => { if (msg) console.log(`INFO: ${msg}`); return self; },
    clear: () => self,
    get text() { return text; },
    set text(value: string) { text = value; },
    get isSpinning() { return false; },
    get color() { return 'cyan' as const; },
    set color(_: string) { /* no-op */ },
  } as unknown as Spinner;
  return self;
}

/**
 * Start a spinner with the given text. Returns a wrapped spinner instance
 * whose success/error/warning/info methods add an extra space after the symbol
 * for consistent formatting.
 *
 * In non-TTY environments (CI pipelines, piped output), returns a no-op
 * spinner that outputs plain text without ANSI escape codes.
 */
export function startSpinner(text: string): Spinner {
  if (!isTTY()) {
    return createNoOpSpinner(text);
  }

  const spinner = yoctoSpinner({ text, color: 'cyan' }).start();

  const origSuccess = spinner.success.bind(spinner);
  const origError = spinner.error.bind(spinner);
  const origWarning = spinner.warning.bind(spinner);
  const origInfo = spinner.info.bind(spinner);

  spinner.success = (msg?: string) => origSuccess(msg ? ` ${msg}` : undefined);
  spinner.error = (msg?: string) => origError(msg ? ` ${msg}` : undefined);
  spinner.warning = (msg?: string) => origWarning(msg ? ` ${msg}` : undefined);
  spinner.info = (msg?: string) => origInfo(msg ? ` ${msg}` : undefined);

  return spinner;
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

/**
 * A progress bar that overwrites itself on the same line.
 */
export interface ProgressBar {
  update(percent: number, status: string): void;
  complete(msg: string): void;
  fail(msg: string): void;
}

export function createProgressBar(): ProgressBar {
  if (!isTTY()) {
    // Plain text progress for non-interactive environments
    return {
      update(percent: number, status: string) {
        const clamped = Math.min(100, Math.max(0, percent));
        console.log(`[${clamped.toFixed(0)}%] ${status}`);
      },
      complete(msg: string) {
        console.log(`SUCCESS: ${msg}`);
      },
      fail(msg: string) {
        console.error(`ERROR: ${msg}`);
      },
    };
  }

  const barWidth = 30;
  let lastLine = '';

  function render(percent: number, status: string): string {
    const clamped = Math.min(100, Math.max(0, percent));
    const filled = Math.round((clamped / 100) * barWidth);
    const empty = barWidth - filled;
    const bar = chalk.cyan('\u2588').repeat(filled) + chalk.dim('\u2591').repeat(empty);
    const pct = `${clamped.toFixed(1)}%`.padStart(6);
    return `  ${bar} ${chalk.bold(pct)}  ${chalk.dim(status)}`;
  }

  return {
    update(percent: number, status: string) {
      if (lastLine) {
        process.stderr.write('\r\x1b[K');
      }
      lastLine = render(percent, status);
      process.stderr.write(lastLine);
    },
    complete(msg: string) {
      if (lastLine) {
        process.stderr.write('\r\x1b[K');
      }
      console.log(`${chalk.green('\u2714')}  ${msg}`);
      lastLine = '';
    },
    fail(msg: string) {
      if (lastLine) {
        process.stderr.write('\r\x1b[K');
      }
      console.error(`${chalk.red('\u2716')}  ${chalk.red(msg)}`);
      lastLine = '';
    },
  };
}
