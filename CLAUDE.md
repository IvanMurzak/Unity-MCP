# CLAUDE.md

## Repository Overview

Unity-MCP bridges LLMs (Claude, Cursor, Copilot, etc.) with Unity Editor and Runtime via the [Model Context Protocol](https://modelcontextprotocol.io/). It consists of these sub-projects:

| Sub-project | Location | Description |
|---|---|---|
| MCP Server | [Unity-MCP-Server/](Unity-MCP-Server/) | C# ASP.NET Core server — bridges MCP clients and Unity Plugin via SignalR |
| MCP Plugin | [Unity-MCP-Plugin/](Unity-MCP-Plugin/) | Unity Editor/Runtime plugin — executes MCP tools and manages connection |
| CLI | [cli/](cli/) | Command-line interface for Unity-MCP |
| Installer | [Installer/](Installer/) | Unity package that installs the plugin and its dependencies |
| Unity-Tests | [Unity-Tests/](Unity-Tests/) | Test project for Unity-MCP |

## System Architecture

```
MCP Client (Claude/Cursor/etc.)
        ↕ stdio or streamableHttp
  Unity-MCP-Server  (ASP.NET Core + MCP SDK)
        ↕ SignalR
  Unity-MCP-Plugin  (Unity Editor/Runtime)
        ↕ Unity API (main thread)
      Unity Engine
```

- The **MCP Server** is a standalone binary downloaded automatically by the plugin to `Library/mcp-server/{platform}/`. It is also published to Docker Hub and NuGet.
- The **MCP Plugin** auto-starts the server binary on Unity Editor load (`[InitializeOnLoad]`). Port is deterministic: SHA256 hash of project path, mapped to 50000–59999.
- Communication inside Unity always runs on the **main thread** via `MainThread.Instance.Run()`.

## Release & Versioning

Version is sourced from [Unity-MCP-Plugin/Assets/root/package.json](Unity-MCP-Plugin/Assets/root/package.json). Bump with `.\commands\bump-version.ps1 <version>`.

## CI/CD

See `.github/workflows/` for all pipelines. PRs from untrusted contributors require a `ci-ok` label from a maintainer before CI runs.

## Key Coding Conventions

These apply across both C# sub-projects:

- `#nullable enable` at the top of every file
- Copyright box comment header in every file
- MCP tool classes are `partial` — one operation per file (e.g., `Tool_GameObject.Create.cs`)
- MCP tools MUST return structured types (data models, `List<T>`, `void`, or `Task`) — avoid raw string returns
- All Unity API calls must use `MainThread.Instance.Run(() => ...)` or `RunAsync()`
- Tool/prompt names use **kebab-case** with category prefix (e.g., `gameobject-create`, `assets-find`)
- Namespace pattern: `com.IvanMurzak.Unity.MCP.[Tier].[Component]`

## Project Constitution

Non-negotiable principles and architecture constraints: [`.specify/memory/constitution.md`](.specify/memory/constitution.md)

## Rules

Important rules that must be followed:

- `./Unity-MCP-Plugin/Assets/root/README.md` must be a copy of `./README.md`.
- `./Unity-MCP-Plugin/Assets/root/README.md` must be translated to related translated versions of this file under `./docs/README.*.md`.
