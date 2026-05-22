// Copyright (c) 2025 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { execFileSync } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { fileURLToPath } from 'url';
import {
  pinLocalModeConfig,
  isAlreadyPinned,
} from '../src/commands/bootstrap-local.js';
import {
  readConfig,
  type UnityConnectionConfig,
} from '../src/utils/config.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');

function runCli(args: string[]): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 10000,
    });
    return { stdout, exitCode: 0 };
  } catch (err: unknown) {
    const error = err as { stdout?: string; stderr?: string; status?: number };
    return {
      stdout: (error.stdout ?? '') + (error.stderr ?? ''),
      exitCode: error.status ?? 1,
    };
  }
}

describe('bootstrap-local pure helpers', () => {
  describe('pinLocalModeConfig', () => {
    it('produces a new object (does not mutate input)', () => {
      const original: UnityConnectionConfig = {
        connectionMode: 'Cloud',
        cloudToken: 'cloud-secret',
        host: 'http://old.example.com',
      };
      const frozen = Object.freeze({ ...original });

      const next = pinLocalModeConfig(frozen, 'http://localhost:5140', 'local-token');

      expect(next).not.toBe(frozen);
      expect(next.connectionMode).toBe('Custom');
      expect(next.host).toBe('http://localhost:5140');
      expect(next.token).toBe('local-token');
      // Original untouched
      expect(frozen.connectionMode).toBe('Cloud');
      expect(frozen.host).toBe('http://old.example.com');
    });

    it('preserves unrelated keys (tools, cloudToken, logLevel, ...)', () => {
      const original: UnityConnectionConfig = {
        connectionMode: 'Cloud',
        cloudToken: 'keep-me',
        logLevel: 2,
        timeoutMs: 5000,
        tools: [{ name: 'tool-a', enabled: true }],
        prompts: [{ name: 'prompt-a', enabled: false }],
        resources: [],
        someCustomKey: 42,
      };

      const next = pinLocalModeConfig(original, 'http://localhost:5140', 'local-token');

      expect(next.cloudToken).toBe('keep-me');
      expect(next.logLevel).toBe(2);
      expect(next.timeoutMs).toBe(5000);
      expect(next.tools).toEqual([{ name: 'tool-a', enabled: true }]);
      expect(next.prompts).toEqual([{ name: 'prompt-a', enabled: false }]);
      expect(next.resources).toEqual([]);
      expect(next.someCustomKey).toBe(42);
    });

    it('normalizes connectionMode to the string "Custom" (not 0)', () => {
      const original: UnityConnectionConfig = { connectionMode: 0 };
      const next = pinLocalModeConfig(original, 'http://localhost:5140', 'tok');
      expect(next.connectionMode).toBe('Custom');
    });
  });

  describe('isAlreadyPinned', () => {
    it('returns true when mode=Custom, host=url, token=token', () => {
      const cfg: UnityConnectionConfig = {
        connectionMode: 'Custom',
        host: 'http://localhost:5140',
        token: 'tok',
      };
      expect(isAlreadyPinned(cfg, 'http://localhost:5140', 'tok')).toBe(true);
    });

    it('accepts legacy integer 0 (Custom) as already-local', () => {
      const cfg: UnityConnectionConfig = {
        connectionMode: 0,
        host: 'http://localhost:5140',
        token: 'tok',
      };
      expect(isAlreadyPinned(cfg, 'http://localhost:5140', 'tok')).toBe(true);
    });

    it('returns false when mode=Cloud', () => {
      const cfg: UnityConnectionConfig = {
        connectionMode: 'Cloud',
        host: 'http://localhost:5140',
        token: 'tok',
      };
      expect(isAlreadyPinned(cfg, 'http://localhost:5140', 'tok')).toBe(false);
    });

    it('returns false when mode=integer 1 (Cloud)', () => {
      const cfg: UnityConnectionConfig = {
        connectionMode: 1,
        host: 'http://localhost:5140',
        token: 'tok',
      };
      expect(isAlreadyPinned(cfg, 'http://localhost:5140', 'tok')).toBe(false);
    });

    it('returns false when host differs', () => {
      const cfg: UnityConnectionConfig = {
        connectionMode: 'Custom',
        host: 'http://localhost:9999',
        token: 'tok',
      };
      expect(isAlreadyPinned(cfg, 'http://localhost:5140', 'tok')).toBe(false);
    });

    it('returns false when token differs', () => {
      const cfg: UnityConnectionConfig = {
        connectionMode: 'Custom',
        host: 'http://localhost:5140',
        token: 'wrong',
      };
      expect(isAlreadyPinned(cfg, 'http://localhost:5140', 'tok')).toBe(false);
    });

    it('returns false when connectionMode is unset', () => {
      const cfg: UnityConnectionConfig = {
        host: 'http://localhost:5140',
        token: 'tok',
      };
      expect(isAlreadyPinned(cfg, 'http://localhost:5140', 'tok')).toBe(false);
    });
  });
});

describe('bootstrap-local CLI integration', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-bootstrap-test-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('shows help with --help', () => {
    const { stdout, exitCode } = runCli(['bootstrap-local', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('--url');
    expect(stdout).toContain('--token');
    expect(stdout).toContain('--path');
    expect(stdout).toContain('--dry-run');
  });

  it('fails when --url is missing', () => {
    const { stdout, exitCode } = runCli([
      'bootstrap-local',
      '--path', tmpDir,
      '--token', 'tok',
    ]);
    expect(exitCode).toBe(1);
    expect(stdout).toMatch(/required option|--url/i);
  });

  it('fails when --token is missing', () => {
    const { stdout, exitCode } = runCli([
      'bootstrap-local',
      '--path', tmpDir,
      '--url', 'http://localhost:5140',
    ]);
    expect(exitCode).toBe(1);
    expect(stdout).toMatch(/required option|--token/i);
  });

  it('creates a new config file pinned to local mode when none exists', () => {
    const { exitCode } = runCli([
      'bootstrap-local',
      '--path', tmpDir,
      '--url', 'http://localhost:5140',
      '--token', 'tok-abc',
    ]);
    expect(exitCode).toBe(0);

    const cfg = readConfig(tmpDir);
    expect(cfg).not.toBeNull();
    expect(cfg!.connectionMode).toBe('Custom');
    expect(cfg!.host).toBe('http://localhost:5140');
    expect(cfg!.token).toBe('tok-abc');
  });

  it('strips trailing slash from --url', () => {
    const { exitCode } = runCli([
      'bootstrap-local',
      '--path', tmpDir,
      '--url', 'http://localhost:5140/',
      '--token', 'tok',
    ]);
    expect(exitCode).toBe(0);
    const cfg = readConfig(tmpDir);
    expect(cfg!.host).toBe('http://localhost:5140');
  });

  it('flips a pre-existing Cloud config to Custom, preserving other keys', () => {
    // Pre-seed a Cloud-mode config with extra keys
    const preSeed: UnityConnectionConfig = {
      connectionMode: 'Cloud',
      cloudToken: 'cloud-secret',
      host: 'http://localhost:9999',
      logLevel: 2,
      tools: [{ name: 'gameobject-create', enabled: false }],
    };
    fs.mkdirSync(path.join(tmpDir, 'UserSettings'), { recursive: true });
    fs.writeFileSync(
      path.join(tmpDir, 'UserSettings', 'AI-Game-Developer-Config.json'),
      JSON.stringify(preSeed, null, 2) + '\n',
    );

    const { exitCode } = runCli([
      'bootstrap-local',
      '--path', tmpDir,
      '--url', 'http://localhost:5140',
      '--token', 'new-tok',
    ]);
    expect(exitCode).toBe(0);

    const cfg = readConfig(tmpDir);
    expect(cfg!.connectionMode).toBe('Custom');
    expect(cfg!.host).toBe('http://localhost:5140');
    expect(cfg!.token).toBe('new-tok');
    // Preserved:
    expect(cfg!.cloudToken).toBe('cloud-secret');
    expect(cfg!.logLevel).toBe(2);
    expect(cfg!.tools).toEqual([{ name: 'gameobject-create', enabled: false }]);
  });

  it('is idempotent — second run with matching url/token reports no-op', () => {
    const args = [
      'bootstrap-local',
      '--path', tmpDir,
      '--url', 'http://localhost:5140',
      '--token', 'tok',
    ];
    const first = runCli(args);
    expect(first.exitCode).toBe(0);
    expect(first.stdout).toMatch(/created|updated/i);

    // Capture mtime after first run
    const configPath = path.join(tmpDir, 'UserSettings', 'AI-Game-Developer-Config.json');
    const mtime1 = fs.statSync(configPath).mtimeMs;

    // Wait a tick so any rewrite would show a different mtime
    const second = runCli(args);
    expect(second.exitCode).toBe(0);
    expect(second.stdout).toMatch(/already pinned|no changes/i);

    const mtime2 = fs.statSync(configPath).mtimeMs;
    expect(mtime2).toBe(mtime1); // file was NOT rewritten
  });

  it('--dry-run does not create the file when none exists', () => {
    const { exitCode, stdout } = runCli([
      'bootstrap-local',
      '--path', tmpDir,
      '--url', 'http://localhost:5140',
      '--token', 'tok',
      '--dry-run',
    ]);
    expect(exitCode).toBe(0);
    expect(stdout).toMatch(/dry run|no changes written/i);

    const configPath = path.join(tmpDir, 'UserSettings', 'AI-Game-Developer-Config.json');
    expect(fs.existsSync(configPath)).toBe(false);
  });

  it('--dry-run does not modify an existing file', () => {
    const preSeed: UnityConnectionConfig = {
      connectionMode: 'Cloud',
      cloudToken: 'cloud-secret',
    };
    fs.mkdirSync(path.join(tmpDir, 'UserSettings'), { recursive: true });
    const configPath = path.join(tmpDir, 'UserSettings', 'AI-Game-Developer-Config.json');
    fs.writeFileSync(configPath, JSON.stringify(preSeed, null, 2) + '\n');
    const before = fs.readFileSync(configPath, 'utf-8');

    const { exitCode } = runCli([
      'bootstrap-local',
      '--path', tmpDir,
      '--url', 'http://localhost:5140',
      '--token', 'tok',
      '--dry-run',
    ]);
    expect(exitCode).toBe(0);

    const after = fs.readFileSync(configPath, 'utf-8');
    expect(after).toBe(before);
  });

  it('fails when project path does not exist', () => {
    const { exitCode, stdout } = runCli([
      'bootstrap-local',
      '--path', path.join(tmpDir, 'does-not-exist-xyz'),
      '--url', 'http://localhost:5140',
      '--token', 'tok',
    ]);
    expect(exitCode).toBe(1);
    expect(stdout).toMatch(/does not exist/i);
  });
});

describe('bootstrap-local help discoverability', () => {
  it('bootstrap-local appears in the global --help listing', () => {
    const { stdout, exitCode } = runCli(['--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('bootstrap-local');
  });
});
