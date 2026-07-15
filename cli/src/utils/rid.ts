// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

/**
 * Runtime Identifier (RID) detection for the shared GameDev-MCP-Server binary.
 *
 * The RID string `<os>-<arch>` (e.g. `win-x64`, `osx-arm64`) selects which
 * `gamedev-mcp-server-<rid>.zip` release asset to download. Ported from the plugin's C#
 * `McpServerManager.PlatformName` (`OperationSystem` + `-` + `CpuArch`) so the CLI and plugin
 * resolve the same asset for the same host.
 */

/** The exact set of RIDs published as `gamedev-mcp-server-<rid>.zip` on every server release. */
export const KNOWN_RIDS = [
  'linux-arm64',
  'linux-x64',
  'osx-arm64',
  'osx-x64',
  'win-arm64',
  'win-x64',
  'win-x86',
] as const;

export type Rid = (typeof KNOWN_RIDS)[number];

/** Map a Node `process.platform` value to the server's `OperationSystem` token, or null. */
function osToken(platform: NodeJS.Platform): string | null {
  if (platform === 'win32') return 'win';
  if (platform === 'darwin') return 'osx';
  if (platform === 'linux') return 'linux';
  return null;
}

/** Map a Node `process.arch` value to the server's `CpuArch` token, or null. */
function archToken(arch: string): string | null {
  if (arch === 'x64') return 'x64';
  if (arch === 'arm64') return 'arm64';
  if (arch === 'ia32') return 'x86';
  return null;
}

/**
 * Resolve the host RID (defaulting to the current process's platform/arch). Throws a clear,
 * actionable error when the host has no published server build — a fail-closed guard so
 * `install-plugin --with-server` never silently downloads the wrong asset (or a 404 page).
 */
export function resolveHostRid(
  platform: NodeJS.Platform = process.platform,
  arch: string = process.arch,
): Rid {
  const os = osToken(platform);
  const cpu = archToken(arch);
  if (!os || !cpu) {
    throw new Error(
      `Unsupported host platform/architecture (${platform}/${arch}). ` +
        `Supported GameDev-MCP-Server RIDs: ${KNOWN_RIDS.join(', ')}.`,
    );
  }
  const rid = `${os}-${cpu}`;
  if (!(KNOWN_RIDS as readonly string[]).includes(rid)) {
    throw new Error(
      `No GameDev-MCP-Server build exists for host RID '${rid}' (${platform}/${arch}). ` +
        `Supported RIDs: ${KNOWN_RIDS.join(', ')}.`,
    );
  }
  return rid as Rid;
}
