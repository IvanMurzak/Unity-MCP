import { describe, expect, it, vi } from 'vitest';
import { configureVscodeProject } from './cliAdapter';
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
