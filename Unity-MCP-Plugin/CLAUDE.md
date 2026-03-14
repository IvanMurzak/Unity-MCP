# CLAUDE.md

## Overview

Unity-MCP Plugin — Unity Editor/Runtime side of the MCP bridge. Attribute-based framework that registers and executes MCP tools, prompts, and resources. Includes a self-hosted MCP server manager and auto-configuration for AI clients.

## Development

- **Open**: `Unity-MCP-Plugin` folder in Unity Editor (compiles automatically)
- **Tests**: Unity Test Runner (`Window > General > Test Runner`) — EditMode in `Assets/root/Tests/Editor`, PlayMode in `Assets/root/Tests/Runtime`
- **MCP Inspector**: `Commands/start_mcp_inspector.bat` (requires Node.js)

## Directory Structure

```
Assets/root/
├── Runtime/
│   ├── UnityMcpPluginRuntime.cs      # Runtime singleton (+ .Static.cs)
│   ├── Data/                         # ObjectRef hierarchy, GameObjectData, etc.
│   ├── Converter/                    # Json/ and Reflection/ converters
│   ├── Logger/                       # UnityLogger, factory, provider
│   ├── Extensions/                   # Extension methods
│   └── Utils/                        # MainThread dispatcher
├── Editor/
│   ├── Scripts/
│   │   ├── UnityMcpPluginEditor.cs   # Editor singleton (+ .Static, .Build, .Config)
│   │   ├── Startup.cs                # [InitializeOnLoad] entry (+ .Editor.cs)
│   │   ├── McpServerManager.cs       # Server binary lifecycle
│   │   ├── API/
│   │   │   ├── Tool/                 # MCP tools (partial classes, 1 op per file)
│   │   │   ├── Prompt/               # MCP prompts
│   │   │   └── Resource/             # MCP resources
│   │   ├── Services/                 # Device auth flow
│   │   └── UI/                       # Editor windows, AI agent configurators
│   └── Gizmos/                       # Icons
├── Tests/
│   ├── Editor/                       # EditMode tests
│   └── Runtime/                      # PlayMode tests
└── Plugins/                          # Bundled DLLs (McpPlugin, ReflectorNet)
```

### Key Classes

- **UnityMcpPluginEditor** (4 partials: `.cs`, `.Static.cs`, `.Build.cs`, `.Config.cs`) — Editor-only singleton managing persistent MCP connection, config persistence (JSON file I/O), and lazy assembly scanning via `McpPluginBuilder`
- **UnityMcpPluginRuntime** (2 partials: `.cs`, `.Static.cs`) — Runtime singleton with `Initialize(Action<IMcpPluginBuilder>?)` API for game builds; no JSON config dependency, separate MCP connection from Editor
- **Startup** (2 partials: `.cs`, `.Editor.cs`) — `[InitializeOnLoad]` entry point; `.Editor.cs` handles assembly reload, play mode transitions, graceful disconnect

## Startup Flow

Triggered by `[InitializeOnLoad]` static constructor in `Startup.cs`:

1. Build `IMcpPlugin` instance (scan assemblies for tools/prompts/resources)
2. Add `BufferedFileLogStorage` log collector for early log capture
3. **Deferred** connection via `EditorApplication.delayCall` (prevents Unity freeze on async SignalR)
4. Start server binary download asynchronously
5. **Deferred** server auto-start via `EditorApplication.delayCall`
6. Validate project path (no spaces)
7. Subscribe to editor lifecycle events (domain reload, play mode transitions)
8. CI environment detection — skips connection and server start in CI

## MCP Protocol Implementation

Attribute-based registration for three MCP primitives. All use `[System.ComponentModel.Description]` for AI-readable documentation.

- **Tools**: `[McpPluginToolType]` on class, `[McpPluginTool(Name = "category-action")]` on methods
- **Prompts**: `[McpPluginPromptType]` on class, `[McpPluginPrompt]` on methods
- **Resources**: `[McpPluginResourceType]` on class, `[McpPluginResource]` on methods (e.g., `gameobject://currentScene/{path}`)

## Object Reference Hierarchy

```text
ObjectRef (base — contains InstanceID)
├── AssetObjectRef (+ AssetPath, AssetGuid, AssetType)
│   └── GameObjectRef (+ Path, Name)
├── ComponentRef (+ Index, TypeName)
└── SceneRef (+ Path, BuildIndex)
```

Supporting types: `GameObjectData`, `ComponentData`, `SceneData`, `GameObjectMetadata`, plus `*Shallow` and `*List` variants. All use `[JsonPropertyName]` and implement `IsValid(out string? error)`.

## Connection & Transport

- **Port**: Deterministic — SHA256 of project path, mapped to 50000–59999
- **Server Binary**: Downloaded from GitHub releases to `Library/mcp-server/{platform}/`. Version tracked in a `version` file alongside binary.
- **Process Lifecycle** (`McpServerStatus`): `Stopped` → `Starting` → `Running` → `Stopping` → `Stopped`, plus `External`. PID persisted in EditorPrefs for domain reload resilience.
- **Domain Reload**: Disconnects before reload (only if `Connected`), rebuilds and reconnects after. Play mode transitions trigger delayed reconnection.

## AI Agent Configurators

Auto-configuration system generating MCP config files for AI clients. Each configurator has platform-specific and transport variants.

**Key patterns**:
- Fluent builder: `.SetProperty(key, value, requiredForConfiguration, comparison).SetPropertyToRemove(key)`
- `ValueComparisonMode`: `Exact`, `Path` (normalized separators), `Url` (case-insensitive scheme+host)
- Duplicate detection via identity keys. Deprecated section cleanup for old "Unity-MCP" entries.

## Testing Patterns

- Extend `BaseTest` class — provides `[UnitySetUp]` (initializes singleton, creates logger) and `[UnityTearDown]` (destroys all GameObjects)
- `BaseTest.RunTool(string toolName, string json)` helper — executes a tool and asserts success
- Use `[UnityTest]` with `IEnumerator` return type; call `yield return base.SetUp()` / `base.TearDown()`
- Some tests use standard NUnit `[Test]`/`[SetUp]`/`[TearDown]` when Unity APIs aren't needed

## Error Handling

- Structured error responses for AI consumption; graceful non-blocking cleanup on disconnect/quit
- SIGTERM for Unix (falls back to `Kill()`), immediate `Kill()` on Windows
- Log all exceptions (never silently swallowed)

## Configuration

- **No spaces in project path** — validated on startup with user warning
- **Unity 2022.3+** minimum
- Main UI: `Window/AI Game Developer`
- Config file: `Assets/Resources/AI-Game-Developer-Config.json` (auto-created). Editor mode: file path; Play mode: `Resources.Load<TextAsset>()`
