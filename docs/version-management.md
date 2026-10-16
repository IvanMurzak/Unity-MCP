# Version Management

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/aigamedeveloper/mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/aigamedeveloper/mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

This project uses an automated PowerShell script to handle version bumping across multiple files (C# constants, JSON manifests, README links).

## The Bump Script

Located at: `commands/bump-version.ps1`

### Usage

**Preview changes (Dry Run):**
```powershell
.\commands\bump-version.ps1 -NewVersion "0.36.0" -WhatIf
```

**Apply changes:**
```powershell
.\commands\bump-version.ps1 -NewVersion "0.36.0"
```

### What gets updated?

The script automatically finds and replaces version strings in:
1.  **`Installer/Assets/com.IvanMurzak/AI Game Dev Installer/Installer.cs`**: Updates the embedded version constant in the installer.
2.  **`Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json`**: Updates the Unity Package version (this file is also the source of the current version).
3.  **`Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Runtime/UnityMcpPlugin.cs`**: Updates the runtime plugin version constant.
4.  **`cli/package.json`**: Updates the `unity-mcp-cli` npm package version.

> **MCP server version is separate.** The MCP server is no longer part of this repo and is NOT
> bumped with the plugin. The plugin consumes the shared
> [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) release pinned by the
> `ServerVersion` constant in
> `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/McpServerManager.cs`.
> Bumping the consumed server = changing that constant; the corresponding `v<ServerVersion>`
> release (with all 7 RID zips) must already exist on GameDev-MCP-Server.

## Workflow for Release

1.  **Commit** all current changes.
2.  Run the bump script: `.\commands\bump-version.ps1 -NewVersion "X.Y.Z"`
3.  **Verify** the changes (git diff).
4.  **Commit** the version bump: `git commit -am "chore: bump version to X.Y.Z"`
5.  **Tag** the release: `git tag X.Y.Z`
6.  **Push**: `git push && git push --tags`

> The GitHub Actions workflow will automatically build the Release and Unity Package when a new Tag is pushed.