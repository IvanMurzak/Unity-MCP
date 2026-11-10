// A modal dialog blocks the editor's main thread, so a call that timed out, was refused as busy or found no answering
// editor may really be waiting for a human. Every command reports that the same way: exit 18, `AWAITING_INPUT`.

import { findUnityProcess } from './unity-process.js';
import { listEditorDialogs, type EditorDialog } from './editor-dialogs.js';
import { ExitCode, type ExitCodeValue } from './exit-codes.js';
import type { CommandOutput, EnvelopeError } from './output.js';

/** Failures a blocking dialog can cause; only these pay for the dialog scan. */
const DIALOG_SUSPECTS: ReadonlySet<ExitCodeValue> = new Set([ExitCode.Timeout, ExitCode.Busy, ExitCode.Unreachable]);

/** A windowless editor: it never shows a dialog, so scanning it only costs time (and, on macOS, a permission prompt). */
export function isBatchModeEditor(commandLine: string): boolean {
  return /(^|\s)-batchmode(\s|$)/i.test(commandLine);
}

/**
 * The modal dialogs of the project's running editor (Windows, macOS; an error such as a missing macOS
 * Accessibility permission counts as none). A `-batchmode` editor never shows one, so it is not
 * scanned.
 */
export async function findBlockingDialogs(projectPath: string): Promise<EditorDialog[]> {
  const proc = findUnityProcess(projectPath);
  if (!proc || isBatchModeEditor(proc.commandLine)) return [];
  const listed = await listEditorDialogs([proc.pid]);
  return listed.kind === 'ok' ? listed.dialogs : [];
}

export function describeDialog(d: EditorDialog): string {
  return `"${d.title}"${d.message ? `: ${d.message.replace(/\s+/g, ' ')}` : ''} [${d.buttons.join(' | ')}]`;
}

/** The awaiting-input error for `dialogs`, with the hint to answer them. */
export function awaitingInputError(dialogs: EditorDialog[]): EnvelopeError {
  return {
    code: 'AWAITING_INPUT',
    message: `The Unity Editor is blocked by a modal dialog: ${dialogs.map(describeDialog).join('; ')}. ` +
      'Answer it in the editor, or with `unity-mcp-cli dialog --press <button>`.',
  };
}

/**
 * When `exitCode` is a failure a dialog can cause and the project's editor shows one, fail with exit 18 instead (the
 * dialogs added to `data`); otherwise return so the caller reports its own failure. `raw` prints the message alone on
 * stderr, as `--raw` failures do.
 */
export async function failIfAwaitingInput(
  out: CommandOutput,
  projectPath: string,
  exitCode: ExitCodeValue,
  data: Record<string, unknown> = {},
  raw = false,
): Promise<void> {
  if (!DIALOG_SUSPECTS.has(exitCode)) return;
  const dialogs = await findBlockingDialogs(projectPath);
  if (dialogs.length === 0) return;
  const error = awaitingInputError(dialogs);
  if (raw && !out.machine) {
    process.stderr.write(error.message + '\n');
    process.exit(ExitCode.AwaitingInput);
  }
  out.fail(ExitCode.AwaitingInput, error, { ...data, dialogs });
}
