import { describe, expect, it, vi } from 'vitest';
import { configureVscodeProject, installUnityMcpPlugin } from './cliAdapter';
import { ExtensionLogger } from './logging';

describe('configureVscodeProject', () => {
  it('calls setupMcp for the vscode-copilot agent', async () => {
    const setupMcp = vi.fn().mockResolvedValue({
      kind: 'success',
      success: true,
      agentId: 'vscode-copilot',
      configPath: '/tmp/project/.vscode/mcp.json',
      transport: 'http',
      warnings: [],
      nextSteps: [],
    });

    const logger = createLogger();
    const result = await configureVscodeProject(
      logger,
      {
        workspacePath: '/tmp/project',
        transport: 'http',
      },
      async () => ({
        setupMcp,
        listAgentIds: () => ['vscode-copilot'],
      }),
    );

    expect(setupMcp).toHaveBeenCalledWith(
      expect.objectContaining({
        agentId: 'vscode-copilot',
        unityProjectPath: '/tmp/project',
        transport: 'http',
      }),
    );
    expect(result.kind).toBe('success');
  });

  it('returns a failure when the adapter cannot load the CLI module', async () => {
    const logger = createLogger();
    const result = await configureVscodeProject(
      logger,
      {
        workspacePath: '/tmp/project',
        transport: 'http',
      },
      async () => {
        throw new Error('load failed');
      },
    );

    expect(result.kind).toBe('failure');
    if (result.kind === 'failure') {
      expect(result.error.message).toContain('Could not load unity-mcp-cli');
    }
  });

  it('calls installPlugin through the shared CLI loader', async () => {
    const installPlugin = vi.fn().mockResolvedValue({
      kind: 'success',
      success: true,
      installedVersion: '0.79.0',
      manifestPath: '/tmp/project/Packages/manifest.json',
      warnings: [],
      nextSteps: ['Open the Unity project in the Editor to complete installation.'],
    });

    const logger = createLogger();
    const result = await installUnityMcpPlugin(
      logger,
      {
        workspacePath: '/tmp/project',
      },
      async () => ({
        installPlugin,
        setupMcp: vi.fn(),
        listAgentIds: () => ['vscode-copilot'],
      }),
    );

    expect(installPlugin).toHaveBeenCalledWith(
      expect.objectContaining({
        unityProjectPath: '/tmp/project',
      }),
    );
    expect(result.kind).toBe('success');
  });
});

function createLogger(): ExtensionLogger {
  return {
    dispose(): void {},
    show(): void {},
    error(): void {},
    warn(): void {},
    info(): void {},
    debug(): void {},
    appendReport(): void {},
  } as unknown as ExtensionLogger;
}
