# CLAUDE.md

## What this is

Unity-MCP Plugin — Unity Editor/Runtime side of the MCP bridge. Attribute-based framework that registers and executes MCP tools, prompts, and resources. Includes a self-hosted MCP server manager and auto-configuration for AI clients.

## Build / run

- **Open**: `Unity-MCP-Plugin` folder in Unity Editor (compiles automatically)
- **Tests**: Unity Test Runner (`Window > General > Test Runner`) — EditMode in `Packages/com.ivanmurzak.unity.mcp/Tests/Editor`, PlayMode in `Packages/com.ivanmurzak.unity.mcp/Tests/Runtime`
- **MCP Inspector**: `Commands/start_mcp_inspector.bat` (requires Node.js)

## Critical invariants

- Edits to `.cs` files cause MCP silence during recompile — read `Editor.log` directly from disk to recover compile errors.
- **No spaces in project path** — validated on startup; will warn user.
- **Unity 2022.3+** minimum.
- All Unity API calls must use `MainThread.Instance.Run()`.

## Find detail in

- `docs/claude/startup-flow.md` — `[InitializeOnLoad]` 8-step init, deferred SignalR connect, CI detection
- `docs/claude/transport.md` — Port hashing, server binary download, process lifecycle, domain reload
- `docs/claude/models.md` — `ObjectRef` hierarchy, supporting data types, `IsValid` validation
- `docs/claude/auto-config.md` — AI agent configurators: fluent builder, duplicate detection, deprecated cleanup
- `docs/claude/structure.md` — Directory layout and key classes (`UnityMcpPluginEditor`, `Runtime`, `Startup`)
- `docs/claude/protocol.md` — MCP attributes, testing patterns, error handling, config file location
