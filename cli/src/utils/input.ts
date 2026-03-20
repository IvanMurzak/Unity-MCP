import * as fs from 'fs';
import * as path from 'path';
import * as ui from './ui.js';
import { verbose } from './ui.js';
import { parseJsonRobust, JsonParseError } from './json-parse.js';

export interface InputOptions {
  input?: string;
  inputFile?: string;
}

/**
 * Parse tool input from --input, --input-file, or stdin.
 *
 * - `--input-file -` or `--input-file /dev/stdin` reads from stdin (cross-platform).
 * - `--input-file <path>` reads from a file.
 * - `--input <json>` parses inline JSON.
 * - Falls back to `{}` if nothing provided.
 */
export function parseInput(options: InputOptions): string {
  if (options.inputFile) {
    const isStdin = options.inputFile === '-' || options.inputFile === '/dev/stdin';
    let content: string;

    if (isStdin) {
      verbose('Reading input from stdin...');
      try {
        content = fs.readFileSync(0, 'utf-8');
      } catch {
        ui.error('Failed to read from stdin.');
        process.exit(1);
      }
      if (!content.trim()) {
        ui.error('No input received from stdin.');
        process.exit(1);
      }
    } else {
      const filePath = path.resolve(options.inputFile);
      if (!fs.existsSync(filePath)) {
        ui.error(`Input file does not exist: ${filePath}`);
        process.exit(1);
      }
      content = fs.readFileSync(filePath, 'utf-8');
    }

    try {
      const result = parseJsonRobust(content);
      const source = isStdin ? 'stdin' : options.inputFile;
      if (result.wasStringified) {
        ui.error(`Input from ${source} does not contain valid JSON`);
        process.exit(1);
      }
      return result.raw;
    } catch (err) {
      const source = isStdin ? 'stdin' : options.inputFile;
      if (err instanceof JsonParseError) {
        ui.error(`Input from ${source}: ${err.message}`);
      } else {
        ui.error(`Input from ${source} does not contain valid JSON`);
      }
      process.exit(1);
    }
  }

  if (options.input) {
    try {
      const result = parseJsonRobust(options.input);
      if (result.wasStringified) {
        ui.error('--input must be valid JSON');
        process.exit(1);
      }
      return result.raw;
    } catch (err) {
      if (err instanceof JsonParseError) {
        ui.error(`--input must be valid JSON\n${err.message}`);
      } else {
        ui.error('--input must be valid JSON');
      }
      process.exit(1);
    }
  }

  return '{}';
}
