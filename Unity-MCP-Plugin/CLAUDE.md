# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity-MCP is a bridge between Large Language Models (LLMs) and Unity Editor that implements the Model Context Protocol (MCP). It enables AI assistants to interact with Unity projects through a comprehensive set of tools for managing GameObjects, assets, scripts, and more.

**Key Architecture Components:**

- **MCP Plugin System**: Core framework that manages tool registration and execution
- **SignalR Hub Connection**: Real-time communication between Unity and external MCP clients
- **Reflection-based Tools**: Dynamic access to Unity API using advanced reflection
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
├── Runtime/                 # Core runtime functionality
│   ├── Config/             # UnityMcpPlugin configuration system
│   ├── Extensions/         # Unity-specific extension methods
│   ├── JsonConverters/     # Custom JSON serialization for Unity types
│   ├── ReflectionConverters/ # Advanced reflection system for Unity objects
│   └── Utils/              # Utility classes and helper functions
├── Editor/                 # Unity Editor integration
│   ├── Scripts/API/Tool/   # MCP tool implementations
│   └── Scripts/UI/         # Editor UI windows and components
└── Tests/                  # Unit and integration tests
```

### MCP Tool System

Tools are implemented using attributes:

- `[McpPluginToolType]` - Marks a class as containing MCP tools
- `[McpPluginTool]` - Marks methods as callable MCP tools
- `[Description]` - Provides AI-readable documentation

### Unity-Specific Patterns

- **MainThread Execution**: All Unity API calls must use `MainThread.Instance.Run(() => ...)` for thread safety
- **Object References**: Use `GameObjectRef`, `ComponentRef`, `AssetObjectRef` for persistent object referencing
- **Reflection System**: Custom converters in `ReflectionConverters/` enable AI to read/write complex Unity data structures

### Connection Management

- **Configuration**: `UnityMcpPlugin` class manages connection settings (host, port, logging level)
- **Transport**: SignalR-based communication with external MCP clients
- **Real-time Updates**: Reactive extensions (R3) for connection state management

## Development Guidelines

### Adding Custom Tools

1. Create class with `[McpPluginToolType]` attribute
2. Add methods with `[McpPluginTool]` and `[Description]` attributes
3. Use optional parameters with `?` and defaults for flexible AI interaction
4. Wrap Unity API calls in `MainThread.Instance.Run()` when needed

### Testing Patterns

- Use `BaseTest` class for test infrastructure
- Test both successful operations and error conditions
- Use Unity's coroutine testing (`[UnityTest]`) for async operations
- Mock external dependencies when testing tool logic

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
- Connection settings stored in `UnityMcpPlugin.Config.cs`
- Runtime configuration via `Assets/Resources/AI-Game-Developer-Config.json`

### Dependencies

- **SignalR Client**: Real-time communication
- **Roslyn**: C# code compilation and execution
- **R3 Reactive Extensions**: Reactive programming patterns
- **ReflectorNet**: Advanced reflection system for Unity objects
