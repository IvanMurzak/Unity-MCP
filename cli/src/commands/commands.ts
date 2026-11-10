import { Command, type Argument, type Option } from 'commander';
import { EXIT_CODE_DESCRIPTIONS } from '../utils/exit-codes.js';
import { parseFormat, writeEnvelope } from '../utils/output.js';

export interface CommandManifestEntry {
  name: string;
  description: string;
  arguments: Array<{ name: string; required: boolean; variadic: boolean; description: string }>;
  options: Array<{ flags: string; description: string; default?: unknown }>;
}

export interface CommandManifest {
  name: string;
  version: string;
  /** Options every command accepts (`--verbose`, `--help`) and the root's own (`--version`). */
  globalOptions: Array<{ flags: string; description: string; default?: unknown }>;
  commands: CommandManifestEntry[];
  exitCodes: typeof EXIT_CODE_DESCRIPTIONS;
}

function describeArgument(arg: Argument) {
  return { name: arg.name(), required: arg.required, variadic: arg.variadic, description: arg.description };
}

function describeOption(opt: Option) {
  return { flags: opt.flags, description: opt.description, ...(opt.defaultValue !== undefined ? { default: opt.defaultValue } : {}) };
}

/** The manifest of `root`'s commands, read from the commander tree (so it never drifts from the real CLI). */
export function buildCommandManifest(root: Command, version: string): CommandManifest {
  return {
    name: root.name(),
    version,
    globalOptions: [...root.options.filter(o => !o.hidden).map(describeOption), { flags: '-h, --help', description: 'Display help for the command' }],
    commands: root.commands.filter(cmd => !(cmd as unknown as { _hidden?: boolean })._hidden).map(cmd => ({
      name: cmd.name(),
      description: cmd.description(),
      arguments: cmd.registeredArguments.map(describeArgument),
      options: cmd.options.filter(o => !o.hidden).map(describeOption),
    })),
    exitCodes: EXIT_CODE_DESCRIPTIONS,
  };
}

/** `commands`: every command, argument, option and exit code, for agents and tooling. */
export function createCommandsCommand(getRoot: () => Command, version: string): Command {
  return new Command('commands')
    .description('List every command with its arguments, options and the exit codes (--format json: a machine-readable manifest)')
    .option('--format <format>', 'text, json or ndjson', 'text')
    .action((options: { format?: string }) => {
      const manifest = buildCommandManifest(getRoot(), version);
      const format = parseFormat(options.format);
      if (format === 'json' || format === 'ndjson') {
        writeEnvelope({ success: true, command: 'commands', data: manifest, errors: [], warnings: [] }, format);
        return;
      }
      for (const cmd of manifest.commands) {
        const args = cmd.arguments.map(a => (a.required ? `<${a.name}${a.variadic ? '...' : ''}>` : `[${a.name}${a.variadic ? '...' : ''}]`)).join(' ');
        console.log(`${manifest.name} ${cmd.name}${args ? ` ${args}` : ''}`);
        console.log(`    ${cmd.description}`);
      }
      console.log('\nExit codes:');
      for (const e of manifest.exitCodes) console.log(`  ${String(e.code).padStart(2)}  ${e.name.padEnd(15)} ${e.description}`);
    });
}
