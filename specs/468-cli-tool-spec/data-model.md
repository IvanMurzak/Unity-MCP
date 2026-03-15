# Data Model: Unity-MCP CLI Tool

**Branch**: `468-cli-tool-spec` | **Date**: 2026-03-14

## Entities

### Unity Project

A directory on the file system containing a Unity project structure.

| Attribute | Type | Description |
|-----------|------|-------------|
| path | string | Absolute file system path to the project root |
| editorVersion | string | Unity Editor version from `ProjectSettings/ProjectVersion.txt` (e.g., `2022.3.10f1`) |
| hasManifest | boolean | Whether `Packages/manifest.json` exists |
| hasPlugin | boolean | Whether Unity-MCP plugin is present in manifest |
| pluginVersion | string? | Current plugin version if installed, null otherwise |

**Validation rules**:
- `path` must be an existing directory
- `path` must contain `Assets/` directory (minimum Unity project marker)
- `editorVersion` must match Unity version format: `YYYY.N.NNfN` or `YYYY.N.NNaN` or `YYYY.N.NNbN`

### Unity Editor

An installed Unity Editor version managed by Unity Hub.

| Attribute | Type | Description |
|-----------|------|-------------|
| version | string | Version identifier (e.g., `2022.3.10f1`) |
| path | string | Absolute path to the editor executable |
| isInstalled | boolean | Whether this version is currently installed |

**Identity**: Uniquely identified by version string.

**Validation rules**:
- Version string must match pattern: `MAJOR.MINOR.PATCHfREVISION` (also `a` for alpha, `b` for beta)

### Unity Hub

The Unity Hub application used to manage editor installations.

| Attribute | Type | Description |
|-----------|------|-------------|
| isInstalled | boolean | Whether Unity Hub is present on the machine |
| cliPath | string? | Path to the Unity Hub CLI executable |
| installedEditors | UnityEditor[] | List of editors installed via Hub |

**Platform-specific discovery**:
- Windows: Registry + default install paths
- macOS: `/Applications/Unity Hub.app`
- Linux: AppImage or snap locations

### Package Manifest

The `Packages/manifest.json` file within a Unity project.

| Attribute | Type | Description |
|-----------|------|-------------|
| dependencies | Record<string, string> | Package name → version mappings |
| scopedRegistries | ScopedRegistry[] | Custom package registries (e.g., OpenUPM) |

**State transitions**:
- `no-plugin` → `plugin-installed`: `install-plugin` adds dependency + scoped registry
- `plugin-installed` → `no-plugin`: `remove-plugin` removes dependency, preserves registry if other scopes use it
- `plugin-installed` → `plugin-upgraded`: `install-plugin` updates version (never downgrades)

### MCP Configuration

The `UserSettings/AI-Game-Developer-Config.json` file.

| Attribute | Type | Description |
|-----------|------|-------------|
| tools | Record<string, boolean> | Tool name → enabled/disabled |
| prompts | Record<string, boolean> | Prompt name → enabled/disabled |
| resources | Record<string, boolean> | Resource name → enabled/disabled |
| port | number | Deterministic port derived from project path |

### Connection Settings

Environment variables set when launching Unity with MCP connection.

| Variable | Type | Description |
|----------|------|-------------|
| UNITY_MCP_HOST | string | MCP server URL |
| UNITY_MCP_TOOLS | string? | Comma-separated tool names to enable |
| UNITY_MCP_TOKEN | string? | Authentication token |
| UNITY_MCP_AUTH_OPTION | "none" \| "required" | Authentication mode |
| UNITY_MCP_KEEP_CONNECTED | "true" \| "false" | Persistent connection flag |
| UNITY_MCP_TRANSPORT | "streamableHttp" \| "stdio" | Transport protocol |
| UNITY_MCP_START_SERVER | "true" \| "false" | Whether to start MCP server |

## Relationships

```text
Unity Hub ─── manages ──→ Unity Editor (1:N)
Unity Editor ─── creates ──→ Unity Project (1:N)
Unity Project ─── contains ──→ Package Manifest (1:1)
Unity Project ─── contains ──→ MCP Configuration (1:1)
Package Manifest ─── references ──→ Plugin (0..1)
Unity Project ─── launched with ──→ Connection Settings (0..1)
```
