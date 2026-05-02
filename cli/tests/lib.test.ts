import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import type { ProgressEvent, InstallResult } from '../src/lib.js';
import {
  installPlugin,
  removePlugin,
  configure,
  setupMcp,
  listAgentIds,
} from '../src/lib.js';

const PACKAGE_ID = 'com.ivanmurzak.unity.mcp';
const TEST_VERSION = '0.51.6';

// ---------------------------------------------------------------------------
// Utilities
// ---------------------------------------------------------------------------

function mkUnityProject(): string {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-lib-test-'));
  fs.mkdirSync(path.join(tmpDir, 'Packages'), { recursive: true });
  fs.writeFileSync(
    path.join(tmpDir, 'Packages', 'manifest.json'),
    JSON.stringify({ dependencies: {} }, null, 2),
  );
  return tmpDir;
}

function mkEmptyDir(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-lib-empty-'));
}

// ---------------------------------------------------------------------------
// No-side-effect guarantee of the library entry
// ---------------------------------------------------------------------------

describe('library entry — no top-level side effects', () => {
  it('importing the library does not write to stdout or stderr', async () => {
    // Install spies BEFORE the module is evaluated, then force a fresh
    // evaluation via `vi.resetModules()` + dynamic `import()`. Without
    // the reset the top-of-file static import would already have been
    // cached and the spies would be installed too late to observe any
    // initial side effects.
    const stdout = vi.spyOn(process.stdout, 'write').mockImplementation(() => true);
    const stderr = vi.spyOn(process.stderr, 'write').mockImplementation(() => true);
    const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    const errFn = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    vi.resetModules();
    const mod = await import('../src/lib.js');
    expect(typeof mod.installPlugin).toBe('function');
    expect(typeof mod.removePlugin).toBe('function');
    expect(typeof mod.configure).toBe('function');
    expect(typeof mod.setupMcp).toBe('function');

    // None of the above — the fresh module evaluation nor the property
    // reads — should have produced any output.
    expect(stdout).not.toHaveBeenCalled();
    expect(stderr).not.toHaveBeenCalled();
    expect(log).not.toHaveBeenCalled();
    expect(errFn).not.toHaveBeenCalled();

    stdout.mockRestore();
    stderr.mockRestore();
    log.mockRestore();
    errFn.mockRestore();
  });

  it('listAgentIds is a pure function with no console output', () => {
    const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    const err = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    const ids = listAgentIds();
    expect(Array.isArray(ids)).toBe(true);
    expect(ids.length).toBeGreaterThan(0);

    expect(log).not.toHaveBeenCalled();
    expect(err).not.toHaveBeenCalled();

    log.mockRestore();
    err.mockRestore();
  });
});

// ---------------------------------------------------------------------------
// installPlugin
// ---------------------------------------------------------------------------

describe('installPlugin', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = mkUnityProject();
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('installs a specific version into a fresh manifest', async () => {
    const result = await installPlugin({
      unityProjectPath: tmpDir,
      version: TEST_VERSION,
    });

    expect(result.kind).toBe('success');
    expect(result.success).toBe(true);
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.installedVersion).toBe(TEST_VERSION);
    expect(result.warnings).toEqual([]);
    expect(result.nextSteps.length).toBeGreaterThan(0);
    expect(result.manifestPath).toContain('manifest.json');

    const manifest = JSON.parse(
      fs.readFileSync(path.join(tmpDir, 'Packages', 'manifest.json'), 'utf-8'),
    );
    expect(manifest.dependencies[PACKAGE_ID]).toBe(TEST_VERSION);
    expect(manifest.scopedRegistries[0].name).toBe('package.openupm.com');
  });

  it('returns kind:"failure" with an Error when unityProjectPath is missing', async () => {
    const result = await installPlugin({ unityProjectPath: '' });
    expect(result.kind).toBe('failure');
    expect(result.success).toBe(false);
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.error).toBeInstanceOf(Error);
    expect(result.error.message).toContain('unityProjectPath is required');
  });

  it('returns kind:"failure" when manifest.json is missing', async () => {
    const emptyDir = mkEmptyDir();
    try {
      const result = await installPlugin({
        unityProjectPath: emptyDir,
        version: TEST_VERSION,
      });
      expect(result.kind).toBe('failure');
      expect(result.success).toBe(false);
      if (result.kind !== 'failure') throw new Error('expected failure kind');
      expect(result.error.message).toContain('Not a valid Unity project');
    } finally {
      fs.rmSync(emptyDir, { recursive: true, force: true });
    }
  });

  it('emits progress events in the expected phases', async () => {
    const events: ProgressEvent[] = [];
    const result = await installPlugin({
      unityProjectPath: tmpDir,
      version: TEST_VERSION,
      onProgress: (e) => {
        events.push(e);
      },
    });

    expect(result.kind).toBe('success');

    const phases = events.map((e) => e.phase);
    expect(phases).toContain('start');
    expect(phases).toContain('manifest-patched');
    expect(phases).toContain('done');
    // dependencies-resolved is only emitted when version is auto-resolved
    expect(phases).not.toContain('dependencies-resolved');
  });

  it('explicit-version install is idempotent when the manifest already matches', async () => {
    // Auto-resolve downgrade-skip (`force=false` path) requires
    // stubbing the OpenUPM network call, so we cover the related
    // idempotent behaviour of the explicit-version path instead:
    // installing the exact version that's already pinned is a no-op
    // and does NOT emit a warning.
    fs.writeFileSync(
      path.join(tmpDir, 'Packages', 'manifest.json'),
      JSON.stringify({ dependencies: { [PACKAGE_ID]: '99.0.0' } }, null, 2),
    );

    const result: InstallResult = await installPlugin({
      unityProjectPath: tmpDir,
      version: '99.0.0',
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.installedVersion).toBe('99.0.0');
    expect(result.warnings).toEqual([]);
  });

  it('never throws — a broken onProgress callback does not abort', async () => {
    const result = await installPlugin({
      unityProjectPath: tmpDir,
      version: TEST_VERSION,
      onProgress: () => {
        throw new Error('boom');
      },
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.installedVersion).toBe(TEST_VERSION);
  });

  it('produces no stdout / stderr noise while running', async () => {
    const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    const errFn = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const stdout = vi.spyOn(process.stdout, 'write').mockImplementation(() => true);
    const stderr = vi.spyOn(process.stderr, 'write').mockImplementation(() => true);

    const result = await installPlugin({
      unityProjectPath: tmpDir,
      version: TEST_VERSION,
    });

    expect(result.kind).toBe('success');
    expect(log).not.toHaveBeenCalled();
    expect(errFn).not.toHaveBeenCalled();
    expect(stdout).not.toHaveBeenCalled();
    expect(stderr).not.toHaveBeenCalled();

    log.mockRestore();
    errFn.mockRestore();
    stdout.mockRestore();
    stderr.mockRestore();
  });

  it('wire-compatible: result.success mirrors result.kind === "success"', async () => {
    const ok = await installPlugin({ unityProjectPath: tmpDir, version: TEST_VERSION });
    expect(ok.success).toBe(ok.kind === 'success');

    const fail = await installPlugin({ unityProjectPath: '' });
    expect(fail.success).toBe(fail.kind === 'success');
  });
});

// ---------------------------------------------------------------------------
// removePlugin
// ---------------------------------------------------------------------------

describe('removePlugin', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = mkUnityProject();
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('removes an installed plugin', async () => {
    fs.writeFileSync(
      path.join(tmpDir, 'Packages', 'manifest.json'),
      JSON.stringify(
        {
          dependencies: {
            'com.unity.ugui': '1.0.0',
            [PACKAGE_ID]: TEST_VERSION,
          },
        },
        null,
        2,
      ),
    );

    const result = await removePlugin({ unityProjectPath: tmpDir });
    expect(result.kind).toBe('success');
    expect(result.success).toBe(true);
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.removed).toBe(true);
    expect(result.warnings).toEqual([]);

    const manifest = JSON.parse(
      fs.readFileSync(path.join(tmpDir, 'Packages', 'manifest.json'), 'utf-8'),
    );
    expect(manifest.dependencies[PACKAGE_ID]).toBeUndefined();
    expect(manifest.dependencies['com.unity.ugui']).toBe('1.0.0');
  });

  it('returns kind:"success" with removed=false when plugin is not installed', async () => {
    const result = await removePlugin({ unityProjectPath: tmpDir });
    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.removed).toBe(false);
    expect(result.warnings.length).toBeGreaterThan(0);
  });

  it('returns kind:"failure" with an Error when manifest is missing', async () => {
    const emptyDir = mkEmptyDir();
    try {
      const result = await removePlugin({ unityProjectPath: emptyDir });
      expect(result.kind).toBe('failure');
      if (result.kind !== 'failure') throw new Error('expected failure kind');
      expect(result.error.message).toContain('Not a valid Unity project');
    } finally {
      fs.rmSync(emptyDir, { recursive: true, force: true });
    }
  });

  it('validates unityProjectPath', async () => {
    const result = await removePlugin({ unityProjectPath: '' });
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.error.message).toContain('unityProjectPath is required');
  });
});

// ---------------------------------------------------------------------------
// configure
// ---------------------------------------------------------------------------

describe('configure', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-lib-cfg-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('creates a default config when none exists and returns a snapshot', async () => {
    const result = await configure({ unityProjectPath: tmpDir });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.configPath).toContain('AI-Game-Developer-Config.json');
    expect(result.snapshot.host).toContain('localhost');
    expect(result.snapshot.tools).toEqual([]);
    expect(result.snapshot.prompts).toEqual([]);
    expect(result.snapshot.resources).toEqual([]);
  });

  it('enables specific tools', async () => {
    const result = await configure({
      unityProjectPath: tmpDir,
      tools: { enableNames: ['tool-a', 'tool-b'] },
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    const tools = result.snapshot.tools;
    expect(tools).toContainEqual({ name: 'tool-a', enabled: true });
    expect(tools).toContainEqual({ name: 'tool-b', enabled: true });
  });

  it('disables a previously-enabled tool', async () => {
    await configure({
      unityProjectPath: tmpDir,
      tools: { enableNames: ['tool-a', 'tool-b'] },
    });
    const result = await configure({
      unityProjectPath: tmpDir,
      tools: { disableNames: ['tool-a'] },
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    const tools = result.snapshot.tools;
    expect(tools).toContainEqual({ name: 'tool-a', enabled: false });
    expect(tools).toContainEqual({ name: 'tool-b', enabled: true });
  });

  it('returns kind:"failure" when the project path does not exist', async () => {
    const result = await configure({
      unityProjectPath: path.join(tmpDir, 'does-not-exist'),
    });
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.error.message).toContain('does not exist');
  });

  it('validates unityProjectPath', async () => {
    const result = await configure({ unityProjectPath: '' });
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.error.message).toContain('unityProjectPath is required');
  });

  it('supports prompts and resources independently', async () => {
    const result = await configure({
      unityProjectPath: tmpDir,
      prompts: { enableNames: ['p1'] },
      resources: { disableNames: ['r1'] },
    });
    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.snapshot.prompts).toContainEqual({ name: 'p1', enabled: true });
    expect(result.snapshot.resources).toContainEqual({ name: 'r1', enabled: false });
    expect(result.snapshot.tools).toEqual([]);
  });
});

// ---------------------------------------------------------------------------
// setupMcp
// ---------------------------------------------------------------------------

describe('setupMcp', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-lib-setup-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('writes config for a known agent (http)', async () => {
    const ids = listAgentIds();
    expect(ids.length).toBeGreaterThan(0);
    const agentId = ids.includes('claude-code') ? 'claude-code' : ids[0];

    const result = await setupMcp({
      agentId,
      unityProjectPath: tmpDir,
      transport: 'http',
    });

    expect(result.kind).toBe('success');
    if (result.kind !== 'success') throw new Error('expected success kind');
    expect(result.agentId).toBe(agentId);
    expect(result.transport).toBe('http');
    expect(result.configPath).toBeDefined();
    expect(fs.existsSync(result.configPath)).toBe(true);
  });

  it('fails with a descriptive error when the agent is unknown', async () => {
    const result = await setupMcp({
      agentId: 'totally-not-a-real-agent-id',
      unityProjectPath: tmpDir,
    });

    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.error.message).toContain('Unknown agent');
  });

  it('fails when agentId is empty', async () => {
    const result = await setupMcp({
      agentId: '',
      unityProjectPath: tmpDir,
    });
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.error.message).toContain('agentId is required');
  });

  it('fails when the supplied project path does not exist', async () => {
    const result = await setupMcp({
      agentId: listAgentIds()[0],
      unityProjectPath: path.join(tmpDir, 'does-not-exist'),
    });
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.error.message).toContain('does not exist');
  });

  it('rejects an invalid transport', async () => {
    const result = await setupMcp({
      agentId: listAgentIds()[0],
      unityProjectPath: tmpDir,
      transport: 'ftp' as unknown as 'stdio' | 'http',
    });
    expect(result.kind).toBe('failure');
    if (result.kind !== 'failure') throw new Error('expected failure kind');
    expect(result.error.message).toContain('Invalid transport');
  });
});
