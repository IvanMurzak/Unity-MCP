# Unity MCP Server Installation Guide

## Installing as a Global .NET Tool

1. Install the Unity MCP Server globally using dotnet:
```bash
dotnet tool install -g com.IvanMurzak.Unity.MCP.Server
```

2. Verify the installation:
```bash
unity-mcp-server --help
```

## VS Code Integration

### Method 1: Using Claude Desktop App
Add the following configuration to your Claude Desktop config file:

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
**macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
**Linux**: `~/.config/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "unity-mcp-server": {
      "command": "unity-mcp-server",
      "env": {
        "MCP_PLUGIN_CLIENT_TRANSPORT": "stdio",
        "MCP_PLUGIN_PORT": "8080",
        "MCP_PLUGIN_CLIENT_TIMEOUT": "10000"
      }
    }
  }
}
```

### Method 2: VS Code MCP Extension
If using a VS Code MCP extension, add this to your VS Code settings.json:

```json
{
  "servers": {
    "unity-mcp-server": {
      "command": "unity-mcp-server",
      "env": {
        "MCP_PLUGIN_CLIENT_TRANSPORT": "stdio",
        "MCP_PLUGIN_PORT": "8080",
        "MCP_PLUGIN_CLIENT_TIMEOUT": "10000"
      }
    }
  }
}
```

## Configuration Options

### Command Line Arguments
- `--client-transport`: Transport method (`stdio` or `http`)
- `--port`: Unity Plugin connection port (default: 8080)
- `--plugin-timeout`: Plugin connection timeout in milliseconds (default: 10000)

### Environment Variables
- `MCP_PLUGIN_CLIENT_TRANSPORT`: Transport type
- `MCP_PLUGIN_PORT`: Plugin port
- `MCP_PLUGIN_CLIENT_TIMEOUT`: Plugin timeout

## Usage

1. **Install the Unity MCP Plugin** in your Unity project
2. **Start Unity Editor** with your project open
3. **Configure your MCP client** (VS Code, Claude Desktop, etc.) to use `unity-mcp-server`
4. **Connect your AI assistant** - it will now be able to interact with Unity

## Troubleshooting

### Common Issues

1. **Server not found**: Ensure the dotnet tool is installed globally and accessible in your PATH
2. **Connection timeout**: Verify Unity is running and the MCP Plugin is installed
3. **Port conflicts**: Change the `--port` if 8080 is already in use

### Debugging

Run the server manually to see debug output:
```bash
unity-mcp-server --client-transport=stdio --port=8080
```

For more information, visit: https://github.com/IvanMurzak/Unity-MCP