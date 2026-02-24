import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import path from 'path';
import { Command } from 'commander';

vi.mock('../../src/utils/unity.js', () => ({
  openUnityProject: vi.fn(),
}));

import { openUnityProject } from '../../src/utils/unity.js';
import { registerOpenCommand } from '../../src/commands/open.js';

describe('registerOpenCommand', () => {
  let program;
  let exitSpy;
  let logSpy;
  let errSpy;

  beforeEach(() => {
    vi.resetAllMocks();
    program = new Command();
    program.exitOverride();
    registerOpenCommand(program);
    // Use named parameter instead of `arguments` (not available in ESM arrow functions)
    exitSpy = vi.spyOn(process, 'exit').mockImplementation((code) => { throw new Error('exit:' + code); });
    logSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
    errSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('calls openUnityProject with the resolved project path', async () => {
    openUnityProject.mockResolvedValue({ pid: 99 });

    await program.parseAsync(['node', 'gamedev', 'open', './MyGame']);

    expect(openUnityProject).toHaveBeenCalledWith(
      path.resolve('./MyGame'),
      expect.objectContaining({ unityPath: undefined, unityVersion: undefined })
    );
  });

  it('forwards --unity-path to openUnityProject', async () => {
    openUnityProject.mockResolvedValue({ pid: 99 });

    await program.parseAsync(['node', 'gamedev', 'open', './MyGame', '--unity-path', '/usr/bin/unity']);

    expect(openUnityProject).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({ unityPath: '/usr/bin/unity' })
    );
  });

  it('forwards --unity-version to openUnityProject', async () => {
    openUnityProject.mockResolvedValue({ pid: 99 });

    await program.parseAsync(['node', 'gamedev', 'open', './MyGame', '--unity-version', '2022.3.62f1']);

    expect(openUnityProject).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({ unityVersion: '2022.3.62f1' })
    );
  });

  it('calls process.exit(1) when openUnityProject rejects', async () => {
    openUnityProject.mockRejectedValue(new Error('Could not find Unity'));

    await expect(
      program.parseAsync(['node', 'gamedev', 'open', './MyGame'])
    ).rejects.toThrow('exit:1');

    expect(errSpy).toHaveBeenCalledWith(expect.stringContaining('Could not find Unity'));
    expect(exitSpy).toHaveBeenCalledWith(1);
  });

  it('resolves the project path to an absolute path', async () => {
    openUnityProject.mockResolvedValue({ pid: 99 });

    await program.parseAsync(['node', 'gamedev', 'open', 'relative/path']);

    const [calledPath] = openUnityProject.mock.calls[0];
    expect(path.isAbsolute(calledPath)).toBe(true);
  });
});
