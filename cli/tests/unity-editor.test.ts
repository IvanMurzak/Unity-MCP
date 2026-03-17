import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { fileURLToPath } from 'url';
import { getProjectEditorVersion, resolveEditorPath } from '../src/utils/unity-editor.js';

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
