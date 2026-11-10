import { Command } from 'commander';
import { DEFAULT_SESSION_READY_TIMEOUT_MS } from '@baizor/gamedev-cli-core';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath, resolveConnection, resolveProbeUrls } from '../utils/connection.js';
import { ExitCode } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';
import { formatCompilerMessage } from '../utils/compiler-messages.js';
import { escapeAnnotationData } from '../utils/nunit-report.js';
import { compileProject, DEFAULT_COMPILE_TIMEOUT_MS } from '../lib/compile.js';
import { awaitingInputError, classifyEditorFailure, findBlockingDialogs } from './run-tool-builder.js';
import { parsePositiveIntFlag } from './open.js';

interface CompileCliOptions {
  path?: string;
  strict?: boolean;
  timeout?: string;
  readyTimeout?: string;
  once?: boolean;
  url?: string;
  token?: string;
  format?: string;
}

export const compileCommand = new Command('compile')
  .description('Check that the project\'s C# compiles: refresh assets in the editor (started headless when none runs), wait for the compile and report its errors and warnings')
  .argument('[path]', 'Unity project path (default: current directory)')
  .option('--path <path>', 'Unity project path')
  .option('--strict', 'Fail on compiler warnings too')
  .option('--timeout <seconds>', 'How long to wait for the compile and the domain reload', String(DEFAULT_COMPILE_TIMEOUT_MS / 1000))
  .option('--ready-timeout <ms>', 'How long to wait for a started editor to become ready', String(DEFAULT_SESSION_READY_TIMEOUT_MS))
  .option('--once', 'Close the editor afterwards if this call started it')
  .option('--url <url>', 'Direct server URL override (bypasses config)')
  .option('--token <token>', 'Bearer token override (bypasses config)')
  .option('--format <format>', `${FORMAT_OPTION_DESCRIPTION}, or github (GitHub Actions annotations)`, 'text')
  .addHelpText('after', '\nExit codes: 0 compiles, 6 compile errors (or warnings with --strict), 7 no editor could be reached, 14 timeout, 18 a dialog blocks the editor.')
  .action(async (positionalPath: string | undefined, options: CompileCliOptions) => {
    const format = parseFormat(options.format, ['github'] as const);
    // Explicitly typed: `out.fail` returns `never`, which narrows only through a declared type.
    const out: CommandOutput = new CommandOutput('compile', format);
    const timeoutSeconds = parsePositiveIntFlag(options.timeout, '--timeout', DEFAULT_COMPILE_TIMEOUT_MS / 1000);
    if (timeoutSeconds === 0) out.fail(ExitCode.Usage, { code: 'USAGE', message: '--timeout must be greater than 0' });
    const readyTimeoutMs = parsePositiveIntFlag(options.readyTimeout, '--ready-timeout', DEFAULT_SESSION_READY_TIMEOUT_MS);
    const projectPath = resolveAndValidateProjectPath(positionalPath, options);
    const { url, token, cloudAuthMissing, tokenFromCloudStore } = await resolveConnection(projectPath, options);
    if (cloudAuthMissing) {
      out.fail(ExitCode.CommandFailed, { code: 'NOT_AUTHENTICATED', message: 'Not logged in to ai-game.dev. Run `unity-mcp-cli login` to sign in, then retry — or pass --token / --url.' });
    }

    const spinner = out.machine || format === 'github' ? null : ui.startSpinner('Connecting...');
    const result = await compileProject({
      projectPath,
      probeUrls: resolveProbeUrls(projectPath, url, options.url),
      token,
      tokenFromCloudStore,
      overrideUrl: options.url,
      overrideToken: options.token,
      readyTimeoutMs,
      timeoutMs: timeoutSeconds * 1000,
      once: options.once === true,
      onStatus: (message) => {
        verbose(message);
        out.progress(message);
        if (spinner) spinner.text = message;
      },
    });
    spinner?.stop();

    if (result.kind === 'failure') {
      const data = result.closedPid !== undefined ? { editor: { closedPid: result.closedPid } } : null;
      if (result.reason === 'timeout' || result.reason === 'unreachable') {
        const dialogs = await findBlockingDialogs(projectPath);
        if (dialogs.length > 0) out.fail(ExitCode.AwaitingInput, awaitingInputError(dialogs), { dialogs });
      }
      if (result.reason === 'unreachable') {
        const { exitCode, code } = result.editorFailure ? classifyEditorFailure(result.editorFailure) : { exitCode: ExitCode.Unreachable, code: 'UNREACHABLE' };
        out.fail(exitCode, { code, message: result.message }, data);
      }
      if (result.reason === 'timeout') out.fail(ExitCode.Timeout, { code: 'TIMEOUT', message: result.message }, data);
      out.fail(ExitCode.CommandFailed, { code: 'TOOL_FAILED', message: result.message }, data);
    }

    const failed = result.errors.length > 0 || result.state === 'failed' || (options.strict === true && result.warnings.length > 0);
    const data = {
      compiled: !failed,
      state: result.state,
      errors: result.errors,
      warnings: result.warnings,
      editor: { ...(result.editorSource ? { source: result.editorSource } : {}), ...(result.closedPid !== undefined ? { closedPid: result.closedPid } : {}) },
    };
    if (format === 'github') {
      for (const m of [...result.errors, ...result.warnings]) {
        console.log(`::${m.severity} file=${m.file},line=${m.line},col=${m.column},title=${m.code}::${escapeAnnotationData(m.message)}`);
      }
    } else if (!out.machine) {
      // Diagnostics on stderr, in the compiler's own spelling that editors link.
      for (const m of [...result.errors, ...result.warnings]) console.error(formatCompilerMessage(m));
    }
    if (failed) {
      const message = result.errors.length > 0
        ? `Compilation failed: ${result.errors.length} error(s), ${result.warnings.length} warning(s).`
        : result.state === 'failed'
          ? 'Compilation failed (the editor reports failed scripts; see its Console).'
          : `--strict: ${result.warnings.length} compiler warning(s).`;
      out.fail(ExitCode.CommandFailed, { code: 'COMPILATION_FAILED', message }, data);
    }
    if (!out.machine) ui.success(`Scripts compile (${result.warnings.length} warning(s)).`);
    out.succeed(data);
  });
