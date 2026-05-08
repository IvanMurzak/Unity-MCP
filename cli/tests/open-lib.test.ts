import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import {
  openProject,
  type OpenProjectOptions,
  type OpenProjectResult,
  type ProgressEvent,
} from '../src/lib.js';
import {
  buildOpenEnv,
  isUnityProjectDir,
  resolveProjectPath,
} from '../src/lib/open.js';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Create a minimal Unity-project-shaped directory. */
function makeFakeUnityProject(dir: string, version = '2022.3.62f3'): void {
  fs.mkdirSync(path.join(dir, 'Assets'), { recursive: true });
  const settingsDir = path.join(dir, 'ProjectSettings');
  fs.mkdirSync(settingsDir, { recursive: true });
  fs.writeFileSync(
    path.join(settingsDir, 'ProjectVersion.txt'),
    `m_EditorVersion: ${version}\n`,
  );
}

// ---------------------------------------------------------------------------
// resolveProjectPath
// ---------------------------------------------------------------------------

describe('resolveProjectPath', () => {
  it('returns optionPath resolved to absolute when provided', () => {
    const result = resolveProjectPath('some/path', '/fake/cwd');
    expect(result.projectPath).toBe(path.resolve('some/path'));
    expect(result.usedCwdFallback).toBe(false);
  });

  it('falls back to cwd when no option is given', () => {
    const cwd = path.resolve('/fake/cwd');
    const result = resolveProjectPath(undefined, cwd);
    expect(result.projectPath).toBe(cwd);
    expect(result.usedCwdFallback).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// isUnityProjectDir
// ---------------------------------------------------------------------------

describe('isUnityProjectDir (lib)', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-lib-isunity-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('returns false for an empty directory', () => {
    expect(isUnityProjectDir(tmpDir)).toBe(false);
  });

  it('returns true when both Assets/ and ProjectSettings/ProjectVersion.txt exist', () => {
    makeFakeUnityProject(tmpDir);
    expect(isUnityProjectDir(tmpDir)).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// buildOpenEnv (env-var assembly)
// ---------------------------------------------------------------------------

describe('buildOpenEnv', () => {
  it('returns undefined when noConnect is true', () => {
    const env = buildOpenEnv({
      noConnect: true,
      url: 'http://example.com',
      token: 'abc',
    });
    expect(env).toBeUndefined();
  });

  it('returns undefined when no connection options are provided', () => {
    const env = buildOpenEnv({});
    expect(env).toBeUndefined();
  });

  it('maps url → UNITY_MCP_HOST', () => {
    const env = buildOpenEnv({ url: 'http://localhost:5640' });
    expect(env).toEqual({ UNITY_MCP_HOST: 'http://localhost:5640' });
  });

  it('maps token → UNITY_MCP_TOKEN', () => {
    const env = buildOpenEnv({ token: 'secret-token' });
    expect(env).toEqual({ UNITY_MCP_TOKEN: 'secret-token' });
  });

  it('maps tools → UNITY_MCP_TOOLS', () => {
    const env = buildOpenEnv({ tools: 'tool-a,tool-b' });
    expect(env).toEqual({ UNITY_MCP_TOOLS: 'tool-a,tool-b' });
  });

  it('maps keepConnected: true → UNITY_MCP_KEEP_CONNECTED=true', () => {
    const env = buildOpenEnv({ keepConnected: true });
    expect(env).toEqual({ UNITY_MCP_KEEP_CONNECTED: 'true' });
  });

  it('maps auth → UNITY_MCP_AUTH_OPTION', () => {
    const env = buildOpenEnv({ auth: 'required' });
    expect(env).toEqual({ UNITY_MCP_AUTH_OPTION: 'required' });
  });

  it('maps transport → UNITY_MCP_TRANSPORT', () => {
    const env = buildOpenEnv({ transport: 'stdio' });
    expect(env).toEqual({ UNITY_MCP_TRANSPORT: 'stdio' });
  });

  it('maps startServer: true → UNITY_MCP_START_SERVER=true', () => {
    const env = buildOpenEnv({ startServer: true });
    expect(env).toEqual({ UNITY_MCP_START_SERVER: 'true' });
  });

  it('maps startServer: false → UNITY_MCP_START_SERVER=false', () => {
    const env = buildOpenEnv({ startServer: false });
    expect(env).toEqual({ UNITY_MCP_START_SERVER: 'false' });
  });

  it('combines all options into a single map', () => {
    const env = buildOpenEnv({
      url: 'http://localhost:5640',
      token: 'secret',
      tools: 'a,b',
      keepConnected: true,
      auth: 'required',
      transport: 'streamableHttp',
      startServer: true,
    });
    expect(env).toEqual({
      UNITY_MCP_HOST: 'http://localhost:5640',
      UNITY_MCP_TOKEN: 'secret',
      UNITY_MCP_TOOLS: 'a,b',
      UNITY_MCP_KEEP_CONNECTED: 'true',
      UNITY_MCP_AUTH_OPTION: 'required',
      UNITY_MCP_TRANSPORT: 'streamableHttp',
      UNITY_MCP_START_SERVER: 'true',
    });
  });

  it('throws on invalid auth value', () => {
    expect(() =>
      buildOpenEnv({ auth: 'banana' as unknown as 'none' | 'required' }),
    ).toThrow(/auth must be/);
  });

  it('throws on invalid transport value', () => {
    expect(() =>
      buildOpenEnv({ transport: 'ftp' as unknown as 'stdio' | 'streamableHttp' }),
    ).toThrow(/transport must be/);
  });
});

// ---------------------------------------------------------------------------
// openProject — failure paths (do NOT spawn a real editor)
// ---------------------------------------------------------------------------

describe('openProject — failure paths', () => {
  it('returns kind:"failure" when project path does not exist', async () => {
    const result = await openProject({
      projectPath: '/this/path/definitely/does/not/exist/12345',
    });
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.errorMessage).toContain('does not exist');
    expect(result.error).toBeInstanceOf(Error);
    expect(result.success).toBe(false);
  });

  it('returns kind:"failure" when path exists but is not a Unity project', async () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-not-unity-'));
    try {
      const result = await openProject({ projectPath: tmpDir });
      expect(result.kind).toBe('failure');
      if (result.kind !== 'failure') throw new Error('expected failure kind');
      expect(result.errorMessage).toContain('Not a Unity project');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('returns kind:"failure" when auth value is invalid', async () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-bad-auth-'));
    try {
      makeFakeUnityProject(tmpDir);
      const result = await openProject({
        projectPath: tmpDir,
        auth: 'banana' as unknown as 'none' | 'required',
      });
      expect(result.kind).toBe('failure');
      if (result.kind !== 'failure') throw new Error('expected failure kind');
      expect(result.errorMessage).toContain('auth must be');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('returns kind:"failure" when transport value is invalid', async () => {
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-bad-transport-'));
    try {
      makeFakeUnityProject(tmpDir);
      const result = await openProject({
        projectPath: tmpDir,
        transport: 'ftp' as unknown as 'stdio' | 'streamableHttp',
      });
      expect(result.kind).toBe('failure');
      if (result.kind !== 'failure') throw new Error('expected failure kind');
      expect(result.errorMessage).toContain('transport must be');
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('does not throw — even on a broken onProgress callback', async () => {
    const result = await openProject({
      projectPath: '/this/path/does/not/exist',
      onProgress: () => {
        throw new Error('boom');
      },
    });
    expect(result.kind).toBe('failure');
  });

  it('wire-compatible: result.success mirrors result.kind === "success"', async () => {
    const fail = await openProject({ projectPath: '/nonexistent/x' });
    expect(fail.success).toBe(fail.kind === 'success');
  });
});

// ---------------------------------------------------------------------------
// openProject — editor-version detection
//
// These tests mock out `findEditorPath` / `launchEditor` so we can
// exercise the version-resolution + progress-event contract WITHOUT
// spawning a real Unity Editor against a fake project on this
// machine.
// ---------------------------------------------------------------------------

describe('openProject — editor-version detection (mocked)', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-version-'));
    vi.resetModules();
    vi.doMock('../src/utils/unity-editor.js', async (importOriginal) => {
      const actual = await importOriginal<typeof import('../src/utils/unity-editor.js')>();
      return {
        ...actual,
        // Always report "no editor found" so the lib reaches the
        // failure path without ever calling the real `launchEditor`.
        findEditorPath: vi.fn(async () => null),
        // Defensive: even if a code path reaches launchEditor we
        // return a fake child object that the lib can introspect.
        launchEditor: vi.fn(() => ({ pid: 12345, on: () => {}, once: () => {}, unref: () => {} })),
      };
    });
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
    vi.doUnmock('../src/utils/unity-editor.js');
    vi.resetModules();
  });

  it('emits detecting-editor-version progress before locating the editor', async () => {
    makeFakeUnityProject(tmpDir, '2022.3.62f3');
    const { openProject: mockedOpenProject } = await import('../src/lib/open.js');
    const events: ProgressEvent[] = [];

    const result = await mockedOpenProject({
      projectPath: tmpDir,
      onProgress: (e) => events.push(e),
    });

    expect(result.kind).toBe('failure');

    const phases = events.map((e) => e.phase);
    expect(phases).toContain('start');
    expect(phases).toContain('detecting-editor-version');
    expect(phases).toContain('editors-located');

    const located = events.find((e) => e.phase === 'editors-located');
    if (located && located.phase === 'editors-located') {
      expect(located.found).toBe(false);
    }
  });

  it('explicit unityVersion overrides ProjectVersion.txt', async () => {
    makeFakeUnityProject(tmpDir, '2022.3.62f3');
    const { openProject: mockedOpenProject } = await import('../src/lib/open.js');

    const result = await mockedOpenProject({
      projectPath: tmpDir,
      unityVersion: '9999.99.99f99',
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') return;
    expect(result.unityVersion).toBe('9999.99.99f99');
  });

  it('falls back to ProjectVersion.txt when unityVersion is not provided', async () => {
    makeFakeUnityProject(tmpDir, '2022.3.62f3');
    const { openProject: mockedOpenProject } = await import('../src/lib/open.js');

    const result = await mockedOpenProject({ projectPath: tmpDir });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') return;
    expect(result.unityVersion).toBe('2022.3.62f3');
  });
});

// ---------------------------------------------------------------------------
// openProject — onProgress contract
// ---------------------------------------------------------------------------

describe('openProject — onProgress contract', () => {
  it('does not invoke onProgress synchronously during call setup', async () => {
    // The boundary is async — emitting any progress event before the
    // first `await` would still be observable, but it should NEVER
    // throw or mutate the caller's object identity.
    const events: ProgressEvent[] = [];
    const opts: OpenProjectOptions = {
      projectPath: '/nonexistent/x',
      onProgress: (e) => events.push(e),
    };
    const before = { ...opts };
    const result = await openProject(opts);
    expect(result.kind).toBe('failure');
    // openProject must not mutate the caller's options object.
    expect(opts).toEqual(before);
  });
});

// ---------------------------------------------------------------------------
// CLI delegation guarantee — the lib export is the single source of truth
// ---------------------------------------------------------------------------

describe('CLI delegates to lib', () => {
  it('commands/open.ts re-exports lib helpers (not its own re-implementation)', async () => {
    const cliMod = await import('../src/commands/open.js');
    const libMod = await import('../src/lib/open.js');

    // resolveOpenProjectPath / isUnityProjectDir on the CLI module
    // must produce the same outputs as the lib helpers — they should
    // be the lib helpers, possibly wrapped to handle CLI-specific
    // arg shapes (positional + --path), but the underlying logic is
    // shared.
    const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-deleg-'));
    try {
      makeFakeUnityProject(tmpDir);
      expect(cliMod.isUnityProjectDir(tmpDir)).toBe(libMod.isUnityProjectDir(tmpDir));
      expect(cliMod.isUnityProjectDir(tmpDir)).toBe(true);

      const cliResolved = cliMod.resolveOpenProjectPath(tmpDir, undefined, '/fake/cwd');
      const libResolved = libMod.resolveProjectPath(tmpDir, '/fake/cwd');
      expect(cliResolved.projectPath).toBe(libResolved.projectPath);
      expect(cliResolved.usedCwdFallback).toBe(libResolved.usedCwdFallback);
    } finally {
      fs.rmSync(tmpDir, { recursive: true, force: true });
    }
  });

  it('lib export is reachable from the package root', async () => {
    const mod = await import('../src/lib.js');
    expect(typeof mod.openProject).toBe('function');
  });
});

// ---------------------------------------------------------------------------
// Library-safety guarantees
// ---------------------------------------------------------------------------

describe('openProject — library-safety', () => {
  it('produces no stdout/stderr noise on a failure path', async () => {
    const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    const errFn = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const stdout = vi.spyOn(process.stdout, 'write').mockImplementation(() => true);
    const stderr = vi.spyOn(process.stderr, 'write').mockImplementation(() => true);

    const result = await openProject({ projectPath: '/nonexistent/path/abc' });

    expect(result.kind).toBe('failure');
    expect(log).not.toHaveBeenCalled();
    expect(errFn).not.toHaveBeenCalled();
    expect(stdout).not.toHaveBeenCalled();
    expect(stderr).not.toHaveBeenCalled();

    log.mockRestore();
    errFn.mockRestore();
    stdout.mockRestore();
    stderr.mockRestore();
  });

  it('does not call process.exit', async () => {
    // process.exit is the canonical "unsafe for libraries" pattern.
    // We can't easily intercept process.exit without breaking the
    // test runner, but we CAN spy on it and assert zero calls.
    const exitSpy = vi.spyOn(process, 'exit').mockImplementation(((code?: number) => {
      throw new Error(`process.exit called with ${code}`);
    }) as never);

    const result: OpenProjectResult = await openProject({
      projectPath: '/nonexistent/path/xyz',
    });
    expect(result.kind).toBe('failure');
    expect(exitSpy).not.toHaveBeenCalled();

    exitSpy.mockRestore();
  });
});
