# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity-MCP is a bridge between Large Language Models (LLMs) and Unity Editor that implements the Model Context Protocol (MCP). It enables AI assistants to interact with Unity projects through 52 tools, 48 prompts, and 1 resource for managing GameObjects, assets, scripts, scenes, and more.

**Key Architecture Components:**

- **MCP Plugin System**: Attribute-based framework that registers and executes tools, prompts, and resources
- **Self-Hosted MCP Server**: Managed server binary (`McpServerManager`) downloaded from GitHub releases, with configurable transport (`streamableHttp` or `stdio`)
- **Reflection-based Tools**: Dynamic access to Unity API using ReflectorNet
- **AI Agent Configurators**: Auto-configuration system for 10 AI clients (Claude Desktop, Cursor, VS Code Copilot, Gemini, etc.)

**Package**: `com.ivanmurzak.unity.mcp` (current version: `0.45.0`)

## Development Commands

### Unity Operations

- **Open Unity Project**: Open the `Unity-MCP-Plugin` folder in Unity Editor
- **Run Tests**: Use Unity Test Runner window (`Window > General > Test Runner`)
  - EditMode tests: `Assets/root/Tests/Editor` (76 files)
  - PlayMode tests: `Assets/root/Tests/Runtime` (1 file)
- **Build Plugin**: Unity handles compilation automatically when scripts change

### MCP Development

- **Start MCP Inspector**: `Commands/start_mcp_inspector.bat` — runs `npx @modelcontextprotocol/inspector` (requires Node.js)
- **Copy README**: `Commands/copy_readme.bat` — syncs `README.md` from repo root to `Assets/root/`
- **Package Management**: Uses OpenUPM registry (`package.openupm.com`) and Unity Package Manager
- No traditional build/lint commands — Unity handles C# compilation and validation

## Code Architecture

### Directory Structure

```
Assets/root/
├── Runtime/                     # Core runtime (namespace: com.IvanMurzak.Unity.MCP)
│   ├── Converter/
│   │   ├── Json/               # Custom JSON serialization for Unity types
│   │   └── Reflection/         # Reflection converters for complex Unity data
│   ├── Data/                   # Data models (ObjectRef hierarchy, scene/component data)
│   ├── Extensions/             # Unity-specific extension methods
│   ├── Logger/                 # Logging infrastructure (BufferedFileLogStorage)
│   ├── Unity/                  # Unity utilities (logs, serialization)
│   ├── Utils/                  # MainThread dispatcher, helpers
│   ├── UnityMcpPlugin.cs       # Main plugin class — core + IDisposable
│   ├── UnityMcpPlugin.Build.cs  # McpPluginBuilder — assembly scanning
│   ├── UnityMcpPlugin.Config.cs # UnityConnectionConfig model
│   ├── UnityMcpPlugin.Converters.cs # Reflection + JSON converter registration
│   ├── UnityMcpPlugin.Editor.cs # Config persistence (JSON file I/O)
│   ├── UnityMcpPlugin.Log.cs   # Logging wrapper methods
│   └── UnityMcpPlugin.Static.cs # Thread-safe singleton + static accessors
├── Editor/                     # Unity Editor integration
│   ├── Scripts/
│   │   ├── API/
│   │   │   ├── Tool/          # 52 MCP tools (11 categories, partial classes)
│   │   │   ├── Prompt/        # 48 MCP prompts (6 categories)
│   │   │   └── Resource/      # 1 MCP resource (GameObject hierarchy)
│   │   ├── UI/
│   │   │   ├── AiAgentConfigurators/ # 10 configurators (base + implementations)
│   │   │   ├── Window/        # Editor windows (Main, Tools, Prompts, Resources, etc.)
│   │   │   └── Utils/         # UI utilities
│   │   ├── Utils/
│   │   │   └── AiAgentConfig/ # Config file classes (AiAgentConfig, Json*, Toml*)
│   │   ├── McpServerManager.cs # Server binary lifecycle management
│   │   ├── Startup.cs          # [InitializeOnLoad] entry point
│   │   └── Startup.Editor.cs   # Editor lifecycle event handlers
│   └── Gizmos/                 # Icons and UI assets
├── Plugins/                    # Bundled DLLs
│   ├── com.IvanMurzak.McpPlugin/   # MCP protocol implementation
│   └── com.IvanMurzak.ReflectorNet/ # Advanced reflection system
└── Tests/                      # 77 test files
    ├── Editor/
    └── Runtime/
```

### UnityMcpPlugin Partial Class

The main plugin class is split across 7 files for separation of concerns:

| File | Responsibility |
| ---- | -------------- |
| `UnityMcpPlugin.cs` | Core structure, `IDisposable` implementation |
| `UnityMcpPlugin.Build.cs` | `McpPluginBuilder` — scans assemblies, registers tools/prompts/resources |
| `UnityMcpPlugin.Config.cs` | `UnityConnectionConfig` data model |
| `UnityMcpPlugin.Converters.cs` | Reflection and JSON converter registration |
| `UnityMcpPlugin.Editor.cs` | Config persistence — load/save JSON to `Assets/Resources/` |
| `UnityMcpPlugin.Log.cs` | Logging wrapper methods (`LogTrace` through `LogException`) |
| `UnityMcpPlugin.Static.cs` | Thread-safe lazy singleton with mutex protection |

### Startup Flow

Triggered by `[InitializeOnLoad]` static constructor in `Startup.cs`:

1. Build `IMcpPlugin` instance (scan assemblies for tools/prompts/resources)
2. Add `BufferedFileLogStorage` log collector for early log capture
3. **Deferred** connection via `EditorApplication.delayCall` (prevents Unity freeze on async SignalR)
4. Start server binary download asynchronously
5. **Deferred** server auto-start via `EditorApplication.delayCall`
6. Validate project path (no spaces)
7. Subscribe to editor lifecycle events (domain reload, play mode transitions)
8. CI environment detection — skips connection and server start in CI

### MCP Protocol Implementation

The plugin implements three MCP primitives using attribute-based registration. All use `[System.ComponentModel.Description]` for AI-readable documentation and `#nullable enable` throughout.

**Tools** (52 total, kebab-case names like `assets-find`, `gameobject-create`):

- `[McpPluginToolType]` — marks a class as containing MCP tools
- `[McpPluginTool]` — marks methods as callable MCP tools
- Categories: assets (16), gameobject (11), scene (7), editor (4), package (4), script (4), reflection (2), object (2), console (1), tests (1)

**Prompts** (48 total, kebab-case names like `setup-basic-scene`):

- `[McpPluginPromptType]` — marks a class as containing MCP prompts
- `[McpPluginPrompt]` — marks methods as MCP prompts
- Categories: asset-management (8), scene-management (6), gameobject-component (8), scripting-code (8), debugging-testing (8), animation-timeline (8)

**Resources** (1 total):

- `[McpPluginResourceType]` / `[McpPluginResource]` — resource registration
- `gameobject://currentScene/{path}` — GameObject hierarchy from current scene

### Object Reference Hierarchy

```text
ObjectRef (base — contains InstanceID)
├── AssetObjectRef (+ AssetPath, AssetGuid, AssetType)
│   └── GameObjectRef (+ Path, Name)
├── ComponentRef (+ Index, TypeName)
└── SceneRef (+ Path, BuildIndex)
```

Supporting types: `GameObjectData`, `ComponentData`, `SceneData`, `GameObjectMetadata`, plus `*Shallow` and `*List` variants. All use `[JsonPropertyName]` attributes and implement `IsValid(out string? error)`.

### Connection & Transport

- **Transport**: `TransportMethod` enum — `streamableHttp` (default) or `stdio`. Server binary is always launched with `streamableHttp`; client-facing config can be either.
- **Port**: Deterministic — SHA256 hash of project directory path, mapped to range 50000–59999. Same path always yields the same port.
- **Server Binary**: Downloaded from GitHub releases to `Library/mcp-server/{platform}/` (win-x64, osx-x64, linux-x64). Unix gets executable permissions (0755). Version tracked in a `version` file alongside binary.
- **Process Lifecycle** (`McpServerStatus`): `Stopped` → `Starting` → `Running` → `Stopping` → `Stopped`, plus `External` (external server detected on port). PID persisted in EditorPrefs for domain reload resilience. Orphaned process cleanup on startup.
- **Reactive State**: `ReactiveProperty<HubConnectionState>` for connection, `ReactiveProperty<McpServerStatus>` for server, `ReadOnlyReactiveProperty<bool>` for `IsConnected`.
- **Domain Reload**: Disconnects before reload (only if `Connected`), rebuilds and reconnects after. Play mode transitions trigger delayed reconnection.
- **Application Quit**: Synchronous disconnect and process cleanup to prevent orphaned servers.

### AI Agent Configurators

Auto-configuration system generating MCP config files for AI clients. Each configurator has platform-specific variants (Windows / macOS+Linux) and transport variants (stdio / HTTP).

**10 Configurators**: Antigravity, ClaudeCode, ClaudeDesktop, Codex, Cursor, Custom, Gemini, OpenCode, VisualStudioCodeCopilot, VisualStudioCopilot

**Config file formats**:

- `JsonAiAgentConfig` — uses `System.Text.Json` (`JsonNode`). Properties stored as `Dictionary<string, (JsonNode value, bool required, ValueComparisonMode comparison)>`.
- `TomlAiAgentConfig` — custom TOML parser (no external library). Properties stored as `Dictionary<string, (object value, bool required, ValueComparisonMode comparison)>`. Handles inline comments, type preservation via `RawTomlValue`, typed arrays (string[], int[], bool[]).

**Key patterns**:

- Fluent builder: `.SetProperty(key, value, requiredForConfiguration, comparison).SetPropertyToRemove(key)`
- `ValueComparisonMode`: `Exact` (literal), `Path` (normalized separators/trailing slashes), `Url` (case-insensitive scheme+host)
- Properties sorted alphabetically in output
- Duplicate detection via identity keys (default: `command`, `url`). Removes duplicate server sections with matching identity values.
- Deprecated section cleanup — automatically removes old "Unity-MCP" server entries
- `SetPropertyToRemove()` enforces transport exclusivity (e.g., removes `url` when configuring stdio, removes `command`+`args` when configuring HTTP)

## Development Guidelines

### Adding Custom Tools

1. Create class with `[McpPluginToolType]` attribute
2. Add methods with `[McpPluginTool(Name = "category-action")]` and `[Description]` attributes
3. Use `#nullable enable` and nullable parameters (`string? filter = null`) for optional AI inputs
4. Wrap Unity API calls in `MainThread.Instance.Run(() => ...)` (sync) or `MainThread.Instance.RunAsync()` (async)
5. Tool names use **kebab-case** with category prefix (e.g., `assets-find`, `gameobject-create`, `scene-open`)

The same pattern applies to prompts (`[McpPluginPromptType]`/`[McpPluginPrompt]`) and resources (`[McpPluginResourceType]`/`[McpPluginResource]`).

### Testing Patterns

- Extend `BaseTest` class — provides `[UnitySetUp]` (initializes singleton, creates logger) and `[UnityTearDown]` (destroys all GameObjects)
- `BaseTest.RunTool(string toolName, string json)` helper — executes a tool and asserts success
- Use `[UnityTest]` with `IEnumerator` return type; call `yield return base.SetUp()` / `base.TearDown()`
- Temp files via `Path.GetTempFileName()`, cleaned up in `[UnityTearDown]`
- Some tests use standard NUnit `[Test]`/`[SetUp]`/`[TearDown]` when Unity APIs aren't needed

### Coding Conventions

- **Namespace**: `com.IvanMurzak.Unity.MCP.[Tier].[Component]` (e.g., `com.IvanMurzak.Unity.MCP.Editor.UI`)
- **Nullable**: `#nullable enable` at top of every file
- **Thread safety**: Mutex locks for config (`configMutex`), singleton (`_instanceMutex`), process (`_processMutex`)
- **Main thread**: All Unity API calls via `MainThread.Instance.Run()` / `RunAsync()`
- **Logging**: `UnityMcpPlugin.Log*` methods wrapping `UnityLoggerFactory`

### Error Handling

- Structured error responses for AI consumption
- Graceful degradation — non-blocking cleanup on disconnect/quit
- SIGTERM for Unix (falls back to `Kill()`), immediate `Kill()` on Windows
- File deletion failures show user dialog with retry option
- Missing config falls back to defaults
- Log all exceptions (never silently swallowed)

## Important Notes

### Requirements

- **No spaces in project path** — validated on startup with user warning
- **Unity 2022.3+** — minimum supported Unity version
- **MCP Client** — requires compatible client (Claude Desktop, Cursor, VS Code Copilot, etc.)
- **Node.js** — required only for `Commands/start_mcp_inspector.bat`

### Configuration

- Main UI: `Window/AI Game Developer`
- Config file: `Assets/Resources/AI-Game-Developer-Config.json` (auto-created)
  - Editor mode: loaded from file path; Play mode: loaded via `Resources.Load<TextAsset>()`
  - Stores: host, timeout, transport, log level, keep-alive, per-feature enable/disable for all tools/prompts/resources
- AI agent configs generated by configurators in platform-specific locations

### Editor Windows

- **MainWindowEditor** — primary configuration UI (`Window/AI Game Developer`)
- **McpToolsWindow / McpPromptsWindow / McpResourcesWindow** — list registered MCP features with enable/disable
- **NotificationPopupWindow / UpdatePopupWindow** — user notifications and update alerts
- **SerializationCheckWindow** — serialization validation

### Dependencies

Key packages (via OpenUPM `org.nuget.*` scope):

- **McpPlugin** (bundled DLL): MCP protocol implementation and plugin framework
- **ReflectorNet** (bundled DLL): Advanced reflection system for Unity objects
- **SignalR Client** `10.0.1`: Real-time communication (abstracted via `IMcpPlugin`)
- **Roslyn** `4.14.0`: C# code compilation and execution
- **R3** `1.3.0`: Reactive programming (`ReactiveProperty`, `Subject`, `Observable`)
- **System.Text.Json** `10.0.1`: JSON serialization
- **Microsoft.Extensions.Hosting** `10.0.1`: Server hosting infrastructure
- **Microsoft.Extensions.Logging** `10.0.1`: Logging abstractions
