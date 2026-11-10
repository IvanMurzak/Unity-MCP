import chalk from 'chalk';
import yoctoSpinner, { type Spinner } from 'yocto-spinner';
import type { Command, Help } from 'commander';

// --- Verbose mode ---
let verboseEnabled = false;

export function setVerbose(enabled: boolean): void {
  verboseEnabled = enabled;
}

/** A diagnostic line on stderr, so stdout stays the command's result. */
export function verbose(msg: string): void {
  if (verboseEnabled) {
    if (process.stderr.isTTY) {
      console.error(chalk.dim(`[verbose] ${msg}`));
    } else {
      console.error(`[verbose] ${msg}`);
    }
  }
}

// --- TTY detection ---
function isTTY(): boolean {
  return !!process.stdout.isTTY;
}

/** The root help's featured commands: how an agent gets from nothing to a working call. */
const ROOT_EDITOR_COMMANDS: ReadonlyArray<readonly [string, string, string]> = [
  ['rql', "rql '<query>'", 'read or change scenes, GameObjects, components, assets (RQL; recipes below)'],
  ['exec', "exec '<C# code>'", 'C# in the editor (the last expression is the result)'],
  ['tools', 'tools [name]', "the editor's AI tools, or one in detail; run-tool <name> --input '<json>' calls one"],
];
const ROOT_CODE_COMMANDS: ReadonlyArray<readonly [string, string, string]> = [
  ['compile', 'compile', 'check that the scripts compile (file:line errors)'],
  ['test', 'test', 'run the Unity tests (no plugin needed)'],
];
/** How many of `rql --help`'s recipes the root help shows. */
const ROOT_RQL_RECIPES = 6;

/** RQL recipes: [query, what it does]. */
export type RqlRecipes = ReadonlyArray<readonly [string, string]>;

/**
 * The root help: short on purpose (agents read it first, and every later turn re-reads it): the everyday commands, the
 * other command names, and the RQL recipes most tasks need. Each command's own --help has the details.
 */
function formatRootHelp(target: Command, helper: Help, appVersion: string | undefined, recipes: RqlRecipes): string {
  const name = target.name();
  const featured = new Set([...ROOT_EDITOR_COMMANDS, ...ROOT_CODE_COMMANDS].map(([cmd]) => cmd));
  const row = ([, usage, what]: readonly [string, string, string]) => `  ${chalk.yellow(usage.padEnd(26))} ${chalk.dim(what)}`;
  const others = helper.visibleCommands(target).map((c) => c.name()).filter((c) => !featured.has(c) && c !== 'help');
  const lines = [
    `${chalk.bold.cyan('Unity-MCP CLI')}${appVersion ? ` v${appVersion}` : ''}: drive the Unity Editor of the project in the current folder (or --path <dir>).`,
    '',
    chalk.bold('Editor work (the first call starts a headless editor and keeps it warm; no open / status needed):'),
    ...ROOT_EDITOR_COMMANDS.map(row),
    chalk.bold('Code:'),
    ...ROOT_CODE_COMMANDS.map(row),
    `${chalk.bold('More commands:')} ${others.join(' ')}`,
  ];
  if (recipes.length > 0) {
    lines.push('', chalk.bold('RQL recipes (strings inside the query take double quotes; a set / call is saved to disk at once):'));
    for (const [query, what] of recipes.slice(0, ROOT_RQL_RECIPES)) lines.push(`  ${chalk.yellow(`rql '${query}'`)}  ${chalk.dim(`# ${what}`)}`);
  }
  lines.push(
    '',
    chalk.dim(`Details: ${name} <command> --help (rql --help: operators, more recipes). Machine-readable output: --format json.`),
    '',
  );
  return lines.join('\n');
}

/**
 * Apply styled help formatting to a Commander command.
 * Call this on every Command instance (root and subcommands)
 * so that --help / -h always renders the fancy UI.
 *
 * @param appVersion - The CLI version string, shown in the root help.
 */
export function configureStyledHelp(cmd: Command, appVersion?: string, rootRecipes: RqlRecipes = []): Command {
  cmd.configureHelp({
    formatHelp(target: Command, helper: Help): string {
      if (!target.parent) return formatRootHelp(target, helper, appVersion, rootRecipes);
      const lines: string[] = [];

      // A plain line, not a box: agents read this help, and box borders cost as much as the text again.
      lines.push(`${chalk.bold.cyan(target.name())} ${chalk.dim('\u2014')} ${target.description()}`);
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
  // Progress, not the result: on stderr, so a piped stdout holds only the command's output.
  console.error(text);
  const self: Spinner = {
    start: () => self,
    stop: () => self,
    success: (msg?: string) => { if (msg) console.error(`SUCCESS: ${msg}`); return self; },
    error: (msg?: string) => { if (msg) console.error(`ERROR: ${msg}`); return self; },
    warning: (msg?: string) => { if (msg) console.error(`WARN: ${msg}`); return self; },
    info: (msg?: string) => { if (msg) console.error(`INFO: ${msg}`); return self; },
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
export function badge(visible: boolean): string {
  return visible
    ? chalk.bgGreen.black('[visible]')
    : chalk.bgRed.white('[hidden]');
}

/**
 * Print a feature row with a styled badge and name.
 */
export function featureRow(name: string, visible: boolean): void {
  console.log(`    ${badge(visible)} ${name}`);
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
