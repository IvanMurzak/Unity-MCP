# Directory Structure

```
Packages/com.ivanmurzak.unity.mcp/
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

## Key Classes

- **UnityMcpPluginEditor** (4 partials: `.cs`, `.Static.cs`, `.Build.cs`, `.Config.cs`) — Editor-only singleton managing persistent MCP connection, config persistence (JSON file I/O), and lazy assembly scanning via `McpPluginBuilder`
- **UnityMcpPluginRuntime** (2 partials: `.cs`, `.Static.cs`) — Runtime singleton with `Initialize(Action<IMcpPluginBuilder>?)` API for game builds; no JSON config dependency, separate MCP connection from Editor
- **Startup** (2 partials: `.cs`, `.Editor.cs`) — `[InitializeOnLoad]` entry point; `.Editor.cs` handles assembly reload, play mode transitions, graceful disconnect
