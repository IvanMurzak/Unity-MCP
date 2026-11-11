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

/** OpenUPM stub: `latest` per package id; a package absent from the map fails its lookup. */
function stubOpenUpm(latest: Record<string, string>): void {
  vi.stubGlobal('fetch', vi.fn(async (url: string) => {
    const version = latest[String(url).split('/').pop()!];
    return version
      ? { ok: true, status: 200, json: async () => ({ 'dist-tags': { latest: version } }) }
      : { ok: false, status: 503, statusText: 'Unavailable', json: async () => ({}) };
  }));
}

describe('updateProject', () => {
  it('moves the plugin and installed extensions to latest, and installs nothing new', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0', 'com.unity.ugui': '2.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.1.1', [SPLINES]: '2.1.1' });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.updated.map((item) => item.packageId)).toEqual([PLUGIN, TILEMAP]);
    expect(dependenciesOf(root)).toMatchObject({ [PLUGIN]: '1.8.0', [TILEMAP]: '2.1.1', 'com.unity.ugui': '2.0.0' });
    expect(dependenciesOf(root)[SPLINES]).toBeUndefined();
  });

  it('dryRun reports without writing', async () => {
    const root = project({ [PLUGIN]: '1.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0' });

    const result = await updateProject({ unityProjectPath: root, dryRun: true });

    if (result.kind !== 'success') throw result.error;
    expect(result.items).toEqual([expect.objectContaining({ packageId: PLUGIN, from: '1.0.0', to: '1.8.0', status: 'outdated' })]);
    expect(result.updated).toEqual([]);
    expect(dependenciesOf(root)[PLUGIN]).toBe('1.0.0');
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
    expect(dependenciesOf(root)[TILEMAP]).toBe('2.0.0');
  });

  it('one failed lookup is a warning and does not stop the others', async () => {
    const root = project({ [PLUGIN]: '1.0.0', [TILEMAP]: '2.0.0' });
    stubOpenUpm({ [PLUGIN]: '1.8.0' });

    const result = await updateProject({ unityProjectPath: root });

    if (result.kind !== 'success') throw result.error;
    expect(result.updated.map((item) => item.packageId)).toEqual([PLUGIN]);
    expect(result.items.find((item) => item.packageId === TILEMAP)?.status).toBe('unknown');
    expect(result.warnings.join('\n')).toContain('Tilemap');
  });

  it('compares prereleases by their numbers', async () => {
    const root = project({ [PLUGIN]: '0.9.0-beta', [TILEMAP]: '1.2.0-rc1', [TIMELINE]: '1.10.0' });
    stubOpenUpm({ [PLUGIN]: '0.10.0', [TILEMAP]: '1.2.0', [TIMELINE]: '1.9.0-rc1' });

    const result = await updateProject({ unityProjectPath: root, dryRun: true });

    if (result.kind !== 'success') throw result.error;
    expect(result.items.map((item) => item.status)).toEqual(['outdated', 'outdated', 'current']);
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
