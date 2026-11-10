import { describe, it, expect, vi } from 'vitest';
import { formatCompilerMessage, parseCompilerMessage, parseCompilerMessages } from '../src/utils/compiler-messages.js';
import { COMPILE_STATUS_SNIPPET, parseCompileStatus, REFRESH_SNIPPET } from '../src/lib/compile-snippets.js';
import { compileProject, type CompileDeps } from '../src/lib/compile.js';
import type { EnsureEditorResult } from '../src/lib/ensure-editor.js';
import type { RunToolOptions, RunToolResult } from '../src/lib/types.js';

describe('compiler messages', () => {
  it('parses Unity\'s diagnostic lines (backslashes normalized)', () => {
    expect(parseCompilerMessage('Assets\\Scripts\\Player.cs(12,5): error CS0103: The name \'x\' does not exist in the current context')).toEqual({
      severity: 'error', file: 'Assets/Scripts/Player.cs', line: 12, column: 5, code: 'CS0103',
      message: 'The name \'x\' does not exist in the current context',
    });
    expect(parseCompilerMessage('Packages/com.x/Foo.cs(1,2): warning CS0168: The variable \'e\' is declared but never used')?.severity).toBe('warning');
    expect(parseCompilerMessage('Refreshing native plugins compatible for Editor')).toBeUndefined();
  });

  it('collects each diagnostic once (Unity logs them more than once)', () => {
    const log = [
      'Assets/A.cs(1,1): error CS1002: ; expected',
      'noise',
      'Assets/A.cs(1,1): error CS1002: ; expected',
      'Assets/B.cs(3,4): warning CS0414: unused',
    ].join('\r\n');
    expect(parseCompilerMessages(log).map(formatCompilerMessage)).toEqual([
      'Assets/A.cs(1,1): error CS1002: ; expected',
      'Assets/B.cs(3,4): warning CS0414: unused',
    ]);
  });
});

describe('compile snippets', () => {
  it('the status snippet ends with the expression that is its result', () => {
    expect(COMPILE_STATUS_SNIPPET.trimEnd().endsWith('sb.ToString()')).toBe(true);
    expect(REFRESH_SNIPPET).toContain('AssetDatabase.Refresh()');
  });

  it('parses the status answer', () => {
    expect(parseCompileStatus('state:busy\n')).toEqual({ state: 'busy', messages: [] });
    expect(parseCompileStatus('state:failed\nAssets/A.cs(1,1): error CS1002: ; expected\n')?.messages).toHaveLength(1);
    expect(parseCompileStatus('something else')).toBeUndefined();
  });

  it('unwraps the typed answer of script-execute (System.String: "<json>")', () => {
    // Exactly what the plugin returned in a live run: the value as JSON after its type name.
    expect(parseCompileStatus(String.raw`System.String: "state:ok\r\n"`)).toEqual({ state: 'ok', messages: [] });
    expect(parseCompileStatus(String.raw`System.String: "state:failed\r\nAssets\\A.cs(1,1): error CS1002: ; expected\r\n"`)?.messages[0].file)
      .toBe('Assets/A.cs');
  });
});

const URL = 'http://localhost:1';

function ok(text: string): RunToolResult {
  return { kind: 'success', success: true, endpoint: URL, httpStatus: 200, data: { status: 'success', structured: { result: text } } };
}

function refused(): RunToolResult {
  return { kind: 'failure', success: false, endpoint: URL, reason: 'connection-refused', message: 'refused' };
}

function deps(answers: RunToolResult[], ensured?: Partial<Extract<EnsureEditorResult, { kind: 'ready' }>>) {
  let clock = 0;
  const closeIfStarted = vi.fn(async () => 7);
  const invoke = vi.fn(async (_: RunToolOptions) => answers.shift() ?? ok('state:ok\n'));
  const d: Partial<CompileDeps> = {
    ensure: vi.fn(async () => ({ kind: 'ready' as const, baseUrl: URL, source: 'running' as const, closeIfStarted, ...ensured })),
    invoke,
    sleep: async (ms: number) => { clock += ms; },
    now: () => clock,
  };
  return { d, invoke, closeIfStarted };
}

describe('compileProject', () => {
  it('refreshes, waits for two settled answers and reports the diagnostics', async () => {
    const { d, invoke } = deps([
      ok('True'),
      ok('state:busy'),
      refused(), // the domain reload
      ok('state:failed\nAssets/A.cs(1,1): error CS1002: ; expected\nAssets/B.cs(2,2): warning CS0414: unused'),
      ok('state:failed\nAssets/A.cs(1,1): error CS1002: ; expected\nAssets/B.cs(2,2): warning CS0414: unused'),
    ]);
    const result = await compileProject({ projectPath: '/p', probeUrls: [URL], deps: d });
    expect(result.kind).toBe('completed');
    if (result.kind !== 'completed') return;
    expect(result.state).toBe('failed');
    expect(result.errors.map(e => e.code)).toEqual(['CS1002']);
    expect(result.warnings.map(e => e.code)).toEqual(['CS0414']);
    expect((invoke.mock.calls[0][0].input as { csharpCode: string }).csharpCode).toBe(REFRESH_SNIPPET);
    expect(invoke).toHaveBeenCalledTimes(5);
  });

  it('times out when the compile never settles', async () => {
    const { d } = deps(Array.from({ length: 1000 }, (_, i) => (i === 0 ? ok('True') : ok('state:busy'))));
    const result = await compileProject({ projectPath: '/p', probeUrls: [URL], timeoutMs: 5_000, deps: d });
    expect(result).toMatchObject({ kind: 'failure', reason: 'timeout' });
  });

  it('reports an editor that cannot be reached', async () => {
    const d: Partial<CompileDeps> = {
      ensure: vi.fn(async () => ({ kind: 'failure' as const, reason: 'launch-failed' as const, message: 'no editor', error: new Error('no editor') })),
    };
    expect(await compileProject({ projectPath: '/p', probeUrls: [URL], deps: d })).toMatchObject({ kind: 'failure', reason: 'unreachable', message: 'no editor' });
  });

  it('stops on a non-transient tool error', async () => {
    const { d } = deps([{ kind: 'failure', success: false, endpoint: URL, reason: 'http-error', httpStatus: 404, message: 'no such tool' }]);
    expect(await compileProject({ projectPath: '/p', probeUrls: [URL], deps: d })).toMatchObject({ kind: 'failure', reason: 'tool-failed' });
  });

  it('with once, closes an editor it started', async () => {
    const { d, closeIfStarted } = deps([ok('False')], { source: 'started' });
    const result = await compileProject({ projectPath: '/p', probeUrls: [URL], once: true, deps: d });
    expect(closeIfStarted).toHaveBeenCalledTimes(1);
    expect(result).toMatchObject({ kind: 'completed', state: 'ok', closedPid: 7 });
  });
});
