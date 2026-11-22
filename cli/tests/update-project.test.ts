// Copyright (c) 2025 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, vi, afterEach } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { findUnityProjectRoot, updateProject } from '../src/lib/update-project.js';
import { makeTempDir, removeTempDirs } from './helpers/temp-dir.js';

const PLUGIN = 'com.ivanmurzak.unity.mcp';
const TILEMAP = 'com.ivanmurzak.unity.mcp.tilemap';
const SPLINES = 'com.ivanmurzak.unity.mcp.splines';
const TIMELINE = 'com.ivanmurzak.unity.mcp.timeline';

afterEach(() => {
  vi.unstubAllGlobals();
  removeTempDirs();
});

function project(dependencies: Record<string, string>): string {
  const root = makeTempDir('unity-update-');
  fs.mkdirSync(path.join(root, 'Packages'));
  fs.mkdirSync(path.join(root, 'ProjectSettings'));
  fs.writeFileSync(path.join(root, 'ProjectSettings', 'ProjectVersion.txt'), 'm_EditorVersion: 6000.3.1f1\n');
  fs.writeFileSync(path.join(root, 'Packages', 'manifest.json'), JSON.stringify({ dependencies }));
  return root;
}

function dependenciesOf(root: string): Record<string, string> {
  return JSON.parse(fs.readFileSync(path.join(root, 'Packages', 'manifest.json'), 'utf-8')).dependencies;
}

/**
 * OpenUPM stub. Per package id: its one published version, or its versions (ascending) with the plugin
 * release each is built for. A package absent from the map fails its lookup.
 */
function stubOpenUpm(packages: Record<string, string | Record<string, string>>): void {
  vi.stubGlobal('fetch', vi.fn(async (url: string) => {
    const entry = packages[String(url).split('/').pop()!];
    if (entry === undefined) return { ok: false, status: 503, statusText: 'Unavailable', json: async () => ({}) };
    const versions = typeof entry === 'string' ? { [entry]: undefined } : entry;
    const document = {
      'dist-tags': { latest: Object.keys(versions).pop() },
      versions: Object.fromEntries(Object.entries(versions).map(([version, plugin]) =>
        [version, { dependencies: plugin === undefined ? {} : { [PLUGIN]: plugin } }])),
    };
    return { ok: true, status: 200, json: async () => document };
  }));
}

describe('updateProject', () => {
  it('moves the plugin to latest and installed extensions to their versions for it, and installs nothing new', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0', 'com.unity.ugui': '2.0.0' });
    // 2.2.0 is the extension's latest, built for a plugin that is not out: the one for plugin 1.8.0 is 2.1.1.
    stubOpenUpm({
      [PLUGIN]: '1.8.0',
      [TILEMAP]: { '2.0.0': '1.0.0', '2.1.0': '1.8.0', '2.1.1': '1.8.0', '2.2.0': '1.9.0' },
      [SPLINES]: { '2.1.1': '1.8.0' },
    });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.updated.map((item) => item.packageId)).toEqual([PLUGIN, TILEMAP]);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.1.1', 'com.unity.ugui': '2.0.0' });
    expect(dependenciesOf(root)[SPLINES]).toBeUndefined();
    expect(result.warnings).toEqual([]);
  });

  it('dryRun reports without writing, with the extensions for the plugin the update would install', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0', [TILEMAP]: { '2.0.0': '1.0.0', '2.1.1': '1.8.0' } });

    const result = await updateProject({ unityProjectPath: root, dryRun: true });

    if (result.kind !== 'success') throw result.error;
    expect(result.items).toEqual([
      expect.objectContaining({ packageId: PLUGIN, from: '1.0.0', to: '1.8.0', status: 'outdated' }),
      expect.objectContaining({ packageId: TILEMAP, from: '2.0.0', to: '2.1.1', status: 'outdated' }),
    ]);
    expect(result.updated).toEqual([]);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0' });
  });

  it('an extension built for a newer plugin lifts the project to it: the others follow up, it is not moved down', async () => {
    // Unity runs the highest plugin anything names (1.9.0), whatever the manifest entry says.
    const root = project({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.2.0', [TIMELINE]: '2.1.1' });
    stubOpenUpm({
      [PLUGIN]: '1.8.0',
      [TILEMAP]: { '2.1.1': '1.8.0', '2.2.0': '1.9.0' },
      [TIMELINE]: { '2.1.1': '1.8.0', '2.2.0': '1.9.0' },
    });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.status)).toEqual(['current', 'current', 'outdated']);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.2.0', [TIMELINE]: '2.2.0' });
    expect(result.warnings).toEqual([]);
  });

  it('a plugin release whose extensions are not all published yet is held back, and nothing is written', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0', [TIMELINE]: '2.0.0' });
    const before = fs.readFileSync(path.join(root, 'Packages', 'manifest.json'), 'utf-8');
    // Timeline is out for plugin 1.8.0, Tilemap is not yet: the new plugin alone would switch Tilemap off.
    stubOpenUpm({ [PLUGIN]: '1.8.0', [TILEMAP]: { '2.0.0': '1.0.0' }, [TIMELINE]: { '2.0.0': '1.0.0', '2.1.1': '1.8.0' } });

    for (const dryRun of [true, false]) {
      const result = await updateProject({ unityProjectPath: root, dryRun });

      if (result.kind !== 'success') throw result.error;
      expect(result.items).toEqual([
        expect.objectContaining({ packageId: PLUGIN, from: '1.0.0', to: '1.8.0', status: 'held' }),
        expect.objectContaining({ packageId: TILEMAP, status: 'current' }),
        expect.objectContaining({ packageId: TIMELINE, status: 'current' }),
      ]);
      expect(result.updated).toEqual([]);
      expect(result.warnings.join('\n')).toContain('MCP plugin 1.8.0 is out, but no version for it is known yet of: Tilemap');
      expect(fs.readFileSync(path.join(root, 'Packages', 'manifest.json'), 'utf-8')).toBe(before);
    }
  });

  it('an extension with no version for the installed plugin stays and is reported', async () => {
    const root = project({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0', [TILEMAP]: { '2.0.0': '1.0.0' } });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.updated).toEqual([]);
    expect(result.items.find((item) => item.packageId === TILEMAP)).toMatchObject({ status: 'unavailable', to: null });
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.0.0' });
    expect(result.warnings.join('\n')).toContain('Tilemap 2.0.0: no version for plugin 1.8.0 is published');
  });

  it('a plugin whose latest version is unknown keeps its extensions on the installed plugin', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '1.9.0' });
    stubOpenUpm({ [TILEMAP]: { '2.0.0': '1.0.0', '2.1.1': '1.8.0' } });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.status)).toEqual(['unknown', 'outdated']);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0' });
  });

  it('noPlugin moves only the extensions, to their versions for the plugin the project has', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '1.9.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0', [TILEMAP]: { '2.0.0': '1.0.0', '2.1.1': '1.8.0' } });

    const result = await updateProject({ unityProjectPath: root, noPlugin: true });

    if (result.kind !== 'success') throw result.error;
    expect(result.items).toEqual([expect.objectContaining({ packageId: TILEMAP, from: '1.9.0', to: '2.0.0', status: 'outdated' })]);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0' });
  });

  it('never downgrades the plugin', async () => {
    const root = project({ [PLUGIN]: '9.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0' });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.status)).toEqual(['current']);
    expect(dependenciesOf(root)[PLUGIN]).toBe('9.0.0');
  });

  it('leaves file: and git installs alone, and never downgrades', async () => {
    const root = project({ [PLUGIN]: 'file:../plugin', [TILEMAP]: 'git+https://example.invalid/x.git', [TIMELINE]: '9.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.1.1', [TIMELINE]: '2.1.1' });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.updated).toEqual([]);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: 'file:../plugin', [TILEMAP]: 'git+https://example.invalid/x.git', [TIMELINE]: '9.0.0' });
  });

  it('noExtensions updates only the plugin', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.1.1' });

    const result = await updateProject({ unityProjectPath: root, noExtensions: true });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.packageId)).toEqual([PLUGIN]);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.0.0' });
    // The extension is now built for another plugin release: that is said, not left to be found out.
    expect(result.warnings.join('\n')).toContain('The installed extensions (Tilemap) are left as they are');
  });

  it('one failed lookup is a warning and does not stop the others', async () => {
    const root = project({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.0.0', [TIMELINE]: '2.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0', [TIMELINE]: { '2.1.1': '1.8.0' } });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.updated.map((item) => item.packageId)).toEqual([TIMELINE]);
    expect(result.items.find((item) => item.packageId === TILEMAP)?.status).toBe('unknown');
    expect(result.warnings.join('\n')).toContain('Tilemap');
  });

  it('an extension whose versions are unknown holds a plugin update back', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0' });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.status)).toEqual(['held', 'unknown']);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0' });
  });

  it('compares prereleases by their numbers', async () => {
    // A local plugin names no release, so its extensions compare with their latest versions.
    const root = project({ [PLUGIN]: 'file:../plugin', [TILEMAP]: '1.2.0-rc1', [TIMELINE]: '1.10.0' });
    stubOpenUpm({ [TILEMAP]: '1.2.0', [TIMELINE]: '1.9.0-rc1' });

    const result = await updateProject({ unityProjectPath: root, dryRun: true });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.status)).toEqual(['local', 'outdated', 'current']);

    const prerelease = project({ [PLUGIN]: '0.9.0-beta' });
    stubOpenUpm({ [PLUGIN]: '0.10.0' });
    const plugin = await updateProject({ unityProjectPath: prerelease, dryRun: true });
    if (plugin.kind !== 'success') throw plugin.error;
    expect(plugin.items.map((item) => item.status)).toEqual(['outdated']);
  });

  it('fails without throwing outside a Unity project', async () => {
    const empty = makeTempDir('unity-update-');

    const result = await updateProject({ unityProjectPath: empty });

    expect(result.kind).toBe('failure');
  });
});

describe('findUnityProjectRoot', () => {
  it('finds the project from a subfolder, and null outside one', () => {
    const root = project({});
    const nested = path.join(root, 'Assets', 'Scripts');
    fs.mkdirSync(nested, { recursive: true });

    expect(findUnityProjectRoot(nested)).toBe(root);
    expect(findUnityProjectRoot(os.tmpdir())).toBeNull();
  });
});
