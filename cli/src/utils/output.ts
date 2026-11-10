// Machine-readable command results (`--format json|ndjson`): the envelope, its writer and the exit safety net are the
// shared contract of @baizor/gamedev-cli-core. The result goes to stdout and nothing else does; progress and logs go to
// stderr. This file adds the unity-mcp-cli way of ending a command: `fail` prints and exits at once.

import {
  CommandOutput as ContractOutput,
  ExitCode,
  UsageError,
  parseFormat as parseContractFormat,
  type EnvelopeError,
  type ExitCodeValue,
  type OutputFormat,
} from '@baizor/gamedev-cli-core';
import * as ui from './ui.js';

export {
  FORMAT_OPTION_DESCRIPTION,
  OUTPUT_FORMATS,
  isMachineFormat,
  renderEnvelope as formatEnvelope,
  writeEnvelope,
} from '@baizor/gamedev-cli-core';
export type { Envelope, EnvelopeError, OutputFormat } from '@baizor/gamedev-cli-core';

/** The value of a `--format` flag, or a usage error (exit 2). `extra` lists command-specific formats (`github`). */
export function parseFormat<F extends string = never>(raw: string | undefined, extra: readonly F[] = []): OutputFormat | F {
  try {
    return parseContractFormat(raw, extra);
  } catch (err) {
    if (!(err instanceof UsageError)) throw err;
    ui.error(err.message);
    process.exit(ExitCode.Usage);
  }
}

/** Collects warnings and builds the envelope of one command run. */
export class CommandOutput extends ContractOutput {
  /** Print the success envelope (machine formats) and return; text output is the caller's. */
  succeed(data: unknown): void {
    this.emit(ExitCode.Success, data);
  }

  /**
   * Print the failure (an envelope in machine formats, `ui.error` lines otherwise) and exit with `exitCode`.
   * `hint` is printed in text mode only.
   */
  fail(exitCode: ExitCodeValue, errors: EnvelopeError | EnvelopeError[], data: unknown = null, hint?: string): never {
    const list = Array.isArray(errors) ? errors : [errors];
    for (const err of list) this.record(err.code, err.message);
    if (this.machine) {
      this.emit(exitCode, data);
    } else {
      for (const err of list) ui.error(err.message);
      if (hint) ui.info(hint);
    }
    process.exit(exitCode);
  }
}
