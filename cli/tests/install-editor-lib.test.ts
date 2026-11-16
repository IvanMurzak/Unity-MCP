import { describe, it, expect, vi } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { installUnityEditor } from '../src/lib.js';
import type { InstallUnityEditorOptions, ProgressEvent } from '../src/lib.js';
import { parseHubProgress, queryAvailableReleases, runHubInstall, type RunHubInstallOptions } from '../src/utils/unity-hub.js';

const VERSION = '6000.3.1f1';
const FAKE_HUB = '/hub/Unity Hub';
const EDITOR = { version: VERSION, path: `/editors/${VERSION}/Editor/Unity` };

type RunImpl = NonNullable<InstallUnityEditorOptions['runHubInstallImpl']>;

/**
 * Options with every side-effecting dependency mocked: no editor installed, a Hub at FAKE_HUB that lists VERSION as
 * a promoted release, and an install (the given `runHubInstallImpl`, else a no-op) whose success makes the editor
 * appear on disk. `runHubInstallImpl` of the result is a spy.
 */
function makeOptions(overrides: Partial<InstallUnityEditorOptions> = {}): InstallUnityEditorOptions {
  let installed = false;
  const install: RunImpl = overrides.runHubInstallImpl ?? (async () => {});
  const runHubInstallImpl = vi.fn<RunImpl>(async (...args) => {
    await install(...args);
    installed = true;
  });
  return {
    version: VERSION,
    findHubImpl: () => FAKE_HUB,
    listLocalEditorsImpl: () => (installed ? [EDITOR] : []),
    listEditorsImpl: async () => [],
    listReleasesImpl: async () => [{ version: VERSION, isStable: true }],
    resolveChangesetImpl: vi.fn(async () => 'abcdef123456'),
    runHubInstallImpl,
    resolveEditorExecutableImpl: (p) => p,
    ...overrides,
    runHubInstallImpl,
  };
}

describe('installUnityEditor — validation and discovery', () => {
  it('fails on an invalid version without looking for the Hub', async () => {
    const findHubImpl = vi.fn(() => FAKE_HUB);
    const result = await installUnityEditor(makeOptions({ version: '6000.3', findHubImpl }));
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure');
    expect(result.reason).toBe('invalid-version');
    expect(result.success).toBe(false);
    expect(findHubImpl).not.toHaveBeenCalled();
  });

  it('reports an editor already on disk without needing the Hub', async () => {
    const findHubImpl = vi.fn(() => null);
    const options = makeOptions({ findHubImpl, listLocalEditorsImpl: () => [EDITOR] });
    const events: ProgressEvent[] = [];
    const result = await installUnityEditor({ ...options, onProgress: (e) => events.push(e) });
    expect(result).toMatchObject({ kind: 'success', alreadyInstalled: true, editorPath: EDITOR.path, version: VERSION });
    expect(findHubImpl).not.toHaveBeenCalled();
    expect(options.runHubInstallImpl).not.toHaveBeenCalled();
    expect(events.map((e) => e.phase)).toEqual(['start', 'already-installed']);
  });

  it('reports an editor the Hub lists as already installed', async () => {
    const options = makeOptions({ listEditorsImpl: async () => [EDITOR] });
    const result = await installUnityEditor(options);
    expect(result).toMatchObject({ kind: 'success', alreadyInstalled: true, editorPath: EDITOR.path });
    expect(options.runHubInstallImpl).not.toHaveBeenCalled();
  });

  it('fails with hub-not-found and installs nothing when the Hub is missing', async () => {
    const options = makeOptions({ findHubImpl: () => null });
    const result = await installUnityEditor(options);
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure');
    expect(result.reason).toBe('hub-not-found');
    expect(result.errorMessage).toContain('Unity Hub not found');
    expect(options.runHubInstallImpl).not.toHaveBeenCalled();
  });

  it('fails with hub-not-found when an explicit hubPath does not exist', async () => {
    const options = makeOptions({ hubPath: '/no/such/hub', hubExistsImpl: () => false });
    const result = await installUnityEditor(options);
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure');
    expect(result.reason).toBe('hub-not-found');
    expect(options.runHubInstallImpl).not.toHaveBeenCalled();
  });

  it('uses an explicit hubPath without looking for the Hub', async () => {
    const findHubImpl = vi.fn(() => null);
    const options = makeOptions({ hubPath: '/custom/hub', findHubImpl, hubExistsImpl: () => true });
    const result = await installUnityEditor(options);
    expect(result.kind).toBe('success');
    expect(findHubImpl).not.toHaveBeenCalled();
    expect(vi.mocked(options.runHubInstallImpl!).mock.calls[0][0]).toBe('/custom/hub');
  });
});

describe('installUnityEditor — install', () => {
  it('installs a promoted version by version alone', async () => {
    const options = makeOptions();
    const result = await installUnityEditor(options);
    expect(result).toMatchObject({ kind: 'success', alreadyInstalled: false, editorPath: EDITOR.path });
    expect(options.resolveChangesetImpl).not.toHaveBeenCalled();
    const [hub, args, version] = vi.mocked(options.runHubInstallImpl!).mock.calls[0];
    expect(hub).toBe(FAKE_HUB);
    expect(args).toEqual(['--', '--headless', 'install', '--version', VERSION]);
    expect(version).toBe(VERSION);
  });

  it('resolves the changeset for a version off the releases list', async () => {
    const options = makeOptions({ listReleasesImpl: async () => [{ version: '6000.0.1f1', isStable: true }] });
    const events: ProgressEvent[] = [];
    const result = await installUnityEditor({ ...options, onProgress: (e) => events.push(e) });
    expect(result).toMatchObject({ kind: 'success', changeset: 'abcdef123456' });
    expect(options.resolveChangesetImpl).toHaveBeenCalledWith(VERSION, undefined);
    expect(vi.mocked(options.runHubInstallImpl!).mock.calls[0][1])
      .toEqual(['--', '--headless', 'install', '--version', VERSION, '--changeset', 'abcdef123456']);
    expect(events.map((e) => e.phase)).toEqual(['start', 'hub-located', 'resolving-changeset', 'installed']);
  });

  it('falls back to the changeset when the releases list fails, with a warning', async () => {
    const options = makeOptions({ listReleasesImpl: async () => { throw new Error('Failed to fetch available releases: boom'); } });
    const result = await installUnityEditor(options);
    expect(result.kind).toBe('success');
    expect(result.warnings).toEqual(['Failed to fetch available releases: boom']);
    expect(options.resolveChangesetImpl).toHaveBeenCalledWith(VERSION, undefined);
  });

  it('fails with changeset-not-found and runs no install when the changeset cannot be resolved', async () => {
    const options = makeOptions({ listReleasesImpl: async () => [], resolveChangesetImpl: async () => null });
    const result = await installUnityEditor(options);
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure');
    expect(result.reason).toBe('changeset-not-found');
    expect(result.hubPath).toBe(FAKE_HUB);
    expect(options.runHubInstallImpl).not.toHaveBeenCalled();
  });

  it('fails with install-failed when the Hub install fails', async () => {
    const options = makeOptions({ runHubInstallImpl: async () => { throw new Error('Failed to install Unity Editor (exit code 1)'); } });
    const result = await installUnityEditor(options);
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure');
    expect(result.reason).toBe('install-failed');
    expect(result.errorMessage).toContain('exit code 1');
  });

  it('fails with not-found-after-install when the editor does not appear', async () => {
    const result = await installUnityEditor(makeOptions({ listLocalEditorsImpl: () => [] }));
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure');
    expect(result.reason).toBe('not-found-after-install');
  });

  it('maps Hub progress lines to progress events, at most once per whole percent', async () => {
    const lines = [
      'Progress:',
      `[Unity (${VERSION})] in progress`,
      `[Unity (${VERSION})] downloading 10.00%`,
      `[Unity (${VERSION})] downloading 10.00%`,
      `[Unity (${VERSION})] downloading 55.50%`,
      `[Unity (${VERSION})] downloading 55.90%`,
      `[Unity (${VERSION})] finished downloading`,
      `[Unity (${VERSION})] validating download...`,
      `[Unity (${VERSION})] queued for install`,
      `[Unity (${VERSION})] installing...`,
      `[Unity (${VERSION})] installed successfully.`,
      'All Tasks Completed Successfully.',
    ];
    const runHubInstallImpl: RunImpl = async (_hub, _args, _version, opts?: RunHubInstallOptions) => {
      for (const line of lines) {
        const parsed = parseHubProgress(line);
        if (parsed) opts?.onProgress?.(parsed);
      }
    };
    const events: ProgressEvent[] = [];
    const result = await installUnityEditor(makeOptions({ runHubInstallImpl, onProgress: (e) => events.push(e) }));
    expect(result.kind).toBe('success');
    const summary = events.map((e) => (e.phase === 'downloading' ? `downloading ${e.percent}` : e.phase));
    expect(summary).toEqual([
      'start', 'hub-located',
      'downloading 0', 'downloading 10', 'downloading 55.5', 'downloading 100',
      'validating', 'installing',
      'installed',
    ]);
  });

  it('swallows exceptions thrown by onProgress', async () => {
    const result = await installUnityEditor(makeOptions({ onProgress: () => { throw new Error('ui crashed'); } }));
    expect(result.kind).toBe('success');
  });
});

describe('installUnityEditor — abort', () => {
  it('fails with aborted before doing anything when the signal already fired', async () => {
    const controller = new AbortController();
    controller.abort();
    const options = makeOptions({ signal: controller.signal });
    const result = await installUnityEditor(options);
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure');
    expect(result.reason).toBe('aborted');
    expect(options.runHubInstallImpl).not.toHaveBeenCalled();
  });

  it('reports the installed editor when the signal fires after the Hub finished', async () => {
    const controller = new AbortController();
    const listEditorsImpl = vi.fn(async (_hub: string, signal?: AbortSignal) => {
      if (signal?.aborted) throw new Error('aborted listing');
      return controller.signal.aborted ? [EDITOR] : [];
    });
    const options = makeOptions({
      signal: controller.signal,
      listLocalEditorsImpl: () => [],
      listEditorsImpl,
      runHubInstallImpl: async () => { controller.abort(); },
    });
    const result = await installUnityEditor(options);
    expect(result).toMatchObject({ kind: 'success', alreadyInstalled: false, editorPath: EDITOR.path });
  });

  it('fails with aborted when the signal fires during the install', async () => {
    const controller = new AbortController();
    const runHubInstallImpl: RunImpl = (_hub, _args, _version, opts) => new Promise((_resolve, reject) => {
      expect(opts?.signal).toBe(controller.signal);
      opts?.signal?.addEventListener('abort', () => reject(new Error('aborted by signal')));
      controller.abort();
    });
    const result = await installUnityEditor(makeOptions({ signal: controller.signal, runHubInstallImpl }));
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure');
    expect(result.reason).toBe('aborted');
  });
});

function isAlive(pid: number): boolean {
  try {
    process.kill(pid, 0);
    return true;
  } catch {
    return false;
  }
}

// The real spawn path, with Node standing in for Unity Hub (`process.execPath -e <script>`).
describe('runHubInstall', () => {
  const nodeArgs = (script: string) => ['-e', script];

  it('reports parsed progress lines, including a last line without a newline', async () => {
    const statuses: string[] = [];
    await runHubInstall(process.execPath, nodeArgs(
      `process.stdout.write('[Unity (${VERSION})] downloading 42.00%\\nnoise\\n[Unity (${VERSION})] installed successfully.')`,
    ), VERSION, { onProgress: (p) => statuses.push(`${p.status} ${p.percent}`) });
    expect(statuses).toEqual(['Downloading 42', 'Installed 100']);
  });

  it('splits progress on bare carriage returns', async () => {
    const statuses: string[] = [];
    await runHubInstall(process.execPath, nodeArgs(
      `process.stdout.write('[Unity (${VERSION})] downloading 1.00%\\r[Unity (${VERSION})] downloading 2.00%\\r\\n')`,
    ), VERSION, { onProgress: (p) => statuses.push(`${p.status} ${p.percent}`) });
    expect(statuses).toEqual(['Downloading 1', 'Downloading 2']);
  });

  it('runs the Hub without ELECTRON_RUN_AS_NODE', async () => {
    process.env['ELECTRON_RUN_AS_NODE'] = '1';
    try {
      await expect(runHubInstall(process.execPath, nodeArgs(
        `process.exit(process.env.ELECTRON_RUN_AS_NODE ? 5 : 0)`,
      ), VERSION)).resolves.toBeUndefined();
    } finally {
      delete process.env['ELECTRON_RUN_AS_NODE'];
    }
  });

  it('rejects with the exit code and stderr when the Hub fails', async () => {
    await expect(runHubInstall(process.execPath, nodeArgs(
      `process.stderr.write('no such version'); process.exit(3)`,
    ), VERSION)).rejects.toThrow(/exit code 3\):\nno such version/);
  });

  it('kills the Hub process and rejects with an AbortError on abort', async () => {
    const pidFile = path.join(fs.mkdtempSync(path.join(os.tmpdir(), 'hub-abort-')), 'pid');
    const controller = new AbortController();
    const run = runHubInstall(process.execPath, nodeArgs(
      `require('fs').writeFileSync(${JSON.stringify(pidFile)}, String(process.pid)); ` +
      `console.log('[Unity (${VERSION})] downloading 1.00%'); setTimeout(() => {}, 60000)`,
    ), VERSION, { signal: controller.signal, onProgress: () => controller.abort() });
    await expect(run).rejects.toMatchObject({ name: 'AbortError' });
    const pid = Number(fs.readFileSync(pidFile, 'utf-8'));
    await vi.waitFor(() => expect(isAlive(pid)).toBe(false), { timeout: 10000, interval: 100 });
  });

  it('rejects at once when the signal already fired', async () => {
    const controller = new AbortController();
    controller.abort();
    await expect(runHubInstall(process.execPath, nodeArgs('setTimeout(() => {}, 60000)'), VERSION, { signal: controller.signal }))
      .rejects.toMatchObject({ name: 'AbortError' });
  });
});

describe.skipIf(process.platform === 'win32')('queryAvailableReleases — abort', () => {
  it('kills the Hub and the processes it started', async () => {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'hub-query-abort-'));
    const pidFile = path.join(dir, 'pid');
    const hub = path.join(dir, 'hub.sh');
    fs.writeFileSync(hub, `#!/bin/sh\nsleep 60 &\necho $! > ${pidFile}\nwait\n`, { mode: 0o755 });
    try {
      const controller = new AbortController();
      const query = queryAvailableReleases(hub, controller.signal);
      await vi.waitFor(() => expect(fs.readFileSync(pidFile, 'utf-8').trim()).not.toBe(''), { timeout: 10000, interval: 50 });
      controller.abort();
      await expect(query).rejects.toThrow(/aborted/);
      const childPid = Number(fs.readFileSync(pidFile, 'utf-8'));
      await vi.waitFor(() => expect(isAlive(childPid)).toBe(false), { timeout: 10000, interval: 100 });
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });
});
