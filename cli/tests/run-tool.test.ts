import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { execFileSync } from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { fileURLToPath } from 'url';
import {
  resolveConnectionFromConfig,
  readMachineStoreCloudToken,
  isCloudMode,
  CLOUD_SERVER_URL,
  type UnityConnectionConfig,
} from '../src/utils/config.js';
import { resolveConnection } from '../src/utils/connection.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CLI_PATH = path.resolve(__dirname, '..', 'bin', 'unity-mcp-cli.js');

function runCli(
  args: string[],
  options?: { cwd?: string; env?: NodeJS.ProcessEnv },
): { stdout: string; exitCode: number } {
  try {
    const stdout = execFileSync('node', [CLI_PATH, ...args], {
      encoding: 'utf-8',
      timeout: 10000,
      cwd: options?.cwd,
      env: options?.env,
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

/**
 * A fresh, empty `$HOME` / `%USERPROFILE%` so the CLI's shared machine credential store
 * (`<home>/.ai-game-dev/credentials.json`) resolves to a directory that holds NO credential —
 * i.e. the "not logged in" state — deterministically, regardless of whether this dev machine is
 * actually signed in. Used by the Cloud-mode integration tests.
 */
function loggedOutEnv(): { env: NodeJS.ProcessEnv; cleanup: () => void } {
  const home = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-logged-out-'));
  return {
    env: { ...process.env, HOME: home, USERPROFILE: home },
    cleanup: () => fs.rmSync(home, { recursive: true, force: true }),
  };
}

// ── resolveConnectionFromConfig unit tests ────────────────────────────────────

describe('resolveConnectionFromConfig', () => {
  it('returns host and token in Custom mode', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 'Custom',
      host: 'http://localhost:55000',
      token: 'custom-secret',
      cloudToken: 'cloud-secret',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.url).toBe('http://localhost:55000');
    expect(result.token).toBe('custom-secret');
  });

  it('returns hardcoded cloud URL and the machine-store credential in Cloud mode', () => {
    // Post-T9 the plugin no longer writes `cloudToken`; the Cloud-mode Bearer now comes from the
    // shared machine credential store, NOT the on-disk `cloudToken` (defect E / D11).
    const config: UnityConnectionConfig = {
      connectionMode: 'Cloud',
      host: 'http://localhost:55000',
      token: 'custom-secret',
      cloudToken: 'cloud-secret',
    };
    const result = resolveConnectionFromConfig(config, { readCloudToken: () => 'store-bearer-token' });
    expect(result.url).toBe(CLOUD_SERVER_URL);
    expect(result.token).toBe('store-bearer-token');
  });

  it('does NOT read cloudToken in Cloud mode (store empty ⇒ undefined token)', () => {
    // The dead `cloudToken` read is gone: even with a `cloudToken` present in the config, an empty
    // machine store resolves to no token (which the caller turns into a "not logged in" error).
    const config: UnityConnectionConfig = {
      connectionMode: 'Cloud',
      cloudToken: 'cloud-secret',
    };
    const result = resolveConnectionFromConfig(config, { readCloudToken: () => undefined });
    expect(result.url).toBe(CLOUD_SERVER_URL);
    expect(result.token).toBeUndefined();
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

  it('returns hardcoded cloud URL and undefined token when not logged in (Cloud mode)', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 'Cloud',
    };
    const result = resolveConnectionFromConfig(config, { readCloudToken: () => undefined });
    expect(result.url).toBe(CLOUD_SERVER_URL);
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

  it('ignores both config.token and config.cloudToken in Cloud mode (uses the machine store)', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 'Cloud',
      token: 'custom-only',
      cloudToken: 'cloud-only',
    };
    const result = resolveConnectionFromConfig(config, { readCloudToken: () => 'store-bearer' });
    expect(result.token).toBe('store-bearer');
  });

  // Legacy integer enum values (written by older Unity plugin versions)

  it('handles legacy integer 0 as Custom mode', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 0,
      host: 'http://localhost:55000',
      token: 'custom-secret',
      cloudToken: 'cloud-secret',
    };
    const result = resolveConnectionFromConfig(config);
    expect(result.url).toBe('http://localhost:55000');
    expect(result.token).toBe('custom-secret');
  });

  it('handles legacy integer 1 as Cloud mode with the machine-store credential', () => {
    const config: UnityConnectionConfig = {
      connectionMode: 1,
      host: 'http://localhost:55000',
      token: 'custom-secret',
      cloudToken: 'cloud-secret',
    };
    const result = resolveConnectionFromConfig(config, { readCloudToken: () => 'store-bearer-token' });
    expect(result.url).toBe(CLOUD_SERVER_URL);
    expect(result.token).toBe('store-bearer-token');
  });
});

// ── machine-store credential wiring (default, un-injected path) ───────────────

describe('readMachineStoreCloudToken', () => {
  let savedHome: string | undefined;
  let savedUserProfile: string | undefined;
  let home: string;

  beforeEach(() => {
    savedHome = process.env.HOME;
    savedUserProfile = process.env.USERPROFILE;
    // Point the shared machine store at an empty dir ⇒ deterministic "not logged in".
    home = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-store-'));
    process.env.HOME = home;
    process.env.USERPROFILE = home;
  });

  afterEach(() => {
    if (savedHome === undefined) delete process.env.HOME;
    else process.env.HOME = savedHome;
    if (savedUserProfile === undefined) delete process.env.USERPROFILE;
    else process.env.USERPROFILE = savedUserProfile;
    fs.rmSync(home, { recursive: true, force: true });
  });

  it('returns undefined when the machine store holds no credential', () => {
    expect(readMachineStoreCloudToken()).toBeUndefined();
  });

  it('is the default Cloud-mode token source for resolveConnectionFromConfig (empty store ⇒ undefined)', () => {
    // No injected readCloudToken ⇒ the resolver falls through to the real (here empty) machine store.
    const result = resolveConnectionFromConfig({ connectionMode: 'Cloud', cloudToken: 'ignored' });
    expect(result.url).toBe(CLOUD_SERVER_URL);
    expect(result.token).toBeUndefined();
  });
});

// ── resolveConnection cloud-auth-missing signal (feeds the run-tool guard) ─────

describe('resolveConnection — Cloud auth missing', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-resolve-conn-'));
    fs.mkdirSync(path.join(tmpDir, 'Assets'));
    fs.mkdirSync(path.join(tmpDir, 'UserSettings'), { recursive: true });
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  function writeCloudConfig(): void {
    fs.writeFileSync(
      path.join(tmpDir, 'UserSettings', 'AI-Game-Developer-Config.json'),
      JSON.stringify({ connectionMode: 'Cloud' }),
    );
  }

  it('flags cloudAuthMissing when Cloud mode has no machine-store credential', () => {
    writeCloudConfig();
    const result = resolveConnection(tmpDir, {}, { readCloudToken: () => undefined });
    expect(result.url).toBe(CLOUD_SERVER_URL);
    expect(result.token).toBeUndefined();
    expect(result.cloudAuthMissing).toBe(true);
  });

  it('does not flag cloudAuthMissing when the store has a credential', () => {
    writeCloudConfig();
    const result = resolveConnection(tmpDir, {}, { readCloudToken: () => 'store-bearer' });
    expect(result.token).toBe('store-bearer');
    expect(result.cloudAuthMissing).toBe(false);
  });

  it('does not flag cloudAuthMissing when --token overrides in Cloud mode', () => {
    writeCloudConfig();
    const result = resolveConnection(tmpDir, { token: 'explicit' }, { readCloudToken: () => undefined });
    expect(result.token).toBe('explicit');
    expect(result.cloudAuthMissing).toBe(false);
  });

  it('does not flag cloudAuthMissing when --url overrides the endpoint in Cloud mode', () => {
    writeCloudConfig();
    const result = resolveConnection(
      tmpDir,
      { url: 'http://localhost:9999' },
      { readCloudToken: () => undefined },
    );
    expect(result.url).toBe('http://localhost:9999');
    expect(result.cloudAuthMissing).toBe(false);
  });

  it('never flags cloudAuthMissing in Custom mode (self-host / derived-port)', () => {
    fs.writeFileSync(
      path.join(tmpDir, 'UserSettings', 'AI-Game-Developer-Config.json'),
      JSON.stringify({ connectionMode: 'Custom', host: 'http://localhost:55000' }),
    );
    const result = resolveConnection(tmpDir, {}, { readCloudToken: () => undefined });
    expect(result.cloudAuthMissing).toBe(false);
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

  it('exposes --token option', () => {
    const { stdout } = runCli(['run-tool', '--help']);
    expect(stdout).toContain('--token');
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
    fs.mkdirSync(path.join(tmpDir, 'Assets'));
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

  it('resolves the hardcoded cloud URL in Cloud mode, then refuses when not logged in', () => {
    writeProjectConfig({
      connectionMode: 'Cloud',
      host: 'http://localhost:55555',
      authOption: 'none',
    });
    const { env, cleanup } = loggedOutEnv();
    try {
      const { stdout, exitCode } = runCli(
        ['run-tool', 'test-tool', '--path', tmpDir, '--verbose', '--raw'],
        { env },
      );
      // The cloud URL still resolves (verbose shows it) ...
      expect(stdout).toContain('Cloud mode');
      expect(stdout).toContain('https://ai-game.dev/mcp');
      // ... but with no machine-store credential the call is refused, never sent unauthenticated.
      expect(stdout).toContain('Not logged in to ai-game.dev');
      expect(exitCode).toBe(1);
    } finally {
      cleanup();
    }
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
    expect(stdout).toContain('Authorization header set');
  });

  it('ignores a legacy cloudToken in Cloud mode (auth comes from the machine store)', () => {
    writeProjectConfig({
      connectionMode: 'Cloud',
      cloudToken: 'cloud-token-123',
    });
    const { env, cleanup } = loggedOutEnv();
    try {
      const { stdout, exitCode } = runCli(
        ['run-tool', 'test-tool', '--path', tmpDir, '--verbose', '--raw'],
        { env },
      );
      // The on-disk cloudToken is NOT read: with an empty store the resolver sees no token ...
      expect(stdout).toContain('hasToken=false');
      expect(stdout).not.toContain('Authorization header set');
      // ... and the run-tool command refuses to fire an unauthenticated cloud request.
      expect(stdout).toContain('Not logged in to ai-game.dev');
      expect(exitCode).toBe(1);
    } finally {
      cleanup();
    }
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
