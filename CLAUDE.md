# CLAUDE.md

## What This Is

Unity-MCP bridges LLMs (Claude, Cursor, Copilot, etc.) with Unity Editor and Runtime via the [Model Context Protocol](https://modelcontextprotocol.io/). Sub-projects: `Unity-MCP-Plugin/` (Unity Editor/Runtime plugin), `cli/`, `Installer/`, `Unity-Tests/`.

The MCP server is NOT in this repo — the plugin consumes the shared [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (binary `gamedev-mcp-server`, release assets `gamedev-mcp-server-<rid>.zip`, Docker `aigamedeveloper/mcp-server`) — one engine-agnostic server shared by Unity-MCP, Godot-MCP, and Unreal-MCP. The plugin downloads the release pinned by the `ServerVersion` constant in `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/McpServerManager.cs` (decoupled from the plugin `Version`). **Server version pin:** bumping the consumed server = changing that constant; the pinned `v<ServerVersion>` release (with all 7 RID zips) must already exist on GameDev-MCP-Server BEFORE cutting a plugin release that pins it.

## Build / Run

- Bump version: `.\commands\bump-version.ps1 <version>`
- CI/CD pipelines live in `.github/workflows/`.

## Project Constitution

Non-negotiable principles and architecture constraints: [`.specify/memory/constitution.md`](.specify/memory/constitution.md). You MUST read the constitution before performing any code review.

## Find Detail In

- [docs/claude/architecture.md](docs/claude/architecture.md) — System architecture: SignalR bridge, main-thread execution, deterministic port hashing
- [docs/claude/style.md](docs/claude/style.md) — Coding conventions: `#nullable enable`, no reflection for private access, namespace pattern, copyright headers
- [docs/claude/release.md](docs/claude/release.md) — Release, versioning, CI/CD
- [docs/claude/documentation-sync.md](docs/claude/documentation-sync.md) — README translation/copy sync requirements
- `Unity-MCP-Plugin/CLAUDE.md` — sub-project specifics
