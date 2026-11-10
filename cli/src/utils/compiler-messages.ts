// C# compiler diagnostics as Unity prints them in the Editor log and the Console:
// `Assets/Scripts/Player.cs(12,5): error CS0103: The name 'x' does not exist in the current context`.

export interface CompilerMessage {
  severity: 'error' | 'warning';
  /** Project-relative path with `/` separators, as Unity prints it. */
  file: string;
  line: number;
  column: number;
  /** `CS0103`. */
  code: string;
  message: string;
}

const PATTERN = /^\s*(.+?\.cs)\((\d+),(\d+)\):\s*(error|warning)\s+([A-Z]+\d+):\s*(.*?)\s*$/;

/** One diagnostic from one line, or undefined. */
export function parseCompilerMessage(line: string): CompilerMessage | undefined {
  const m = PATTERN.exec(line);
  if (!m) return undefined;
  return {
    severity: m[4] as 'error' | 'warning',
    file: m[1].replace(/\\/g, '/'),
    line: parseInt(m[2], 10),
    column: parseInt(m[3], 10),
    code: m[5],
    message: m[6],
  };
}

/** Every diagnostic in `text` (a log, or console messages joined by newlines), each once, in order. */
export function parseCompilerMessages(text: string): CompilerMessage[] {
  const seen = new Set<string>();
  const result: CompilerMessage[] = [];
  for (const line of text.split(/\r?\n/)) {
    const msg = parseCompilerMessage(line);
    if (!msg) continue;
    const key = formatCompilerMessage(msg);
    if (seen.has(key)) continue;
    seen.add(key);
    result.push(msg);
  }
  return result;
}

/** `file(line,col): error CS0103: message` — the compiler's own spelling, which editors and CI link. */
export function formatCompilerMessage(msg: CompilerMessage): string {
  return `${msg.file}(${msg.line},${msg.column}): ${msg.severity} ${msg.code}: ${msg.message}`;
}
