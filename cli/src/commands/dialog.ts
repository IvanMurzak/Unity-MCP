import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { resolveAndValidateProjectPath } from '../utils/connection.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { listEditorDialogs, pressEditorDialogButton } from '../utils/editor-dialogs.js';
import { ExitCode } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';

interface DialogCliOptions {
  path?: string;
  press?: string;
  id?: string;
  format?: string;
}

export const dialogCommand = new Command('dialog')
  .description('List the modal dialogs blocking the project\'s Unity Editor, or press a button on one (Windows, macOS)')
  .argument('[path]', 'Unity project path (default: current directory)')
  .option('--path <path>', 'Unity project path')
  .option('--press <button>', 'Press the button with this label (case-insensitive)')
  .option('--id <id>', 'With --press: the dialog to answer (its id from the list); default: any dialog with that button')
  .option('--format <format>', FORMAT_OPTION_DESCRIPTION, 'text')
  .action(async (positionalPath: string | undefined, options: DialogCliOptions) => {
    // Explicitly typed: `out.fail` returns `never`, which narrows only through a declared type.
    const out: CommandOutput = new CommandOutput('dialog', parseFormat(options.format));
    if (options.id !== undefined && options.press === undefined) out.fail(ExitCode.Usage, { code: 'USAGE', message: '--id needs --press' });
    if (options.id !== undefined && !/^\d+$/.test(options.id)) out.fail(ExitCode.Usage, { code: 'USAGE', message: `--id must be a dialog id from the list (got: "${options.id}")` });
    const projectPath = resolveAndValidateProjectPath(positionalPath, options);
    const proc = findUnityProcess(projectPath);
    if (!proc) out.fail(ExitCode.Unreachable, { code: 'UNREACHABLE', message: 'No Unity Editor has this project open.' });

    if (options.press !== undefined) {
      const pressed = await pressEditorDialogButton([proc.pid], options.press, options.id);
      if (pressed.kind === 'unsupported') out.fail(ExitCode.CommandFailed, { code: 'UNSUPPORTED_PLATFORM', message: `Dialog control is supported on Windows and macOS only (this is ${pressed.platform}).` });
      if (pressed.kind !== 'pressed') out.fail(ExitCode.CommandFailed, { code: pressed.kind === 'not-found' ? 'NOT_FOUND' : 'DIALOG_ERROR', message: pressed.message });
      if (!out.machine) ui.success(`Pressed "${pressed.button}".`);
      out.succeed({ pid: proc.pid, pressed: pressed.button });
      return;
    }

    const listed = await listEditorDialogs([proc.pid]);
    if (listed.kind === 'unsupported') out.fail(ExitCode.CommandFailed, { code: 'UNSUPPORTED_PLATFORM', message: `Dialog detection is supported on Windows and macOS only (this is ${listed.platform}).` });
    if (listed.kind === 'error') out.fail(ExitCode.CommandFailed, { code: 'DIALOG_ERROR', message: listed.message });
    if (!out.machine) {
      if (listed.dialogs.length === 0) ui.success(`No modal dialog blocks the editor (PID ${proc.pid}).`);
      for (const d of listed.dialogs) {
        ui.warn(`"${d.title}" (id ${d.id})`);
        if (d.message) console.log(`  ${d.message.split('\n').join('\n  ')}`);
        console.log(`  Buttons: ${d.buttons.map(b => `[${b}]`).join(' ')}`);
      }
    }
    out.succeed({ pid: proc.pid, dialogs: listed.dialogs });
  });
