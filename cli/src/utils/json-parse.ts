/**
 * Robust JSON parsing with auto-stringify fallback and detailed error reporting.
 *
 * 1. Try to parse the raw string as JSON.
 * 2. If that fails, wrap it in quotes (stringify) and try again.
 * 3. If still invalid, report the exact position where parsing fails.
 */
export interface JsonParseResult {
  readonly value: unknown;
  readonly raw: string;
  readonly wasStringified: boolean;
}

export class JsonParseError extends Error {
  readonly position: number;
  readonly snippet: string;

  constructor(message: string, position: number, snippet: string) {
    super(message);
    this.name = 'JsonParseError';
    this.position = position;
    this.snippet = snippet;
  }
}

/**
 * Extract the error position from a JSON.parse SyntaxError message.
 * Common formats:
 *   - "... at position 42"           (V8 / Node)
 *   - "... at line 3 column 5"       (some engines)
 *   - "Unexpected token X in JSON at position 42"
 */
function extractErrorPosition(error: SyntaxError, input: string): number {
  const msg = error.message;

  const posMatch = msg.match(/position\s+(\d+)/i);
  if (posMatch) {
    return parseInt(posMatch[1], 10);
  }

  const lineColMatch = msg.match(/line\s+(\d+)\s+column\s+(\d+)/i);
  if (lineColMatch) {
    const targetLine = parseInt(lineColMatch[1], 10);
    const targetCol = parseInt(lineColMatch[2], 10);
    const lines = input.split('\n');
    let offset = 0;
    for (let i = 0; i < targetLine - 1 && i < lines.length; i++) {
      offset += lines[i].length + 1; // +1 for newline
    }
    return offset + targetCol - 1;
  }

  return -1;
}

/**
 * Build a human-readable snippet around the error position.
 */
function buildSnippet(input: string, position: number): string {
  if (position < 0 || position > input.length) {
    return input.length <= 80 ? input : input.slice(0, 80) + '...';
  }

  const contextBefore = 20;
  const contextAfter = 20;

  const start = Math.max(0, position - contextBefore);
  const end = Math.min(input.length, position + contextAfter);

  const before = (start > 0 ? '...' : '') + input.slice(start, position);
  const errorChar = position < input.length ? input[position] : '<end>';
  const after = input.slice(position + 1, end) + (end < input.length ? '...' : '');

  const pointer = ' '.repeat(before.length) + '^';

  return `${before}${errorChar}${after}\n${pointer}`;
}

/**
 * Build a detailed error message for a JSON parse failure.
 */
function buildDetailedError(input: string, error: SyntaxError): JsonParseError {
  const position = extractErrorPosition(error, input);
  const snippet = buildSnippet(input, position);

  const posInfo = position >= 0 ? ` at position ${position}` : '';
  const message =
    `Invalid JSON${posInfo}: ${error.message}\n\n` +
    `  ${snippet.split('\n').join('\n  ')}\n`;

  return new JsonParseError(message, position, snippet);
}

/**
 * Parse a string as JSON with auto-stringify fallback and detailed errors.
 *
 * @param input  The raw string to parse as JSON.
 * @returns      A result containing the parsed value.
 * @throws       {@link JsonParseError} with position and snippet on failure.
 */
export function parseJsonRobust(input: string): JsonParseResult {
  // Attempt 1: parse as-is
  try {
    const value = JSON.parse(input);
    return { value, raw: input, wasStringified: false };
  } catch (firstError) {
    // Attempt 2: wrap in quotes (stringify the raw input) and re-parse
    const stringified = JSON.stringify(input);
    try {
      const value = JSON.parse(stringified);
      return { value, raw: stringified, wasStringified: true };
    } catch {
      // stringified JSON.stringify output should always parse, but if it
      // somehow fails, fall through to report the original error.
    }

    // Both attempts failed — report the *original* error with detail
    if (firstError instanceof SyntaxError) {
      throw buildDetailedError(input, firstError);
    }
    throw firstError;
  }
}
