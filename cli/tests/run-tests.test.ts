import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { EventEmitter } from 'events';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ChildProcess } from 'child_process';
import { buildTestArgs, findReservedArg, runTests, type RunTestsDeps } from '../src/lib/run-tests.js';

const PASSING = '<test-run result="Passed" total="2" passed="2" failed="0" skipped="0" inconclusive="0" duration="0.5">'
  + '<test-case fullname="A.T.One" name="One" result="Passed" duration="0.1"/><test-case fullname="A.T.Two" name="Two" result="Passed" duration="0.1"/></test-run>';
const FAILING = '<test-run result="Failed" total="1" passed="0" failed="1" skipped="0" duration="0.5">'
  + '<test-case fullname="A.T.One" name="One" result="Failed"><failure><message><![CDATA[boom]]></message></failure></test-case></test-run>';

let dir: string;
beforeEach(() => { dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-run-tests-')); });
afterEach(() => { fs.rmSync(dir, { recursive: true, force: true }); });

/** A fake editor process: on spawn it writes `report`/`log` (when given) and exits with `code` (never, when undefined). */
function fakeEditor(opts: { report?: string; log?: string; code?: number }) {
  const spawned: Array<{ command: string; args: string[] }> = [];
  const killTree = vi.fn(async () => true);
  let child: EventEmitter & { pid: number; kill: () => boolean };
  const deps: Partial<RunTestsDeps> = {
    findEditor: async () => '/fake/Unity',
    findOpenEditor: () => undefined,
    killTree: async (pid: number) => { await killTree(pid); setImmediate(() => child.emit('exit', null)); return true; },
    spawn: (command, args) => {
      spawned.push({ command, args });
      child = Object.assign(new EventEmitter(), { pid: 4242, kill: () => true });
      const out = args[args.indexOf('-testResults') + 1];
      const log = args[args.indexOf('-logFile') + 1];
      setImmediate(() => {
        if (opts.report !== undefined) fs.writeFileSync(out, opts.report);
        if (opts.log !== undefined) fs.writeFileSync(log, opts.log);
        if (opts.code !== undefined) child.emit('exit', opts.code);
      });
      return child as unknown as ChildProcess;
    },
  };
  return { deps, spawned, killTree };
}

describe('buildTestArgs / findReservedArg', () => {
  it('builds the batch-mode test invocation', () => {
    const args = buildTestArgs('/proj', { projectPath: '/proj', mode: 'PlayMode', filter: 'A.B', noGraphics: true, extraArgs: ['-foo'] },
      { editorPath: '/u', output: '/r.xml', logFile: '/r.log' });
    expect(args).toEqual([
      '-batchmode', '-nographics', '-projectPath', path.resolve('/proj'), '-runTests', '-testPlatform', 'PlayMode',
      '-testResults', '/r.xml', '-logFile', '/r.log', '-testFilter', 'A.B', '-foo',
    ]);
    expect(args).not.toContain('-quit'); // -runTests quits by itself once the report is written
  });

  it('finds a reserved argument whatever its case', () => {
    expect(findReservedArg(['-x', '-BatchMode'])).toBe('-BatchMode');
    expect(findReservedArg(['-x'])).toBeUndefined();
  });
});

describe('runTests', () => {
  it('passes when the report has no failure, and writes JUnit', async () => {
    const { deps, spawned } = fakeEditor({ report: PASSING, code: 0 });
    const junit = path.join(dir, 'junit.xml');
    const result = await runTests({ projectPath: dir, output: path.join(dir, 'r.xml'), junitOutput: junit, deps });
    expect(result).toMatchObject({ kind: 'completed', verdict: 'passed', editorExitCode: 0 });
    expect(spawned[0].command).toBe('/fake/Unity');
    expect(fs.readFileSync(junit, 'utf-8')).toContain('<testsuite name="A.T" tests="2" failures="0"');
  });

  it('fails when a test failed, even with editor exit code 0', async () => {
    const { deps } = fakeEditor({ report: FAILING, code: 0 });
    const result = await runTests({ projectPath: dir, output: path.join(dir, 'r.xml'), deps });
    expect(result).toMatchObject({ kind: 'completed', verdict: 'failed' });
  });

  it('has no verdict without a report, and names the compile errors from the log', async () => {
    const { deps } = fakeEditor({ log: 'x\nAssets/A.cs(3,1): error CS1002: ; expected\n', code: 1 });
    const result = await runTests({ projectPath: dir, output: path.join(dir, 'r.xml'), deps });
    expect(result).toMatchObject({ kind: 'failure', reason: 'no-verdict', editorExitCode: 1 });
    if (result.kind === 'failure') expect(result.compilerErrors?.map(e => e.file)).toEqual(['Assets/A.cs']);
  });

  it('never reads a report an earlier run left behind', async () => {
    const output = path.join(dir, 'r.xml');
    fs.writeFileSync(output, PASSING);
    const { deps } = fakeEditor({ code: 1 });
    expect(await runTests({ projectPath: dir, output, deps })).toMatchObject({ kind: 'failure', reason: 'no-verdict' });
  });

  it('kills the editor tree on timeout', async () => {
    const { deps, killTree } = fakeEditor({});
    const result = await runTests({ projectPath: dir, output: path.join(dir, 'r.xml'), timeoutSeconds: 0.05, deps });
    expect(result).toMatchObject({ kind: 'failure', reason: 'timeout' });
    expect(killTree).toHaveBeenCalledWith(4242);
  });

  it('refuses a project an editor has open', async () => {
    const { deps, spawned } = fakeEditor({ report: PASSING, code: 0 });
    const result = await runTests({ projectPath: dir, deps: { ...deps, findOpenEditor: () => 99 } });
    expect(result).toMatchObject({ kind: 'failure', reason: 'project-open' });
    expect(spawned).toHaveLength(0);
  });

  it('rejects reserved editor arguments before starting anything', async () => {
    const { deps, spawned } = fakeEditor({ report: PASSING, code: 0 });
    expect(await runTests({ projectPath: dir, extraArgs: ['-quit'], deps })).toMatchObject({ kind: 'failure', reason: 'usage' });
    expect(spawned).toHaveLength(0);
  });

  it('reports a missing editor', async () => {
    const { deps } = fakeEditor({});
    expect(await runTests({ projectPath: dir, deps: { ...deps, findEditor: async () => null } })).toMatchObject({ kind: 'failure', reason: 'editor-not-found' });
  });

  it('warns when no test matched the filter', async () => {
    const { deps } = fakeEditor({ report: '<test-run result="Passed" total="0" passed="0" failed="0"/>', code: 0 });
    const result = await runTests({ projectPath: dir, output: path.join(dir, 'r.xml'), filter: 'Nope', deps });
    expect(result).toMatchObject({ kind: 'completed', verdict: 'passed', warnings: ['No test matched the filter.'] });
  });
});
