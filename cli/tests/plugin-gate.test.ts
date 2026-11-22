// Copyright (c) 2025 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, afterEach, vi } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { installPlugin } from '../src/lib.js';
import { closePluginGate } from '../src/utils/manifest.js';
import { makeTempDir, removeTempDirs } from './helpers/temp-dir.js';

const PLUGIN = 'com.ivanmurzak.unity.mcp';

afterEach(() => {
  vi.unstubAllGlobals();
  removeTempDirs();
});

/** The part of ProjectSettings.asset Unity writes around the scripting defines. */
function settings(newline: string): string {
  return [
    'PlayerSettings:',
    '  productName: Game',
    '  scriptingDefineSymbols:',
    '    Android: ',
    '    Standalone: MY_GAME;UNITY_MCP_DEPS_49;UNITY_MCP_READY',
    '    WebGL: UNITY_MCP_READY',
    '    iPhone: UNITY_MCP_READY;UNITY_MCP_DEPS_49',
    '  additionalCompilerArguments: {}',
    '  note: UNITY_MCP_READY outside the defines stays',
    '',
  ].join(newline);
}

function project(dependencies: Record<string, string>, newline = '\n'): string {
  const root = makeTempDir('unity-gate-');
  fs.mkdirSync(path.join(root, 'Packages'));
  fs.mkdirSync(path.join(root, 'ProjectSettings'));
  fs.writeFileSync(path.join(root, 'Packages', 'manifest.json'), JSON.stringify({ dependencies }));
  fs.writeFileSync(path.join(root, 'ProjectSettings', 'ProjectSettings.asset'), settings(newline));
  return root;
}

const settingsOf = (root: string): string => fs.readFileSync(path.join(root, 'ProjectSettings', 'ProjectSettings.asset'), 'utf-8');

describe('closePluginGate', () => {
  it.each([['\n'], ['\r\n']])('removes only the ready define, on every platform, keeping the file as it is (%j)', (newline) => {
    const root = project({}, newline);

    expect(closePluginGate(root)).toBe(true);

    expect(settingsOf(root)).toBe(settings(newline)
      .replace('MY_GAME;UNITY_MCP_DEPS_49;UNITY_MCP_READY', 'MY_GAME;UNITY_MCP_DEPS_49')
      .replace('WebGL: UNITY_MCP_READY', 'WebGL: ')
      .replace('iPhone: UNITY_MCP_READY;UNITY_MCP_DEPS_49', 'iPhone: UNITY_MCP_DEPS_49'));
    // Nothing left to remove: the file is not written again.
    expect(closePluginGate(root)).toBe(false);
  });

  it('leaves a project without the settings file, or with a binary one, alone', () => {
    const root = makeTempDir('unity-gate-');
    expect(closePluginGate(root)).toBe(false);

    fs.mkdirSync(path.join(root, 'ProjectSettings'));
    const binary = Buffer.from([0, 1, 2, 3, 255, 254]);
    fs.writeFileSync(path.join(root, 'ProjectSettings', 'ProjectSettings.asset'), binary);
    expect(closePluginGate(root)).toBe(false);
    expect(fs.readFileSync(path.join(root, 'ProjectSettings', 'ProjectSettings.asset')).equals(binary)).toBe(true);
  });
});

describe('installPlugin and the gate', () => {
  it('closes the gate when the plugin version changes: what is built on the old plugin must not compile before the new one is ready', async () => {
    const root = project({ [PLUGIN]: '1.0.0' });

    const result = await installPlugin({ unityProjectPath: root, version: '1.8.0' });

    if (result.kind !== 'success') throw result.error;
    expect(settingsOf(root)).not.toContain('Standalone: MY_GAME;UNITY_MCP_DEPS_49;UNITY_MCP_READY');
    expect(settingsOf(root)).toContain('Standalone: MY_GAME;UNITY_MCP_DEPS_49');
  });

  it('leaves the gate open when the plugin stays at its version', async () => {
    const root = project({ [PLUGIN]: '1.8.0' });

    const result = await installPlugin({ unityProjectPath: root, version: '1.8.0' });

    if (result.kind !== 'success') throw result.error;
    expect(settingsOf(root)).toBe(settings('\n'));
  });
});
