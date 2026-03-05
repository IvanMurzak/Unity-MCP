import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import path from 'path';
import { Command } from 'commander';

vi.mock('fs');
vi.mock('../../src/utils/config.js', () => ({
  readConfig: vi.fn().mockReturnValue({
    host: 'http://localhost:8080',
    keepConnected: false,
    tools: [],
    prompts: [],
    resources: [],
  }),
  writeConfig: vi.fn(),
}));
vi.mock('../../src/utils/unity.js', () => ({
  openUnityProject: vi.fn(),
  getConnectResultPath: vi.fn().mockReturnValue('/fake/project/Library/mcp-connect-result.json'),
  pollForFile: vi.fn(),
}));

import fs from 'fs';
import { readConfig, writeConfig } from '../../src/utils/config.js';
import { openUnityProject, getConnectResultPath, pollForFile } from '../../src/utils/unity.js';
import { registerConnectCommand } from '../../src/commands/connect.js';

const FAKE_PROJECT = '/fake/project';
const RESULT_FILE = '/fake/project/Library/mcp-connect-result.json';

describe('registerConnectCommand', () => {
  let program;
  let exitSpy;
  let logSpy;
  let errSpy;

  beforeEach(() => {
    vi.resetAllMocks();

    readConfig.mockReturnValue({ host: 'http://localhost:8080', keepConnected: false });
    writeConfig.mockImplementation(() => {});
    openUnityProject.mockResolvedValue({ pid: 123 });
    getConnectResultPath.mockReturnValue(RESULT_FILE);
    pollForFile.mockResolvedValue(true);
    fs.existsSync.mockReturnValue(false);
    fs.unlinkSync.mockImplementation(() => {});
    fs.readFileSync.mockReturnValue(JSON.stringify({ success: true }));

    program = new Command();
    program.exitOverride();
    registerConnectCommand(program);

    // Use named parameter — `arguments` is not available in ESM arrow functions
    exitSpy = vi.spyOn(process, 'exit').mockImplementation((code) => { /* captured, no throw by default */ });
    logSpy = vi.spyOn(console, 'log').mockImplementation(() => {});
    errSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('updates config host with the provided URL', async () => {
    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://myserver:9090']);

    expect(writeConfig).toHaveBeenCalledWith(
      path.resolve(FAKE_PROJECT),
      expect.objectContaining({ host: 'http://myserver:9090', keepConnected: true })
    );
  });

  it('removes stale result file before launching Unity', async () => {
    fs.existsSync.mockReturnValue(true); // stale file exists

    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080']);

    expect(fs.unlinkSync).toHaveBeenCalledWith(RESULT_FILE);
  });

  it('does not try to remove result file when it does not exist', async () => {
    fs.existsSync.mockReturnValue(false);

    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080']);

    expect(fs.unlinkSync).not.toHaveBeenCalled();
  });

  it('launches Unity with -mcpServerUrl argument', async () => {
    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080']);

    expect(openUnityProject).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({
        extraArgs: ['-mcpServerUrl', 'http://localhost:8080'],
      })
    );
  });

  it('launches Unity detached when --wait is not specified', async () => {
    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080']);

    expect(openUnityProject).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({ detached: true })
    );
  });

  it('launches Unity non-detached and waits for result when --wait is specified', async () => {
    fs.readFileSync.mockReturnValue(JSON.stringify({ success: true }));

    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080', '--wait']);

    expect(openUnityProject).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({ detached: false })
    );
    expect(pollForFile).toHaveBeenCalled();
  });

  it('reports success when result file indicates success', async () => {
    fs.readFileSync.mockReturnValue(JSON.stringify({ success: true }));

    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080', '--wait']);

    expect(logSpy).toHaveBeenCalledWith(expect.stringContaining('[Success]'));
  });

  it('exits with error and prints details when result file indicates failure', async () => {
    fs.readFileSync.mockReturnValue(JSON.stringify({
      success: false,
      errorType: 'InvalidOperationException',
      errorMessage: 'Plugin not initialized',
      stackTrace: 'at CommandLineArgs.EnforceConnect()',
    }));

    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080', '--wait']);

    expect(errSpy).toHaveBeenCalledWith(expect.stringContaining('[Error]'));
    expect(errSpy).toHaveBeenCalledWith(expect.stringContaining('Plugin not initialized'));
    expect(exitSpy).toHaveBeenCalledWith(1);
  });

  it('exits with error when poll times out', async () => {
    pollForFile.mockResolvedValue(false);

    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080', '--wait']);

    expect(errSpy).toHaveBeenCalledWith(expect.stringContaining('Timed out'));
    expect(exitSpy).toHaveBeenCalledWith(1);
  });

  it('exits with error when openUnityProject throws', async () => {
    openUnityProject.mockRejectedValue(new Error('Could not find Unity'));

    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080']);

    expect(errSpy).toHaveBeenCalledWith(expect.stringContaining('Could not find Unity'));
    expect(exitSpy).toHaveBeenCalledWith(1);
  });

  it('forwards --unity-path option to openUnityProject', async () => {
    await program.parseAsync([
      'node', 'gamedev', 'connect', FAKE_PROJECT,
      '--url', 'http://localhost:8080',
      '--unity-path', '/opt/Unity/Editor/Unity',
    ]);

    expect(openUnityProject).toHaveBeenCalledWith(
      expect.any(String),
      expect.objectContaining({ unityPath: '/opt/Unity/Editor/Unity' })
    );
  });

  it('uses custom --wait-timeout when --wait is set', async () => {
    fs.readFileSync.mockReturnValue(JSON.stringify({ success: true }));

    await program.parseAsync([
      'node', 'gamedev', 'connect', FAKE_PROJECT,
      '--url', 'http://localhost:8080',
      '--wait', '--wait-timeout', '120',
    ]);

    const [, timeoutMs] = pollForFile.mock.calls[0];
    expect(timeoutMs).toBe(120 * 1000);
  });

  it('does not call pollForFile when --wait is not specified', async () => {
    await program.parseAsync(['node', 'gamedev', 'connect', FAKE_PROJECT, '--url', 'http://localhost:8080']);

    expect(pollForFile).not.toHaveBeenCalled();
  });
});
