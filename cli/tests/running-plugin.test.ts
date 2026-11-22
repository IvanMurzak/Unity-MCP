// Copyright (c) 2025 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, afterEach, vi } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { installExtension, updateProject } from '../src/lib.js';
import { lockedPluginVersions, runningPluginRelease } from '../src/utils/manifest.js';
import { makeTempDir, removeTempDirs } from './helpers/temp-dir.js';

const PLUGIN = 'com.ivanmurzak.unity.mcp';
const TILEMAP = 'com.ivanmurzak.unity.mcp.tilemap';
const TIMELINE = 'com.ivanmurzak.unity.mcp.timeline';

afterEach(() => {
  vi.unstubAllGlobals();
  removeTempDirs();
});

/** A project; `lock`: what Unity recorded per package as [version, the plugin version it names]. */
function project(dependencies: Record<string, string>, lock?: Record<string, [string, string?]>): string {
  const root = makeTempDir('unity-running-');
  fs.mkdirSync(path.join(root, 'Packages'));
  fs.writeFileSync(path.join(root, 'Packages', 'manifest.json'), JSON.stringify({ dependencies }));
  if (lock) {
    fs.writeFileSync(path.join(root, 'Packages', 'packages-lock.json'), JSON.stringify({
      dependencies: Object.fromEntries(Object.entries(lock).map(([packageId, [version, plugin]]) =>
        [packageId, { version, depth: 0, source: 'registry', dependencies: plugin === undefined ? {} : { [PLUGIN]: plugin } }])),
    }));
  }
  return root;
}

const dependenciesOf = (root: string): Record<string, string> =>
  JSON.parse(fs.readFileSync(path.join(root, 'Packages', 'manifest.json'), 'utf-8')).dependencies;

/** OpenUPM stub: per package id its versions (ascending) with the plugin release each is built for. */
function stubOpenUpm(packages: Record<string, Record<string, string>>): void {
  vi.stubGlobal('fetch', vi.fn(async (url: string) => {
    const versions = packages[String(url).split('/').pop()!];
    if (versions === undefined) return { ok: false, status: 503, json: async () => ({}) };
    return {
      ok: true,
      status: 200,
      json: async () => ({
        'dist-tags': { latest: Object.keys(versions).pop() },
        versions: Object.fromEntries(Object.entries(versions).map(([version, plugin]) => [version, { dependencies: { [PLUGIN]: plugin } }])),
      }),
    };
  }));
}

// Measured in Unity 2022.3.62f3 with registry packages: a manifest with plugin 1.12.0 and an extension that
// depends on plugin 1.13.0 runs plugin 1.13.0, and the manifest entry stays 1.12.0.
describe('runningPluginRelease', () => {
  it('is the highest of the manifest entry and the plugin versions the installed extensions name', () => {
    expect(runningPluginRelease('1.12.0', ['1.13.0', '1.6.2'])).toBe('1.13.0');
    expect(runningPluginRelease('1.13.0', ['1.12.0'])).toBe('1.13.0');
    expect(runningPluginRelease('1.13.0', [])).toBe('1.13.0');
    expect(runningPluginRelease('1.9.0', ['1.10.0'])).toBe('1.10.0'); // numbers, not text
  });

  it('skips what names no release, and knows none for a local plugin or when nothing names one', () => {
    expect(runningPluginRelease('1.12.0', [null, undefined, '^1.13.0', 'file:../x', '1.14.0-rc.1'])).toBe('1.12.0');
    expect(runningPluginRelease(undefined, ['1.13.0'])).toBe('1.13.0'); // Unity installs the plugin an extension names
    expect(runningPluginRelease(undefined, [])).toBeNull();
    expect(runningPluginRelease('file:../plugin', ['1.13.0'])).toBeNull(); // the local one runs, whatever is named
    expect(runningPluginRelease('1.13.0-rc.1', [])).toBeNull();
  });
});

describe('lockedPluginVersions', () => {
  it('reads what the lock holds for the very versions the manifest asks for', () => {
    const dependencies = { [PLUGIN]: '1.12.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.0.0', 'com.unity.ugui': '1.0.0' };
    const root = project(dependencies, {
      [PLUGIN]: ['1.13.0'],
      [TILEMAP]: ['2.1.1', '1.13.0'],
      [TIMELINE]: ['1.9.9', '1.6.2'], // the manifest has moved past it: says nothing about 2.0.0
      'com.unity.ugui': ['1.0.0'],
    });

    expect([...lockedPluginVersions(root, dependencies)]).toEqual([[TILEMAP, '1.13.0']]);
  });

  it('knows nothing without a readable lock', () => {
    expect(lockedPluginVersions(project({}), {}).size).toBe(0);
    const root = project({});
    fs.writeFileSync(path.join(root, 'Packages', 'packages-lock.json'), 'not json');
    expect(lockedPluginVersions(root, {}).size).toBe(0);
  });
});

describe('the plugin a project runs decides the extension versions', () => {
  const published = { '2.0.0': '1.0.0', '2.1.1': '1.8.0', '2.2.0': '1.9.0' };

  it('install-extension installs the version for the plugin an installed extension lifts the project to', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TIMELINE]: '2.1.1' }, { [TIMELINE]: ['2.1.1', '1.8.0'] });
    stubOpenUpm({ [TILEMAP]: published });

    const result = await installExtension({ unityProjectPath: root, extensionId: 'Tilemap' });

    if (result.kind !== 'success') throw result.error;
    // Not 2.0.0 (the manifest's plugin entry): Unity runs plugin 1.8.0 here and would skip it.
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TIMELINE]: '2.1.1', [TILEMAP]: '2.1.1' });
  });

  it('install-extension never moves the extension that lifts the plugin down to the manifest entry', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1' }, { [TILEMAP]: ['2.1.1', '1.8.0'] });
    stubOpenUpm({ [TILEMAP]: published });

    const result = await installExtension({ unityProjectPath: root, extensionId: 'Tilemap' });

    if (result.kind !== 'success') throw result.error;
    expect(result).toMatchObject({ outcome: 'already-up-to-date', warnings: [] });
    expect(dependenciesOf(root)[TILEMAP]).toBe('2.1.1');
  });

  it('update --no-plugin moves the others up to the plugin that runs, with or without a lock', async () => {
    for (const lock of [undefined, { [TILEMAP]: ['2.1.1', '1.8.0'] as [string, string] }]) {
      const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.0.0' }, lock);
      stubOpenUpm({ [PLUGIN]: { '1.9.0': '' }, [TILEMAP]: published, [TIMELINE]: published });

      const result = await updateProject({ unityProjectPath: root, noPlugin: true });

      if (result.kind !== 'success') throw result.error;
      expect(result.items.map((item) => [item.packageId, item.status])).toEqual([[TILEMAP, 'current'], [TIMELINE, 'outdated']]);
      expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.1.1' });
      expect(result.warnings).toEqual([]);
    }
  });

  it('a held plugin keeps the extensions on the plugin that runs, never on the lower manifest entry', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.0.0' });
    // Plugin 1.9.0 is out; Timeline has no version for it yet, so the plugin is held. The project runs 1.8.0.
    stubOpenUpm({ [PLUGIN]: { '1.9.0': '' }, [TILEMAP]: published, [TIMELINE]: { '2.0.0': '1.0.0', '2.1.1': '1.8.0' } });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.status)).toEqual(['held', 'current', 'outdated']);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.1.1' });
  });
});
