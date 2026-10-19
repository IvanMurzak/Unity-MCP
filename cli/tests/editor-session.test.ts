import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { EditorSessionError, SESSION_ENV, readSessionLease, type SessionProcessOps } from '@baizor/gamedev-cli-core';
import {
  createUnitySession, sessionErrorMessage, SESSION_QUIT_TOOL, type UnitySessionDeps,
} from '../src/lib/editor-session.js';

const LOCAL = 'http://localhost:53101';
const REMOTE = 'https://ai-game.dev/mcp';

/** A fake machine: editor processes, a server that answers once `up`, and the plugin's quit tool. */
function fakeWorld() {
  const alive = new Set<number>();
  const world = {
    alive,
    serverUp: false,
    /** The PID of a Unity process that has the project open (ours or the user's). */
    editorPid: undefined as number | undefined,
    nextPid: 4242,
    quitAccepted: true,
    spawned: [] as Array<{ command: string; args: readonly string[]; env: NodeJS.ProcessEnv; hideWindow: boolean }>,
  };
  const processOps: Partial<SessionProcessOps> = {
    spawnDetached: async (command, args, options) => {
      world.spawned.push({ command, args, env: options.env, hideWindow: options.hideWindow });
      const pid = world.nextPid++;
      alive.add(pid);
      world.editorPid = pid;
      world.serverUp = true;
      return pid;
    },
    isAlive: pid => alive.has(pid),
    processStart: async pid => (alive.has(pid) ? `start-${pid}` : undefined),
    requestClose: async () => false,
    forceKill: async pid => { alive.delete(pid); return true; },
    sleep: () => new Promise(resolve => setTimeout(resolve, 1)),
  };
  const fetchImpl = vi.fn(async (input: string | URL | Request) => {
    if (!world.serverUp) throw new TypeError('fetch failed');
    return new Response(JSON.stringify({ status: 'success', url: String(input) }), { status: 200 });
  }) as unknown as typeof fetch;
  const exit = (pid: number) => { alive.delete(pid); world.editorPid = undefined; world.serverUp = false; };
  /** The user's editor (not started by a session) opens the project. */
  const userEditor = (pid = 1111) => { alive.add(pid); world.editorPid = pid; };
  const deps: Partial<UnitySessionDeps> = {
    findEditorPid: () => world.editorPid,
    quitViaPlugin: vi.fn(async (_url: string, _token: string | undefined, _nonce: string) => {
      if (!world.quitAccepted) return false;
      for (const pid of [...alive]) exit(pid);
      return true;
    }),
    requestOsClose: vi.fn(() => false),
    findEditor: vi.fn(async () => '/opt/Unity/Editor/Unity'),
    processOps,
    fetchImpl,
  };
  return { world, deps, exit, userEditor };
}

describe('createUnitySession', () => {
  let project: string;
  const hasLease = () => fs.existsSync(path.join(project, '.ai-game-dev', 'session.json'));

  beforeEach(() => {
    project = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-session-'));
    fs.mkdirSync(path.join(project, 'ProjectSettings'), { recursive: true });
    fs.writeFileSync(path.join(project, 'ProjectSettings', 'ProjectVersion.txt'), 'm_EditorVersion: 2022.3.62f3\n');
  });

  afterEach(() => {
    fs.rmSync(project, { recursive: true, force: true });
  });

  it('reuses an editor whose local server already answers, starting nothing', async () => {
    const { world, deps, userEditor } = fakeWorld();
    world.serverUp = true;
    userEditor();
    const session = createUnitySession({ projectPath: project, urls: [LOCAL], deps });
    expect(await session.ensure()).toEqual({ ready: LOCAL, source: 'running' });
    expect(world.spawned).toHaveLength(0);
  });

  it('starts one headless editor for two parallel cold calls, with the session env', async () => {
    const { world, deps } = fakeWorld();
    const make = () => createUnitySession({
      projectPath: project, urls: [LOCAL], env: { UNITY_MCP_KEEP_CONNECTED: 'true' }, idleSeconds: 120, deps,
    });
    const [a, b] = await Promise.all([make().ensure(), make().ensure()]);

    expect(world.spawned).toHaveLength(1);
    const sources = [a.source, b.source];
    expect(sources.filter(s => s === 'started')).toHaveLength(1);
    expect(['owned', 'running']).toContain(sources.find(s => s !== 'started'));
    expect(a.ready).toBe(LOCAL);
    expect(b.ready).toBe(LOCAL);
    const spawned = world.spawned[0];
    expect(spawned.command).toBe('/opt/Unity/Editor/Unity');
    expect(spawned.args).toEqual(['-batchmode', '-projectPath', path.resolve(project)]);
    expect(spawned.hideWindow).toBe(true);
    expect(spawned.env).toMatchObject({
      UNITY_MCP_KEEP_CONNECTED: 'true',
      [SESSION_ENV.idleSeconds]: '120',
      [SESSION_ENV.owner]: 'cli',
    });
    expect(spawned.env[SESSION_ENV.nonce]).toMatch(/^[0-9a-f]{32}$/);
    expect(hasLease()).toBe(true);
    // The session files stay out of git.
    expect(fs.readFileSync(path.join(project, '.ai-game-dev', '.gitignore'), 'utf-8')).toContain('session.json');
  });

  it('records the server URL and connection mode in the lease', async () => {
    const { deps } = fakeWorld();
    await createUnitySession({ projectPath: project, urls: [LOCAL], serverUrl: LOCAL, deps }).ensure();
    expect(readSessionLease(project)).toMatchObject({ serverUrl: LOCAL, connectionMode: 'Custom' });
  });

  it('opens a window without -batchmode in window mode', async () => {
    const { world, deps } = fakeWorld();
    await createUnitySession({ projectPath: project, urls: [LOCAL], deps }).ensure({ mode: 'window' });
    expect(world.spawned[0].args).toEqual(['-projectPath', path.resolve(project)]);
    expect(world.spawned[0].hideWindow).toBe(false);
  });

  it('waits for a running editor that is not ready yet instead of starting a second one', async () => {
    const { world, deps, userEditor } = fakeWorld();
    userEditor(); // still loading
    setTimeout(() => { world.serverUp = true; }, 20);
    const result = await createUnitySession({ projectPath: project, urls: [LOCAL], deps }).ensure();
    expect(result.source).toBe('foreign');
    expect(world.spawned).toHaveLength(0);
  });

  it('fails fast with a hint when the user editor never answers', async () => {
    const { world, deps, userEditor } = fakeWorld();
    userEditor(); // e.g. its plugin is connected to the cloud only
    const error = await createUnitySession({ projectPath: project, urls: [LOCAL], foreignReadyTimeoutMs: 30, deps })
      .ensure().catch(e => e);
    expect(error).toBeInstanceOf(EditorSessionError);
    expect(error).toMatchObject({ reason: 'not-ready', source: 'foreign' });
    expect(world.spawned).toHaveLength(0);
    expect(sessionErrorMessage(error, REMOTE)).toContain('Cloud mode');
    expect(sessionErrorMessage(error, LOCAL)).toBe(error.message);
    expect(sessionErrorMessage(new Error('boom'), REMOTE)).toBe('boom');
  });

  it('starts its own editor when the user editor exits while it waits', async () => {
    const { world, deps, userEditor, exit } = fakeWorld();
    userEditor();
    setTimeout(() => exit(1111), 20);
    const result = await createUnitySession({ projectPath: project, urls: [LOCAL], deps }).ensure();
    expect(result.source).toBe('started');
    expect(world.spawned).toHaveLength(1);
  });

  it('counts a remote server only while a Unity process has the project open', async () => {
    const { world, deps } = fakeWorld();
    world.serverUp = true; // the cloud answers regardless
    const result = await createUnitySession({ projectPath: project, urls: [REMOTE], deps }).ensure();
    expect(result).toMatchObject({ ready: REMOTE, source: 'started' });
    expect(world.spawned).toHaveLength(1);
  });

  it('closes its own editor through the plugin, with the lease nonce, and removes the lease', async () => {
    const { world, deps } = fakeWorld();
    const session = createUnitySession({ projectPath: project, urls: [LOCAL], token: 'tk', deps });
    const { lease } = await session.ensure();
    expect(lease!.serverUrl).toBeUndefined(); // no recorded URL: the plugin is asked where the probe answers

    expect(await session.closeOwned({ timeoutMs: 1000 })).toEqual({ kind: 'closed', pid: lease!.pid, forced: false });
    expect(deps.quitViaPlugin).toHaveBeenCalledWith(LOCAL, 'tk', lease!.nonce);
    expect(deps.requestOsClose).not.toHaveBeenCalled();
    expect(hasLease()).toBe(false);
    expect(world.alive.size).toBe(0);
  });

  it('asks the plugin at the server URL recorded in the lease', async () => {
    const recorded = 'http://localhost:53999';
    const { deps } = fakeWorld();
    const { lease } = await createUnitySession({ projectPath: project, urls: [LOCAL], serverUrl: recorded, deps }).ensure();
    // A later call (e.g. `close`) that resolved other URLs still reaches the editor where it was started.
    const closing = createUnitySession({ projectPath: project, urls: [LOCAL], token: 'tk', deps });
    expect(await closing.closeOwned({ timeoutMs: 1000 })).toEqual({ kind: 'closed', pid: lease!.pid, forced: false });
    expect(deps.quitViaPlugin).toHaveBeenCalledWith(recorded, 'tk', lease!.nonce);
  });

  it('never targets the server URL of a dead lease, even when that URL answers', async () => {
    const other = 'http://localhost:53888'; // now served by another project's editor
    const writeDeadLease = () => {
      fs.mkdirSync(path.join(project, '.ai-game-dev'), { recursive: true });
      fs.writeFileSync(path.join(project, '.ai-game-dev', 'session.json'), JSON.stringify({
        pid: 999, nonce: 'dead', engine: 'unity', mode: 'headless', startedAt: '2026-10-18T00:00:00Z', ownedBy: 'cli',
        hostId: os.hostname(), processStart: 'start-999', serverUrl: other,
      }));
    };
    writeDeadLease();
    const { world, deps } = fakeWorld();
    const probed: string[] = [];
    deps.fetchImpl = (async (input: string | URL | Request) => {
      const url = String(input);
      probed.push(url);
      if (url.startsWith(other) || world.serverUp) return new Response('{}', { status: 200 });
      throw new TypeError('fetch failed');
    }) as typeof fetch;

    const session = createUnitySession({ projectPath: project, urls: [LOCAL], token: 'tk', deps });
    expect(await session.closeOwned({ timeoutMs: 100 })).toEqual({ kind: 'not-owned', staleLeaseRemoved: true });
    expect(deps.quitViaPlugin).not.toHaveBeenCalled();

    writeDeadLease();
    const result = await createUnitySession({ projectPath: project, urls: [LOCAL], deps }).ensure();
    expect(result).toMatchObject({ ready: LOCAL, source: 'started' });
    expect(probed.some(url => url.startsWith(other))).toBe(false);
  });

  it('falls back to the OS polite quit when the plugin refuses', async () => {
    const { world, deps } = fakeWorld();
    world.quitAccepted = false;
    const session = createUnitySession({ projectPath: project, urls: [LOCAL], deps });
    const { lease } = await session.ensure();

    expect(await session.closeOwned({ timeoutMs: 50 })).toEqual({ kind: 'still-running', pid: lease!.pid });
    expect(deps.requestOsClose).toHaveBeenCalledWith(lease!.pid);
    expect(hasLease()).toBe(true);
  });

  it('never closes an editor it did not start', async () => {
    const { world, deps, userEditor } = fakeWorld();
    world.serverUp = true;
    userEditor();
    const session = createUnitySession({ projectPath: project, urls: [LOCAL], deps });
    await session.ensure();

    expect(await session.closeOwned()).toEqual({ kind: 'not-owned', staleLeaseRemoved: false });
    expect(deps.quitViaPlugin).not.toHaveBeenCalled();
    expect(world.alive.has(1111)).toBe(true);
  });

  it('stops our headless editor before a windowed open', async () => {
    const { deps } = fakeWorld();
    const session = createUnitySession({ projectPath: project, urls: [LOCAL], deps });
    const { lease } = await session.ensure();
    expect(await session.prepareWindowOpen({ timeoutMs: 1000 })).toEqual({ kind: 'proceed', stoppedPid: lease!.pid });
    expect(deps.quitViaPlugin).toHaveBeenCalledTimes(1);
  });

  it('reports a missing editor of the project version', async () => {
    const { deps } = fakeWorld();
    deps.findEditor = vi.fn(async () => null);
    const error = await createUnitySession({ projectPath: project, urls: [LOCAL], deps }).ensure().catch(e => e);
    expect(error).toBeInstanceOf(EditorSessionError);
    expect(error).toMatchObject({ reason: 'no-editor-binary' });
    expect(error.message).toContain('Unity Editor 2022.3.62f3 is not installed.');
    expect(deps.findEditor).toHaveBeenCalledWith('2022.3.62f3');
  });

  it('forgets a discovered editor path that fails to start, but never an explicit one', async () => {
    const { deps } = fakeWorld();
    deps.processOps = { ...deps.processOps, spawnDetached: async () => { throw new Error('EACCES'); } };
    deps.forgetEditor = vi.fn();
    const error = await createUnitySession({ projectPath: project, urls: [LOCAL], deps }).ensure().catch(e => e);
    expect(error).toMatchObject({ reason: 'launch-failed' });
    expect(deps.forgetEditor).toHaveBeenCalledWith('2022.3.62f3');

    const explicit = path.join(project, 'Unity');
    fs.writeFileSync(explicit, '');
    deps.forgetEditor = vi.fn();
    await createUnitySession({ projectPath: project, urls: [LOCAL], editorPath: explicit, deps }).ensure().catch(() => {});
    expect(deps.forgetEditor).not.toHaveBeenCalled();
  });

  it('exposes the quit tool name the plugin registers', () => {
    expect(SESSION_QUIT_TOOL).toBe('editor-session-quit');
  });
});
