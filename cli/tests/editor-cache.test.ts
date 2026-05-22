/**
 * Unit tests for editor-cache.ts
 *
 * The CACHE_FILE path is captured at module-load time via os.homedir(), so
 * each test suite re-loads the module after redirecting HOME / USERPROFILE to
 * a temporary directory.  vi.resetModules() + a dynamic import inside
 * beforeEach achieves this cleanly without touching production code.
 */
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

type EditorCacheModule = typeof import('../src/utils/editor-cache.js');

/** Create a fresh temp dir, point HOME/USERPROFILE at it, reload the module. */
async function loadCacheModuleInTempDir(): Promise<{
  tmpDir: string;
  mod: EditorCacheModule;
}> {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'editor-cache-test-'));
  process.env['HOME'] = tmpDir;
  process.env['USERPROFILE'] = tmpDir;
  vi.resetModules();
  const mod = (await import('../src/utils/editor-cache.js')) as EditorCacheModule;
  return { tmpDir, mod };
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('editor-cache', () => {
  let tmpDir: string;
  let mod: EditorCacheModule;

  /** Original env values so we can restore them. */
  let origHome: string | undefined;
  let origUserProfile: string | undefined;

  beforeEach(async () => {
    origHome = process.env['HOME'];
    origUserProfile = process.env['USERPROFILE'];
    ({ tmpDir, mod } = await loadCacheModuleInTempDir());
  });

  afterEach(() => {
    // Restore env
    if (origHome === undefined) {
      delete process.env['HOME'];
    } else {
      process.env['HOME'] = origHome;
    }
    if (origUserProfile === undefined) {
      delete process.env['USERPROFILE'];
    } else {
      process.env['USERPROFILE'] = origUserProfile;
    }

    // Clean up temp dir
    fs.rmSync(tmpDir, { recursive: true, force: true });

    // Reset module registry so next test gets a clean slate
    vi.resetModules();
  });

  // -------------------------------------------------------------------------
  it('readCachedEditorPath returns null when no cache file exists', () => {
    const result = mod.readCachedEditorPath('6000.3.1f1');
    expect(result).toBeNull();
  });

  it('readCachedEditorPath returns null for undefined version when no cache file exists', () => {
    const result = mod.readCachedEditorPath(undefined);
    expect(result).toBeNull();
  });

  // -------------------------------------------------------------------------
  it('write + read round-trip preserves the path for a versioned key', () => {
    // Create a real file so existsSync passes
    const fakeBinary = path.join(tmpDir, 'Unity.exe');
    fs.writeFileSync(fakeBinary, '');

    mod.writeCachedEditorPath('6000.3.1f1', fakeBinary);
    const result = mod.readCachedEditorPath('6000.3.1f1');
    expect(result).toBe(fakeBinary);
  });

  it('write + read round-trip preserves the path for the auto (undefined) key', () => {
    const fakeBinary = path.join(tmpDir, 'Unity');
    fs.writeFileSync(fakeBinary, '');

    mod.writeCachedEditorPath(undefined, fakeBinary);
    const result = mod.readCachedEditorPath(undefined);
    expect(result).toBe(fakeBinary);
  });

  // -------------------------------------------------------------------------
  it('readCachedEditorPath evicts a stale entry and returns null', () => {
    const missingPath = path.join(tmpDir, 'does-not-exist', 'Unity.exe');

    // Write directly to the cache file — bypass writeCachedEditorPath so we
    // can store a path that doesn't exist on disk.
    const cacheFile = path.join(tmpDir, '.unity-mcp-cli-editor-cache.json');
    fs.writeFileSync(
      cacheFile,
      JSON.stringify({ '6000.3.1f1': { path: missingPath, savedAt: Date.now() } }),
      'utf-8',
    );

    const result = mod.readCachedEditorPath('6000.3.1f1');
    expect(result).toBeNull();

    // Verify the stale key was removed from the cache file
    const remaining = JSON.parse(fs.readFileSync(cacheFile, 'utf-8')) as Record<string, unknown>;
    expect(Object.keys(remaining)).not.toContain('6000.3.1f1');
  });

  // -------------------------------------------------------------------------
  it('versioned key and auto key are stored in separate slots', () => {
    const binaryV = path.join(tmpDir, 'Unity-versioned.exe');
    const binaryAuto = path.join(tmpDir, 'Unity-auto.exe');
    fs.writeFileSync(binaryV, '');
    fs.writeFileSync(binaryAuto, '');

    mod.writeCachedEditorPath('6000.3.1f1', binaryV);
    mod.writeCachedEditorPath(undefined, binaryAuto);

    // Read each slot independently — neither should clobber the other
    expect(mod.readCachedEditorPath('6000.3.1f1')).toBe(binaryV);
    expect(mod.readCachedEditorPath(undefined)).toBe(binaryAuto);
  });

  it('writing undefined key does not overwrite a versioned key', () => {
    const binaryV = path.join(tmpDir, 'Unity-versioned.exe');
    const binaryAuto = path.join(tmpDir, 'Unity-auto.exe');
    fs.writeFileSync(binaryV, '');
    fs.writeFileSync(binaryAuto, '');

    mod.writeCachedEditorPath('6000.3.1f1', binaryV);
    mod.writeCachedEditorPath(undefined, binaryAuto);  // should not touch '6000.3.1f1'

    expect(mod.readCachedEditorPath('6000.3.1f1')).toBe(binaryV);
  });

  // -------------------------------------------------------------------------
  it('clearCachedEditorPath removes a specific version entry without touching others', () => {
    const binaryA = path.join(tmpDir, 'Unity-A.exe');
    const binaryB = path.join(tmpDir, 'Unity-B.exe');
    fs.writeFileSync(binaryA, '');
    fs.writeFileSync(binaryB, '');

    mod.writeCachedEditorPath('6000.3.1f1', binaryA);
    mod.writeCachedEditorPath('2022.3.62f3', binaryB);

    mod.clearCachedEditorPath('6000.3.1f1');

    // Cleared entry is gone
    expect(mod.readCachedEditorPath('6000.3.1f1')).toBeNull();
    // Other entry is untouched
    expect(mod.readCachedEditorPath('2022.3.62f3')).toBe(binaryB);
  });

  it('clearCachedEditorPath is a no-op when the key does not exist', () => {
    // Should not throw
    expect(() => mod.clearCachedEditorPath('nonexistent.version')).not.toThrow();
  });
});
