import { describe, expect, it } from 'vitest';
import { normalizeFsPath } from './utils';

describe('normalizeFsPath', () => {
  it('normalizes path separators on every platform', () => {
    expect(normalizeFsPath('folder\\nested\\file.txt', 'darwin')).toBe('folder/nested/file.txt');
  });

  it('normalizes drive-letter paths case-insensitively on Windows', () => {
    expect(normalizeFsPath('C:\\Workspace\\Assets\\Scene.unity', 'win32')).toBe(
      'c:/workspace/assets/scene.unity',
    );
  });
});
