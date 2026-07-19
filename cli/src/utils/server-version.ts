// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

/**
 * The pinned GameDev-MCP-Server version the CLI fetches by default with
 * `install-plugin --with-server`.
 *
 * Kept in LOCKSTEP with the Unity plugin's `ServerVersion` constant in
 *   Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/McpServerManager.cs
 * so the CLI-downloaded binary and the plugin-downloaded binary can never drift. A drift guard
 * in `tests/server-version.test.ts` reads that C# constant and asserts the two are byte-equal.
 * Override at runtime with `--server-version <v>`.
 *
 * The CLI mirrors the constant rather than reading the `.cs` at runtime because the published npm
 * package ships no plugin sources — the plugin release cadence bumps both in the same PR, and the
 * drift-guard test fails CI if a bump touches only one side.
 */
export const DEFAULT_SERVER_VERSION = '9.1.1';

/** GitHub `owner/repo` that hosts the shared server's tagged releases + per-RID zips. */
export const SERVER_RELEASE_REPO = 'IvanMurzak/GameDev-MCP-Server';

/** Base executable name (no extension) of the shared server binary. */
export const SERVER_EXECUTABLE_NAME = 'gamedev-mcp-server';

/**
 * The Git release TAG for a server version: the version with a leading `v` (e.g. `9.0.0` →
 * `v9.0.0`). GameDev-MCP-Server tags every release `v<version>` and the per-RID zips + the
 * `SHA256SUMS` manifest are attached to THAT tag — so the download path MUST use the v-prefixed
 * tag (a bare-version path 404s). Already-v-prefixed input is passed through unchanged so a caller
 * cannot accidentally double-prefix. Mirrors the C# `McpServerManager.ServerReleaseTag`.
 */
export function serverReleaseTag(version: string): string {
  const v = (version ?? '').trim();
  return v.startsWith('v') ? v : `v${v}`;
}
