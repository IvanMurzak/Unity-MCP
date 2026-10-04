import { promises as fs } from 'node:fs';
import * as path from 'node:path';
import { readUnityMcpProjectConfig } from './unityConfig';
import { pathExists, toErrorMessage } from './utils';

const MCP_SERVER_NAME = 'ai-game-developer';
const UNITY_MCP_PACKAGE_NAME = 'com.ivanmurzak.unity.mcp';
const VALID_VSCODE_MCP_TRANSPORTS = new Set(['http', 'stdio']);

export interface WorkspaceStatus {
  workspaceName: string;
  workspacePath: string;
  trustState: 'trusted' | 'restricted';
  unityProjectDetected: boolean;
  unityMarkers: string[];
  pluginInstalled: boolean;
  pluginVersion?: string;
  unityMcpProjectConfigExists: boolean;
  unityMcpProjectConfigReady: boolean;
  mcpConfigExists: boolean;
  mcpServerConfigured: boolean;
  mcpServerTransport?: string;
  warnings: string[];
  recommendedActions: WorkspaceAction[];
}

export type WorkspaceAction =
  | 'trust-workspace'
  | 'install-plugin'
  | 'open-unity-without-mcp'
  | 'configure-vscode-mcp'
  | 'open-unity-with-mcp';

export async function inspectWorkspaceStatus(
  workspacePath: string,
  workspaceName: string,
  trustState: 'trusted' | 'restricted',
): Promise<WorkspaceStatus> {
  const warnings: string[] = [];
  const unityMarkers: string[] = [];

  const assetsPath = path.join(workspacePath, 'Assets');
  const projectSettingsPath = path.join(workspacePath, 'ProjectSettings');
  const packageManifestPath = path.join(workspacePath, 'Packages', 'manifest.json');
  const mcpConfigPath = path.join(workspacePath, '.vscode', 'mcp.json');

  const hasAssets = await pathExists(assetsPath);
  if (hasAssets) {
    unityMarkers.push('Assets/');
  }

  const hasProjectSettings = await pathExists(projectSettingsPath);
  if (hasProjectSettings) {
    unityMarkers.push('ProjectSettings/');
  }

  const manifestInfo = await readPackageManifest(packageManifestPath);
  if (manifestInfo.exists) {
    unityMarkers.push('Packages/manifest.json');
  }
  warnings.push(...manifestInfo.warnings);

  const unityProjectDetected = hasAssets && (hasProjectSettings || manifestInfo.exists);

  const pluginVersion = manifestInfo.dependencies[UNITY_MCP_PACKAGE_NAME];
  const pluginInstalled = typeof pluginVersion === 'string' && pluginVersion.length > 0;
  const projectConfig = await readUnityMcpProjectConfig(workspacePath);
  warnings.push(...projectConfig.warnings);

  if (pluginInstalled && !projectConfig.exists) {
    warnings.push(
      'Unity MCP project config is missing. Open Unity once without MCP after installing the plugin so the package can import and initialize.',
    );
  } else if (pluginInstalled && !projectConfig.ready) {
    warnings.push(
      'Unity MCP project config is present but invalid or incomplete. Open Unity without MCP and fix or regenerate UserSettings/AI-Game-Developer-Config.json before retrying connected launch.',
    );
  }

  const mcpInfo = await readMcpConfig(mcpConfigPath);
  warnings.push(...mcpInfo.warnings);
  const recommendedActions = buildRecommendedActions({
    trustState,
    unityProjectDetected,
    pluginInstalled,
    unityMcpProjectConfigReady: projectConfig.ready,
    mcpServerConfigured: mcpInfo.hasServerEntry,
  });

  return {
    workspaceName,
    workspacePath,
    trustState,
    unityProjectDetected,
    unityMarkers,
    pluginInstalled,
    pluginVersion,
    unityMcpProjectConfigExists: projectConfig.exists,
    unityMcpProjectConfigReady: projectConfig.ready,
    mcpConfigExists: mcpInfo.exists,
    mcpServerConfigured: mcpInfo.hasServerEntry,
    mcpServerTransport: mcpInfo.transport,
    warnings,
    recommendedActions,
  };
}

export function formatWorkspaceStatusReport(status: WorkspaceStatus): string {
  const lines = [
    `Workspace: ${status.workspaceName}`,
    `Path: ${status.workspacePath}`,
    `Workspace trust: ${status.trustState}`,
    `Unity project detected: ${status.unityProjectDetected ? 'yes' : 'no'}`,
    `Unity markers: ${status.unityMarkers.length > 0 ? status.unityMarkers.join(', ') : 'none'}`,
    `Unity MCP plugin installed: ${status.pluginInstalled ? `yes (${status.pluginVersion ?? 'unknown version'})` : 'no'}`,
    `Unity MCP project config present: ${status.unityMcpProjectConfigExists ? 'yes' : 'no'}`,
    `Unity MCP project config ready: ${status.unityMcpProjectConfigReady ? 'yes' : 'no'}`,
    `.vscode/mcp.json present: ${status.mcpConfigExists ? 'yes' : 'no'}`,
    `ai-game-developer configured: ${status.mcpServerConfigured ? 'yes' : 'no'}`,
    `Configured transport: ${status.mcpServerTransport ?? 'unknown'}`,
  ];

  if (status.recommendedActions.length > 0) {
    lines.push('Recommended next actions:');
    for (const action of status.recommendedActions) {
      lines.push(`- ${describeAction(action)}`);
    }
  }

  if (status.warnings.length > 0) {
    lines.push('Warnings:');
    for (const warning of status.warnings) {
      lines.push(`- ${warning}`);
    }
  }

  return lines.join('\n');
}

function buildRecommendedActions(input: {
  trustState: 'trusted' | 'restricted';
  unityProjectDetected: boolean;
  pluginInstalled: boolean;
  unityMcpProjectConfigReady: boolean;
  mcpServerConfigured: boolean;
}): WorkspaceAction[] {
  if (input.trustState !== 'trusted') {
    return ['trust-workspace'];
  }

  if (!input.unityProjectDetected) {
    return [];
  }

  const actions: WorkspaceAction[] = [];

  if (!input.pluginInstalled) {
    actions.push('install-plugin');
    return actions;
  }

  if (!input.unityMcpProjectConfigReady) {
    actions.push('open-unity-without-mcp');
    return actions;
  }

  if (!input.mcpServerConfigured) {
    actions.push('configure-vscode-mcp');
  }

  actions.push('open-unity-with-mcp');
  return actions;
}

function describeAction(action: WorkspaceAction): string {
  switch (action) {
    case 'trust-workspace':
      return 'Trust this workspace before running Unity MCP write or launch actions.';
    case 'install-plugin':
      return 'Run "Unity MCP: Install Plugin".';
    case 'open-unity-without-mcp':
      return 'Run "Unity MCP: Open Unity" and choose "Open Unity" once so the package can initialize.';
    case 'configure-vscode-mcp':
      return 'Run "Unity MCP: Configure Project".';
    case 'open-unity-with-mcp':
      return 'Run "Unity MCP: Open Unity" and choose "Open Unity With MCP Connection".';
  }
}

interface PackageManifestInfo {
  exists: boolean;
  dependencies: Record<string, string>;
  warnings: string[];
}

interface McpConfigInfo {
  exists: boolean;
  hasServerEntry: boolean;
  transport?: string;
  warnings: string[];
}

async function readPackageManifest(packageManifestPath: string): Promise<PackageManifestInfo> {
  if (!(await pathExists(packageManifestPath))) {
    return { exists: false, dependencies: {}, warnings: [] };
  }

  try {
    const raw = await fs.readFile(packageManifestPath, 'utf8');
    const parsed = JSON.parse(raw) as { dependencies?: Record<string, string> };
    return {
      exists: true,
      dependencies: parsed.dependencies ?? {},
      warnings: [],
    };
  } catch (error) {
    return {
      exists: true,
      dependencies: {},
      warnings: [
        `Could not parse Packages/manifest.json: ${toErrorMessage(error)}`,
      ],
    };
  }
}

async function readMcpConfig(mcpConfigPath: string): Promise<McpConfigInfo> {
  if (!(await pathExists(mcpConfigPath))) {
    return {
      exists: false,
      hasServerEntry: false,
      warnings: [],
    };
  }

  try {
    const raw = await fs.readFile(mcpConfigPath, 'utf8');
    const parsed = JSON.parse(raw) as {
      servers?: Record<string, { type?: string; url?: string; command?: string }>;
    };

    const serverEntry = parsed.servers?.[MCP_SERVER_NAME];
    const transport = parseMcpTransport(serverEntry?.type);
    const warnings: string[] = [];

    if (serverEntry && transport === undefined) {
      warnings.push(
        `The ai-game-developer server entry in .vscode/mcp.json is missing a supported transport type. Expected one of: ${Array.from(VALID_VSCODE_MCP_TRANSPORTS).join(', ')}.`,
      );
    }

    const hasServerEntry =
      transport === 'http'
        ? typeof serverEntry?.url === 'string' && serverEntry.url.trim().length > 0
        : transport === 'stdio'
          ? typeof serverEntry?.command === 'string' && serverEntry.command.trim().length > 0
          : false;

    if (transport === 'http' && serverEntry && !hasServerEntry) {
      warnings.push('The ai-game-developer server entry in .vscode/mcp.json is missing a url for http transport.');
    }

    if (transport === 'stdio' && serverEntry && !hasServerEntry) {
      warnings.push('The ai-game-developer server entry in .vscode/mcp.json is missing a command for stdio transport.');
    }

    return {
      exists: true,
      hasServerEntry,
      transport,
      warnings,
    };
  } catch (error) {
    return {
      exists: true,
      hasServerEntry: false,
      warnings: [
        `Could not parse .vscode/mcp.json: ${toErrorMessage(error)}`,
      ],
    };
  }
}

function parseMcpTransport(value: unknown): string | undefined {
  return typeof value === 'string' && VALID_VSCODE_MCP_TRANSPORTS.has(value)
    ? value
    : undefined;
}
