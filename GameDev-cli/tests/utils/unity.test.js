import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import path from 'path';
import os from 'os';

vi.mock('fs');
vi.mock('child_process');

import fs from 'fs';
import { execSync, spawn } from 'child_process';
import {
  findUnityHub,
  findUnityExecutable,
  openUnityProject,
  pollForFile,
  getConnectResultPath,
  CONNECT_RESULT_RELATIVE_PATH,
  UNITY_HUB_PATHS,
  UNITY_EDITOR_BASE_DIRS,
  UNITY_EXECUTABLE_SUBPATH,
} from '../../src/utils/unity.js';

const FAKE_PROJECT = '/fake/unity/project';

// ---------------------------------------------------------------------------
// getConnectResultPath
// ---------------------------------------------------------------------------
describe('getConnectResultPath', () => {
  it('returns Library/mcp-connect-result.json inside the project', () => {
    const result = getConnectResultPath(FAKE_PROJECT);
    expect(result).toBe(path.join(FAKE_PROJECT, CONNECT_RESULT_RELATIVE_PATH));
  });
});

// ---------------------------------------------------------------------------
// findUnityHub
// ---------------------------------------------------------------------------
describe('findUnityHub', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('returns "unityhub" when the command is in PATH', () => {
    execSync.mockImplementation(() => Buffer.from('0.0.0'));

    const result = findUnityHub();

    expect(result).toBe('unityhub');
    expect(execSync).toHaveBeenCalledWith('unityhub --version', { stdio: 'ignore' });
  });

  it('falls back to known filesystem locations when not in PATH', () => {
    execSync.mockImplementation(() => { throw new Error('not found'); });

    const platform = process.platform;
    const candidates = UNITY_HUB_PATHS[platform] ?? [];
    if (candidates.length === 0) {
      // Nothing to test on this platform
      return;
    }

    // Make the first candidate exist
    fs.existsSync.mockImplementation(p => p === candidates[0]);

    const result = findUnityHub();

    expect(result).toBe(candidates[0]);
  });

  it('returns null when Unity Hub is not found anywhere', () => {
    execSync.mockImplementation(() => { throw new Error('not found'); });
    fs.existsSync.mockReturnValue(false);

    const result = findUnityHub();

    expect(result).toBeNull();
  });
});

// ---------------------------------------------------------------------------
// findUnityExecutable
// ---------------------------------------------------------------------------
describe('findUnityExecutable', () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it('returns null when no base directories exist', () => {
    fs.existsSync.mockReturnValue(false);

    const result = findUnityExecutable(undefined);

    expect(result).toBeNull();
  });

  it('returns the exact versioned executable when version is specified', () => {
    const platform = process.platform;
    const baseDirs = UNITY_EDITOR_BASE_DIRS[platform] ?? [];
    const subpath = UNITY_EXECUTABLE_SUBPATH[platform] ?? 'Editor/Unity';

    if (baseDirs.length === 0) return;

    const baseDir = baseDirs[0];
    const version = '2022.3.62f1';
    const expectedExe = path.join(baseDir, version, subpath);

    fs.existsSync.mockImplementation(p => p === baseDir || p === expectedExe);

    const result = findUnityExecutable(version);

    expect(result).toBe(expectedExe);
  });

  it('returns null when specified version is not installed', () => {
    const platform = process.platform;
    const baseDirs = UNITY_EDITOR_BASE_DIRS[platform] ?? [];
    if (baseDirs.length === 0) return;

    const baseDir = baseDirs[0];
    fs.existsSync.mockImplementation(p => p === baseDir);
    fs.readdirSync.mockReturnValue([]);

    const result = findUnityExecutable('9999.9.9f9');

    expect(result).toBeNull();
  });

  it('returns the highest version when no version specified', () => {
    const platform = process.platform;
    const baseDirs = UNITY_EDITOR_BASE_DIRS[platform] ?? [];
    const subpath = UNITY_EXECUTABLE_SUBPATH[platform] ?? 'Editor/Unity';
    if (baseDirs.length === 0) return;

    const baseDir = baseDirs[0];
    const versions = ['2021.3.0f1', '2022.3.62f1', '2023.1.0f1'];
    const highestExe = path.join(baseDir, '2023.1.0f1', subpath);

    fs.existsSync.mockImplementation(p =>
      p === baseDir || versions.some(v => p === path.join(baseDir, v, subpath))
    );
    fs.readdirSync.mockReturnValue(versions);
    fs.statSync.mockReturnValue({ isDirectory: () => true });

    const result = findUnityExecutable(undefined);

    expect(result).toBe(highestExe);
  });
});

// ---------------------------------------------------------------------------
// openUnityProject
// ---------------------------------------------------------------------------
describe('openUnityProject', () => {
  let mockChild;

  beforeEach(() => {
    vi.resetAllMocks();
    mockChild = { pid: 12345, unref: vi.fn() };
    spawn.mockReturnValue(mockChild);
    vi.spyOn(console, 'log').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('uses explicit unityPath when provided', async () => {
    const unityExe = '/custom/Unity';
    await openUnityProject(FAKE_PROJECT, { unityPath: unityExe });

    expect(spawn).toHaveBeenCalledWith(
      unityExe,
      expect.arrayContaining(['-projectPath', FAKE_PROJECT]),
      expect.objectContaining({ detached: true })
    );
  });

  it('uses Unity Hub when found and no explicit path given', async () => {
    execSync.mockImplementation(() => Buffer.from('0.0.0')); // unityhub in PATH
    fs.existsSync.mockReturnValue(false);

    await openUnityProject(FAKE_PROJECT);

    expect(spawn).toHaveBeenCalledWith(
      'unityhub',
      expect.arrayContaining(['--headless', 'launch-project']),
      expect.any(Object)
    );
  });

  it('appends extraArgs to the Unity process arguments', async () => {
    await openUnityProject(FAKE_PROJECT, {
      unityPath: '/custom/Unity',
      extraArgs: ['-mcpServerUrl', 'http://localhost:8080'],
    });

    const [, args] = spawn.mock.calls[0];
    expect(args).toContain('-mcpServerUrl');
    expect(args).toContain('http://localhost:8080');
  });

  it('throws when no Unity installation is found', async () => {
    execSync.mockImplementation(() => { throw new Error('not found'); });
    fs.existsSync.mockReturnValue(false);

    await expect(openUnityProject(FAKE_PROJECT)).rejects.toThrow(
      /Could not find a Unity installation/
    );
  });

  it('calls unref() when detached is true (default)', async () => {
    await openUnityProject(FAKE_PROJECT, { unityPath: '/custom/Unity' });

    expect(mockChild.unref).toHaveBeenCalled();
  });

  it('does not call unref() when detached is false', async () => {
    await openUnityProject(FAKE_PROJECT, { unityPath: '/custom/Unity', detached: false });

    expect(mockChild.unref).not.toHaveBeenCalled();
  });

  it('spawns with detached: true by default', async () => {
    await openUnityProject(FAKE_PROJECT, { unityPath: '/custom/Unity' });

    const [,, spawnOpts] = spawn.mock.calls[0];
    expect(spawnOpts.detached).toBe(true);
  });

  it('spawns with detached: false when option is set', async () => {
    await openUnityProject(FAKE_PROJECT, { unityPath: '/custom/Unity', detached: false });

    const [,, spawnOpts] = spawn.mock.calls[0];
    expect(spawnOpts.detached).toBe(false);
  });

  it('returns the child process', async () => {
    const result = await openUnityProject(FAKE_PROJECT, { unityPath: '/custom/Unity' });

    expect(result).toBe(mockChild);
  });
});

// ---------------------------------------------------------------------------
// pollForFile
// ---------------------------------------------------------------------------
describe('pollForFile', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('resolves true immediately when file already exists', async () => {
    fs.existsSync.mockReturnValue(true);

    const promise = pollForFile('/some/file.json', 5000, 100);
    // Advance timers slightly to let async code run
    await vi.runAllTimersAsync();
    const result = await promise;

    expect(result).toBe(true);
  });

  it('resolves true when file appears after a few polls', async () => {
    let callCount = 0;
    fs.existsSync.mockImplementation(() => {
      callCount++;
      return callCount >= 3; // appears on 3rd check
    });

    const promise = pollForFile('/some/file.json', 5000, 100);
    await vi.runAllTimersAsync();
    const result = await promise;

    expect(result).toBe(true);
  });

  it('resolves false when timeout expires before file appears', async () => {
    fs.existsSync.mockReturnValue(false);

    const promise = pollForFile('/some/file.json', 300, 100);
    await vi.runAllTimersAsync();
    const result = await promise;

    expect(result).toBe(false);
  });
});
