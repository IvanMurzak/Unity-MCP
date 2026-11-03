import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { SESSION_ENV, type EditorSession, type EnsureResult, type SessionLease } from '@baizor/gamedev-cli-core';
import type { UnitySessionOptions } from '../src/lib/editor-session.js';
import type { ProgressEvent } from '../src/lib/types.js';

// No real process scan: the session decides everything here.
vi.mock('../src/utils/unity-process.js', () => ({ findUnityProcess: () => null, isAssetImportWorker: () => false }));

const { openProject } = await import('../src/lib/open.js');

const LEASE: SessionLease = {
  pid: 4242, nonce: 'n', engine: 'unity', mode: 'headless', startedAt: '2026-10-18T00:00:00Z', ownedBy: 'cli', hostId: 'h',
};

function fakeSession(overrides: Partial<EditorSession<string>> = {}) {
  const session = {
    ensure: vi.fn(async (): Promise<EnsureResult<string>> => ({ ready: 'http://localhost:53102', source: 'started', lease: LEASE })),
    prepareWindowOpen: vi.fn(async () => ({ kind: 'proceed' as const })),
    ...overrides,
  };
  const factory = vi.fn((_: UnitySessionOptions) => session as unknown as EditorSession<string>);
  return { session, factory };
}

describe('openProject with editor sessions', () => {
  let project: string;

  beforeEach(() => {
    project = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-open-session-'));
    fs.mkdirSync(path.join(project, 'Assets'), { recursive: true });
    fs.mkdirSync(path.join(project, 'ProjectSettings'), { recursive: true });
    fs.writeFileSync(path.join(project, 'ProjectSettings', 'ProjectVersion.txt'), 'm_EditorVersion: 2022.3.62f3\n');
  });

  afterEach(() => {
    fs.rmSync(project, { recursive: true, force: true });
  });

  it('headless: starts through the session, idle as the session env, and waits for readiness', async () => {
    const { session, factory } = fakeSession();
    const events: ProgressEvent[] = [];
    const result = await openProject({
      projectPath: project, headless: true, idleTimeoutSeconds: 90, readyTimeoutMs: 1234, sessionFactory: factory,
      onProgress: e => events.push(e),
    });

    expect(result).toMatchObject({ kind: 'success', editorPid: 4242, baseUrl: 'http://localhost:53102', unityVersion: '2022.3.62f3' });
    expect(result.kind === 'success' && result.alreadyRunning).toBeFalsy();
    expect(session.ensure).toHaveBeenCalledWith({ mode: 'headless' });
    const options = factory.mock.calls[0][0];
    expect(options).toMatchObject({ projectPath: project, idleSeconds: 90, readyTimeoutMs: 1234, env: { UNITY_MCP_KEEP_CONNECTED: 'true' } });
    expect(options.env).not.toHaveProperty('UNITY_MCP_IDLE_TIMEOUT');
    expect(options.env).not.toHaveProperty(SESSION_ENV.idleSeconds); // the session adds it at launch
    expect(options.urls.some(url => url.startsWith('http://localhost:'))).toBe(true);
    expect(events.map(e => e.phase)).toContain('editor-launched');
  });

  it('headless without an idle timeout never quits by itself', async () => {
    const { factory } = fakeSession();
    await openProject({ projectPath: project, headless: true, sessionFactory: factory });
    expect(factory.mock.calls[0][0].idleSeconds).toBe(0);
  });

  it('headless: a running editor is reported as already running', async () => {
    const { factory } = fakeSession({ ensure: vi.fn(async () => ({ ready: 'http://localhost:1', source: 'running' as const })) });
    expect(await openProject({ projectPath: project, headless: true, sessionFactory: factory }))
      .toMatchObject({ kind: 'success', alreadyRunning: true });
  });

  it('headless: a session failure is a failure result', async () => {
    const { factory } = fakeSession({ ensure: vi.fn(async () => { throw new Error('no editor'); }) });
    expect(await openProject({ projectPath: project, headless: true, sessionFactory: factory }))
      .toMatchObject({ kind: 'failure', errorMessage: 'no editor' });
  });

  it('window: fails when our headless editor does not exit, without launching', async () => {
    fs.mkdirSync(path.join(project, '.ai-game-dev'));
    fs.writeFileSync(path.join(project, '.ai-game-dev', 'session.json'), JSON.stringify(LEASE));
    const { session, factory } = fakeSession({
      prepareWindowOpen: vi.fn(async () => ({ kind: 'still-running' as const, pid: 4242 })),
    });
    const result = await openProject({ projectPath: project, sessionFactory: factory });
    expect(session.prepareWindowOpen).toHaveBeenCalled();
    expect(result).toMatchObject({ kind: 'failure' });
    expect(result.kind === 'failure' && result.errorMessage).toContain('PID 4242');
  });
});
