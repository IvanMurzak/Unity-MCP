import { describe, it, expect, vi } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { installUnityEditor } from '../src/lib.js';
import type { InstallUnityEditorOptions, ProgressEvent } from '../src/lib.js';
import { parseHubProgress, runHubInstall, type RunHubInstallOptions } from '../src/utils/unity-hub.js';

const VERSION = '6000.3.1f1';
const FAKE_HUB = '/hub/Unity Hub';
const EDITOR = { version: VERSION, path: `/editors/${VERSION}/Editor/Unity` };

type RunImpl = NonNullable<InstallUnityEditorOptions['runHubInstallImpl']>;

/**
 * Options with every side-effecting dependency mocked: no editor installed, a Hub at FAKE_HUB that lists VERSION as
 * a promoted release, and an install that "succeeds" and makes the editor appear on disk.
 */
function makeOptions(overrides: Partial<InstallUnityEditorOptions> = {}): InstallUnityEditorOptions {
  let installed = false;
  const runHubInstallImpl = vi.fn<RunImpl>(async () => { installed = true; });
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

  it('uses an explicit hubPath without looking for the Hub', async () => {
    const findHubImpl = vi.fn(() => null);
    const options = makeOptions({ hubPath: '/custom/hub', findHubImpl });
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
    expect(options.resolveChangesetImpl).toHaveBeenCalledWith(VERSION);
    expect(vi.mocked(options.runHubInstallImpl!).mock.calls[0][1])
      .toEqual(['--', '--headless', 'install', '--version', VERSION, '--changeset', 'abcdef123456']);
    expect(events.map((e) => e.phase)).toEqual(['start', 'hub-located', 'resolving-changeset', 'installed']);
  });

  it('falls back to the changeset when the releases list fails, with a warning', async () => {
    const options = makeOptions({ listReleasesImpl: async () => { throw new Error('Failed to fetch available releases: boom'); } });
    const result = await installUnityEditor(options);
    expect(result.kind).toBe('success');
    expect(result.warnings).toEqual(['Failed to fetch available releases: boom']);
    expect(options.resolveChangesetImpl).toHaveBeenCalledWith(VERSION);
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
    const options = makeOptions({ runHubInstallImpl: async () => {} });
    const result = await installUnityEditor(options);
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure');
    expect(result.reason).toBe('not-found-after-install');
  });

  it('maps Hub progress lines to progress events, skipping repeated percentages', async () => {
    const lines = [
      'Progress:',
      `[Unity (${VERSION})] in progress`,
      `[Unity (${VERSION})] downloading 10.00%`,
      `[Unity (${VERSION})] downloading 10.00%`,
      `[Unity (${VERSION})] downloading 55.50%`,
      `[Unity (${VERSION})] finished downloading`,
      `[Unity (${VERSION})] validating download...`,
      `[Unity (${VERSION})] queued for install`,
      `[Unity (${VERSION})] installing...`,
      `[Unity (${VERSION})] installed successfully.`,
      'All Tasks Completed Successfully.',
    ];
    let installed = false;
    const runHubInstallImpl: RunImpl = async (_hub, _args, _version, opts?: RunHubInstallOptions) => {
      for (const line of lines) {
        const parsed = parseHubProgress(line);
        if (parsed) opts?.onProgress?.(parsed);
      }
      installed = true;
    };
    const events: ProgressEvent[] = [];
    const result = await installUnityEditor(makeOptions({
      runHubInstallImpl,
      listLocalEditorsImpl: () => (installed ? [EDITOR] : []),
      onProgress: (e) => events.push(e),
    }));
    expect(result.kind).toBe('success');
    const summary = events.map((e) => (e.phase === 'downloading' ? `downloading ${e.percent}` : e.phase));
    expect(summary).toEqual([
      'start', 'hub-located',
      'downloading 0', 'downloading 10', 'downloading 55.5', 'downloading 100',
      'validating', 'installing', 'installing',
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
