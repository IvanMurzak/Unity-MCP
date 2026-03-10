# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

Unity-MCP bridges LLMs (Claude, Cursor, Copilot, etc.) with Unity Editor and Runtime via the [Model Context Protocol](https://modelcontextprotocol.io/). It consists of three sub-projects with their own CLAUDE.md files:

| Sub-project | Location | Description |
|---|---|---|
| 🔹 MCP Server | [Unity-MCP-Server/](Unity-MCP-Server/) | C# ASP.NET Core server — bridges MCP clients ↔ Unity Plugin via SignalR |
| 🔸 MCP Plugin | [Unity-MCP-Plugin/](Unity-MCP-Plugin/) | Unity Editor/Runtime plugin — executes MCP tools and manages connection |
| ◾ Installer | [Installer/](Installer/) | Unity package that installs the plugin and its dependencies |

**See detailed guidance in each sub-project's CLAUDE.md:**
- [Unity-MCP-Server/CLAUDE.md](Unity-MCP-Server/CLAUDE.md) — build commands, server architecture, transport config
- [Unity-MCP-Plugin/CLAUDE.md](Unity-MCP-Plugin/CLAUDE.md) — Unity architecture, tool/prompt/resource patterns, testing

## System Architecture

```
MCP Client (Claude/Cursor/etc.)
        ↕ stdio or streamableHttp
  Unity-MCP-Server  (ASP.NET Core + MCP SDK)
        ↕ SignalR (port 8080 by default)
  Unity-MCP-Plugin  (Unity Editor/Runtime)
        ↕ Unity API (main thread)
      Unity Engine
```

- The **MCP Server** is a standalone binary downloaded automatically by the plugin to `Library/mcp-server/{platform}/`. It is also published to Docker Hub and NuGet.
- The **MCP Plugin** auto-starts the server binary on Unity Editor load (`[InitializeOnLoad]`). Port is deterministic: SHA256 hash of project path, mapped to 50000–59999.
- Communication inside Unity always runs on the **main thread** via `MainThread.Instance.Run()`.

## Development Commands

### MCP Server (dotnet)
```bash
cd Unity-MCP-Server
dotnet build com.IvanMurzak.Unity.MCP.Server.csproj
dotnet run --project com.IvanMurzak.Unity.MCP.Server.csproj -- --client-transport stdio --port 8080

# Cross-platform publish (creates publish/ dir)
./build-all.sh          # Linux/macOS
.\build-all.ps1         # Windows PowerShell
```

### MCP Plugin (Unity)
- Open the `Unity-MCP-Plugin` folder in Unity Editor
- Tests run via `Window > General > Test Runner`
  - EditMode tests: `Assets/root/Tests~/Editor`
  - PlayMode tests: `Assets/root/Tests~/Runtime`
- No standalone build command — Unity compiles C# automatically

### MCP Inspector (debugging)
```bash
Unity-MCP-Plugin/Commands/start_mcp_inspector.bat   # requires Node.js
```

## Release & Versioning

Version is sourced from [Unity-MCP-Plugin/Assets/root/package.json](Unity-MCP-Plugin/Assets/root/package.json).

- **Bump version**: `.\commands\bump-version.ps1 <version>` — updates all version references across the repo
- **Release**: Push to `main` triggers `.github/workflows/release.yml`, which runs the 18-combination Unity test matrix (3 Unity versions × 3 test modes × 2 OS), publishes executables to GitHub Releases, Docker Hub, and NuGet

## CI/CD

| Workflow | Trigger | Purpose |
|---|---|---|
| `release.yml` | Push to `main` | Full release pipeline |
| `test_pull_request.yml` | PR to `main`/`dev` | Validates all 18 test matrix combinations |
| `test_pull_request_manual.yml` | Manual | Manual trigger for PR test matrix |
| `test_unity_plugin.yml` | Reusable | Unity plugin test runner (called by other workflows) |
| `deploy.yml` | Release published / manual | NuGet + Docker Hub deploy |
| `deploy_server_executables.yml` | Release published | Cross-platform binary upload |
| `bump_version.yml` | Manual | Automated version bumping |
| `claude.yml` | Issue/PR comments | Claude Code AI assistant |
| `copilot-setup-steps.yml` | Reusable | GitHub Copilot setup steps |

PRs from untrusted contributors require a `ci-ok` label from a maintainer before CI runs.

## Key Coding Conventions

These apply across both C# sub-projects:

- `#nullable enable` at the top of every file
- Copyright box comment header in every file
- MCP tool classes are `partial` — one operation per file (e.g., `Tool_GameObject.Create.cs`)
- Return strings prefixed with `[Success]` or `[Error]` for structured AI feedback
- All Unity API calls must use `MainThread.Instance.Run(() => ...)` or `RunAsync()`
- Tool/prompt names use **kebab-case** with category prefix (e.g., `gameobject-create`, `assets-find`)
- Namespace pattern: `com.IvanMurzak.Unity.MCP.[Tier].[Component]`

## Rules

Important rules that must be followed:

- `./Unity-MCP-Plugin/Assets/root/README.md` must be a copy of `./README.md`.
- `./Unity-MCP-Plugin/Assets/root/README.md` must be translated to related translated versions of this file under `./docs/README.*.md`.
