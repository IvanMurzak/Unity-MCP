import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import path from 'path';
import { Command } from 'commander';

vi.mock('../../src/utils/manifest.js', () => ({
  fetchLatestVersion: vi.fn(),
  installUnityMcp: vi.fn(),
}));

import { fetchLatestVersion, installUnityMcp } from '../../src/utils/manifest.js';
import { registerInstallMcpCommand } from '../../src/commands/install-mcp.js';

describe('registerInstallMcpCommand', () => {
  let program;
  let exitSpy;
  let logSpy;
  let errSpy;

  beforeEach(() => {
    vi.resetAllMocks();
    program = new Command();
    program.exitOverride();
    registerInstallMcpCommand(program);
    exitSpy = vi.spyOn(process, 'exit').mockImplementation(() => { throw new Error('exit'); });
    logSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
    errSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('fetches latest version from OpenUPM when --version is not given', async () => {
    fetchLatestVersion.mockResolvedValue('0.50.0');
    installUnityMcp.mockImplementation(() => {});

    await program.parseAsync(['node', 'gamedev', 'install-mcp', './MyGame']);

    expect(fetchLatestVersion).toHaveBeenCalledOnce();
    expect(installUnityMcp).toHaveBeenCalledWith(
      path.resolve('./MyGame'),
      '0.50.0'
    );
  });

  it('uses the specified --version without fetching from registry', async () => {
    installUnityMcp.mockImplementation(() => {});

    await program.parseAsync(['node', 'gamedev', 'install-mcp', './MyGame', '--version', '0.45.0']);

    expect(fetchLatestVersion).not.toHaveBeenCalled();
    expect(installUnityMcp).toHaveBeenCalledWith(
      path.resolve('./MyGame'),
      '0.45.0'
    );
  });

  it('calls process.exit(1) when installUnityMcp throws', async () => {
    fetchLatestVersion.mockResolvedValue('0.50.0');
    installUnityMcp.mockImplementation(() => { throw new Error('manifest not found'); });

    await expect(
      program.parseAsync(['node', 'gamedev', 'install-mcp', './MyGame'])
    ).rejects.toThrow('exit');

    expect(errSpy).toHaveBeenCalledWith(expect.stringContaining('manifest not found'));
    expect(exitSpy).toHaveBeenCalledWith(1);
  });

  it('resolves the project path to an absolute path', async () => {
    fetchLatestVersion.mockResolvedValue('0.50.0');
    installUnityMcp.mockImplementation(() => {});

    await program.parseAsync(['node', 'gamedev', 'install-mcp', 'relative/game']);

    const [calledPath] = installUnityMcp.mock.calls[0];
    expect(path.isAbsolute(calledPath)).toBe(true);
  });

  it('logs a success message after installation', async () => {
    fetchLatestVersion.mockResolvedValue('0.50.0');
    installUnityMcp.mockImplementation(() => {});

    await program.parseAsync(['node', 'gamedev', 'install-mcp', './MyGame']);

    expect(logSpy).toHaveBeenCalledWith(
      expect.stringContaining('Successfully installed')
    );
  });
});
