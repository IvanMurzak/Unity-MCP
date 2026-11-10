// Machine-readable command results (`--format json|ndjson`), the same envelope in unity-mcp-cli, unreal-mcp-cli and
// godot-cli. The result goes to stdout and nothing else does; progress and logs go to stderr.

import * as fs from 'fs';
import * as ui from './ui.js';
import { EXIT_CODE_DESCRIPTIONS, ExitCode, type ExitCodeValue } from './exit-codes.js';

export type OutputFormat = 'text' | 'json' | 'ndjson';

export const OUTPUT_FORMATS: readonly OutputFormat[] = ['text', 'json', 'ndjson'];

/** Help text of the `--format` option. */
export const FORMAT_OPTION_DESCRIPTION = 'Output format: text, json (one result envelope on stdout) or ndjson (progress lines on stderr, the envelope as one line on stdout)';

export interface EnvelopeError {
  /** Stable UPPER_SNAKE token a script can match (never a translated message). */
  code: string;
  message: string;
}

export interface Envelope<T = unknown> {
  success: boolean;
  command: string;
  data: T;
  errors: EnvelopeError[];
  warnings: string[];
}

/**
 * The value of a `--format` flag, or a usage error (exit 2). `extra` lists command-specific formats (`github`).
 */
export function parseFormat<F extends string = OutputFormat>(raw: string | undefined, extra: readonly F[] = []): OutputFormat | F {
  const value = (raw ?? 'text').trim().toLowerCase();
  const allowed: string[] = [...OUTPUT_FORMATS, ...extra];
  if (!allowed.includes(value)) {
    ui.error(`--format must be one of: ${allowed.join(', ')} (got: "${raw}")`);
    process.exit(ExitCode.Usage);
  }
  return value as OutputFormat | F;
}

export function isMachineFormat(format: string): format is 'json' | 'ndjson' {
  return format === 'json' || format === 'ndjson';
}

/** The envelope as `format` prints it (with the trailing newline). */
export function formatEnvelope(envelope: Envelope, format: 'json' | 'ndjson'): string {
  return (format === 'json' ? JSON.stringify(envelope, null, 2) : JSON.stringify(envelope)) + '\n';
}

/** Whether an envelope already went out: exactly one per run. */
let envelopeWritten = false;

/**
 * Write the envelope synchronously: `process.stdout.write` to a pipe is asynchronous on macOS, so a `process.exit` right
 * after it could cut the JSON short.
 */
export function writeEnvelope(envelope: Envelope, format: 'json' | 'ndjson'): void {
  if (envelopeWritten) return;
  envelopeWritten = true;
  const buffer = Buffer.from(formatEnvelope(envelope, format), 'utf-8');
  let offset = 0;
  while (offset < buffer.length) {
    try {
      offset += fs.writeSync(1, buffer, offset);
    } catch (err) {
      if ((err as NodeJS.ErrnoException).code !== 'EAGAIN') throw err;
    }
  }
}

/** The output of the command being run (one per process), for the exit safety net. */
let active: CommandOutput | undefined;

function onExit(code: number): void {
  if (!active?.machine || envelopeWritten) return;
  // A path that exited without an envelope (a shared validator, an unexpected error): stdout still gets one.
  const name = EXIT_CODE_DESCRIPTIONS.find(e => e.code === code)?.name ?? 'ERROR';
  writeEnvelope({
    success: code === 0,
    command: active.command,
    data: null,
    errors: code === 0 ? [] : [{ code: name, message: 'The command failed before it produced a result; see stderr for details.' }],
    warnings: active.warnings,
  }, active.format as 'json' | 'ndjson');
}

/** Collects warnings and builds the envelope of one command run. */
export class CommandOutput {
  readonly warnings: string[] = [];

  constructor(readonly command: string, readonly format: OutputFormat | string) {
    if (active === undefined) process.on('exit', onExit);
    active = this;
    // In a machine format stdout carries the envelope and nothing else: anything printed with console.log (shared
    // helpers, hints, verbose lines) goes to stderr.
    if (this.machine) console.log = console.error;
  }

  get machine(): boolean {
    return isMachineFormat(this.format);
  }

  /** A progress line on stderr in ndjson; nothing otherwise (json reserves stdout for the envelope, text has spinners). */
  progress(message: string): void {
    if (this.format === 'ndjson') process.stderr.write(JSON.stringify({ type: 'progress', command: this.command, message }) + '\n');
  }

  warn(message: string): void {
    this.warnings.push(message);
    if (!this.machine) ui.warn(message);
  }

  /** Print the success envelope (machine formats) and return; text output is the caller's. */
  succeed(data: unknown): void {
    if (this.machine) {
      writeEnvelope({ success: true, command: this.command, data, errors: [], warnings: this.warnings }, this.format as 'json' | 'ndjson');
    }
  }

  /**
   * Print the failure (an envelope in machine formats, `ui.error` lines otherwise) and exit with `exitCode`.
   * `hint` is printed in text mode only.
   */
  fail(exitCode: ExitCodeValue, errors: EnvelopeError | EnvelopeError[], data: unknown = null, hint?: string): never {
    const list = Array.isArray(errors) ? errors : [errors];
    if (this.machine) {
      writeEnvelope({ success: false, command: this.command, data, errors: list, warnings: this.warnings }, this.format as 'json' | 'ndjson');
    } else {
      for (const err of list) ui.error(err.message);
      if (hint) ui.info(hint);
    }
    process.exit(exitCode);
  }
}
