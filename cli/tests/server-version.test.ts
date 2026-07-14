// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import { DEFAULT_SERVER_VERSION, serverReleaseTag } from '../src/utils/server-version.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// The plugin's pinned ServerVersion constant. The CLI default MUST match it byte-for-byte so a
// CLI-downloaded server can never drift from what the plugin downloads.
const MCP_SERVER_MANAGER_CS = path.resolve(
  __dirname,
  '..',
  '..',
  'Unity-MCP-Plugin',
  'Packages',
  'com.ivanmurzak.unity.mcp',
  'Editor',
  'Scripts',
  'McpServerManager.cs',
);

describe('DEFAULT_SERVER_VERSION drift guard', () => {
  it('is a valid semver-ish version string', () => {
    expect(DEFAULT_SERVER_VERSION).toMatch(/^\d+\.\d+\.\d+/);
  });

  it('matches the plugin ServerVersion constant in McpServerManager.cs', () => {
    // Only asserted in-repo (the published npm package ships no plugin sources).
    if (!fs.existsSync(MCP_SERVER_MANAGER_CS)) {
      expect(DEFAULT_SERVER_VERSION).toMatch(/^\d+\.\d+\.\d+/);
      return;
    }
    const source = fs.readFileSync(MCP_SERVER_MANAGER_CS, 'utf-8');
    const match = source.match(/public const string ServerVersion\s*=\s*"([^"]+)"/);
    expect(match, 'ServerVersion constant not found in McpServerManager.cs').not.toBeNull();
    expect(DEFAULT_SERVER_VERSION).toBe(match![1]);
  });
});

describe('serverReleaseTag', () => {
  it('adds a leading v to a bare version', () => {
    expect(serverReleaseTag('9.0.0')).toBe('v9.0.0');
  });
  it('passes a v-prefixed version through unchanged (no double prefix)', () => {
    expect(serverReleaseTag('v9.0.0')).toBe('v9.0.0');
  });
  it('trims surrounding whitespace', () => {
    expect(serverReleaseTag('  9.0.0 ')).toBe('v9.0.0');
  });
});
