// Copyright (c) 2025 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, afterEach, vi } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { installExtension, removePlugin, updateProject } from '../src/lib.js';
import { resolveRunningPlugin } from '../src/lib/running-plugin.js';
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
  fs.mkdirSync(path.join(root, 'ProjectSettings'));
  fs.writeFileSync(path.join(root, 'ProjectSettings', 'ProjectSettings.asset'),
    'PlayerSettings:\n  scriptingDefineSymbols:\n    Standalone: UNITY_MCP_DEPS_49;UNITY_MCP_READY\n  other: 1\n');
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

const gateOpen = (root: string): boolean =>
  fs.readFileSync(path.join(root, 'ProjectSettings', 'ProjectSettings.asset'), 'utf-8').includes('UNITY_MCP_READY');

/** OpenUPM stub: per package id its versions (ascending) with the plugin release each is built for. */
function stubOpenUpm(packages: Record<string, Record<string, string>>): ReturnType<typeof vi.fn> {
  const fetchMock = vi.fn(async (url: string) => {
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
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

// An extension's versions: 2.0.0 for plugin 1.0.0, 2.1.1 for 1.8.0, 2.2.0 for 1.9.0.
const published = { '2.0.0': '1.0.0', '2.1.1': '1.8.0', '2.2.0': '1.9.0' };
const plugins = (latest: string): Record<string, string> => ({ [latest]: '' });

// Measured in Unity 2022.3.62f3 with registry packages: a manifest with plugin 1.12.0 and an extension that
// depends on plugin 1.13.0 runs plugin 1.13.0, and the manifest entry stays 1.12.0.
describe('runningPluginRelease', () => {
  it('is the highest of the manifest entry and the plugin versions the installed extensions name', () => {
    expect(runningPluginRelease('1.12.0', ['1.13.0', '1.6.2'])).toBe('1.13.0');
    expect(runningPluginRelease('1.13.0', ['1.12.0'])).toBe('1.13.0');
    expect(runningPluginRelease('1.13.0', [])).toBe('1.13.0');
    expect(runningPluginRelease('1.9.0', ['1.10.0'])).toBe('1.10.0'); // numbers, not text
  });

  it('skips what names no release', () => {
    expect(runningPluginRelease('1.12.0', [null, undefined, '^1.13.0', 'file:../x', '1.14.0-rc.1'])).toBe('1.12.0');
    expect(runningPluginRelease(undefined, ['1.13.0'])).toBe('1.13.0'); // Unity installs the plugin an extension names
    expect(runningPluginRelease(undefined, [])).toBeNull();
  });

  it('knows none when the entry itself names no release: that plugin runs as it is, whatever is named', () => {
    expect(runningPluginRelease('file:../plugin', ['1.13.0'])).toBeNull();
    expect(runningPluginRelease('1.14.0-rc.1', ['1.12.0'])).toBeNull();
    expect(runningPluginRelease('^1.13.0', ['1.12.0'])).toBeNull();
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

  it('knows nothing without a lock Unity could have written', () => {
    expect(lockedPluginVersions(project({}), {}).size).toBe(0);
    const root = project({});
    const lock = path.join(root, 'Packages', 'packages-lock.json');
    fs.writeFileSync(lock, 'not json');
    expect(lockedPluginVersions(root, {}).size).toBe(0);
    // An entry without a version must not match a package the manifest does not list (undefined === undefined).
    fs.writeFileSync(lock, JSON.stringify({ dependencies: { 'com.other': { dependencies: { [PLUGIN]: '1.99.0' } }, 'com.null': null } }));
    expect(lockedPluginVersions(root, {}).size).toBe(0);
  });
});

describe('resolveRunningPlugin', () => {
  it('asks OpenUPM only about installed extensions the lock does not hold at the manifest version', async () => {
    const dependencies = { [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.2.0' };
    const root = project(dependencies, { [TILEMAP]: ['2.1.1', '1.8.0'] });
    const fetchMock = stubOpenUpm({ [TIMELINE]: published });

    expect(await resolveRunningPlugin(root, dependencies)).toBe('1.9.0');
    expect(fetchMock.mock.calls.map((call) => String(call[0]).split('/').pop())).toEqual([TIMELINE]);
  });

  it('is the best known release when a lookup fails, and reads nothing for a local plugin', async () => {
    const fetchMock = stubOpenUpm({});
    const dependencies = { [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1' };
    expect(await resolveRunningPlugin(project(dependencies), dependencies)).toBe('1.0.0');

    fetchMock.mockClear();
    const local = { [PLUGIN]: 'file:../plugin', [TILEMAP]: '2.1.1' };
    expect(await resolveRunningPlugin(project(local), local)).toBeNull();
    expect(fetchMock).not.toHaveBeenCalled();
  });
});

describe('the plugin a project runs decides the extension versions', () => {
  it('install-extension installs the version for the plugin an installed extension lifts the project to', async () => {
    // With a lock that knows it, and without one (a project Unity has not resolved since the manifest edit).
    for (const lock of [{ [TIMELINE]: ['2.1.1', '1.8.0'] as [string, string] }, undefined]) {
      const root = project({ [PLUGIN]: '1.0.0', [TIMELINE]: '2.1.1' }, lock);
      stubOpenUpm({ [TILEMAP]: published, [TIMELINE]: published });

      const result = await installExtension({ unityProjectPath: root, extensionId: 'Tilemap' });

      if (result.kind !== 'success') throw result.error;
      // Not 2.0.0 (the manifest's plugin entry): Unity runs plugin 1.8.0 here and would skip it.
      expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TIMELINE]: '2.1.1', [TILEMAP]: '2.1.1' });
    }
  });

  it('install-extension without a plugin entry installs for the plugin the installed extensions bring along', async () => {
    const root = project({ [TIMELINE]: '2.1.1' });
    stubOpenUpm({ [TILEMAP]: published, [TIMELINE]: published });

    const result = await installExtension({ unityProjectPath: root, extensionId: 'Tilemap' });

    if (result.kind !== 'success') throw result.error;
    // Not the latest 2.2.0: it would lift the plugin to 1.9.0 and switch Timeline off.
    expect(dependenciesOf(root)).toMatchObject({ [TIMELINE]: '2.1.1', [TILEMAP]: '2.1.1' });
  });

  it('install-extension never moves the extension that lifts the plugin down to the manifest entry', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1' }, { [TILEMAP]: ['2.1.1', '1.8.0'] });
    stubOpenUpm({ [TILEMAP]: published });

    const result = await installExtension({ unityProjectPath: root, extensionId: 'Tilemap' });

    if (result.kind !== 'success') throw result.error;
    expect(result).toMatchObject({ outcome: 'already-up-to-date', warnings: [] });
    expect(dependenciesOf(root)[TILEMAP]).toBe('2.1.1');
  });

  it('update --no-plugin moves the others up to the plugin that runs, known from OpenUPM or from the lock', async () => {
    // Second round: Tilemap's lookup fails, and only the lock says which plugin it names.
    for (const tilemapKnown of [true, false]) {
      const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.0.0' }, tilemapKnown ? undefined : { [TILEMAP]: ['2.1.1', '1.8.0'] });
      stubOpenUpm({ [PLUGIN]: plugins('1.9.0'), ...(tilemapKnown ? { [TILEMAP]: published } : {}), [TIMELINE]: published });

      const result = await updateProject({ unityProjectPath: root, noPlugin: true });

      if (result.kind !== 'success') throw result.error;
      expect(result.items.map((item) => [item.packageId, item.status])).toEqual([[TILEMAP, tilemapKnown ? 'current' : 'unknown'], [TIMELINE, 'outdated']]);
      expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.1.1' });
    }
  });

  it('a held plugin keeps the extensions on the plugin that runs, never on the lower manifest entry', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.0.0' });
    // Plugin 1.9.0 is out; Timeline has no version for it yet, so the plugin is held. The project runs 1.8.0.
    stubOpenUpm({ [PLUGIN]: plugins('1.9.0'), [TILEMAP]: published, [TIMELINE]: { '2.0.0': '1.0.0', '2.1.1': '1.8.0' } });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.status)).toEqual(['held', 'current', 'outdated']);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.1.1' });
  });

  it('nothing is held when the project already runs the latest plugin: only the stale entry is made true', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.0.0' });
    // Latest is 1.8.0, which Tilemap already lifts the project to; Timeline has no version for it.
    stubOpenUpm({ [PLUGIN]: plugins('1.8.0'), [TILEMAP]: published, [TIMELINE]: { '2.0.0': '1.0.0' } });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.status)).toEqual(['outdated', 'current', 'unavailable']);
    expect(result.warnings.join('\n')).not.toContain('is out, but');
    expect(result.warnings.join('\n')).toContain('Timeline 2.0.0: no version for plugin 1.8.0 is published');
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.0.0' });
  });

  it('an extension that names a plugin newer than the latest one keeps the others with it', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.2.0', [TIMELINE]: '2.0.0' });
    // Tilemap lifts the project to 1.9.0; OpenUPM's latest plugin is 1.8.0.
    stubOpenUpm({ [PLUGIN]: plugins('1.8.0'), [TILEMAP]: published, [TIMELINE]: published });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    // Timeline goes to its version for 1.9.0, not for 1.8.0: Unity would skip that one.
    expect(dependenciesOf(root)).toMatchObject({ [TILEMAP]: '2.2.0', [TIMELINE]: '2.2.0' });
    expect(result.items.map((item) => item.status)).toEqual(['outdated', 'current', 'outdated']);
  });

  it('without a plugin entry the extensions still move to the latest plugin, together, and the gate is closed', async () => {
    const root = project({ [TILEMAP]: '2.1.1', [TIMELINE]: '2.1.1' });
    stubOpenUpm({ [PLUGIN]: plugins('1.9.0'), [TILEMAP]: published, [TIMELINE]: published });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => [item.kind, item.status])).toEqual([['extension', 'outdated'], ['extension', 'outdated']]);
    expect(dependenciesOf(root)).toEqual({ [TILEMAP]: '2.2.0', [TIMELINE]: '2.2.0' });
    // Unity now installs plugin 1.9.0 for them: nothing built on 1.8.0 may compile before it is ready.
    expect(gateOpen(root)).toBe(false);
  });

  it('without a plugin entry a plugin release one extension is not out for holds them all', async () => {
    const root = project({ [TILEMAP]: '2.1.1', [TIMELINE]: '2.1.1' });
    stubOpenUpm({ [PLUGIN]: plugins('1.9.0'), [TILEMAP]: published, [TIMELINE]: { '2.1.1': '1.8.0' } });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.updated).toEqual([]);
    expect(result.warnings.join('\n')).toContain('MCP plugin 1.9.0 is out, but no version for it is known yet of: Timeline');
    expect(dependenciesOf(root)).toEqual({ [TILEMAP]: '2.1.1', [TIMELINE]: '2.1.1' });
    expect(gateOpen(root)).toBe(true);
  });

  it('noExtensions warns about the extensions only when the plugin the project runs really moves', async () => {
    // The entry is stale: Tilemap already lifts the project to the latest plugin, 1.8.0.
    const lifted = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.1.1' }, { [TILEMAP]: ['2.1.1', '1.8.0'] });
    stubOpenUpm({ [PLUGIN]: plugins('1.8.0') });
    const same = await updateProject({ unityProjectPath: lifted, noExtensions: true });
    if (same.kind !== 'success') throw same.error;
    expect(same.warnings).toEqual([]);
    expect(dependenciesOf(lifted)[PLUGIN]).toBe('1.8.0');

    const behind = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0' }, { [TILEMAP]: ['2.0.0', '1.0.0'] });
    const moved = await updateProject({ unityProjectPath: behind, noExtensions: true });
    if (moved.kind !== 'success') throw moved.error;
    expect(moved.warnings.join('\n')).toContain('The installed extensions (Tilemap) are left as they are: they do not work with plugin 1.8.0');
  });
});

describe('removePlugin', () => {
  it('says so when Unity will still install the plugin for an extension that names it', async () => {
    const withExtensions = project({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.1.1' });
    const result = await removePlugin({ unityProjectPath: withExtensions });
    if (result.kind !== 'success') throw result.error;
    expect(result.removed).toBe(true);
    expect(result.warnings.join('\n')).toContain('Unity still installs the plugin: Tilemap, Timeline depend on it');

    // Also when there was no entry to remove: "not installed" alone would be wrong.
    const again = await removePlugin({ unityProjectPath: withExtensions });
    if (again.kind !== 'success') throw again.error;
    expect(again.removed).toBe(false);
    expect(again.warnings.join('\n')).toContain('Unity still installs the plugin: Tilemap, Timeline depend on it');

    const alone = await removePlugin({ unityProjectPath: project({ [PLUGIN]: '1.8.0' }) });
    if (alone.kind !== 'success') throw alone.error;
    expect(alone.warnings).toEqual([]);
  });
});
