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
- The **MCP Plugin** auto-starts the server binary on Unity Editor load (`[InitializeOnLoad]`). Port is deterministic: SHA256 hash of project path, mapped to 20000–29999.
- Communication inside Unity always runs on the **main thread** via `MainThread.Instance.Run()`.

## Release & Versioning

Version is sourced from [Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json](Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json). Bump with `.\commands\bump-version.ps1 <version>`.

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
- Namespace pattern: `com.IvanMurzak.Unity.MCP.[Tier].[Component]` for all code, except AI-facing data models exposed in JSON Schemas (`Runtime/Data/*` classes) which use the short `Unity.MCP.Data` namespace
- **No Reflection for private access** — C# Reflection (`System.Reflection`) MUST NOT be used to access private, internal, or non-public members. Exception: `ReflectorNet` library usage is allowed.

## Project Constitution

Non-negotiable principles and architecture constraints: [`.specify/memory/constitution.md`](.specify/memory/constitution.md)

**Code review**: You MUST read the constitution before performing any code review. It contains critical rules for code quality, safety, and project governance that all reviews must verify against.

## Documentation

```
README.md                                                        ← Root project documentation
Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/README.md                           ← Copy of root README.md *
Installer/Assets/com.IvanMurzak/AI Game Dev Installer/README.md  ← Copy of root README.md *
docs/
├── README.es.md                                                 ← Spanish translation of root README *
├── README.ja.md                                                 ← Japanese translation *
└── README.zh-CN.md                                              ← Simplified Chinese translation *
Unity-MCP-Server/README.md                                       ← MCP Server documentation
Unity-MCP-Server/MCP-Test-Client/README.md                       ← Test client documentation
cli/README.md                                                    ← CLI documentation
cli/docs/
├── README.es.md                                                 ← Spanish translation of CLI README
├── README.ja.md                                                 ← Japanese translation
└── README.zh-CN.md                                              ← Simplified Chinese translation
```

Files marked with `*` MUST be kept in sync with `README.md` when it changes.
