import { describe, it, expect, vi } from 'vitest';
import {
  buildScriptExecuteInput,
  execCode,
  readToolResponse,
  type ExecCodeOptions,
} from '../src/lib.js';
import type { ExecDeps } from '../src/lib/exec.js';
import { buildOpenEnv } from '../src/lib/open.js';
import { waitForReady } from '../src/lib/wait-for-ready.js';
import { resolveProbeUrls } from '../src/utils/connection.js';
import type { OpenProjectResult, RunToolResult } from '../src/lib/types.js';
import type { WaitForReadyResult } from '../src/lib/wait-for-ready.js';

const LOCAL = 'http://localhost:1';
const OPENED: OpenProjectResult = { kind: 'success', success: true, editorPath: 'Unity', projectPath: '/proj', warnings: [] };
const CALLED: RunToolResult = { kind: 'success', success: true, endpoint: `${LOCAL}/api/tools/script-execute`, httpStatus: 200, data: { status: 'success', content: [] } };
const READY: WaitForReadyResult = { kind: 'success', success: true, baseUrl: LOCAL, elapsedMs: 1 };
const NOT_READY: WaitForReadyResult = { kind: 'failure', success: false, elapsedMs: 1, error: new Error('no answer') };

function deps(overrides: Partial<ExecDeps> = {}): ExecDeps {
  return {
    open: vi.fn(async () => OPENED),
    wait: vi.fn(async () => READY),
    invoke: vi.fn(async () => CALLED),
    isAlive: vi.fn(() => true),
    ...overrides,
  };
}

/** The warm probe fails (no editor yet), the readiness wait after launch succeeds. */
function coldDeps(overrides: Partial<ExecDeps> = {}): ExecDeps {
  return deps({ wait: vi.fn().mockResolvedValueOnce(NOT_READY).mockResolvedValue(READY), ...overrides });
}

function options(d: ExecDeps, extra: Partial<ExecCodeOptions> = {}): ExecCodeOptions {
  return { projectPath: '/proj', code: '1 + 2', probeUrls: [LOCAL], deps: d, ...extra };
}

describe('execCode', () => {
  it('calls straight away when the local server already answers (no launch)', async () => {
    const d = deps();
    const result = await execCode(options(d));
    expect(d.open).not.toHaveBeenCalled();
    expect(d.wait).toHaveBeenCalledTimes(1);
    expect(result).toEqual({ kind: 'success', success: true, baseUrl: LOCAL, data: CALLED.data });
  });

  it('starts a headless editor with the idle timeout when nothing answers', async () => {
    const d = coldDeps();
    await execCode(options(d));
    expect(d.open).toHaveBeenCalledWith(expect.objectContaining({
      projectPath: '/proj', headless: true, keepConnected: true, idleTimeoutSeconds: 300,
    }));
    expect(d.invoke).toHaveBeenCalledTimes(1);
  });

  it('never treats a remote URL as a warm editor', async () => {
    const d = deps();
    await execCode(options(d, { probeUrls: ['https://ai-game.dev/mcp'] }));
    expect(d.open).toHaveBeenCalledTimes(1);
  });

  it('starts a windowed editor with window: true', async () => {
    const d = coldDeps();
    await execCode(options(d, { window: true }));
    expect(d.open).toHaveBeenCalledWith(expect.objectContaining({ headless: false }));
  });

  it('hands an explicit local url to the new editor and asks it to start the server', async () => {
    const d = coldDeps();
    await execCode(options(d, { overrideUrl: 'http://127.0.0.1:5000', overrideToken: 't' }));
    expect(d.open).toHaveBeenCalledWith(expect.objectContaining({ url: 'http://127.0.0.1:5000', token: 't', startServer: true }));
  });

  it('does not start a server for a remote url', async () => {
    const d = coldDeps();
    await execCode(options(d, { overrideUrl: 'https://example.com' }));
    expect(d.open).toHaveBeenCalledWith(expect.not.objectContaining({ startServer: true }));
  });

  it('calls script-execute on the URL that answered, with the cloud-store flag', async () => {
    const d = coldDeps({ wait: vi.fn().mockResolvedValueOnce(NOT_READY).mockResolvedValue({ ...READY, baseUrl: 'http://other:2' }) });
    await execCode(options(d, { usings: ['MyGame'], token: 'tk', tokenFromCloudStore: true }));
    expect(d.invoke).toHaveBeenCalledWith(expect.objectContaining({
      toolName: 'script-execute',
      url: 'http://other:2',
      token: 'tk',
      tokenFromCloudStore: true,
      input: { csharpCode: '1 + 2', usings: ['MyGame'] },
    }));
  });

  it('reports a launch failure without waiting for the server', async () => {
    const error = new Error('no editor');
    const d = coldDeps({ open: vi.fn(async (): Promise<OpenProjectResult> => ({ kind: 'failure', success: false, warnings: [], errorMessage: 'no editor', error })) });
    expect(await execCode(options(d))).toMatchObject({ kind: 'failure', reason: 'launch-failed', message: 'no editor' });
    expect(d.wait).toHaveBeenCalledTimes(1); // only the warm probe
  });

  it('reports a server that never became ready without calling the tool', async () => {
    const d = deps({ wait: vi.fn(async () => NOT_READY) });
    expect(await execCode(options(d))).toMatchObject({ kind: 'failure', reason: 'not-ready' });
    expect(d.invoke).not.toHaveBeenCalled();
  });

  it('stops waiting when the editor it started has exited', async () => {
    const d = coldDeps({
      open: vi.fn(async (): Promise<OpenProjectResult> => ({ ...OPENED, editorPid: 4242 })),
      wait: vi.fn().mockResolvedValueOnce(NOT_READY).mockImplementation(async (o: { stopReason?: () => string | undefined }) =>
        ({ ...NOT_READY, error: new Error(o.stopReason?.() ?? 'timeout') })),
      isAlive: vi.fn(() => false),
    });
    const result = await execCode(options(d));
    expect(result).toMatchObject({ kind: 'failure', reason: 'not-ready' });
    expect((result as { message: string }).message).toContain('PID 4242');
    expect(d.isAlive).toHaveBeenCalledWith(4242);
  });

  it('passes a failed tool call through', async () => {
    const failure: RunToolResult = { kind: 'failure', success: false, endpoint: 'e', reason: 'http-error', httpStatus: 500, message: 'boom' };
    const d = deps({ invoke: vi.fn(async () => failure) });
    expect(await execCode(options(d))).toMatchObject({ kind: 'failure', reason: 'tool-failed', toolFailure: failure });
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

  it('prints the structured result of a value-returning tool', () => {
    expect(readToolResponse({ status: 'success', structured: { result: { typeName: 'System.Int32', value: 3 } } }))
      .toEqual({ ok: true, text: '{"typeName":"System.Int32","value":3}' });
    expect(readToolResponse({ status: 'success', structured: { result: 'hi' } })).toEqual({ ok: true, text: 'hi' });
  });

  it('falls back to JSON when there is no text content', () => {
    expect(readToolResponse({ status: 'success', content: [] })).toEqual({ ok: true, text: '{"status":"success","content":[]}' });
    expect(readToolResponse('plain')).toEqual({ ok: true, text: 'plain' });
  });
});

describe('buildOpenEnv headless and idle timeout', () => {
  it('sets UNITY_MCP_IDLE_TIMEOUT', () => {
    expect(buildOpenEnv({ idleTimeoutSeconds: 300 })).toEqual({ UNITY_MCP_IDLE_TIMEOUT: '300' });
  });

  it('rejects a non-positive or fractional idle timeout', () => {
    expect(() => buildOpenEnv({ idleTimeoutSeconds: 0 })).toThrow();
    expect(() => buildOpenEnv({ idleTimeoutSeconds: 1.5 })).toThrow();
  });

  it('headless keeps the plugin connected and refuses noConnect', () => {
    expect(buildOpenEnv({ headless: true })).toEqual({ UNITY_MCP_KEEP_CONNECTED: 'true' });
    expect(() => buildOpenEnv({ headless: true, noConnect: true })).toThrow();
  });
});

describe('resolveProbeUrls', () => {
  it('adds the local port unless the URL was given explicitly', () => {
    const urls = resolveProbeUrls('/proj', 'https://ai-game.dev/mcp', undefined);
    expect(urls[0]).toBe('https://ai-game.dev/mcp');
    expect(urls[1]).toMatch(/^http:\/\/localhost:\d+$/);
    expect(resolveProbeUrls('/proj', 'http://x:1', 'http://x:1')).toEqual(['http://x:1']);
  });
});

describe('waitForReady', () => {
  it('returns the first URL that answers, sending the Bearer', async () => {
    const probeImpl = vi.fn(async (url: string) =>
      url === 'http://b' ? { ok: true as const, baseUrl: url } : { ok: false as const }) as never;
    const result = await waitForReady({ urls: ['http://a', 'http://b'], token: 't', timeoutMs: 1000, intervalMs: 10, probeImpl });
    expect(result).toMatchObject({ kind: 'success', success: true, baseUrl: 'http://b' });
    expect((probeImpl as unknown as ReturnType<typeof vi.fn>).mock.calls[0][1]).toMatchObject({ Authorization: 'Bearer t' });
  });

  it('fails instead of polling forever on an invalid timeout or interval', async () => {
    const probeImpl = vi.fn(async () => ({ ok: false as const })) as never;
    expect(await waitForReady({ urls: ['http://a'], timeoutMs: NaN, intervalMs: 10, probeImpl })).toMatchObject({ kind: 'failure' });
    expect(await waitForReady({ urls: ['http://a'], timeoutMs: 50, intervalMs: 0, probeImpl })).toMatchObject({ kind: 'failure' });
  });

  it('ends early with the stop reason', async () => {
    const probeImpl = vi.fn(async () => ({ ok: false as const })) as never;
    const result = await waitForReady({ urls: ['http://a'], timeoutMs: 60_000, intervalMs: 10, probeImpl, stopReason: () => 'gone' });
    expect(result).toMatchObject({ kind: 'failure', error: new Error('gone') });
  });

  it('times out when nothing answers', async () => {
    const probeImpl = vi.fn(async () => ({ ok: false as const })) as never;
    const result = await waitForReady({ urls: ['http://a'], timeoutMs: 50, intervalMs: 10, probeImpl });
    expect(result).toMatchObject({ kind: 'failure', success: false });
  });
});
