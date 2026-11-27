import { mkdtemp, mkdir, rm, writeFile } from 'node:fs/promises';
import * as os from 'node:os';
import * as path from 'node:path';
import { afterEach, describe, expect, it } from 'vitest';
import { readUnityMcpProjectConfig } from './unityConfig';

const tempRoots: string[] = [];

afterEach(async () => {
  await Promise.all(
    tempRoots.splice(0).map((dir) => rm(dir, { recursive: true, force: true })),
  );
});

describe('readUnityMcpProjectConfig', () => {
  it('reads the Unity MCP project config when present', async () => {
    const workspace = await createTempWorkspace();
    await mkdir(path.join(workspace, 'UserSettings'), { recursive: true });
    await writeFile(
      path.join(workspace, 'UserSettings', 'AI-Game-Developer-Config.json'),
      JSON.stringify({
        host: 'http://localhost:6501',
        token: 'secret-token',
        authOption: 'required',
        transportMethod: 'streamableHttp',
        keepConnected: true,
      }),
    );

    const config = await readUnityMcpProjectConfig(workspace);

    expect(config.exists).toBe(true);
    expect(config.ready).toBe(true);
    expect(config.host).toBe('http://localhost:6501');
    expect(config.authOption).toBe('required');
    expect(config.transport).toBe('streamableHttp');
    expect(config.keepConnected).toBe(true);
  });

  it('returns a warning when the config is malformed', async () => {
    const workspace = await createTempWorkspace();
    await mkdir(path.join(workspace, 'UserSettings'), { recursive: true });
    await writeFile(
      path.join(workspace, 'UserSettings', 'AI-Game-Developer-Config.json'),
      '{broken json',
    );

    const config = await readUnityMcpProjectConfig(workspace);

    expect(config.exists).toBe(true);
    expect(config.ready).toBe(false);
    expect(
      config.warnings.some((warning) =>
        warning.includes('Could not parse UserSettings/AI-Game-Developer-Config.json')),
    ).toBe(true);
  });

  it('marks the config as not ready when required connection fields are missing', async () => {
    const workspace = await createTempWorkspace();
    await mkdir(path.join(workspace, 'UserSettings'), { recursive: true });
    await writeFile(
      path.join(workspace, 'UserSettings', 'AI-Game-Developer-Config.json'),
      JSON.stringify({
        authOption: 'none',
      }),
    );

    const config = await readUnityMcpProjectConfig(workspace);

    expect(config.exists).toBe(true);
    expect(config.ready).toBe(false);
    expect(
      config.warnings.some((warning) =>
        warning.includes('missing a supported transportMethod')),
    ).toBe(true);
    expect(
      config.warnings.some((warning) =>
        warning.includes('missing a host URL')),
    ).toBe(true);
  });
});

async function createTempWorkspace(): Promise<string> {
  const tempDir = await mkdtemp(path.join(os.tmpdir(), 'unity-mcp-vscode-config-'));
  tempRoots.push(tempDir);
  return tempDir;
}
