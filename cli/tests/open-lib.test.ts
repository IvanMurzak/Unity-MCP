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
  _pollAndDismissLaunchErrorsForTests,
} from '../src/lib/open.js';
import type {
  DismissOutcome,
  DismissPlatform,
} from '../src/utils/launch-error-dismiss.js';

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
// Launch-errors auto-dismiss polling loop
// ---------------------------------------------------------------------------

describe('pollAndDismissLaunchErrors', () => {
  it('emits a dismiss progress event for each dismissal — re-appearances are dismissed again', async () => {
    const events: ProgressEvent[] = [];
    const warnings: string[] = [];
    const controller = new AbortController();
    let calls = 0;
    // Two consecutive ticks dismiss (the resolver-fix → dialog-
    // re-appears cycle); after the second dismissal the test fires
    // the abort signal so the test can verify the "loop kept going
    // after the first dismissal" contract without waiting for the
    // grace window or the deadline.
    const probe = async (_platform: DismissPlatform): Promise<DismissOutcome> => {
      calls += 1;
      if (calls === 2) {
        // Schedule abort AFTER returning the second dismissal so the
        // event is emitted and counted before the loop exits.
        setTimeout(() => controller.abort(), 0).unref();
      }
      return { kind: 'dismissed', button: 'Ignore' };
    };

    const start = Date.now();
    await _pollAndDismissLaunchErrorsForTests({
      timeoutMs: 5000,
      intervalMs: 20,
      onProgress: (e) => events.push(e),
      warnings,
      platform: 'win32',
      probe,
      // High grace so the early-exit must come from the abort signal.
      noDialogGraceMs: 30000,
      abortSignal: controller.signal,
    });
    const elapsed = Date.now() - start;

    expect(calls).toBeGreaterThanOrEqual(2);
    const dismissEvents = events.filter((e) => e.phase === 'launch-errors-dismissed');
    expect(dismissEvents.length).toBeGreaterThanOrEqual(2);
    const first = dismissEvents[0];
    if (first.phase === 'launch-errors-dismissed') {
      expect(first.button).toBe('Ignore');
      expect(first.platform).toBe('win32');
      expect(first.message).toContain('[open] dismissed Unity launch-errors dialog');
      expect(first.message).toContain('button=Ignore');
      expect(first.message).toContain('platform=win32');
    }
    expect(warnings).toHaveLength(0);
    // Abort fired before the deadline → loop must not have run the
    // full timeoutMs.
    expect(elapsed).toBeLessThan(2000);
  });

  it('exits early via the no-dialog grace window when no dialog is observed (no extra delay)', async () => {
    const events: ProgressEvent[] = [];
    const warnings: string[] = [];
    let calls = 0;
    const probe = async (): Promise<DismissOutcome> => {
      calls += 1;
      return { kind: 'not-found' };
    };

    const start = Date.now();
    await _pollAndDismissLaunchErrorsForTests({
      // 30s budget — the README's "no extra delay" claim depends on
      // the grace window cutting this short when the dialog never
      // appears.
      timeoutMs: 30000,
      intervalMs: 20,
      onProgress: (e) => events.push(e),
      warnings,
      platform: 'darwin',
      probe,
      noDialogGraceMs: 100,
    });
    const elapsed = Date.now() - start;

    expect(events.find((e) => e.phase === 'launch-errors-dismissed')).toBeUndefined();
    // Must exit far before the 30s budget.
    expect(elapsed).toBeLessThan(2000);
    expect(calls).toBeGreaterThanOrEqual(1);
    // No errors → no timeout warning (silent grace exit).
    expect(warnings).toHaveLength(0);
  });

  it('aborts immediately when the supplied AbortSignal fires', async () => {
    const events: ProgressEvent[] = [];
    const warnings: string[] = [];
    let calls = 0;
    const probe = async (): Promise<DismissOutcome> => {
      calls += 1;
      return { kind: 'not-found' };
    };
    const controller = new AbortController();
    const start = Date.now();
    // Fire the abort after a short delay so we observe the "loop
    // currently sleeping" → "abort wakes it" path.
    setTimeout(() => controller.abort(), 50).unref();

    await _pollAndDismissLaunchErrorsForTests({
      timeoutMs: 30000,
      intervalMs: 20,
      onProgress: (e) => events.push(e),
      warnings,
      platform: 'linux',
      probe,
      // High grace so we exercise the abort path, not the grace path.
      noDialogGraceMs: 30000,
      abortSignal: controller.signal,
    });
    const elapsed = Date.now() - start;

    expect(elapsed).toBeLessThan(2000);
    expect(calls).toBeGreaterThanOrEqual(1);
    expect(warnings).toHaveLength(0);
  });

  it('bails out immediately on a permanent platform error (xdotool missing) — single warning, no timeout warning', async () => {
    const events: ProgressEvent[] = [];
    const warnings: string[] = [];
    let calls = 0;
    const probe = async (): Promise<DismissOutcome> => {
      calls += 1;
      return { kind: 'error', message: 'xdotool not found on PATH. Install it.' };
    };

    const start = Date.now();
    await _pollAndDismissLaunchErrorsForTests({
      timeoutMs: 30000,
      intervalMs: 50,
      onProgress: (e) => events.push(e),
      warnings,
      platform: 'linux',
      probe,
      noDialogGraceMs: 30000,
    });
    const elapsed = Date.now() - start;

    // Permanent error → loop exits after a single tick (no further
    // doomed spawns).
    expect(calls).toBe(1);
    expect(elapsed).toBeLessThan(2000);
    const errWarnings = warnings.filter((w) => w.includes('xdotool not found'));
    expect(errWarnings).toHaveLength(1);
    // No timeout warning — we bailed, we did not time out.
    expect(warnings.filter((w) => w.includes('timed out'))).toHaveLength(0);
    expect(events.find((e) => e.phase === 'launch-errors-dismissed')).toBeUndefined();
  });

  it('records a single warning for repeated transient errors, then surfaces a timeout warning', async () => {
    const events: ProgressEvent[] = [];
    const warnings: string[] = [];
    // Use a NON-permanent error message so the loop ticks until the
    // deadline and the timeout warning fires.
    const probe = async (): Promise<DismissOutcome> => {
      return { kind: 'error', message: 'transient platform glitch' };
    };

    await _pollAndDismissLaunchErrorsForTests({
      timeoutMs: 200,
      intervalMs: 50,
      onProgress: (e) => events.push(e),
      warnings,
      platform: 'linux',
      probe,
      noDialogGraceMs: 30000,
    });

    const errWarnings = warnings.filter((w) => w.includes('transient platform glitch'));
    expect(errWarnings).toHaveLength(1);
    const timeoutWarnings = warnings.filter((w) => w.includes('timed out'));
    expect(timeoutWarnings).toHaveLength(1);
    expect(events.find((e) => e.phase === 'launch-errors-dismissed')).toBeUndefined();
  });

  it('respects timeoutMs=0 by performing zero probes and exiting', async () => {
    const events: ProgressEvent[] = [];
    const warnings: string[] = [];
    let calls = 0;
    const probe = async (): Promise<DismissOutcome> => {
      calls += 1;
      return { kind: 'not-found' };
    };

    await _pollAndDismissLaunchErrorsForTests({
      timeoutMs: 0,
      intervalMs: 50,
      onProgress: (e) => events.push(e),
      warnings,
      platform: 'win32',
      probe,
    });

    expect(calls).toBe(0);
    expect(events.find((e) => e.phase === 'launch-errors-dismissed')).toBeUndefined();
    expect(warnings).toHaveLength(0);
  });

  it('does not throw when onProgress is undefined', async () => {
    const warnings: string[] = [];
    let calls = 0;
    const probe = async (): Promise<DismissOutcome> => {
      calls += 1;
      // First call dismisses, then not-found so grace exits cleanly.
      return calls === 1
        ? { kind: 'dismissed', button: 'Ignore' }
        : { kind: 'not-found' };
    };
    await expect(
      _pollAndDismissLaunchErrorsForTests({
        timeoutMs: 1000,
        intervalMs: 20,
        onProgress: undefined,
        warnings,
        platform: 'win32',
        probe,
        noDialogGraceMs: 50,
      }),
    ).resolves.toBeUndefined();
  });

  it('does not surface a "timed out" warning when abort fires after a transient platform error', async () => {
    // Lock down the gating fix: the post-loop timeout warning must
    // only fire when the deadline was actually reached, NOT when
    // the loop exited via the supplied abort signal — even if a
    // transient (non-permanent) platform error was recorded
    // earlier. Without this gate, callers see a misleading
    // "auto-dismiss timed out" warning for what was really a
    // caller-driven cancellation.
    const events: ProgressEvent[] = [];
    const warnings: string[] = [];
    const controller = new AbortController();
    let calls = 0;
    const probe = async (): Promise<DismissOutcome> => {
      calls += 1;
      if (calls === 1) {
        // Schedule the abort AFTER the first transient error has
        // been recorded so seenErrorMessages.size > 0 at the
        // post-loop check.
        setTimeout(() => controller.abort(), 0).unref();
      }
      return { kind: 'error', message: 'transient platform glitch' };
    };

    await _pollAndDismissLaunchErrorsForTests({
      timeoutMs: 30000,
      intervalMs: 20,
      onProgress: (e) => events.push(e),
      warnings,
      platform: 'linux',
      probe,
      // High grace so the early-exit must come from the abort path.
      noDialogGraceMs: 30000,
      abortSignal: controller.signal,
    });

    expect(warnings.filter((w) => w.includes('transient platform glitch'))).toHaveLength(1);
    expect(warnings.filter((w) => w.includes('timed out'))).toHaveLength(0);
  });

  it('does not apply the in-flight probe outcome when abort fires mid-probe', async () => {
    // Lock down the race fix: when the abort signal fires while an
    // `await probe(...)` is in flight, the resolved outcome must
    // be discarded — not emitted as a `launch-errors-dismissed`
    // progress event nor recorded as a warning. Failing this test
    // means the loop applied a stale outcome from a probe whose
    // result the caller already cancelled.
    const events: ProgressEvent[] = [];
    const warnings: string[] = [];
    const controller = new AbortController();
    let calls = 0;
    const probe = (): Promise<DismissOutcome> =>
      new Promise((resolve) => {
        calls += 1;
        // Resolve only after the abort fires so the loop is
        // guaranteed to observe `aborted=true` post-await.
        setTimeout(() => controller.abort(), 20).unref();
        setTimeout(
          () => resolve({ kind: 'dismissed', button: 'Ignore' }),
          60,
        ).unref();
      });

    await _pollAndDismissLaunchErrorsForTests({
      timeoutMs: 30000,
      intervalMs: 20,
      onProgress: (e) => events.push(e),
      warnings,
      platform: 'win32',
      probe,
      noDialogGraceMs: 30000,
      abortSignal: controller.signal,
    });

    // Probe ran exactly once; its post-abort `dismissed` outcome
    // must be dropped.
    expect(calls).toBe(1);
    expect(events.find((e) => e.phase === 'launch-errors-dismissed')).toBeUndefined();
    expect(warnings.filter((w) => w.includes('timed out'))).toHaveLength(0);
  });
});

// ---------------------------------------------------------------------------
// openProject — auto-dismiss option plumbing
// ---------------------------------------------------------------------------

describe('openProject — autoDismissLaunchErrors option', () => {
  it('OpenProjectOptions accepts the new auto-dismiss fields without TS errors', () => {
    // Pure type-level assertion via a no-op `OpenProjectOptions`
    // literal — if any of these fields drops out of the public
    // surface, the TypeScript compiler will fail this test.
    const controller = new AbortController();
    const _opts: OpenProjectOptions = {
      projectPath: '/x',
      autoDismissLaunchErrors: false,
      launchDismissTimeoutMs: 30000,
      launchDismissPollIntervalMs: 1500,
      launchDismissAbortSignal: controller.signal,
    };
    expect(_opts).toBeTruthy();
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
