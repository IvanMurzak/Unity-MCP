// Machine-readable command results (`--format json|ndjson`), the same envelope in unity-mcp-cli, unreal-mcp-cli and
// godot-cli. The result goes to stdout and nothing else does; progress and logs go to stderr.

import * as ui from './ui.js';
import { ExitCode, type ExitCodeValue } from './exit-codes.js';

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

/**
 * Reports progress the way `format` wants it: an ndjson line on stderr, nothing for json (stdout is reserved for the
 * envelope), the text callback otherwise.
 */
export function reportProgress(format: string, command: string, message: string, text?: (message: string) => void): void {
  if (format === 'ndjson') process.stderr.write(JSON.stringify({ type: 'progress', command, message }) + '\n');
  else if (format === 'text') text?.(message);
}

/** Collects warnings and builds the envelope of one command run. */
export class CommandOutput {
  readonly warnings: string[] = [];

  constructor(readonly command: string, readonly format: OutputFormat | string) {}

  get machine(): boolean {
    return isMachineFormat(this.format);
  }

  progress(message: string, text?: (message: string) => void): void {
    reportProgress(this.format, this.command, message, text);
  }

  warn(message: string): void {
    this.warnings.push(message);
    if (!this.machine) ui.warn(message);
  }

  /** Print the success envelope (machine formats) and return; text output is the caller's. */
  succeed(data: unknown): void {
    if (this.machine) {
      process.stdout.write(formatEnvelope({ success: true, command: this.command, data, errors: [], warnings: this.warnings }, this.format as 'json' | 'ndjson'));
    }
  }

  /**
   * Print the failure (an envelope in machine formats, `ui.error` lines otherwise) and exit with `exitCode`.
   * `hint` is printed in text mode only.
   */
  fail(exitCode: ExitCodeValue, errors: EnvelopeError | EnvelopeError[], data: unknown = null, hint?: string): never {
    const list = Array.isArray(errors) ? errors : [errors];
    if (this.machine) {
      process.stdout.write(formatEnvelope({ success: false, command: this.command, data, errors: list, warnings: this.warnings }, this.format as 'json' | 'ndjson'));
    } else {
      for (const err of list) ui.error(err.message);
      if (hint) ui.info(hint);
    }
    process.exit(exitCode);
  }
}
