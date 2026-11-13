# Version Management

This project uses an automated Python script (stdlib only, Python 3.9+) to handle version bumping across multiple files (C# constants, JSON manifests, README links).

## The Bump Script

Located at: `commands/bump_version.py`

### Usage

**Preview changes (Dry Run):**
```bash
python commands/bump_version.py --new-version 0.36.0 --what-if
```

**Apply changes:**
```bash
python commands/bump_version.py --new-version 0.36.0
```

### What gets updated?

The script automatically finds and replaces version strings in:
1.  **`Installer/Assets/com.IvanMurzak/AI Game Dev Installer/Installer.cs`**: Updates the embedded version constant in the installer.
2.  **`Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json`**: Updates the Unity Package version (this file is also the source of the current version).
3.  **`Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Runtime/UnityMcpPlugin.cs`**: Updates the runtime plugin version constant.
4.  **`cli/package.json`**: Updates the `unity-mcp-cli` npm package version.
5.  **`cli/package-lock.json`**: Updates the two root-level `unity-mcp-cli` version fields.

> **MCP server version is separate.** The MCP server is no longer part of this repo and is NOT
> bumped with the plugin. The plugin consumes the shared
> [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) release pinned by the
> `ServerVersion` constant in
> `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/McpServerManager.cs`.
> Bumping the consumed server = changing that constant; the corresponding `v<ServerVersion>`
> release (with all 7 RID zips) must already exist on GameDev-MCP-Server.

## Workflow for Release

1.  **Commit** all current changes.
2.  Run the bump script: `python commands/bump_version.py --new-version X.Y.Z`
3.  **Verify** the changes (git diff).
4.  **Commit** the version bump, open a PR and merge it to `main`.
5.  **Release** by manually dispatching `.github/workflows/release.yml`; merging the bump publishes nothing,
    and the workflow refuses a version whose tag already exists.