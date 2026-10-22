import type * as vscode from 'vscode';
import { describe, expect, it, vi } from 'vitest';
import {
  buildDashboardActions,
  buildStatusBarPresentation,
  isAllowedDashboardCommand,
  type DashboardSnapshot,
} from './dashboard';
import type { WorkspaceStatus } from './projectStatus';

vi.mock('vscode', () => ({}));

describe('buildStatusBarPresentation', () => {
  it('shows install state when the Unity MCP plugin is missing', () => {
    const presentation = buildStatusBarPresentation(
      createSnapshot({
        pluginInstalled: false,
        unityMcpProjectConfigExists: false,
        unityMcpProjectConfigReady: false,
        mcpServerConfigured: false,
      }),
    );

    expect(presentation.text).toBe('$(package) Unity MCP: Install');
  });

  it('shows initialization state when the plugin is installed but the Unity config is missing', () => {
    const presentation = buildStatusBarPresentation(
      createSnapshot({
        pluginInstalled: true,
        unityMcpProjectConfigExists: false,
        unityMcpProjectConfigReady: false,
        mcpServerConfigured: false,
      }),
    );

    expect(presentation.text).toBe('$(sync~spin) Unity MCP: Init');
  });

  it('shows ready state when the workspace is fully configured', () => {
    const presentation = buildStatusBarPresentation(
      createSnapshot({
        pluginInstalled: true,
        unityMcpProjectConfigExists: true,
        unityMcpProjectConfigReady: true,
        mcpServerConfigured: true,
      }),
    );

    expect(presentation.text).toBe('$(check) Unity MCP: Ready');
  });
});

describe('buildDashboardActions', () => {
  it('prioritizes trust management for restricted workspaces', () => {
    const actions = buildDashboardActions(
      createSnapshot({
        trustState: 'restricted',
        recommendedActions: ['trust-workspace'],
      }),
    );

    expect(actions.map((action) => action.commandId)).toEqual([
      'workbench.trust.manage',
      'unityMcp.checkStatus',
      'unityMcp.showOutput',
    ]);
  });

  it('prioritizes connected launch for ready projects', () => {
    const actions = buildDashboardActions(
      createSnapshot({
        pluginInstalled: true,
        unityMcpProjectConfigExists: true,
        unityMcpProjectConfigReady: true,
        mcpServerConfigured: true,
        recommendedActions: ['open-unity-with-mcp'],
      }),
    );

    expect(actions[0]?.commandId).toBe('unityMcp.openUnityConnected');
    expect(actions.some((action) => action.commandId === 'unityMcp.openUnityPlain')).toBe(true);
  });

  it('prioritizes configuration when VS Code MCP is not configured yet', () => {
    const actions = buildDashboardActions(
      createSnapshot({
        pluginInstalled: true,
        unityMcpProjectConfigExists: true,
        unityMcpProjectConfigReady: true,
        mcpServerConfigured: false,
        recommendedActions: ['configure-vscode-mcp', 'open-unity-with-mcp'],
      }),
    );

    expect(actions[0]?.commandId).toBe('unityMcp.configureProject');
  });

  it('shows a fix-config state when the Unity config file exists but is not ready', () => {
    const presentation = buildStatusBarPresentation(
      createSnapshot({
        pluginInstalled: true,
        unityMcpProjectConfigExists: true,
        unityMcpProjectConfigReady: false,
        mcpServerConfigured: false,
        recommendedActions: ['open-unity-without-mcp'],
      }),
    );

    expect(presentation.text).toBe('$(warning) Unity MCP: Fix Config');
  });

  it('only emits commands from the dashboard allowlist', () => {
    const actions = buildDashboardActions(createSnapshot({}));

    expect(actions.every((action) => isAllowedDashboardCommand(action.commandId))).toBe(true);
    expect(isAllowedDashboardCommand('workbench.action.closeAllEditors')).toBe(false);
  });
});

function createSnapshot(overrides: Partial<WorkspaceStatus>): DashboardSnapshot {
  return {
    workspaceFolder: {
      name: 'TestProject',
      index: 0,
      uri: { fsPath: '/tmp/TestProject' } as vscode.Uri,
    } as vscode.WorkspaceFolder,
    status: createStatus(overrides),
  };
}

function createStatus(overrides: Partial<WorkspaceStatus>): WorkspaceStatus {
  return {
    workspaceName: 'TestProject',
    workspacePath: '/tmp/TestProject',
    trustState: 'trusted',
    unityProjectDetected: true,
    unityMarkers: ['Assets/', 'ProjectSettings/', 'Packages/manifest.json'],
    pluginInstalled: true,
    pluginVersion: '0.79.0',
    unityMcpProjectConfigExists: true,
    unityMcpProjectConfigReady: true,
    mcpConfigExists: true,
    mcpServerConfigured: true,
    mcpServerTransport: 'http',
    warnings: [],
    recommendedActions: ['open-unity-with-mcp'],
    ...overrides,
  };
}
