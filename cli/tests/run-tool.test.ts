import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { execFileSync } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { fileURLToPath } from 'url';
import { resolveConnectionFromConfig, isCloudMode, type UnityConnectionConfig } from '../src/utils/config.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');

function runCli(args: string[], options?: { cwd?: string }): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 10000,
      cwd: options?.cwd,
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

// ── resolveConnectionFromConfig unit tests ────────────────────────────────────

describe('resolveConnectionFromConfig', () => {
  it('returns host and token in Custom mode', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 'Custom',
      host: 'http://localhost:55000',
      token: 'custom-secret',
      cloudServerUrl: 'https://cloud.example.com',
      cloudToken: 'cloud-secret',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.url).toBe('http://localhost:55000');
    expect(result.token).toBe('custom-secret');
  });

  it('returns cloudServerUrl with /mcp suffix and cloudToken in Cloud mode', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 'Cloud',
      host: 'http://localhost:55000',
      token: 'custom-secret',
      cloudServerUrl: 'https://cloud.example.com',
      cloudToken: 'cloud-secret',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.url).toBe('https://cloud.example.com/mcp');
    expect(result.token).toBe('cloud-secret');
  });

  it('defaults to Custom mode when connectionMode is undefined', () => {
    const config: UnityConnectionConfig = {
      host: 'http://localhost:55000',
      token: 'my-token',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.url).toBe('http://localhost:55000');
    expect(result.token).toBe('my-token');
  });

  it('returns undefined url and token when fields are missing in Custom mode', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 'Custom',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.url).toBeUndefined();
    expect(result.token).toBeUndefined();
  });

  it('returns undefined url and token when fields are missing in Cloud mode', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 'Cloud',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.url).toBeUndefined();
    expect(result.token).toBeUndefined();
  });

  it('does not cross-contaminate Custom token with Cloud token', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 'Custom',
      token: 'custom-only',
      cloudToken: 'cloud-only',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.token).toBe('custom-only');
  });

  it('does not cross-contaminate Cloud token with Custom token', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 'Cloud',
      token: 'custom-only',
      cloudToken: 'cloud-only',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.token).toBe('cloud-only');
  });

  // Legacy integer enum values (written by older Unity plugin versions)

  it('handles legacy integer 0 as Custom mode', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 0,
      host: 'http://localhost:55000',
      token: 'custom-secret',
      cloudServerUrl: 'https://cloud.example.com',
      cloudToken: 'cloud-secret',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.url).toBe('http://localhost:55000');
    expect(result.token).toBe('custom-secret');
  });

  it('handles legacy integer 1 as Cloud mode with /mcp suffix', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 1,
      host: 'http://localhost:55000',
      token: 'custom-secret',
      cloudServerUrl: 'https://cloud.example.com',
      cloudToken: 'cloud-secret',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.url).toBe('https://cloud.example.com/mcp');
    expect(result.token).toBe('cloud-secret');
  });
});

describe('isCloudMode', () => {
  it('returns true for string "Cloud"', () => {
    expect(isCloudMode({ connectionMode: 'Cloud' })).toBe(true);
  });

  it('returns true for integer 1', () => {
    expect(isCloudMode({ connectionMode: 1 })).toBe(true);
  });

  it('returns false for string "Custom"', () => {
    expect(isCloudMode({ connectionMode: 'Custom' })).toBe(false);
  });

  it('returns false for integer 0', () => {
    expect(isCloudMode({ connectionMode: 0 })).toBe(false);
  });

  it('returns false when connectionMode is undefined', () => {
    expect(isCloudMode({})).toBe(false);
  });
});

// ── run-tool CLI integration tests ────────────────────────────────────────────

describe('run-tool command', () => {
  it('shows help with --help', () => {
    const { stdout, exitCode } = runCli(['run-tool', '--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('run-tool');
    expect(stdout).toContain('--url');
    expect(stdout).toContain('--input');
    expect(stdout).toContain('--input-file');
    expect(stdout).toContain('--raw');
    expect(stdout).toContain('--path');
  });

  it('does not expose --token option', () => {
    const { stdout } = runCli(['run-tool', '--help']);
    expect(stdout).not.toContain('--token');
  });

  it('requires tool name argument', () => {
    const { exitCode, stdout } = runCli(['run-tool']);
    expect(exitCode).toBe(1);
    expect(stdout).toContain("missing required argument");
  });

  it('appears in global help', () => {
    const { stdout, exitCode } = runCli(['--help']);
    expect(exitCode).toBe(0);
    expect(stdout).toContain('run-tool');
  });

  it('validates --input is valid JSON', () => {
    const { exitCode, stdout } = runCli([
      'run-tool', 'test-tool',
      '--url', 'http://localhost:99999',
      '--input', 'not-valid-json',
    ]);
    expect(exitCode).toBe(1);
    expect(stdout).toContain('--input must be valid JSON');
  });
});

// ── run-tool config-based resolution integration tests ────────────────────────

describe('run-tool config resolution', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-run-tool-test-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  function writeProjectConfig(config: UnityConnectionConfig): void {
    const configDir = path.join(tmpDir, 'UserSettings');
    fs.mkdirSync(configDir, { recursive: true });
    fs.writeFileSync(
      path.join(configDir, 'AI-Game-Developer-Config.json'),
      JSON.stringify(config, null, 2)
    );
  }

  it('reads URL from config in Custom mode (--verbose shows config URL)', () => {
    writeProjectConfig({
      connectionMode: 'Custom',
      host: 'http://localhost:55555',
      authOption: 'none',
    });
    // The tool call will fail to connect, but verbose output shows the resolved URL
    const { stdout } = runCli([
      'run-tool', 'test-tool',
      '--path', tmpDir,
      '--verbose',
      '--raw',
    ]);
    expect(stdout).toContain('Custom mode');
    expect(stdout).toContain('http://localhost:55555');
  });

  it('reads URL from config in Cloud mode with /mcp suffix (--verbose shows config URL)', () => {
    writeProjectConfig({
      connectionMode: 'Cloud',
      host: 'http://localhost:55555',
      cloudServerUrl: 'http://localhost:55556',
      authOption: 'none',
    });
    const { stdout } = runCli([
      'run-tool', 'test-tool',
      '--path', tmpDir,
      '--verbose',
      '--raw',
    ]);
    expect(stdout).toContain('Cloud mode');
    expect(stdout).toContain('http://localhost:55556/mcp');
  });

  it('--url flag overrides config URL', () => {
    writeProjectConfig({
      connectionMode: 'Custom',
      host: 'http://localhost:55555',
    });
    const { stdout } = runCli([
      'run-tool', 'test-tool',
      '--path', tmpDir,
      '--url', 'http://localhost:9999',
      '--verbose',
      '--raw',
    ]);
    expect(stdout).toContain('--url');
    expect(stdout).toContain('http://localhost:9999');
  });

  it('falls back to deterministic port when no config exists', () => {
    // No config file written — tmpDir has no UserSettings/
    const { stdout } = runCli([
      'run-tool', 'test-tool',
      '--path', tmpDir,
      '--verbose',
      '--raw',
    ]);
    expect(stdout).toContain('deterministic port');
  });

  it('reads token from config in Custom mode (--verbose shows hasToken)', () => {
    writeProjectConfig({
      connectionMode: 'Custom',
      host: 'http://localhost:55555',
      token: 'my-secret-token',
    });
    const { stdout } = runCli([
      'run-tool', 'test-tool',
      '--path', tmpDir,
      '--verbose',
      '--raw',
    ]);
    expect(stdout).toContain('hasToken=true');
    expect(stdout).toContain('Authorization header set from config');
  });

  it('reads cloudToken from config in Cloud mode (--verbose shows hasToken)', () => {
    writeProjectConfig({
      connectionMode: 'Cloud',
      cloudServerUrl: 'https://cloud.example.com',
      cloudToken: 'cloud-token-123',
    });
    const { stdout } = runCli([
      'run-tool', 'test-tool',
      '--path', tmpDir,
      '--verbose',
      '--raw',
    ]);
    expect(stdout).toContain('hasToken=true');
    expect(stdout).toContain('Authorization header set from config');
  });

  it('does not set auth header when config has no token', () => {
    writeProjectConfig({
      connectionMode: 'Custom',
      host: 'http://localhost:55555',
    });
    const { stdout } = runCli([
      'run-tool', 'test-tool',
      '--path', tmpDir,
      '--verbose',
      '--raw',
    ]);
    expect(stdout).toContain('hasToken=false');
    expect(stdout).not.toContain('Authorization header set');
  });
});
