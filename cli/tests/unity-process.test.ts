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

  it('skips asset-import workers of the same project and returns the Editor (macOS / Linux)', async () => {
    const projectPath = '/Users/dev/Game';
    const unity = '/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity';
    const worker = `24435 ${unity} -adb2 -batchMode -noUpm -name AssetImportWorkerHW2 -projectPath ${projectPath} -logFile Logs/AssetImportWorkerHW2.log -parentPid 33383`;
    const editor = `33383 ${unity} -projectPath ${projectPath}`;
    const execSyncMock = vi.fn(() => `${worker}\n${editor}\n`);

    vi.doMock('os', async (importOriginal) => {
      const actual = await importOriginal<typeof import('os')>();
      return { ...actual, platform: () => 'darwin' };
    });
    vi.doMock('child_process', () => ({ execFileSync: vi.fn(), execSync: execSyncMock }));

    const { findUnityProcess } = await import('../src/utils/unity-process.js');
    expect(findUnityProcess(projectPath)?.pid).toBe(33383);
  });

  it('skips asset-import workers on Windows', async () => {
    const projectPath = 'E:\\Game';
    const exe = '"C:\\Unity\\Editor\\Unity.exe"';
    const execFileSyncMock = vi.fn(() =>
      `100|||${exe} -batchMode -name AssetImportWorker0 -projectPath "${projectPath}" -parentPid 200\n` +
      `200|||${exe} -projectPath "${projectPath}"\n`);

    vi.doMock('os', async (importOriginal) => {
      const actual = await importOriginal<typeof import('os')>();
      return { ...actual, platform: () => 'win32' };
    });
    vi.doMock('child_process', () => ({ execFileSync: execFileSyncMock, execSync: vi.fn() }));

    const { findUnityProcess } = await import('../src/utils/unity-process.js');
    expect(findUnityProcess(projectPath)?.pid).toBe(200);
  });
});
