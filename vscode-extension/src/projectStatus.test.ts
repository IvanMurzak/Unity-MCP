import { mkdtemp, mkdir, rm, writeFile } from 'node:fs/promises';
import * as os from 'node:os';
import * as path from 'node:path';
import { afterEach, describe, expect, it } from 'vitest';
import {
  inspectWorkspaceStatus,
} from './projectStatus';

const tempRoots: string[] = [];

afterEach(async () => {
  await Promise.all(
    tempRoots.splice(0).map((dir) => rm(dir, { recursive: true, force: true })),
  );
});

describe('inspectWorkspaceStatus', () => {
  it('detects a Unity project and installed Unity MCP plugin', async () => {
    const workspace = await createTempWorkspace();
    await mkdir(path.join(workspace, 'Assets'), { recursive: true });
    await mkdir(path.join(workspace, 'ProjectSettings'), { recursive: true });
    await mkdir(path.join(workspace, 'Packages'), { recursive: true });
    await mkdir(path.join(workspace, 'UserSettings'), { recursive: true });
    await writeFile(
      path.join(workspace, 'Packages', 'manifest.json'),
      JSON.stringify({
        dependencies: {
          'com.ivanmurzak.unity.mcp': '0.79.0',
        },
      }, null, 2),
    );
    await writeFile(
      path.join(workspace, 'UserSettings', 'AI-Game-Developer-Config.json'),
      JSON.stringify({
        host: 'http://localhost:6501',
        authOption: 'none',
        transportMethod: 'streamableHttp',
      }, null, 2),
    );

    const status = await inspectWorkspaceStatus(workspace, 'TestProject', 'trusted');

    expect(status.unityProjectDetected).toBe(true);
    expect(status.pluginInstalled).toBe(true);
    expect(status.pluginVersion).toBe('0.79.0');
    expect(status.unityMcpProjectConfigExists).toBe(true);
    expect(status.unityMcpProjectConfigReady).toBe(true);
    expect(status.warnings).toEqual([]);
    expect(status.recommendedActions).toEqual(['configure-vscode-mcp', 'open-unity-with-mcp']);
  });

  it('reports missing Unity markers for a non-Unity workspace', async () => {
    const workspace = await createTempWorkspace();
    await writeFile(path.join(workspace, 'README.md'), '# hello\n');

    const status = await inspectWorkspaceStatus(workspace, 'PlainFolder', 'restricted');

    expect(status.unityProjectDetected).toBe(false);
    expect(status.pluginInstalled).toBe(false);
    expect(status.unityMarkers).toEqual([]);
    expect(status.recommendedActions).toEqual(['trust-workspace']);
  });

  it('warns when the plugin is installed but the Unity MCP project config has not been initialized yet', async () => {
    const workspace = await createTempWorkspace();
    await mkdir(path.join(workspace, 'Assets'), { recursive: true });
    await mkdir(path.join(workspace, 'ProjectSettings'), { recursive: true });
    await mkdir(path.join(workspace, 'Packages'), { recursive: true });
    await writeFile(
      path.join(workspace, 'Packages', 'manifest.json'),
      JSON.stringify({
        dependencies: {
          'com.ivanmurzak.unity.mcp': '0.79.0',
        },
      }, null, 2),
    );

    const status = await inspectWorkspaceStatus(workspace, 'NeedsInit', 'trusted');

    expect(status.pluginInstalled).toBe(true);
    expect(status.unityMcpProjectConfigExists).toBe(false);
    expect(status.unityMcpProjectConfigReady).toBe(false);
    expect(status.warnings.some((warning) => warning.includes('Open Unity once without MCP'))).toBe(true);
    expect(status.recommendedActions).toEqual(['open-unity-without-mcp']);
  });

  it('warns on invalid workspace MCP config', async () => {
    const workspace = await createTempWorkspace();
    await mkdir(path.join(workspace, 'Assets'), { recursive: true });
    await mkdir(path.join(workspace, 'ProjectSettings'), { recursive: true });
    await mkdir(path.join(workspace, 'Packages'), { recursive: true });
    await writeFile(
      path.join(workspace, 'Packages', 'manifest.json'),
      JSON.stringify({
        dependencies: {
          'com.ivanmurzak.unity.mcp': '0.79.0',
        },
      }, null, 2),
    );
    await mkdir(path.join(workspace, '.vscode'), { recursive: true });
    await writeFile(path.join(workspace, '.vscode', 'mcp.json'), '{invalid json');

    const status = await inspectWorkspaceStatus(workspace, 'BrokenConfig', 'trusted');

    expect(status.mcpConfigExists).toBe(true);
    expect(status.mcpServerConfigured).toBe(false);
    expect(status.warnings.some((warning) => warning.includes('Could not parse .vscode/mcp.json'))).toBe(true);
    expect(status.recommendedActions).toEqual(['open-unity-without-mcp']);
  });

  it('treats an incomplete ai-game-developer MCP entry as not configured', async () => {
    const workspace = await createTempWorkspace();
    await mkdir(path.join(workspace, 'Assets'), { recursive: true });
    await mkdir(path.join(workspace, 'ProjectSettings'), { recursive: true });
    await mkdir(path.join(workspace, 'Packages'), { recursive: true });
    await mkdir(path.join(workspace, 'UserSettings'), { recursive: true });
    await mkdir(path.join(workspace, '.vscode'), { recursive: true });
    await writeFile(
      path.join(workspace, 'Packages', 'manifest.json'),
      JSON.stringify({
        dependencies: {
          'com.ivanmurzak.unity.mcp': '0.79.0',
        },
      }, null, 2),
    );
    await writeFile(
      path.join(workspace, 'UserSettings', 'AI-Game-Developer-Config.json'),
      JSON.stringify({
        host: 'http://localhost:6501',
        authOption: 'none',
        transportMethod: 'streamableHttp',
      }, null, 2),
    );
    await writeFile(
      path.join(workspace, '.vscode', 'mcp.json'),
      JSON.stringify({
        servers: {
          'ai-game-developer': {},
        },
      }, null, 2),
    );

    const status = await inspectWorkspaceStatus(workspace, 'IncompleteConfig', 'trusted');

    expect(status.mcpConfigExists).toBe(true);
    expect(status.mcpServerConfigured).toBe(false);
    expect(status.mcpServerTransport).toBeUndefined();
    expect(status.unityMcpProjectConfigReady).toBe(true);
    expect(
      status.warnings.some((warning) =>
        warning.includes('missing a supported transport type')),
    ).toBe(true);
    expect(status.recommendedActions).toEqual(['configure-vscode-mcp', 'open-unity-with-mcp']);
  });

  it('requires a url for an http ai-game-developer MCP entry', async () => {
    const workspace = await createReadyWorkspace();
    await mkdir(path.join(workspace, '.vscode'), { recursive: true });
    await writeFile(
      path.join(workspace, '.vscode', 'mcp.json'),
      JSON.stringify({
        servers: {
          'ai-game-developer': {
            type: 'http',
          },
        },
      }, null, 2),
    );

    const status = await inspectWorkspaceStatus(workspace, 'MissingHttpUrl', 'trusted');

    expect(status.mcpConfigExists).toBe(true);
    expect(status.mcpServerConfigured).toBe(false);
    expect(status.mcpServerTransport).toBe('http');
    expect(
      status.warnings.some((warning) =>
        warning.includes('missing a url for http transport')),
    ).toBe(true);
    expect(status.recommendedActions).toEqual(['configure-vscode-mcp', 'open-unity-with-mcp']);
  });

  it('requires a command for a stdio ai-game-developer MCP entry', async () => {
    const workspace = await createReadyWorkspace();
    await mkdir(path.join(workspace, '.vscode'), { recursive: true });
    await writeFile(
      path.join(workspace, '.vscode', 'mcp.json'),
      JSON.stringify({
        servers: {
          'ai-game-developer': {
            type: 'stdio',
          },
        },
      }, null, 2),
    );

    const status = await inspectWorkspaceStatus(workspace, 'MissingStdioCommand', 'trusted');

    expect(status.mcpConfigExists).toBe(true);
    expect(status.mcpServerConfigured).toBe(false);
    expect(status.mcpServerTransport).toBe('stdio');
    expect(
      status.warnings.some((warning) =>
        warning.includes('missing a command for stdio transport')),
    ).toBe(true);
    expect(status.recommendedActions).toEqual(['configure-vscode-mcp', 'open-unity-with-mcp']);
  });

  it('treats a malformed Unity MCP project config as present but not ready', async () => {
    const workspace = await createTempWorkspace();
    await mkdir(path.join(workspace, 'Assets'), { recursive: true });
    await mkdir(path.join(workspace, 'ProjectSettings'), { recursive: true });
    await mkdir(path.join(workspace, 'Packages'), { recursive: true });
    await mkdir(path.join(workspace, 'UserSettings'), { recursive: true });
    await writeFile(
      path.join(workspace, 'Packages', 'manifest.json'),
      JSON.stringify({
        dependencies: {
          'com.ivanmurzak.unity.mcp': '0.79.0',
        },
      }, null, 2),
    );
    await writeFile(
      path.join(workspace, 'UserSettings', 'AI-Game-Developer-Config.json'),
      '{broken json',
    );

    const status = await inspectWorkspaceStatus(workspace, 'BrokenUnityConfig', 'trusted');

    expect(status.unityMcpProjectConfigExists).toBe(true);
    expect(status.unityMcpProjectConfigReady).toBe(false);
    expect(
      status.warnings.some((warning) =>
        warning.includes('Could not parse UserSettings/AI-Game-Developer-Config.json')),
    ).toBe(true);
    expect(
      status.warnings.some((warning) =>
        warning.includes('invalid or incomplete')),
    ).toBe(true);
    expect(status.recommendedActions).toEqual(['open-unity-without-mcp']);
  });
});

async function createTempWorkspace(): Promise<string> {
  const tempDir = await mkdtemp(path.join(os.tmpdir(), 'unity-mcp-vscode-'));
  tempRoots.push(tempDir);
  return tempDir;
}

async function createReadyWorkspace(): Promise<string> {
  const workspace = await createTempWorkspace();
  await mkdir(path.join(workspace, 'Assets'), { recursive: true });
  await mkdir(path.join(workspace, 'ProjectSettings'), { recursive: true });
  await mkdir(path.join(workspace, 'Packages'), { recursive: true });
  await mkdir(path.join(workspace, 'UserSettings'), { recursive: true });
  await writeFile(
    path.join(workspace, 'Packages', 'manifest.json'),
    JSON.stringify({
      dependencies: {
        'com.ivanmurzak.unity.mcp': '0.79.0',
      },
    }, null, 2),
  );
  await writeFile(
    path.join(workspace, 'UserSettings', 'AI-Game-Developer-Config.json'),
    JSON.stringify({
      host: 'http://localhost:6501',
      authOption: 'none',
      transportMethod: 'streamableHttp',
    }, null, 2),
  );

  return workspace;
}
