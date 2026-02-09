# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity-MCP is a bridge between Large Language Models (LLMs) and Unity Editor that implements the Model Context Protocol (MCP). It enables AI assistants to interact with Unity projects through a comprehensive set of tools, prompts, and resources for managing GameObjects, assets, scripts, and more.

**Key Architecture Components:**

- **MCP Plugin System**: Core framework that manages tools, prompts, and resources registration and execution
- **Self-Hosted MCP Server**: Managed server binary (`McpServerManager`) with configurable transport (stdio or streamableHttp)
- **Reflection-based Tools**: Dynamic access to Unity API using advanced reflection (ReflectorNet)
- **AI Agent Configurators**: Auto-configuration system for multiple AI clients (Claude Desktop, Cursor, VS Code Copilot, etc.)
- **Custom Tool Framework**: Extensible system for adding project-specific tools

## Development Commands

### Unity Operations

- **Open Unity Project**: Open the `Unity-MCP-Plugin` folder in Unity Editor
- **Run Tests**: Use Unity Test Runner window (`Window > General > Test Runner`)
  - EditMode tests: `Assets/root/Tests/Editor`
  - PlayMode tests: `Assets/root/Tests/Runtime`
- **Build Plugin**: Unity handles compilation automatically when scripts change

### MCP Development

- **Start MCP Inspector**: `Commands/start_mcp_inspector.bat` - Debug MCP protocol communication
- **Package Management**: Uses OpenUPM and Unity Package Manager for dependencies

### Project Structure Commands

- **Copy README**: `Commands/copy_readme.bat` - Synchronizes README files
- No traditional build/lint commands - Unity handles C# compilation and validation

## Code Architecture

### Core Plugin Structure

```
Assets/root/
├── Runtime/                     # Core runtime functionality
│   ├── Converter/
│   │   ├── Json/               # Custom JSON serialization for Unity types
│   │   └── Reflection/         # Reflection converters for complex Unity data
│   ├── Data/                   # Data models (GameObjectRef, ComponentRef, SceneRef, etc.)
│   ├── Extensions/             # Unity-specific extension methods
│   ├── Logger/                 # Logging infrastructure
│   ├── Unity/                  # Unity-specific utilities (logs, serialization)
│   ├── Utils/                  # Utility classes and helper functions
│   ├── UnityMcpPlugin.cs       # Main plugin class (partial)
│   ├── UnityMcpPlugin.Config.cs # Connection and transport configuration
│   └── UnityMcpPlugin.Log.cs   # Logging configuration
├── Editor/                     # Unity Editor integration
│   ├── Scripts/
│   │   ├── API/
│   │   │   ├── Tool/          # MCP tool implementations
│   │   │   ├── Prompt/        # MCP prompt implementations
│   │   │   └── Resource/      # MCP resource implementations
│   │   ├── UI/
│   │   │   ├── AiAgentConfigurators/ # AI client auto-configuration
│   │   │   ├── Window/        # Editor windows (Main, Tools, Prompts, Resources, etc.)
│   │   │   └── Utils/         # UI utilities
│   │   ├── Utils/
│   │   │   └── AiAgentConfig/ # Config file classes (JSON and TOML)
│   │   └── McpServerManager.cs # Self-hosted MCP server lifecycle management
│   └── Gizmos/                 # Icons and UI assets
├── Plugins/                    # Plugin DLLs
│   ├── com.IvanMurzak.McpPlugin/
│   └── com.IvanMurzak.ReflectorNet/
└── Tests/                      # Unit and integration tests
    ├── Editor/
    └── Runtime/
```

### MCP Protocol Implementation

The plugin implements three MCP primitives, each using attribute-based registration:

**Tools** (AI-callable actions):

- `[McpPluginToolType]` - Marks a class as containing MCP tools
- `[McpPluginTool]` - Marks methods as callable MCP tools

**Prompts** (reusable prompt templates):

- `[McpPluginPromptType]` - Marks a class as containing MCP prompts
- `[McpPluginPrompt]` - Marks methods as MCP prompts

**Resources** (data sources):

- `[McpPluginResourceType]` - Marks a class as containing MCP resources
- `[McpPluginResource]` - Marks methods as MCP resources

All use `[Description]` attributes for AI-readable documentation.

### Unity-Specific Patterns

- **MainThread Execution**: All Unity API calls must use `MainThread.Instance.Run(() => ...)` for thread safety
- **Object References**: Use `GameObjectRef`, `ComponentRef`, `AssetObjectRef`, `SceneRef` for persistent object referencing
- **Reflection System**: Custom converters in `Converter/Reflection/` enable AI to read/write complex Unity data structures

### Connection & Transport

- **Transport Methods**: Configurable via `TransportMethod` enum — `streamableHttp` (default) or `stdio`
- **Self-Hosted Server**: `McpServerManager` manages a server binary with cross-platform support (Windows, macOS, Linux)
- **Configuration**: `UnityMcpPlugin.UnityConnectionConfig` manages host, transport, log level, keep-alive, and feature toggles for tools/prompts/resources
- **Real-time Updates**: Reactive extensions (R3) for connection state management

### AI Agent Configurators

Auto-configuration system that generates MCP config files for various AI clients:

- AntigravityConfigurator, ClaudeCodeConfigurator, ClaudeDesktopConfigurator
- CodexConfigurator, CursorConfigurator, GeminiConfigurator, OpenCodeConfigurator
- VisualStudioCodeCopilotConfigurator, VisualStudioCopilotConfigurator
- CustomConfigurator (user-defined)

Config classes support both JSON (`JsonAiAgentConfig`) and TOML (`TomlAiAgentConfig`) formats with platform-specific paths (Windows, Mac, Linux).

## Development Guidelines

### Adding Custom Tools

1. Create class with `[McpPluginToolType]` attribute
2. Add methods with `[McpPluginTool]` and `[Description]` attributes
3. Use optional parameters with `?` and defaults for flexible AI interaction
4. Wrap Unity API calls in `MainThread.Instance.Run()` when needed

The same pattern applies to prompts (`[McpPluginPromptType]`/`[McpPluginPrompt]`) and resources (`[McpPluginResourceType]`/`[McpPluginResource]`).

### Testing Patterns

- Use `BaseTest` class for test infrastructure with `[UnitySetUp]`/`[UnityTearDown]`
- Use Unity's coroutine testing (`[UnityTest]`) with `IEnumerator` return type
- Test both successful operations and error conditions
- Temp files via `Path.GetTempFileName()`, cleaned up in TearDown

### Unity Integration

- Follow Unity's C# coding conventions
- Use Unity's serialization system for persistent data
- Leverage Unity's asset management for file operations
- Implement proper cleanup in OnDestroy/OnDisable methods

### Error Handling

- Use structured error responses for AI consumption
- Include helpful context in error messages
- Handle both Unity-specific and general exceptions
- Log errors with appropriate log levels using `UnityMcpPlugin.Log`

## Important Notes

### Requirements

- **No spaces in project path** - Unity-MCP requires project paths without spaces
- **Unity 2022.3+** - Minimum supported Unity version
- **MCP Client** - Requires compatible MCP client (Claude Desktop, Cursor, VS Code Copilot, etc.)

### Configuration

- Main configuration through `Window/AI Game Developer`
- Connection settings in `UnityMcpPlugin.Config.cs` (`UnityConnectionConfig` class)
- Runtime configuration via `Assets/Resources/AI-Game-Developer-Config.json`
- AI agent configs generated by configurators in platform-specific locations

### Editor Windows

- **MainWindowEditor** — Primary configuration UI (`Window/AI Game Developer`)
- **McpToolsWindow / McpPromptsWindow / McpResourcesWindow** — List registered MCP features
- **NotificationPopupWindow / UpdatePopupWindow** — User notifications and update alerts
- **SerializationCheckWindow** — Serialization validation

### Dependencies

- **McpPlugin** (bundled DLL): MCP protocol implementation and plugin framework
- **ReflectorNet** (bundled DLL): Advanced reflection system for Unity objects
- **SignalR Client**: Real-time communication
- **Roslyn**: C# code compilation and execution
- **R3 Reactive Extensions**: Reactive programming patterns
- **Microsoft.Extensions.Hosting/Logging**: Server hosting infrastructure
- **System.Text.Json**: JSON serialization
