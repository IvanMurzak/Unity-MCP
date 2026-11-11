import { Command } from 'commander';
import { buildCommandManifest, toolFailureCodeLine } from '@baizor/gamedev-cli-core';
import { parseFormat, writeEnvelope } from '../utils/output.js';

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
      console.log('\nError codes of a failed tool call (errors[].code):');
      for (const e of manifest.errorCodes) console.log(`  ${toolFailureCodeLine(e)}`);
    });
}
