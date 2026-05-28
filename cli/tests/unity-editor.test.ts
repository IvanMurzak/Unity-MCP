import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { fileURLToPath } from 'url';
import {
  getProjectEditorVersion,
  getSecondaryInstallPathFile,
  readSecondaryInstallPaths,
  resolveEditorPath,
} from '../src/utils/unity-editor.js';

describe('resolveEditorPath', () => {
  describe('darwin', () => {
    it('resolves .app bundle path to Contents/MacOS/Unity', () => {
      const result = resolveEditorPath('/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app', 'darwin');
      expect(result).toBe('/Applications/Unity/Hub/Editor/6000.3.11f1/Unity.app/Contents/MacOS/Unity');
    });

    it('resolves install root (non-.app) to Unity.app/Contents/MacOS/Unity', () => {
      const result = resolveEditorPath('/Applications/Unity/Hub/Editor/2022.3.62f3', 'darwin');
      expect(result).toBe('/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity');
    });

    it('returns as-is when path basename is unity (executable)', () => {
      const result = resolveEditorPath('/some/path/Unity.app/Contents/MacOS/Unity', 'darwin');
      expect(result).toBe('/some/path/Unity.app/Contents/MacOS/Unity');
    });
  });

  describe('win32', () => {
    it('resolves install root to Editor/Unity.exe', () => {
      const result = resolveEditorPath('/Unity/Hub/Editor/2022.3.62f3', 'win32');
      expect(result).toBe(path.join('/Unity/Hub/Editor/2022.3.62f3', 'Editor', 'Unity.exe'));
    });

    it('returns as-is when path basename is unity.exe', () => {
      const result = resolveEditorPath('/Unity/Editor/Unity.exe', 'win32');
      expect(result).toBe('/Unity/Editor/Unity.exe');
    });
  });

  describe('linux', () => {
    it('resolves install root to Editor/Unity', () => {
      const result = resolveEditorPath('/opt/unity/editors/2022.3.62f3', 'linux');
      expect(result).toBe('/opt/unity/editors/2022.3.62f3/Editor/Unity');
    });

    it('returns as-is when path basename is unity (executable)', () => {
      const result = resolveEditorPath('/opt/unity/editors/2022.3.62f3/Editor/Unity', 'linux');
      expect(result).toBe('/opt/unity/editors/2022.3.62f3/Editor/Unity');
    });
  });
});

describe('getProjectEditorVersion', () => {
  let tmpDir: string;

  beforeEach(() => {
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-mcp-editor-test-'));
  });

  afterEach(() => {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  it('returns null when ProjectSettings directory does not exist', () => {
    const result = getProjectEditorVersion(tmpDir);
    expect(result).toBeNull();
  });

  it('returns null when ProjectVersion.txt does not exist', () => {
    fs.mkdirSync(path.join(tmpDir, 'ProjectSettings'), { recursive: true });
    const result = getProjectEditorVersion(tmpDir);
    expect(result).toBeNull();
  });

  it('parses version from standard ProjectVersion.txt', () => {
    const projectSettingsDir = path.join(tmpDir, 'ProjectSettings');
    fs.mkdirSync(projectSettingsDir, { recursive: true });
    fs.writeFileSync(
      path.join(projectSettingsDir, 'ProjectVersion.txt'),
      'm_EditorVersion: 2022.3.62f3\nm_EditorVersionWithRevision: 2022.3.62f3 (96770f904ca7)\n'
    );
    const result = getProjectEditorVersion(tmpDir);
    expect(result).toBe('2022.3.62f3');
  });

  it('parses Unity 6 version format', () => {
    const projectSettingsDir = path.join(tmpDir, 'ProjectSettings');
    fs.mkdirSync(projectSettingsDir, { recursive: true });
    fs.writeFileSync(
      path.join(projectSettingsDir, 'ProjectVersion.txt'),
      'm_EditorVersion: 6000.3.1f1\n'
    );
    const result = getProjectEditorVersion(tmpDir);
    expect(result).toBe('6000.3.1f1');
  });

  it('trims whitespace from version', () => {
    const projectSettingsDir = path.join(tmpDir, 'ProjectSettings');
    fs.mkdirSync(projectSettingsDir, { recursive: true });
    fs.writeFileSync(
      path.join(projectSettingsDir, 'ProjectVersion.txt'),
      'm_EditorVersion:   2023.2.22f1  \n'
    );
    const result = getProjectEditorVersion(tmpDir);
    expect(result).toBe('2023.2.22f1');
  });

  it('returns null for malformed file without m_EditorVersion', () => {
    const projectSettingsDir = path.join(tmpDir, 'ProjectSettings');
    fs.mkdirSync(projectSettingsDir, { recursive: true });
    fs.writeFileSync(
      path.join(projectSettingsDir, 'ProjectVersion.txt'),
      'some random content\n'
    );
    const result = getProjectEditorVersion(tmpDir);
    expect(result).toBeNull();
  });
});

// ---------------------------------------------------------------------------
// findEditorPath — cache-hit integration
// ---------------------------------------------------------------------------
// The CACHE_FILE path in editor-cache.ts is captured at module load time, so
// we must redirect HOME/USERPROFILE and then reload the whole module graph
// (editor-cache + unity-editor) via vi.resetModules() + dynamic import.
// Unity Hub helpers are mocked so we can assert they are never invoked on a
// cache hit.
// ---------------------------------------------------------------------------
describe('findEditorPath (cache-hit integration)', () => {
  let tmpDir: string;
  let origHome: string | undefined;
  let origUserProfile: string | undefined;

  beforeEach(() => {
    origHome = process.env['HOME'];
    origUserProfile = process.env['USERPROFILE'];
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-editor-cache-hit-'));
    process.env['HOME'] = tmpDir;
    process.env['USERPROFILE'] = tmpDir;
  });

  afterEach(() => {
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
    fs.rmSync(tmpDir, { recursive: true, force: true });
    vi.resetModules();
    vi.restoreAllMocks();
  });

  it('returns the cached path without invoking Unity Hub helpers when the cache is warm', async () => {
    // Create a fake binary that exists on disk so existsSync passes in the cache
    const fakeBinary = path.join(tmpDir, 'Unity.exe');
    fs.writeFileSync(fakeBinary, '');

    // Pre-populate the cache file directly so readCachedEditorPath finds it
    // after the module is reloaded with HOME=tmpDir.
    const cacheFile = path.join(tmpDir, '.unity-mcp-cli-editor-cache.json');
    fs.writeFileSync(
      cacheFile,
      JSON.stringify({ '6000.3.1f1': { path: fakeBinary, savedAt: Date.now() } }),
      'utf-8',
    );

    // Reload module graph so CACHE_FILE is re-computed against our tmpDir.
    vi.resetModules();

    // Mock unity-hub BEFORE importing unity-editor so the mock is in place
    // when unity-editor.js loads its static imports.
    const ensureUnityHubMock = vi.fn().mockResolvedValue('/fake/hub');
    const listInstalledEditorsMock = vi.fn().mockReturnValue([]);
    vi.doMock('../src/utils/unity-hub.js', () => ({
      findUnityHub: vi.fn().mockReturnValue(null),
      ensureUnityHub: ensureUnityHubMock,
      listInstalledEditors: listInstalledEditorsMock,
    }));

    const { findEditorPath } = (await import(
      '../src/utils/unity-editor.js'
    )) as typeof import('../src/utils/unity-editor.js');

    const result = await findEditorPath('6000.3.1f1');

    expect(result).toBe(fakeBinary);
    expect(ensureUnityHubMock).not.toHaveBeenCalled();
    expect(listInstalledEditorsMock).not.toHaveBeenCalled();
  });
});

// ---------------------------------------------------------------------------
// getSecondaryInstallPathFile — platform path resolution
// ---------------------------------------------------------------------------
describe('getSecondaryInstallPathFile', () => {
  let origAppData: string | undefined;
  let origHome: string | undefined;

  beforeEach(() => {
    origAppData = process.env['APPDATA'];
    origHome = process.env['HOME'];
  });

  afterEach(() => {
    if (origAppData === undefined) delete process.env['APPDATA'];
    else process.env['APPDATA'] = origAppData;
    if (origHome === undefined) delete process.env['HOME'];
    else process.env['HOME'] = origHome;
  });

  it('returns null on win32 when APPDATA is not set', () => {
    delete process.env['APPDATA'];
    expect(getSecondaryInstallPathFile('win32')).toBeNull();
  });

  it('returns %APPDATA%/UnityHub/secondaryInstallPath.json on win32', () => {
    process.env['APPDATA'] = 'C:\\Users\\Test\\AppData\\Roaming';
    expect(getSecondaryInstallPathFile('win32')).toBe(
      path.join('C:\\Users\\Test\\AppData\\Roaming', 'UnityHub', 'secondaryInstallPath.json'),
    );
  });

  it('returns ~/Library/Application Support/UnityHub/secondaryInstallPath.json on darwin', () => {
    // homedir() is OS-native; assert the suffix instead of an exact path.
    const result = getSecondaryInstallPathFile('darwin');
    expect(result).not.toBeNull();
    expect(result!.endsWith(path.join('Library', 'Application Support', 'UnityHub', 'secondaryInstallPath.json'))).toBe(true);
  });

  it('returns ~/.config/UnityHub/secondaryInstallPath.json on linux', () => {
    const result = getSecondaryInstallPathFile('linux');
    expect(result).not.toBeNull();
    expect(result!.endsWith(path.join('.config', 'UnityHub', 'secondaryInstallPath.json'))).toBe(true);
  });

  it('returns null on an unsupported platform', () => {
    expect(getSecondaryInstallPathFile('aix' as NodeJS.Platform)).toBeNull();
  });
});

// ---------------------------------------------------------------------------
// readSecondaryInstallPaths — file-shape tolerance
// ---------------------------------------------------------------------------
// The file is best-effort: every malformed shape collapses to []. We exercise
// each branch by redirecting APPDATA (win32) to a temp dir and dropping
// fixture files there.
// ---------------------------------------------------------------------------
describe('readSecondaryInstallPaths', () => {
  let tmpDir: string;
  let origAppData: string | undefined;

  beforeEach(() => {
    origAppData = process.env['APPDATA'];
    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-secondary-paths-'));
    process.env['APPDATA'] = tmpDir;
  });

  afterEach(() => {
    if (origAppData === undefined) delete process.env['APPDATA'];
    else process.env['APPDATA'] = origAppData;
    fs.rmSync(tmpDir, { recursive: true, force: true });
  });

  /** Path that `getSecondaryInstallPathFile('win32')` resolves to under APPDATA=tmpDir. */
  function secondaryFile(): string {
    return path.join(tmpDir, 'UnityHub', 'secondaryInstallPath.json');
  }

  it('returns [] when the file does not exist', () => {
    expect(readSecondaryInstallPaths('win32')).toEqual([]);
  });

  it('returns [] when the file is empty', () => {
    fs.mkdirSync(path.dirname(secondaryFile()), { recursive: true });
    fs.writeFileSync(secondaryFile(), '', 'utf-8');
    expect(readSecondaryInstallPaths('win32')).toEqual([]);
  });

  it('returns [] when the file is whitespace only', () => {
    fs.mkdirSync(path.dirname(secondaryFile()), { recursive: true });
    fs.writeFileSync(secondaryFile(), '   \n  \t  ', 'utf-8');
    expect(readSecondaryInstallPaths('win32')).toEqual([]);
  });

  it('returns [] when the file is not valid JSON', () => {
    fs.mkdirSync(path.dirname(secondaryFile()), { recursive: true });
    fs.writeFileSync(secondaryFile(), 'not-json-at-all{', 'utf-8');
    expect(readSecondaryInstallPaths('win32')).toEqual([]);
  });

  it('returns the parsed path when the file is a JSON-encoded string (Unity Hub format)', () => {
    fs.mkdirSync(path.dirname(secondaryFile()), { recursive: true });
    // Real-world Windows shape: a JSON-encoded string e.g. "C:\\UnityEditor".
    fs.writeFileSync(secondaryFile(), JSON.stringify('C:\\UnityEditor'), 'utf-8');
    expect(readSecondaryInstallPaths('win32')).toEqual(['C:\\UnityEditor']);
  });

  it('returns [] when the JSON-encoded string is empty / whitespace', () => {
    fs.mkdirSync(path.dirname(secondaryFile()), { recursive: true });
    fs.writeFileSync(secondaryFile(), JSON.stringify(''), 'utf-8');
    expect(readSecondaryInstallPaths('win32')).toEqual([]);

    fs.writeFileSync(secondaryFile(), JSON.stringify('   '), 'utf-8');
    expect(readSecondaryInstallPaths('win32')).toEqual([]);
  });

  it('returns the parsed array when the file is a JSON array of strings (defensive format)', () => {
    fs.mkdirSync(path.dirname(secondaryFile()), { recursive: true });
    fs.writeFileSync(
      secondaryFile(),
      JSON.stringify(['C:\\UnityEditor', 'D:\\UnityEditors']),
      'utf-8',
    );
    expect(readSecondaryInstallPaths('win32')).toEqual(['C:\\UnityEditor', 'D:\\UnityEditors']);
  });

  it('drops non-string and empty-string entries from an array', () => {
    fs.mkdirSync(path.dirname(secondaryFile()), { recursive: true });
    fs.writeFileSync(
      secondaryFile(),
      JSON.stringify(['C:\\UnityEditor', '', 42, null, 'D:\\UnityEditors']),
      'utf-8',
    );
    expect(readSecondaryInstallPaths('win32')).toEqual(['C:\\UnityEditor', 'D:\\UnityEditors']);
  });

  it('returns [] when the parsed JSON is an unsupported shape (object / number / null)', () => {
    fs.mkdirSync(path.dirname(secondaryFile()), { recursive: true });

    fs.writeFileSync(secondaryFile(), JSON.stringify({ path: 'C:\\UnityEditor' }), 'utf-8');
    expect(readSecondaryInstallPaths('win32')).toEqual([]);

    fs.writeFileSync(secondaryFile(), JSON.stringify(42), 'utf-8');
    expect(readSecondaryInstallPaths('win32')).toEqual([]);

    fs.writeFileSync(secondaryFile(), JSON.stringify(null), 'utf-8');
    expect(readSecondaryInstallPaths('win32')).toEqual([]);
  });

  it('returns [] on unsupported platform regardless of file contents', () => {
    // The file may exist for win32 but the function should still return [] when
    // asked about an unsupported platform (defensive — should be unreachable).
    fs.mkdirSync(path.dirname(secondaryFile()), { recursive: true });
    fs.writeFileSync(secondaryFile(), JSON.stringify('C:\\UnityEditor'), 'utf-8');
    expect(readSecondaryInstallPaths('aix' as NodeJS.Platform)).toEqual([]);
  });
});

// ---------------------------------------------------------------------------
// findEditorPath — secondaryInstallPath fast path (Windows-only host gate)
// ---------------------------------------------------------------------------
// `findEditorPathByCommonLocations` uses `platform()` at runtime; we can't
// safely override it. Run the integration check on Windows hosts only — that
// is the platform where the issue was reproduced (and where Unity-MCP CI
// runs). The pure helpers (`readSecondaryInstallPaths`) above cover the
// cross-platform shape; this test confirms the fast path actually picks up a
// secondary root end-to-end.
// ---------------------------------------------------------------------------
describe.skipIf(process.platform !== 'win32')('findEditorPath (secondaryInstallPath fast path on win32)', () => {
  let tmpDir: string;
  let origHome: string | undefined;
  let origUserProfile: string | undefined;
  let origAppData: string | undefined;
  let origProgramFiles: string | undefined;

  beforeEach(() => {
    origHome = process.env['HOME'];
    origUserProfile = process.env['USERPROFILE'];
    origAppData = process.env['APPDATA'];
    origProgramFiles = process.env['PROGRAMFILES'];

    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-secondary-fast-path-'));
    // Redirect HOME / USERPROFILE so editor-cache writes into the temp dir.
    process.env['HOME'] = tmpDir;
    process.env['USERPROFILE'] = tmpDir;
    // Redirect APPDATA so getSecondaryInstallPathFile('win32') resolves under tmpDir.
    process.env['APPDATA'] = tmpDir;
    // Redirect PROGRAMFILES to an empty subdir so the default-Hub-root scan finds nothing.
    const fakeProgramFiles = path.join(tmpDir, 'ProgramFiles');
    fs.mkdirSync(fakeProgramFiles, { recursive: true });
    process.env['PROGRAMFILES'] = fakeProgramFiles;
  });

  afterEach(() => {
    if (origHome === undefined) delete process.env['HOME'];
    else process.env['HOME'] = origHome;
    if (origUserProfile === undefined) delete process.env['USERPROFILE'];
    else process.env['USERPROFILE'] = origUserProfile;
    if (origAppData === undefined) delete process.env['APPDATA'];
    else process.env['APPDATA'] = origAppData;
    if (origProgramFiles === undefined) delete process.env['PROGRAMFILES'];
    else process.env['PROGRAMFILES'] = origProgramFiles;

    fs.rmSync(tmpDir, { recursive: true, force: true });
    vi.resetModules();
    vi.restoreAllMocks();
  });

  it('resolves a version that lives only under secondaryInstallPath without invoking Unity Hub', async () => {
    // Set up a fake secondary install root with one editor binary in it.
    const secondaryRoot = path.join(tmpDir, 'UnityEditor');
    const fakeBinaryDir = path.join(secondaryRoot, '6000.3.1f1', 'Editor');
    fs.mkdirSync(fakeBinaryDir, { recursive: true });
    const fakeBinary = path.join(fakeBinaryDir, 'Unity.exe');
    fs.writeFileSync(fakeBinary, '');

    // Drop a Unity-Hub-shaped secondaryInstallPath.json pointing at it.
    const secondaryFile = path.join(tmpDir, 'UnityHub', 'secondaryInstallPath.json');
    fs.mkdirSync(path.dirname(secondaryFile), { recursive: true });
    fs.writeFileSync(secondaryFile, JSON.stringify(secondaryRoot), 'utf-8');

    vi.resetModules();

    // Mock unity-hub so we can assert the slow path is NEVER taken — a
    // successful fast path means the Electron probe doesn't run.
    const ensureUnityHubMock = vi.fn().mockResolvedValue('/fake/hub');
    const listInstalledEditorsMock = vi.fn().mockReturnValue([]);
    vi.doMock('../src/utils/unity-hub.js', () => ({
      findUnityHub: vi.fn().mockReturnValue(null),
      ensureUnityHub: ensureUnityHubMock,
      listInstalledEditors: listInstalledEditorsMock,
    }));

    const { findEditorPath } = (await import(
      '../src/utils/unity-editor.js'
    )) as typeof import('../src/utils/unity-editor.js');

    const result = await findEditorPath('6000.3.1f1');

    expect(result).toBe(fakeBinary);
    expect(ensureUnityHubMock).not.toHaveBeenCalled();
    expect(listInstalledEditorsMock).not.toHaveBeenCalled();
  });
});

// ---------------------------------------------------------------------------
// findEditorPath — cache-poisoning fix
// ---------------------------------------------------------------------------
// When a caller asks for a version that is NOT installed and the resolver
// falls back to the highest installed editor, the cache MUST NOT be written
// under the requested-but-unmatched key. This was the secondary bug in #784.
// ---------------------------------------------------------------------------
describe('findEditorPath (no cache write on unmatched-version fallback)', () => {
  let tmpDir: string;
  let origHome: string | undefined;
  let origUserProfile: string | undefined;
  let origAppData: string | undefined;
  let origProgramFiles: string | undefined;

  beforeEach(() => {
    origHome = process.env['HOME'];
    origUserProfile = process.env['USERPROFILE'];
    origAppData = process.env['APPDATA'];
    origProgramFiles = process.env['PROGRAMFILES'];

    tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), 'unity-cache-poison-'));
    process.env['HOME'] = tmpDir;
    process.env['USERPROFILE'] = tmpDir;
    // Force the fast path to find nothing so the slow path (the path we care
    // about) is exercised. Both the default Hub root AND the secondary file
    // must be inaccessible.
    const fakeProgramFiles = path.join(tmpDir, 'ProgramFiles');
    fs.mkdirSync(fakeProgramFiles, { recursive: true });
    process.env['PROGRAMFILES'] = fakeProgramFiles;
    process.env['APPDATA'] = path.join(tmpDir, 'NoSuchDir');
  });

  afterEach(() => {
    if (origHome === undefined) delete process.env['HOME'];
    else process.env['HOME'] = origHome;
    if (origUserProfile === undefined) delete process.env['USERPROFILE'];
    else process.env['USERPROFILE'] = origUserProfile;
    if (origAppData === undefined) delete process.env['APPDATA'];
    else process.env['APPDATA'] = origAppData;
    if (origProgramFiles === undefined) delete process.env['PROGRAMFILES'];
    else process.env['PROGRAMFILES'] = origProgramFiles;

    fs.rmSync(tmpDir, { recursive: true, force: true });
    vi.resetModules();
    vi.restoreAllMocks();
  });

  it('does NOT cache the fallback resolution when requested version != resolved version', async () => {
    // Set up a fake "installed" editor that Unity Hub will return.
    const installedDir = path.join(tmpDir, 'installed', '2022.3.62f3');
    fs.mkdirSync(installedDir, { recursive: true });
    // The path on disk doesn't need to actually contain Unity.exe — the
    // production code returns whatever `getEditorBinary(match.path)` resolves
    // to without `existsSync` checking it in the slow path. The cache READ
    // does existsSync, but we're asserting the cache file's CONTENTS, not
    // re-reading via the cache API.

    vi.resetModules();

    const ensureUnityHubMock = vi.fn().mockResolvedValue('/fake/hub');
    const listInstalledEditorsMock = vi.fn().mockReturnValue([
      { version: '2022.3.62f3', path: installedDir },
    ]);
    vi.doMock('../src/utils/unity-hub.js', () => ({
      findUnityHub: vi.fn().mockReturnValue('/fake/hub'),
      ensureUnityHub: ensureUnityHubMock,
      listInstalledEditors: listInstalledEditorsMock,
    }));

    const { findEditorPath } = (await import(
      '../src/utils/unity-editor.js'
    )) as typeof import('../src/utils/unity-editor.js');

    // Ask for a version that is NOT in the installed list. Expect a
    // best-effort fallback (the highest installed editor's path), but the
    // cache MUST NOT learn the bogus key.
    const result = await findEditorPath('999.0.0f0');
    expect(result).not.toBeNull(); // fallback path returned
    expect(result).toContain('2022.3.62f3'); // fell back to the installed editor

    // Verify the cache file does NOT contain the bogus requested key.
    const cacheFile = path.join(tmpDir, '.unity-mcp-cli-editor-cache.json');
    if (fs.existsSync(cacheFile)) {
      const cache = JSON.parse(fs.readFileSync(cacheFile, 'utf-8')) as Record<string, unknown>;
      expect(Object.keys(cache)).not.toContain('999.0.0f0');
    }
    // (If cacheFile doesn't exist at all, that also satisfies the assertion.)
  });

  it('DOES cache when the requested version exactly matches an installed editor', async () => {
    const installedDir = path.join(tmpDir, 'installed', '2022.3.62f3');
    fs.mkdirSync(installedDir, { recursive: true });

    vi.resetModules();

    vi.doMock('../src/utils/unity-hub.js', () => ({
      findUnityHub: vi.fn().mockReturnValue('/fake/hub'),
      ensureUnityHub: vi.fn().mockResolvedValue('/fake/hub'),
      listInstalledEditors: vi.fn().mockReturnValue([
        { version: '2022.3.62f3', path: installedDir },
      ]),
    }));

    const { findEditorPath } = (await import(
      '../src/utils/unity-editor.js'
    )) as typeof import('../src/utils/unity-editor.js');

    await findEditorPath('2022.3.62f3');

    const cacheFile = path.join(tmpDir, '.unity-mcp-cli-editor-cache.json');
    expect(fs.existsSync(cacheFile)).toBe(true);
    const cache = JSON.parse(fs.readFileSync(cacheFile, 'utf-8')) as Record<string, unknown>;
    expect(Object.keys(cache)).toContain('2022.3.62f3');
  });

  it('DOES cache under the auto key when no version is requested (highest-installed lookup)', async () => {
    const installedDir = path.join(tmpDir, 'installed', '6000.3.1f1');
    fs.mkdirSync(installedDir, { recursive: true });

    vi.resetModules();

    vi.doMock('../src/utils/unity-hub.js', () => ({
      findUnityHub: vi.fn().mockReturnValue('/fake/hub'),
      ensureUnityHub: vi.fn().mockResolvedValue('/fake/hub'),
      listInstalledEditors: vi.fn().mockReturnValue([
        { version: '6000.3.1f1', path: installedDir },
      ]),
    }));

    const { findEditorPath } = (await import(
      '../src/utils/unity-editor.js'
    )) as typeof import('../src/utils/unity-editor.js');

    await findEditorPath(undefined);

    const cacheFile = path.join(tmpDir, '.unity-mcp-cli-editor-cache.json');
    expect(fs.existsSync(cacheFile)).toBe(true);
    const cache = JSON.parse(fs.readFileSync(cacheFile, 'utf-8')) as Record<string, unknown>;
    // The auto key is "__auto__" in editor-cache.ts.
    expect(Object.keys(cache)).toContain('__auto__');
  });
});

// ---------------------------------------------------------------------------
// Test against the actual test project files in the repo
describe('getProjectEditorVersion (real projects)', () => {
  const __dirname = path.dirname(fileURLToPath(import.meta.url));
  const repoRoot = path.resolve(__dirname, '..', '..');

  const testCases = [
    { folder: '2022.3.62f3', expected: '2022.3.62f3' },
    { folder: '2023.2.22f1', expected: '2023.2.22f1' },
    { folder: '6000.3.1f1', expected: '6000.3.1f1' },
  ];

  for (const { folder, expected } of testCases) {
    const projectPath = path.join(repoRoot, 'Unity-Tests', folder);
    const versionFileExists = fs.existsSync(path.join(projectPath, 'ProjectSettings', 'ProjectVersion.txt'));

    it.skipIf(!versionFileExists)(`reads version from Unity-Tests/${folder}`, () => {
      const result = getProjectEditorVersion(projectPath);
      expect(result).toBe(expected);
    });
  }
});
