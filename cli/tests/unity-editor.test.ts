import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';
import { getProjectEditorVersion } from '../src/utils/unity-editor.js';

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
  const repoRoot = path.resolve(import.meta.dirname, '..', '..');

  it('reads version from Unity-Tests/2022.3.62f3', () => {
    const projectPath = path.join(repoRoot, 'Unity-Tests', '2022.3.62f3');
    if (fs.existsSync(path.join(projectPath, 'ProjectSettings', 'ProjectVersion.txt'))) {
      const result = getProjectEditorVersion(projectPath);
      expect(result).toBe('2022.3.62f3');
    }
  });

  it('reads version from Unity-Tests/2023.2.22f1', () => {
    const projectPath = path.join(repoRoot, 'Unity-Tests', '2023.2.22f1');
    if (fs.existsSync(path.join(projectPath, 'ProjectSettings', 'ProjectVersion.txt'))) {
      const result = getProjectEditorVersion(projectPath);
      expect(result).toBe('2023.2.22f1');
    }
  });

  it('reads version from Unity-Tests/6000.3.1f1', () => {
    const projectPath = path.join(repoRoot, 'Unity-Tests', '6000.3.1f1');
    if (fs.existsSync(path.join(projectPath, 'ProjectSettings', 'ProjectVersion.txt'))) {
      const result = getProjectEditorVersion(projectPath);
      expect(result).toBe('6000.3.1f1');
    }
  });
});
