import { promises as fs } from 'node:fs';
import * as path from 'node:path';

const MCP_SERVER_NAME = 'ai-game-developer';
const UNITY_MCP_PACKAGE_NAME = 'com.ivanmurzak.unity.mcp';

export interface WorkspaceStatus {
  workspaceName: string;
  workspacePath: string;
  trustState: 'trusted' | 'restricted';
  unityProjectDetected: boolean;
  unityMarkers: string[];
  pluginInstalled: boolean;
  pluginVersion?: string;
  mcpConfigExists: boolean;
  mcpServerConfigured: boolean;
  mcpServerTransport?: string;
  warnings: string[];
}

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

  const mcpInfo = await readMcpConfig(mcpConfigPath);
  warnings.push(...mcpInfo.warnings);

  return {
    workspaceName,
    workspacePath,
    trustState,
    unityProjectDetected,
    unityMarkers,
    pluginInstalled,
    pluginVersion,
    mcpConfigExists: mcpInfo.exists,
    mcpServerConfigured: mcpInfo.hasServerEntry,
    mcpServerTransport: mcpInfo.transport,
    warnings,
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
    `.vscode/mcp.json present: ${status.mcpConfigExists ? 'yes' : 'no'}`,
    `ai-game-developer configured: ${status.mcpServerConfigured ? 'yes' : 'no'}`,
    `Configured transport: ${status.mcpServerTransport ?? 'unknown'}`,
  ];

  if (status.warnings.length > 0) {
    lines.push('Warnings:');
    for (const warning of status.warnings) {
      lines.push(`- ${warning}`);
    }
  }

  return lines.join('\n');
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
      servers?: Record<string, { type?: string }>;
    };

    const serverEntry = parsed.servers?.[MCP_SERVER_NAME];

    return {
      exists: true,
      hasServerEntry: Boolean(serverEntry),
      transport: serverEntry?.type,
      warnings: [],
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

async function pathExists(targetPath: string): Promise<boolean> {
  try {
    await fs.access(targetPath);
    return true;
  } catch {
    return false;
  }
}

function toErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
