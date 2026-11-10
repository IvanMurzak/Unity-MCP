import { Command } from 'commander';
import * as ui from '../utils/ui.js';
import { verbose } from '../utils/ui.js';
import { resolveAndValidateProjectPath } from '../utils/connection.js';
import { ExitCode } from '../utils/exit-codes.js';
import { CommandOutput, FORMAT_OPTION_DESCRIPTION, parseFormat } from '../utils/output.js';
import { formatCompilerMessage } from '../utils/compiler-messages.js';
import { toGitHubAnnotations, escapeAnnotationData, type TestReport } from '../utils/nunit-report.js';
import { DEFAULT_TEST_TIMEOUT_SECONDS, runTests, type RunTestsResult, type TestMode } from '../lib/run-tests.js';
import { parsePositiveIntFlag } from './open.js';

interface TestCliOptions {
  path?: string;
  mode?: string;
  filter?: string;
  category?: string;
  assembly?: string;
  output?: string;
  junitOutput?: string;
  logFile?: string;
  timeout?: string;
  nographics?: boolean;
  unity?: string;
  editorPath?: string;
  format?: string;
}

function parseMode(raw: string): TestMode | undefined {
  const value = raw.trim().toLowerCase();
  if (value === 'editmode' || value === 'edit') return 'EditMode';
  if (value === 'playmode' || value === 'play') return 'PlayMode';
  return undefined;
}

/** The report as a compact JSON payload: counts, failures, and where the files are. */
function reportData(result: Extract<RunTestsResult, { kind: 'completed' }>) {
  const { summary, tests } = result.report;
  return {
    verdict: result.verdict,
    summary,
    failures: tests.filter(t => t.outcome === 'failed').map(t => ({
      fullName: t.fullName, message: t.message ?? null, stackTrace: t.stackTrace ?? null,
    })),
    report: result.paths.output,
    ...(result.paths.junitOutput ? { junitReport: result.paths.junitOutput } : {}),
    logFile: result.paths.logFile,
    editorExitCode: result.editorExitCode,
  };
}

function printSummary(report: TestReport): void {
  const { summary } = report;
  const line = `${summary.total} tests: ${summary.passed} passed, ${summary.failed} failed, ${summary.skipped + summary.inconclusive} skipped (${summary.durationSeconds.toFixed(1)} s)`;
  if (summary.failed > 0) ui.error(line);
  else ui.success(line);
  for (const t of report.tests.filter(c => c.outcome === 'failed')) {
    console.log(`  FAIL ${t.fullName}`);
    if (t.message) console.log(`       ${t.message.split('\n').join('\n       ')}`);
  }
}

export const testCommand = new Command('test')
  .description('Run the project\'s Unity tests in a batch-mode editor (-runTests; no plugin or server needed) and report the verdict')
  .argument('[path]', 'Unity project path (default: current directory)')
  .argument('[editorArgs...]', 'Extra Unity Editor arguments, after --')
  .option('--path <path>', 'Unity project path')
  .option('--mode <mode>', 'EditMode or PlayMode', 'EditMode')
  .option('--filter <pattern>', 'Run only tests whose full name matches (Unity -testFilter: names or a regex, ";" separates several)')
  .option('--category <names>', 'Run only these NUnit categories (Unity -testCategory)')
  .option('--assembly <names>', 'Run only these test assemblies (Unity -assemblyNames, ";" separates several)')
  .option('--output <file>', 'NUnit report path', 'test-results.xml')
  .option('--junit-output <file>', 'Also write a JUnit report (for CI dashboards)')
  .option('--log-file <file>', 'Editor log path (default: the report path with .log)')
  .option('--timeout <seconds>', 'Stop the editor after this many seconds', String(DEFAULT_TEST_TIMEOUT_SECONDS))
  .option('--nographics', 'Run without a GPU (-nographics)')
  .option('--unity <version>', 'Unity Editor version to use (default: the project\'s)')
  .option('--editor-path <path>', 'Explicit path to the Unity Editor executable')
  .option('--format <format>', `${FORMAT_OPTION_DESCRIPTION}, or github (GitHub Actions annotations)`, 'text')
  .addHelpText('after', '\nExit codes: 0 all passed, 8 tests failed, 6 no verdict (compile errors, license, crash, project open), 14 timeout, 2 bad arguments.')
  .action(async (positionalPath: string | undefined, editorArgs: string[], options: TestCliOptions) => {
    const format = parseFormat(options.format, ['github'] as const);
    // Explicitly typed: `out.fail` returns `never`, which narrows only through a declared type.
    const out: CommandOutput = new CommandOutput('test', format);
    const mode = parseMode(options.mode ?? 'EditMode');
    if (!mode) out.fail(ExitCode.Usage, { code: 'USAGE', message: `--mode must be EditMode or PlayMode (got: "${options.mode}")` });
    const timeoutSeconds = parsePositiveIntFlag(options.timeout, '--timeout', DEFAULT_TEST_TIMEOUT_SECONDS);
    if (timeoutSeconds === 0) out.fail(ExitCode.Usage, { code: 'USAGE', message: '--timeout must be greater than 0' });
    // `test -- -foo` leaves no path: the first editor argument (they all start with '-') lands in [path].
    if (positionalPath?.startsWith('-')) {
      editorArgs = [positionalPath, ...editorArgs];
      positionalPath = undefined;
    }
    const projectPath = resolveAndValidateProjectPath(positionalPath, options);

    const human = !out.machine;
    const spinner = human && format !== 'github' ? ui.startSpinner('Preparing the test run...') : null;
    const result = await runTests({
      projectPath,
      mode,
      filter: options.filter,
      category: options.category,
      assemblies: options.assembly,
      output: options.output,
      junitOutput: options.junitOutput,
      logFile: options.logFile,
      timeoutSeconds,
      noGraphics: options.nographics === true,
      editorPath: options.editorPath,
      unityVersion: options.unity,
      extraArgs: editorArgs,
      onStatus: (message) => {
        verbose(message);
        out.progress(message);
        if (spinner) spinner.text = message;
      },
    });
    spinner?.stop();

    if (result.kind === 'failure') {
      const data = {
        ...(result.paths ? { report: result.paths.output ?? null, logFile: result.paths.logFile ?? null } : {}),
        ...(result.editorExitCode !== undefined ? { editorExitCode: result.editorExitCode } : {}),
        ...(result.compilerErrors ? { compilerErrors: result.compilerErrors } : {}),
        ...(result.logTail ? { logTail: result.logTail } : {}),
      };
      if (format === 'github') {
        for (const err of result.compilerErrors ?? []) {
          console.log(`::error file=${err.file},line=${err.line},col=${err.column},title=${err.code}::${escapeAnnotationData(err.message)}`);
        }
        console.log(`::error title=Unity test run::${escapeAnnotationData(result.message)}`);
      } else if (human) {
        for (const err of result.compilerErrors ?? []) console.log(formatCompilerMessage(err));
        if (result.logTail && result.logTail.length > 0 && !(result.compilerErrors?.length)) {
          ui.info('Last lines of the editor log:');
          for (const line of result.logTail) console.log(`  ${line}`);
        }
      }
      switch (result.reason) {
        case 'usage':
          out.fail(ExitCode.Usage, { code: 'USAGE', message: result.message }, data);
        case 'timeout':
          out.fail(ExitCode.Timeout, { code: 'TEST_TIMED_OUT', message: result.message }, data);
        case 'project-open':
          out.fail(ExitCode.CommandFailed, { code: 'PROJECT_OPEN', message: result.message }, data,
            'Close that editor (`unity-mcp-cli close`), or run the tests inside it: `unity-mcp-cli run-tool tests-run`.');
        case 'editor-not-found':
          out.fail(ExitCode.CommandFailed, { code: 'EDITOR_NOT_FOUND', message: result.message }, data,
            'Install it with `unity-mcp-cli install-unity --path <project>`, or pass --editor-path.');
        default:
          out.fail(ExitCode.CommandFailed, { code: 'TEST_RUN_ERROR', message: result.message }, data);
      }
    }

    for (const warning of result.warnings) out.warn(warning);
    const data = reportData(result);
    if (format === 'github') {
      for (const line of toGitHubAnnotations(result.report, projectPath)) console.log(line);
      for (const warning of result.warnings) console.log(`::warning title=Unity tests::${escapeAnnotationData(warning)}`);
      printSummary(result.report);
    } else if (human) {
      printSummary(result.report);
      ui.label('Report', result.paths.output);
      if (result.paths.junitOutput) ui.label('JUnit', result.paths.junitOutput);
      ui.label('Log', result.paths.logFile);
    }
    if (result.verdict === 'failed') {
      out.fail(ExitCode.TestsFailed, { code: 'TESTS_FAILED', message: `${result.report.summary.failed} test(s) failed.` }, data);
    }
    out.succeed(data);
  });
