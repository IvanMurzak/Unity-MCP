import { describe, it, expect, afterEach, vi } from 'vitest';
import * as path from 'path';

describe('findUnityProcess', () => {
  afterEach(() => {
    vi.doUnmock('child_process');
    vi.doUnmock('os');
    vi.resetModules();
  });

  it('uses direct powershell execution on Windows and parses quoted project paths with spaces', async () => {
    const projectPath = 'E:\\Game Projects\\Demo';
    const commandLine = `"D:\\Program Files\\Unity Hub\\Editors\\6000.5.0b8\\Editor\\Unity.exe" -projectpath "${projectPath}"`;
    const execFileSyncMock = vi.fn(() => `107820|||${commandLine}\n`);
    const execSyncMock = vi.fn();

    vi.doMock('os', async (importOriginal) => {
      const actual = await importOriginal<typeof import('os')>();
      return {
        ...actual,
        platform: () => 'win32',
      };
    });
    vi.doMock('child_process', () => ({
      execFileSync: execFileSyncMock,
      execSync: execSyncMock,
    }));

    const { findUnityProcess } = await import('../src/utils/unity-process.js');
    const result = findUnityProcess(projectPath);

    expect(result).not.toBeNull();
    expect(result?.pid).toBe(107820);
    expect(result?.projectPath).toBe(path.resolve(projectPath));
    expect(result?.commandLine).toBe(commandLine);
    expect(execSyncMock).not.toHaveBeenCalled();
    expect(execFileSyncMock).toHaveBeenCalledWith(
      'powershell.exe',
      expect.arrayContaining(['-NoProfile', '-Command']),
      expect.objectContaining({ timeout: 10000 }),
    );
  });
});
