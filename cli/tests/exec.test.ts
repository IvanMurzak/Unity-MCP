import { describe, it, expect, vi } from 'vitest';
import {
  buildScriptExecuteInput,
  isLoopbackUrl,
  readToolResponse,
  runExec,
  type ExecDeps,
  type ExecInput,
} from '../src/commands/exec.js';
import { buildOpenEnv } from '../src/lib/open.js';
import { waitForReady } from '../src/lib/wait-for-ready.js';
import type { OpenProjectResult, RunToolResult } from '../src/lib/types.js';

const INPUT: ExecInput = {
  projectPath: '/proj',
  code: '1 + 2',
  usings: [],
  include: [],
  window: false,
  idleTimeoutSeconds: 300,
  readyTimeoutMs: 1000,
  timeoutMs: 1000,
  probeUrls: ['http://localhost:1'],
  baseUrl: 'http://localhost:1',
  token: undefined,
  tokenFromCloudStore: false,
};

const OPENED: OpenProjectResult = { kind: 'success', success: true, editorPath: 'Unity', projectPath: '/proj', warnings: [] };
const CALLED: RunToolResult = { kind: 'success', success: true, data: { status: 'success', content: [] } } as RunToolResult;

function deps(overrides: Partial<ExecDeps> = {}): ExecDeps {
  return {
    findProcess: vi.fn(() => null),
    open: vi.fn(async () => OPENED),
    wait: vi.fn(async () => ({ kind: 'success' as const, baseUrl: 'http://localhost:1', elapsedMs: 1 })),
    invoke: vi.fn(async () => CALLED),
    ...overrides,
  };
}

describe('runExec', () => {
  it('starts a headless editor with the idle timeout when none is running', async () => {
    const d = deps();
    const outcome = await runExec(INPUT, d);
    expect(d.open).toHaveBeenCalledWith(expect.objectContaining({
      projectPath: '/proj', headless: true, keepConnected: true, idleTimeoutSeconds: 300,
    }));
    expect(outcome).toEqual({ kind: 'called', result: CALLED, launched: true });
  });

  it('hands an explicit local --url to the new editor and asks it to start the server', async () => {
    const d = deps();
    await runExec({ ...INPUT, overrideUrl: 'http://localhost:5000', overrideToken: 't' }, d);
    expect(d.open).toHaveBeenCalledWith(expect.objectContaining({ url: 'http://localhost:5000', token: 't', startServer: true }));
  });

  it('does not start a server for a remote --url', async () => {
    const d = deps();
    await runExec({ ...INPUT, overrideUrl: 'https://example.com' }, d);
    expect(d.open).toHaveBeenCalledWith(expect.not.objectContaining({ startServer: true }));
  });

  it('starts a windowed editor with --window', async () => {
    const d = deps();
    await runExec({ ...INPUT, window: true }, d);
    expect(d.open).toHaveBeenCalledWith(expect.objectContaining({ headless: false }));
  });

  it('reuses a running editor and never launches another', async () => {
    const d = deps({ findProcess: vi.fn(() => ({ pid: 42 })) });
    const outcome = await runExec(INPUT, d);
    expect(d.open).not.toHaveBeenCalled();
    expect(outcome).toMatchObject({ kind: 'called', launched: false });
  });

  it('calls script-execute on the URL that answered', async () => {
    const d = deps({ wait: vi.fn(async () => ({ kind: 'success' as const, baseUrl: 'http://other:2', elapsedMs: 1 })) });
    await runExec({ ...INPUT, usings: ['MyGame'] }, d);
    expect(d.invoke).toHaveBeenCalledWith(expect.objectContaining({
      toolName: 'script-execute',
      url: 'http://other:2',
      input: { csharpCode: '1 + 2', usings: ['MyGame'] },
    }));
  });

  it('reports a launch failure without waiting', async () => {
    const d = deps({ open: vi.fn(async () => ({ kind: 'failure', success: false, warnings: [], errorMessage: 'no editor', error: new Error('no editor') }) as OpenProjectResult) });
    expect(await runExec(INPUT, d)).toEqual({ kind: 'launch-failed', message: 'no editor' });
    expect(d.wait).not.toHaveBeenCalled();
  });

  it('reports a server that never became ready without calling the tool', async () => {
    const d = deps({ wait: vi.fn(async () => ({ kind: 'failure' as const, reason: 'timeout' as const, elapsedMs: 1000 })) });
    expect(await runExec(INPUT, d)).toEqual({ kind: 'not-ready', elapsedMs: 1000 });
    expect(d.invoke).not.toHaveBeenCalled();
  });
});

describe('isLoopbackUrl', () => {
  it('recognises loopback hosts only', () => {
    expect(isLoopbackUrl('http://localhost:1')).toBe(true);
    expect(isLoopbackUrl('http://127.0.0.1:1')).toBe(true);
    expect(isLoopbackUrl('http://[::1]:1')).toBe(true);
    expect(isLoopbackUrl('https://ai-game.dev')).toBe(false);
    expect(isLoopbackUrl('not a url')).toBe(false);
  });
});

describe('buildScriptExecuteInput', () => {
  it('omits empty usings and include', () => {
    expect(buildScriptExecuteInput('x', [], [])).toEqual({ csharpCode: 'x' });
    expect(buildScriptExecuteInput('x', ['A'], ['lib/'])).toEqual({ csharpCode: 'x', usings: ['A'], include: ['lib/'] });
  });
});

describe('readToolResponse', () => {
  it('joins text content and reads the status', () => {
    expect(readToolResponse({ status: 'success', content: [{ type: 'text', text: 'a' }, { type: 'text', text: 'b' }] }))
      .toEqual({ ok: true, text: 'a\nb' });
    expect(readToolResponse({ status: 'error', content: [{ type: 'text', text: 'boom' }] }))
      .toEqual({ ok: false, text: 'boom' });
  });

  it('falls back to JSON when there is no text content', () => {
    expect(readToolResponse({ status: 'success', content: [] })).toEqual({ ok: true, text: '{"status":"success","content":[]}' });
    expect(readToolResponse('plain')).toEqual({ ok: true, text: 'plain' });
  });
});

describe('buildOpenEnv idle timeout', () => {
  it('sets UNITY_MCP_IDLE_TIMEOUT', () => {
    expect(buildOpenEnv({ idleTimeoutSeconds: 300 })).toEqual({ UNITY_MCP_IDLE_TIMEOUT: '300' });
  });

  it('rejects a non-positive or fractional value', () => {
    expect(() => buildOpenEnv({ idleTimeoutSeconds: 0 })).toThrow();
    expect(() => buildOpenEnv({ idleTimeoutSeconds: 1.5 })).toThrow();
  });
});

describe('waitForReady', () => {
  it('returns the first URL that answers', async () => {
    const probeImpl = vi.fn(async (url: string) =>
      url === 'http://b' ? { ok: true as const, baseUrl: url } : { ok: false as const }) as never;
    const result = await waitForReady({ urls: ['http://a', 'http://b'], timeoutMs: 1000, intervalMs: 10, probeImpl });
    expect(result).toMatchObject({ kind: 'success', baseUrl: 'http://b' });
  });

  it('times out when nothing answers', async () => {
    const probeImpl = vi.fn(async () => ({ ok: false as const })) as never;
    const result = await waitForReady({ urls: ['http://a'], timeoutMs: 50, intervalMs: 10, probeImpl });
    expect(result).toMatchObject({ kind: 'failure', reason: 'timeout' });
  });
});
