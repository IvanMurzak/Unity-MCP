// Copyright (c) 2025 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { extensionVersionForPlugin, PLUGIN_PACKAGE_ID, pluginRelease } from '../src/utils/manifest.js';

// The rule "which extension version is built for a plugin release" exists twice: here and in the
// plugin (ExtensionCompatibility.VersionFor). Both read the same cases, so they cannot drift apart:
// the plugin's half is Unity-MCP-Plugin/Tests~/EngineFree/ExtensionCompatibilityTests.cs.
const CASES = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..', 'tests', 'extension-version-rule.json');

interface Cases {
  plugin: string;
  registry: { versions: Record<string, { dependencies?: Record<string, string> }> };
  expected: Record<string, string | null>;
}

describe('extension version for a plugin release: the cases shared with the plugin', () => {
  const cases = JSON.parse(fs.readFileSync(CASES, 'utf-8')) as Cases;

  it('is about this plugin', () => {
    expect(cases.plugin).toBe(PLUGIN_PACKAGE_ID);
    expect(Object.keys(cases.expected).length).toBeGreaterThan(5);
  });

  it.each(Object.entries(cases.expected))('plugin %j -> %j', (plugin, expected) => {
    const release = pluginRelease(plugin);
    expect(release === null ? null : extensionVersionForPlugin(cases.registry.versions, release)).toBe(expected);
  });
});
