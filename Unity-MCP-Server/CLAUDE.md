# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Commands

### Build & Publish
- **Build for all platforms**: `.\build-all.ps1` (Windows) or `./build-all.sh` (Linux/macOS)
- **Build specific configuration**: `.\build-all.ps1 Debug` or `./build-all.sh Debug`
- **Standard .NET build**: `dotnet build com.IvanMurzak.Unity.MCP.Server.csproj`
- **Standard .NET run**: `dotnet run --project com.IvanMurzak.Unity.MCP.Server.csproj`

Build scripts create cross-platform executables in the `publish/` directory for:
- Windows: win-x64, win-x86, win-arm64
- Linux: linux-x64, linux-arm64
- macOS: osx-x64, osx-arm64

### Running the Server
The server supports two transport methods:

#### STDIO Transport (for MCP clients)
```bash
dotnet run -- --client-transport stdio --port 8080
```

#### HTTP Transport (for web-based clients)
```bash
dotnet run -- --client-transport http --port 8080 --port 8080
```

### Configuration
Key command-line arguments and environment variables:
- `MCP_PLUGIN_PORT` / `--port`: Client & Plugin connection port (default: 8080)
- `MCP_PLUGIN_CLIENT_TIMEOUT` / `--plugin-timeout`: Plugin connection timeout (default: 10000ms)
- `MCP_PLUGIN_CLIENT_TRANSPORT` / `--client-transport`: Transport type: `stdio` or `http`

## Architecture Overview

### Core Components
- **Program.cs**: Main entry point, configures ASP.NET Core web host with MCP server and SignalR hub
- **McpServerService.cs**: Hosted service that manages the MCP server lifecycle and tool change notifications
- **Hub/RemoteApp.cs**: SignalR hub for Unity Plugin communication on port 8080
- **Routing/**: MCP protocol handlers for tools and resources
- **Client/**: Utilities for remote tool and resource execution
- **Extension/**: Builder extensions for MCP server configuration

### Communication Flow
```
MCP Client <--> MCP Server <--> Unity Plugin (via SignalR)
     ^              ^                    ^
  stdio/http    ASP.NET Core         port 8080
```

### Key Technologies
- **.NET 9.0**: Target framework
- **ASP.NET Core**: Web host and HTTP transport
- **SignalR**: Real-time communication with Unity Plugin
- **ModelContextProtocol**: MCP server implementation
- **NLog**: Logging framework
- **ReflectorNet**: Advanced reflection utilities

### Project Structure
- `src/`: Main source code
  - `Hub/`: SignalR hub and Unity Plugin communication
  - `Routing/`: MCP protocol request handlers
  - `Client/`: Remote execution utilities
  - `Extension/`: Configuration extensions
- `publish/`: Build outputs (created by build scripts)
- Configuration files: `appsettings.json`, `NLog.config`

The server acts as a bridge between MCP clients (VS Code, Claude Desktop, etc.) and Unity Editor/games via the Unity-MCP Plugin, enabling AI assistants to interact with Unity projects through the Model Context Protocol.