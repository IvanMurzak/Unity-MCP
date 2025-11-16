# Unity MCP - VS Code Extension

**Unity MCP Server** is a Model Context Protocol (MCP) server that enables AI assistants to interact with Unity Editor and games through the Unity-MCP Plugin.

This extension bundles and manages the Unity MCP Server, making it easy to use with VS Code's AI features and other MCP-compatible clients.

## Features

- 🚀 **One-click Installation** - Automatically installs and configures Unity MCP Server
- 🎮 **Unity Integration** - Seamless communication between AI assistants and Unity projects
- 🔧 **Easy Configuration** - Simple settings for port, timeout, and transport method
- 📊 **Status Monitoring** - Real-time server status in VS Code status bar
- 🌐 **Multi-platform** - Supports Windows, macOS, and Linux (x64 and ARM64)

## Requirements

- **Unity Editor** with Unity-MCP Plugin installed
- **VS Code** version 1.85.0 or higher
- **MCP Client** (Claude Code, Claude Desktop, or compatible)

## Installation

1. Install this extension from the VS Code Marketplace
2. Install the Unity-MCP Plugin in your Unity project:
   - [Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/latest)
   - Import into Unity project
   - Follow the setup instructions

## Usage

### Starting the Server

The server starts automatically when VS Code opens (configurable). You can also control it manually:

- **Command Palette** (`Ctrl+Shift+P` / `Cmd+Shift+P`):
  - `Unity MCP: Start Server`
  - `Unity MCP: Stop Server`
  - `Unity MCP: Restart Server`
  - `Unity MCP: Show Status`

- **Status Bar**: Click the Unity MCP status indicator for quick actions

### Configuration

Access settings via `File > Preferences > Settings` and search for "Unity MCP":

- **Port** (`unityMcp.port`): Server communication port (default: 8080)
- **Plugin Timeout** (`unityMcp.pluginTimeout`): Plugin connection timeout in ms (default: 10000)
- **Client Transport** (`unityMcp.clientTransport`): Transport method - `stdio` or `http` (default: `stdio`)
- **Auto Start** (`unityMcp.autoStart`): Automatically start server on VS Code launch (default: `true`)

## How It Works

```
VS Code AI Assistant <-> Unity MCP Server <-> Unity Plugin <-> Unity Editor/Game
```

1. **Unity MCP Server** (this extension) - Acts as a bridge between AI clients and Unity
2. **Unity Plugin** - Runs inside Unity, exposes tools and resources to the server
3. **AI Assistant** - Uses MCP protocol to interact with Unity through natural language

## Supported Platforms

- **Windows**: x64, x86, ARM64
- **macOS**: Intel (x64), Apple Silicon (ARM64)
- **Linux**: x64, ARM64

## Troubleshooting

### Server won't start

1. Check the Output panel (`View > Output`) and select "Unity MCP"
2. Verify the Unity-MCP Plugin is installed in your Unity project
3. Ensure the port (default: 8080) is not in use by another application
4. Try changing the transport method in settings

### Can't connect to Unity

1. Make sure Unity Editor is running with your project open
2. Verify the Unity-MCP Plugin is active (check `Window/AI Game Developer`)
3. Ensure both the server and plugin use the same port
4. Check firewall settings aren't blocking the connection

### Extension crashes

1. Restart VS Code
2. Check for conflicting extensions
3. Review the Output panel for error messages
4. [Report the issue](https://github.com/IvanMurzak/Unity-MCP/issues)

## Documentation

- [Unity MCP GitHub](https://github.com/IvanMurzak/Unity-MCP)
- [MCP Tools Documentation](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)
- [Custom Tools Guide](https://github.com/IvanMurzak/Unity-MCP#customize-mcp)
- [Model Context Protocol](https://modelcontextprotocol.io/)

## Contributing

Contributions are welcome! Please visit the [GitHub repository](https://github.com/IvanMurzak/Unity-MCP) to:

- Report bugs
- Suggest features
- Submit pull requests

## Support

- [GitHub Issues](https://github.com/IvanMurzak/Unity-MCP/issues)
- [Discord Community](https://discord.gg/cfbdMZX99G)

## License

This project is licensed under the terms specified in the [LICENSE](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) file.

## Acknowledgments

Created by [Ivan Murzak](https://github.com/IvanMurzak)

Special thanks to all contributors and the Unity community!

---

**Enjoy AI-powered Unity development!** 🎮✨
