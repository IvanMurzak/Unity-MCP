import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { runTool, runSystemTool, type RunToolOptions, type RunToolResult } from '../src/lib.js';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function jsonResponse(body: unknown, status = 200, statusText = 'OK'): Response {
  return new Response(JSON.stringify(body), {
    status,
    statusText,
    headers: { 'Content-Type': 'application/json' },
  });
}

function textResponse(body: string, status = 200, statusText = 'OK'): Response {
  return new Response(body, { status, statusText });
}

function mkUnityProject(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-runtool-'));
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  return dir;
}

interface FetchSpy {
  fetch: typeof fetch;
  calls: Array<{ url: string; init: RequestInit | undefined }>;
}

function makeFetchSpy(impl: (url: string, init?: RequestInit) => Promise<Response>): FetchSpy {
  const calls: FetchSpy['calls'] = [];
  const fetchImpl: typeof fetch = (input, init) => {
    const url = typeof input === 'string' ? input : input instanceof URL ? input.toString() : (input as Request).url;
    calls.push({ url, init });
    return impl(url, init);
  };
  return { fetch: fetchImpl, calls };
}

// ---------------------------------------------------------------------------
// Successful invocation
// ---------------------------------------------------------------------------

describe('runTool — happy path', () => {
  it('posts to /api/tools/{name} with JSON body and returns the parsed structured response', async () => {
    const spy = makeFetchSpy(async () =>
      jsonResponse({ status: 'success', structured: { result: [{ name: 'tool-list' }] } }),
    );

    const result: RunToolResult = await runTool({
      toolName: 'tool-list',
      url: 'http://localhost:55000',
      input: { regexSearch: 'foo', includeDescription: true },
      fetchImpl: spy.fetch,
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.endpoint).toBe('http://localhost:55000/api/tools/tool-list');
    expect(result.httpStatus).toBe(200);
    expect(result.data).toEqual({ status: 'success', structured: { result: [{ name: 'tool-list' }] } });

    expect(spy.calls).toHaveLength(1);
    const call = spy.calls[0];
    expect(call.url).toBe('http://localhost:55000/api/tools/tool-list');
    expect(call.init?.method).toBe('POST');
    expect(call.init?.body).toBe('{"regexSearch":"foo","includeDescription":true}');
    const headers = call.init?.headers as Record<string, string>;
    expect(headers['Content-Type']).toBe('application/json');
    expect(headers['Authorization']).toBeUndefined();
  });

  it('forwards the bearer token when provided', async () => {
    const spy = makeFetchSpy(async () => jsonResponse({ status: 'success', content: [] }));

    await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      token: 'secret-123',
      fetchImpl: spy.fetch,
    });

    const headers = spy.calls[0].init?.headers as Record<string, string>;
    expect(headers['Authorization']).toBe('Bearer secret-123');
  });

  it('URL-encodes the tool name segment', async () => {
    const spy = makeFetchSpy(async () => jsonResponse({ status: 'success', content: [] }));

    await runTool({
      toolName: 'weird name/with slashes',
      url: 'http://localhost:55000',
      fetchImpl: spy.fetch,
    });

    expect(spy.calls[0].url).toBe(
      'http://localhost:55000/api/tools/weird%20name%2Fwith%20slashes',
    );
  });

  it('strips a trailing slash from the base URL', async () => {
    const spy = makeFetchSpy(async () => jsonResponse({ status: 'success', content: [] }));

    await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000/',
      fetchImpl: spy.fetch,
    });

    expect(spy.calls[0].url).toBe('http://localhost:55000/api/tools/ping');
  });

  it('defaults the body to "{}" when no input is provided', async () => {
    const spy = makeFetchSpy(async () => jsonResponse({ status: 'success', content: [] }));

    await runTool({ toolName: 'ping', url: 'http://localhost:55000', fetchImpl: spy.fetch });

    expect(spy.calls[0].init?.body).toBe('{}');
  });

  it('accepts a pre-serialized JSON string for input', async () => {
    const spy = makeFetchSpy(async () => jsonResponse({ status: 'success', content: [] }));

    await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      input: '{"a":1}',
      fetchImpl: spy.fetch,
    });

    expect(spy.calls[0].init?.body).toBe('{"a":1}');
  });

  it('returns the raw text body when the response is not JSON', async () => {
    const spy = makeFetchSpy(async () => textResponse('plain-text-response', 200));

    const result = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      fetchImpl: spy.fetch,
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.data).toBe('plain-text-response');
  });
});

// ---------------------------------------------------------------------------
// runSystemTool — endpoint differs, everything else mirrors runTool
// ---------------------------------------------------------------------------

describe('runSystemTool', () => {
  it('posts to /api/system-tools/{name}', async () => {
    const spy = makeFetchSpy(async () => jsonResponse({ status: 'success', content: [] }));

    await runSystemTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      fetchImpl: spy.fetch,
    });

    expect(spy.calls[0].url).toBe('http://localhost:55000/api/system-tools/ping');
  });
});

// ---------------------------------------------------------------------------
// Validation failures
// ---------------------------------------------------------------------------

describe('runTool — invalid input', () => {
  it('returns kind:"failure" with reason "invalid-input" when toolName is empty', async () => {
    const result = await runTool({
      toolName: '',
      url: 'http://localhost:55000',
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('invalid-input');
    expect(result.message).toContain('toolName');
  });

  it('returns kind:"failure" when neither url nor unityProjectPath is provided', async () => {
    const result = await runTool({ toolName: 'ping' } as unknown as RunToolOptions);

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('invalid-input');
    expect(result.message).toContain('url');
  });

  it('rejects non-object, non-string, non-null input', async () => {
    const result = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      input: 42 as unknown,
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('invalid-input');
  });

  it('rejects an input string that is not valid JSON', async () => {
    const result = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      input: 'not json',
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('invalid-input');
    expect(result.message).toContain('not valid JSON');
  });
});

// ---------------------------------------------------------------------------
// Transport failures
// ---------------------------------------------------------------------------

describe('runTool — failure modes', () => {
  it('classifies HTTP 4xx as reason "http-error" and surfaces the body', async () => {
    const spy = makeFetchSpy(async () =>
      jsonResponse({ error: 'Bad request' }, 400, 'Bad Request'),
    );

    const result = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      fetchImpl: spy.fetch,
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('http-error');
    expect(result.httpStatus).toBe(400);
    expect(result.data).toEqual({ error: 'Bad request' });
    expect(result.message).toBe('Bad Request');
  });

  it('classifies HTTP 5xx as reason "http-error"', async () => {
    const spy = makeFetchSpy(async () => textResponse('boom', 500, 'Internal Server Error'));

    const result = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      fetchImpl: spy.fetch,
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('http-error');
    expect(result.httpStatus).toBe(500);
    expect(result.data).toBe('boom');
  });

  it('classifies an AbortError as reason "timeout"', async () => {
    const fetchImpl: typeof fetch = (_url, init) =>
      new Promise((_resolve, reject) => {
        const signal = init?.signal;
        if (signal?.aborted) {
          reject(new DOMException('aborted', 'AbortError'));
          return;
        }
        signal?.addEventListener('abort', () => {
          reject(new DOMException('aborted', 'AbortError'));
        });
      });

    const result = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      timeoutMs: 30,
      fetchImpl,
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('timeout');
    expect(result.message).toContain('30ms');
  });

  it('classifies ECONNREFUSED as reason "connection-refused"', async () => {
    const fetchImpl: typeof fetch = async () => {
      const err = new TypeError('fetch failed');
      (err as unknown as { cause: { code: string } }).cause = { code: 'ECONNREFUSED' };
      throw err;
    };

    const result = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      fetchImpl,
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('connection-refused');
  });

  it('classifies an unknown error as reason "unknown"', async () => {
    const fetchImpl: typeof fetch = async () => {
      throw new Error('mystery');
    };

    const result = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      fetchImpl,
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('unknown');
    expect(result.message).toBe('mystery');
  });
});

// ---------------------------------------------------------------------------
// Connection resolution from Unity project path
// ---------------------------------------------------------------------------

describe('runTool — connection resolution', () => {
  let projectPath: string;

  beforeEach(() => {
    projectPath = mkUnityProject();
  });

  afterEach(() => {
    fs.rmSync(projectPath, { recursive: true, force: true });
  });

  it('falls back to deterministic localhost port when no config exists', async () => {
    const spy = makeFetchSpy(async () => jsonResponse({ status: 'success', content: [] }));

    const result = await runTool({
      toolName: 'ping',
      unityProjectPath: projectPath,
      fetchImpl: spy.fetch,
    });

    expect(result.kind).toBe('success');
    expect(spy.calls[0].url).toMatch(/^http:\/\/localhost:\d{5}\/api\/tools\/ping$/);
  });

  it('uses the host from project config when present', async () => {
    fs.mkdirSync(path.join(projectPath, 'UserSettings'), { recursive: true });
    fs.writeFileSync(
      path.join(projectPath, 'UserSettings', 'AI-Game-Developer-Config.json'),
      JSON.stringify({
        connectionMode: 'Custom',
        host: 'http://192.168.1.10:55555',
        token: 'config-token',
      }),
    );
    const spy = makeFetchSpy(async () => jsonResponse({ status: 'success', content: [] }));

    await runTool({
      toolName: 'ping',
      unityProjectPath: projectPath,
      fetchImpl: spy.fetch,
    });

    expect(spy.calls[0].url).toBe('http://192.168.1.10:55555/api/tools/ping');
    const headers = spy.calls[0].init?.headers as Record<string, string>;
    expect(headers['Authorization']).toBe('Bearer config-token');
  });

  it('explicit url override beats project config', async () => {
    fs.mkdirSync(path.join(projectPath, 'UserSettings'), { recursive: true });
    fs.writeFileSync(
      path.join(projectPath, 'UserSettings', 'AI-Game-Developer-Config.json'),
      JSON.stringify({ connectionMode: 'Custom', host: 'http://config-host:1', token: 't' }),
    );
    const spy = makeFetchSpy(async () => jsonResponse({ status: 'success', content: [] }));

    await runTool({
      toolName: 'ping',
      unityProjectPath: projectPath,
      url: 'http://override-host:2',
      fetchImpl: spy.fetch,
    });

    expect(spy.calls[0].url).toBe('http://override-host:2/api/tools/ping');
  });
});

// ---------------------------------------------------------------------------
// Library hygiene
// ---------------------------------------------------------------------------

describe('runTool — library hygiene', () => {
  it('produces no stdout / stderr noise during a successful call', async () => {
    const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    const errFn = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const stdout = vi.spyOn(process.stdout, 'write').mockImplementation(() => true);
    const stderr = vi.spyOn(process.stderr, 'write').mockImplementation(() => true);

    const fetchImpl: typeof fetch = async () =>
      jsonResponse({ status: 'success', content: [] });

    const result = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      fetchImpl,
    });

    expect(result.kind).toBe('success');
    expect(log).not.toHaveBeenCalled();
    expect(errFn).not.toHaveBeenCalled();
    expect(stdout).not.toHaveBeenCalled();
    expect(stderr).not.toHaveBeenCalled();

    log.mockRestore();
    errFn.mockRestore();
    stdout.mockRestore();
    stderr.mockRestore();
  });

  it('wire-compatible: result.success mirrors result.kind === "success"', async () => {
    const fetchImpl: typeof fetch = async () =>
      jsonResponse({ status: 'success', content: [] });

    const ok = await runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      fetchImpl,
    });
    expect(ok.success).toBe(ok.kind === 'success');

    const fail = await runTool({ toolName: '', url: 'http://localhost:55000' });
    expect(fail.success).toBe(fail.kind === 'success');
  });

  it('honours an externally supplied AbortSignal', async () => {
    const controller = new AbortController();
    const fetchImpl: typeof fetch = (_url, init) =>
      new Promise((_resolve, reject) => {
        init?.signal?.addEventListener('abort', () => {
          reject(new DOMException('aborted', 'AbortError'));
        });
      });

    const promise = runTool({
      toolName: 'ping',
      url: 'http://localhost:55000',
      signal: controller.signal,
      fetchImpl,
    });

    // Abort immediately — the signal is forwarded into the internal
    // controller and the fetch should reject with AbortError.
    controller.abort();
    const result = await promise;

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.reason).toBe('timeout');
  });
});
