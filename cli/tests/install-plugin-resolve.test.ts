// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { resolveInstallTarget, unityAdapter } from '@baizor/gamedev-cli-core';

// T5/B1: `install-plugin` no longer demands an explicit path. The command resolves
// `path? → --path? → cwd` and confirms the directory is a real Unity project via the adapter's
// marker probe (`Packages/manifest.json`); on a miss the error lists exactly what was checked.
// The command wires straight through to cli-core's `resolveInstallTarget(unityAdapter, …)`, so this
// gates the exact resolution the `install-plugin` action runs.

function unityProject(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-install-target-'));
  fs.mkdirSync(path.join(dir, 'Packages'), { recursive: true });
  fs.writeFileSync(path.join(dir, 'Packages', 'manifest.json'), JSON.stringify({ dependencies: {} }));
  return dir;
}

describe('install-plugin path resolution (T5 / B1)', () => {
  it('resolves from cwd (no path) when cwd is a Unity project', () => {
    const dir = unityProject();
    try {
      const result = resolveInstallTarget({ adapter: unityAdapter, cwd: dir });
      expect(result.kind).toBe('success');
      if (result.kind !== 'success') return;
      expect(result.source).toBe('cwd');
      expect(result.projectRoot).toBe(path.resolve(dir));
      expect(result.probe.found).toBe(true);
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });

  it('prefers an explicit positional path over cwd', () => {
    const dir = unityProject();
    const other = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-cwd-'));
    try {
      const result = resolveInstallTarget({ adapter: unityAdapter, positional: dir, cwd: other });
      expect(result.kind).toBe('success');
      if (result.kind !== 'success') return;
      expect(result.source).toBe('positional');
      expect(result.projectRoot).toBe(path.resolve(dir));
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
      fs.rmSync(other, { recursive: true, force: true });
    }
  });

  it('fails with a helpful error that lists what was checked when the dir is not a Unity project', () => {
    const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'not-unity-'));
    try {
      const result = resolveInstallTarget({ adapter: unityAdapter, cwd: dir });
      expect(result.kind).toBe('failure');
      if (result.kind !== 'failure') return;
      expect(result.error.message).toContain('Packages');
      expect(result.error.message.toLowerCase()).toContain('manifest.json');
      expect(result.error.message).toContain('unity');
    } finally {
      fs.rmSync(dir, { recursive: true, force: true });
    }
  });
});
