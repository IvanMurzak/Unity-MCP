// Library-safe `runTests`: run a project's Unity tests through the editor's own command line (`-batchmode -runTests`),
// with no MCP plugin or server involved, and turn the NUnit report into a verdict. No console output, no exit.

import { execFile, spawn, type ChildProcess } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import { defaultSessionProcessOps } from '@baizor/gamedev-cli-core';
import { findEditorPath, getProjectEditorVersion } from '../utils/unity-editor.js';
import { findUnityProcess } from '../utils/unity-process.js';
import { parseCompilerMessages, type CompilerMessage } from '../utils/compiler-messages.js';
import { parseNUnitReport, toJUnitXml, type TestReport } from '../utils/nunit-report.js';
import { hasLiveSessionLease, validateEditorPath } from './editor-session.js';

export type TestMode = 'EditMode' | 'PlayMode';

/** Editor arguments `runTests` manages itself; passing one in `extraArgs` is a usage error. */
export const RESERVED_TEST_ARGS: readonly string[] = [
  '-projectPath', '-batchmode', '-runTests', '-testPlatform', '-testResults', '-testFilter', '-testCategory',
  '-assemblyNames', '-logFile', '-quit',
];

/** Default `--timeout`: an hour, so a hung editor never blocks a CI job forever. */
export const DEFAULT_TEST_TIMEOUT_SECONDS = 3600;

/** The largest `--timeout` a timer can hold (2^31-1 ms); a larger one would fire at once. */
export const MAX_TEST_TIMEOUT_SECONDS = 2_147_483;

/** After a kill, how long to wait for the editor's exit before giving up on it. */
const KILL_GRACE_MS = 30_000;

export interface RunTestsOptions {
  projectPath: string;
  /** `EditMode` (default) or `PlayMode`. */
  mode?: TestMode;
  /** `-testFilter`: test names or a regex (Unity's syntax; `;` separates several). */
  filter?: string;
  /** `-testCategory`. */
  category?: string;
  /** `-assemblyNames`. */
  assemblies?: string;
  /** NUnit report path (default `<cwd>/test-results.xml`). */
  output?: string;
  /** Also write a JUnit report here. */
  junitOutput?: string;
  /** Editor log path (default: the report path with `.log`). */
  logFile?: string;
  /** Kill the editor after this many seconds (default `DEFAULT_TEST_TIMEOUT_SECONDS`). */
  timeoutSeconds?: number;
  /** Pass `-nographics` (no GPU on the machine; PlayMode rendering is skipped). */
  noGraphics?: boolean;
  /** Explicit editor executable; default: the installed editor of the project's (or `unityVersion`) version. */
  editorPath?: string;
  unityVersion?: string;
  /** Extra editor arguments (after `--`). */
  extraArgs?: string[];
  onStatus?: (message: string) => void;
  deps?: Partial<RunTestsDeps>;
}

export interface RunTestsDeps {
  findEditor: (version: string | undefined) => Promise<string | null>;
  /** PID of an editor that has the project open, or undefined. */
  findOpenEditor: (projectPath: string) => number | undefined;
  spawn: (command: string, args: string[]) => ChildProcess;
  /** Kill `pid` and every process it started; true when the kill was delivered. */
  killTree: (pid: number) => Promise<boolean>;
}

const defaultDeps: RunTestsDeps = {
  findEditor: version => findEditorPath(version),
  findOpenEditor: projectPath => findUnityProcess(projectPath)?.pid ?? (hasLiveSessionLease(projectPath) ? -1 : undefined),
  // Its own process group on POSIX, so the kill reaches the import workers and compilers the editor starts.
  spawn: (command, args) => spawn(command, args, { stdio: 'ignore', windowsHide: true, detached: process.platform !== 'win32' }),
  killTree: pid => killProcessTree(pid),
};

export interface RunTestsPaths {
  editorPath: string;
  output: string;
  junitOutput?: string;
  logFile: string;
}

export interface RunTestsVerdict {
  kind: 'completed';
  /** `passed` when nothing failed (a run of zero tests too — see `warnings`), `failed` otherwise. */
  verdict: 'passed' | 'failed';
  report: TestReport;
  paths: RunTestsPaths;
  editorExitCode: number | null;
  warnings: string[];
}

export interface RunTestsFailure {
  kind: 'failure';
  /**
   * `usage`: bad options; `editor-not-found`; `project-open`: an editor holds the project (a second one cannot open
   * it); `no-verdict`: the editor ended without a report (compile errors, license, crash); `timeout`.
   */
  reason: 'usage' | 'editor-not-found' | 'project-open' | 'no-verdict' | 'timeout';
  message: string;
  paths?: Partial<RunTestsPaths>;
  editorExitCode?: number | null;
  /** Compiler errors found in the editor log (`no-verdict`): the usual reason there is no report. */
  compilerErrors?: CompilerMessage[];
  /** The last lines of the editor log (`no-verdict`, `timeout`). */
  logTail?: string[];
}

export type RunTestsResult = RunTestsVerdict | RunTestsFailure;

/** The editor arguments of a run. Exposed for tests. */
export function buildTestArgs(projectPath: string, options: RunTestsOptions, paths: RunTestsPaths): string[] {
  return [
    '-batchmode',
    ...(options.noGraphics ? ['-nographics'] : []),
    '-projectPath', path.resolve(projectPath),
    '-runTests',
    '-testPlatform', options.mode ?? 'EditMode',
    '-testResults', paths.output,
    '-logFile', paths.logFile,
    ...(options.filter ? ['-testFilter', options.filter] : []),
    ...(options.category ? ['-testCategory', options.category] : []),
    ...(options.assemblies ? ['-assemblyNames', options.assemblies] : []),
    ...(options.extraArgs ?? []),
  ];
}

/** The first reserved argument in `args` (case-insensitive), or undefined. */
export function findReservedArg(args: readonly string[]): string | undefined {
  const reserved = new Set(RESERVED_TEST_ARGS.map(a => a.toLowerCase()));
  return args.find(a => reserved.has(a.toLowerCase()));
}

/** Kill a process with everything it started: `taskkill /T` on Windows, the process group elsewhere. */
export function killProcessTree(pid: number): Promise<boolean> {
  if (process.platform !== 'win32') {
    try {
      process.kill(-pid, 'SIGKILL');
      return Promise.resolve(true);
    } catch {
      return defaultSessionProcessOps.forceKill(pid);
    }
  }
  return new Promise((resolve) => {
    execFile('taskkill', ['/PID', String(pid), '/T', '/F'], { windowsHide: true, timeout: 15_000 }, err => resolve(err === null));
  });
}

function tail(log: string, lines: number): string[] {
  return log.split(/\r?\n/).filter(l => l.trim() !== '').slice(-lines);
}

function readLog(file: string): string {
  try {
    return fs.readFileSync(file, 'utf-8');
  } catch {
    return '';
  }
}

/**
 * Run the tests: refuses a project another editor has open, starts the editor in batch mode, waits for it (killing it
 * after `timeoutSeconds`), and reads the report it wrote. The verdict comes from the report, never from the editor's
 * exit code alone.
 */
export async function runTests(options: RunTestsOptions): Promise<RunTestsResult> {
  const deps: RunTestsDeps = { ...defaultDeps, ...options.deps };
  const projectPath = path.resolve(options.projectPath);
  const reserved = findReservedArg(options.extraArgs ?? []);
  if (reserved) {
    return { kind: 'failure', reason: 'usage', message: `${reserved} is managed by the test command; remove it from the arguments after --.` };
  }

  const openPid = deps.findOpenEditor(projectPath);
  if (openPid !== undefined) {
    return {
      kind: 'failure',
      reason: 'project-open',
      message: `The project is open in a running Unity Editor${openPid > 0 ? ` (PID ${openPid})` : ''}: a second editor cannot open it.`,
    };
  }

  let editorPath: string;
  if (options.editorPath !== undefined) {
    try {
      editorPath = validateEditorPath(options.editorPath);
    } catch (err) {
      return { kind: 'failure', reason: 'editor-not-found', message: err instanceof Error ? err.message : String(err) };
    }
  } else {
    const version = options.unityVersion ?? getProjectEditorVersion(projectPath) ?? undefined;
    const found = await deps.findEditor(version);
    if (!found) {
      return {
        kind: 'failure',
        reason: 'editor-not-found',
        message: version ? `Unity Editor ${version} is not installed.` : 'No Unity Editor found.',
      };
    }
    editorPath = found;
  }

  const output = path.resolve(options.output ?? 'test-results.xml');
  const logFile = path.resolve(options.logFile ?? output.replace(/\.xml$/i, '') + '.log');
  const paths: RunTestsPaths = {
    editorPath, output, logFile, ...(options.junitOutput ? { junitOutput: path.resolve(options.junitOutput) } : {}),
  };
  fs.mkdirSync(path.dirname(output), { recursive: true });
  fs.mkdirSync(path.dirname(logFile), { recursive: true });
  // A report left by an earlier run must never pass for this run's.
  fs.rmSync(output, { force: true });
  // Nor may an earlier run's log explain a run that never wrote one.
  fs.rmSync(logFile, { force: true });

  const args = buildTestArgs(projectPath, options, paths);
  options.onStatus?.(`Starting Unity in batch mode (${options.mode ?? 'EditMode'} tests)...`);
  const timeoutMs = (options.timeoutSeconds ?? DEFAULT_TEST_TIMEOUT_SECONDS) * 1000;

  const outcome = await new Promise<{ code: number | null; timedOut: boolean; error?: Error }>((resolve) => {
    let child: ChildProcess;
    try {
      child = deps.spawn(editorPath, args);
    } catch (err) {
      resolve({ code: null, timedOut: false, error: err instanceof Error ? err : new Error(String(err)) });
      return;
    }
    let timedOut = false;
    let interrupted = false;
    let giveUp: NodeJS.Timeout | undefined;
    const kill = async (): Promise<void> => {
      const killed = child.pid !== undefined && await deps.killTree(child.pid);
      if (!killed) child.kill('SIGKILL');
    };
    // Ctrl+C or a cancelled CI step ends this process: take the editor down with it, or it keeps the project open and
    // the next run refuses it as already open. The run then never reports a verdict: the signal ends the process.
    const signals: NodeJS.Signals[] = ['SIGINT', 'SIGTERM', 'SIGHUP'];
    const onSignal = (signal: NodeJS.Signals) => {
      interrupted = true;
      cleanup();
      // Windows cannot raise SIGHUP on itself.
      void kill().finally(() => process.kill(process.pid, process.platform === 'win32' && signal === 'SIGHUP' ? 'SIGTERM' : signal));
    };
    for (const s of signals) process.once(s, onSignal);
    const cleanup = () => { for (const s of signals) process.removeListener(s, onSignal); };
    const finish = (result: { code: number | null; timedOut: boolean; error?: Error }) => {
      clearTimeout(timer);
      if (giveUp) clearTimeout(giveUp);
      cleanup();
      if (!interrupted) resolve(result);
    };
    const timer = setTimeout(() => {
      timedOut = true;
      options.onStatus?.(`Timed out after ${timeoutMs / 1000} s; stopping the editor...`);
      void kill();
      // An editor the kill cannot reach (stuck in the kernel) must not hold the CLI past its timeout.
      giveUp = setTimeout(() => finish({ code: null, timedOut: true }), KILL_GRACE_MS);
    }, timeoutMs);
    child.once('error', err => finish({ code: null, timedOut, error: err }));
    child.once('exit', code => finish({ code, timedOut }));
    options.onStatus?.('Running tests...');
  });

  if (outcome.error) {
    return { kind: 'failure', reason: 'no-verdict', message: `Could not start Unity: ${outcome.error.message}`, paths, editorExitCode: null };
  }
  if (outcome.timedOut) {
    return {
      kind: 'failure', reason: 'timeout', message: `The test run did not finish within ${timeoutMs / 1000} seconds.`,
      paths, editorExitCode: outcome.code, logTail: tail(readLog(logFile), 20),
    };
  }

  const report = fs.existsSync(output) ? parseNUnitReport(fs.readFileSync(output, 'utf-8')) : undefined;
  if (!report) {
    const log = readLog(logFile);
    const compilerErrors = parseCompilerMessages(log).filter(m => m.severity === 'error');
    return {
      kind: 'failure',
      reason: 'no-verdict',
      message: compilerErrors.length > 0
        ? `The run produced no test report: the project has ${compilerErrors.length} compile error(s).`
        : `The run produced no test report (editor exit code ${outcome.code ?? 'unknown'}). See the editor log: ${logFile}`,
      paths,
      editorExitCode: outcome.code,
      compilerErrors,
      logTail: tail(log, 20),
    };
  }

  if (paths.junitOutput) {
    fs.mkdirSync(path.dirname(paths.junitOutput), { recursive: true });
    fs.writeFileSync(paths.junitOutput, toJUnitXml(report, `${path.basename(projectPath)} ${options.mode ?? 'EditMode'}`));
  }
  const warnings: string[] = [];
  if (report.summary.total === 0) {
    warnings.push(options.filter || options.category || options.assemblies
      ? 'No test matched the filter.'
      : 'The project has no tests for this mode (is the test assembly listed in "testables" when it lives in a package?).');
  }
  return {
    kind: 'completed',
    verdict: report.summary.failed > 0 ? 'failed' : 'passed',
    report,
    paths,
    editorExitCode: outcome.code,
    warnings,
  };
}
