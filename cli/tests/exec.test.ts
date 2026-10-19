import { describe, it, expect, vi } from 'vitest';
import { EditorSessionError, type EnsureResult } from '@baizor/gamedev-cli-core';
import {
  buildScriptExecuteInput,
  execCode,
  readToolResponse,
  type ExecCodeOptions,
} from '../src/lib.js';
import type { ExecDeps } from '../src/lib/exec.js';
import type { UnitySessionOptions } from '../src/lib/editor-session.js';
import { buildOpenEnv } from '../src/lib/open.js';
import { waitForReady } from '../src/lib/wait-for-ready.js';
import { resolveProbeUrls } from '../src/utils/connection.js';
import type { RunToolResult } from '../src/lib/types.js';

const LOCAL = 'http://localhost:1';
const CALLED: RunToolResult = { kind: 'success', success: true, endpoint: `${LOCAL}/api/tools/script-execute`, httpStatus: 200, data: { status: 'success', content: [] } };

function deps(ensure: () => Promise<EnsureResult<string>> = async () => ({ ready: LOCAL, source: 'running' })) {
  const ensureMock = vi.fn(ensure);
  const session = vi.fn((_: UnitySessionOptions) => ({ ensure: ensureMock }));
  return { session, invoke: vi.fn(async (): Promise<RunToolResult> => CALLED), ensure: ensureMock };
}

function options(d: ExecDeps, extra: Partial<ExecCodeOptions> = {}): ExecCodeOptions {
  return { projectPath: '/proj', code: '1 + 2', probeUrls: [LOCAL], deps: d, ...extra };
}

function sessionOptions(d: ReturnType<typeof deps>): UnitySessionOptions {
  return d.session.mock.calls[0][0];
}

describe('execCode', () => {
  it('runs on the editor the session provides', async () => {
    const d = deps();
    const result = await execCode(options(d));
    expect(d.ensure).toHaveBeenCalledWith({ mode: 'headless' });
    expect(result).toEqual({ kind: 'success', success: true, baseUrl: LOCAL, data: CALLED.data });
  });

  it('hands the session the probe URLs, token, idle timeout and a connected-plugin env', async () => {
    const d = deps();
    await execCode(options(d, { token: 'tk', readyTimeoutMs: 5000 }));
    expect(sessionOptions(d)).toMatchObject({
      projectPath: '/proj', urls: [LOCAL], token: 'tk', serverUrl: LOCAL, idleSeconds: 300, readyTimeoutMs: 5000,
      env: { UNITY_MCP_KEEP_CONNECTED: 'true' },
    });
  });

  it('records an explicit url as the server of a started editor', async () => {
    const d = deps();
    await execCode(options(d, { overrideUrl: 'http://127.0.0.1:5000/', foreignReadyTimeoutMs: 1000 }));
    expect(sessionOptions(d)).toMatchObject({ serverUrl: 'http://127.0.0.1:5000', foreignReadyTimeoutMs: 1000 });
  });

  it('explains a user editor that never answered', async () => {
    const d = deps(async () => { throw new EditorSessionError('not-ready', 'no answer in 60s', { source: 'foreign' }); });
    const result = await execCode(options(d, { probeUrls: ['https://ai-game.dev/mcp', LOCAL] }));
    expect(result).toMatchObject({ kind: 'failure', reason: 'not-ready', sessionReason: 'not-ready', editorSource: 'foreign' });
    expect(result.kind === 'failure' && result.message).toContain('Cloud mode');
    expect(d.invoke).not.toHaveBeenCalled();
  });

  it('passes an idle timeout of 0 through (never quit)', async () => {
    const d = deps();
    await execCode(options(d, { idleTimeoutSeconds: 0 }));
    expect(sessionOptions(d).idleSeconds).toBe(0);
  });

  it('starts a windowed editor with window: true', async () => {
    const d = deps();
    await execCode(options(d, { window: true }));
    expect(d.ensure).toHaveBeenCalledWith({ mode: 'window' });
  });

  it('hands an explicit local url to the new editor and asks it to start the server', async () => {
    const d = deps();
    await execCode(options(d, { overrideUrl: 'http://127.0.0.1:5000', overrideToken: 't' }));
    expect(sessionOptions(d).env).toMatchObject({
      UNITY_MCP_HOST: 'http://127.0.0.1:5000', UNITY_MCP_TOKEN: 't', UNITY_MCP_START_SERVER: 'true',
    });
  });

  it('does not start a server for a remote url', async () => {
    const d = deps();
    await execCode(options(d, { overrideUrl: 'https://example.com' }));
    expect(sessionOptions(d).env).not.toHaveProperty('UNITY_MCP_START_SERVER');
  });

  it('calls script-execute on the URL that answered, with the cloud-store flag', async () => {
    const d = deps(async () => ({ ready: 'http://other:2', source: 'started' }));
    await execCode(options(d, { usings: ['MyGame'], token: 'tk', tokenFromCloudStore: true }));
    expect(d.invoke).toHaveBeenCalledWith(expect.objectContaining({
      toolName: 'script-execute',
      url: 'http://other:2',
      token: 'tk',
      tokenFromCloudStore: true,
      input: { csharpCode: '1 + 2', usings: ['MyGame'] },
    }));
  });

  it.each([
    ['no-editor-binary', 'launch-failed'],
    ['launch-failed', 'launch-failed'],
    ['lock-busy', 'launch-failed'],
    ['exited', 'not-ready'],
    ['not-ready', 'not-ready'],
  ] as const)('maps a %s session failure to %s without calling the tool', async (sessionReason, reason) => {
    const d = deps(async () => { throw new EditorSessionError(sessionReason, 'boom'); });
    expect(await execCode(options(d))).toMatchObject({ kind: 'failure', reason, sessionReason, message: 'boom' });
    expect(d.invoke).not.toHaveBeenCalled();
  });

  it('passes a failed tool call through', async () => {
    const failure: RunToolResult = { kind: 'failure', success: false, endpoint: 'e', reason: 'http-error', httpStatus: 500, message: 'boom' };
    const d = deps();
    d.invoke.mockResolvedValue(failure);
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

/** A fetch that answers with `status` for the URLs in `up` and refuses the rest. */
function fetchFor(up: string[], status = 200) {
  return vi.fn(async (input: string | URL | Request) => {
    const url = String(input);
    if (!up.some(base => url.startsWith(base))) throw new TypeError('fetch failed');
    return new Response('{"status":"success"}', { status });
  });
}

describe('waitForReady', () => {
  it('returns the first URL that answers, sending the Bearer', async () => {
    const fetchImpl = fetchFor(['http://b']);
    const result = await waitForReady({ urls: ['http://a', 'http://b'], token: 't', timeoutMs: 1000, intervalMs: 10, fetchImpl });
    expect(result).toMatchObject({ kind: 'success', success: true, baseUrl: 'http://b' });
    expect((fetchImpl.mock.calls[0] as unknown[])[1]).toMatchObject({ headers: { Authorization: 'Bearer t' } });
  });

  it('counts an auth rejection as a server that is up', async () => {
    const result = await waitForReady({ urls: ['http://a'], timeoutMs: 1000, intervalMs: 10, fetchImpl: fetchFor(['http://a'], 401) });
    expect(result).toMatchObject({ kind: 'success', baseUrl: 'http://a' });
  });

  it('fails instead of polling forever on an invalid timeout or interval', async () => {
    const fetchImpl = fetchFor([]);
    expect(await waitForReady({ urls: ['http://a'], timeoutMs: NaN, intervalMs: 10, fetchImpl })).toMatchObject({ kind: 'failure' });
    expect(await waitForReady({ urls: ['http://a'], timeoutMs: 50, intervalMs: 0, fetchImpl })).toMatchObject({ kind: 'failure' });
  });

  it('ends early with the stop reason', async () => {
    const result = await waitForReady({ urls: ['http://a'], timeoutMs: 60_000, intervalMs: 10, fetchImpl: fetchFor([]), stopReason: () => 'gone' });
    expect(result).toMatchObject({ kind: 'failure', error: new Error('gone') });
  });

  it('times out when nothing answers, ticking with the time left', async () => {
    const onTick = vi.fn();
    const result = await waitForReady({ urls: ['http://a'], timeoutMs: 50, intervalMs: 10, fetchImpl: fetchFor([]), onTick });
    expect(result).toMatchObject({ kind: 'failure', success: false });
    expect(onTick).toHaveBeenCalled();
    expect(onTick.mock.calls[0][0]).toBeLessThanOrEqual(50);
  });
});
