# Unity MCP Server

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

The **MCP Server** acts as the bridge between the **AI Client** (Claude, Cursor, etc.) and the **Unity Editor/Game**.

<div align="center">

`AI Client` ↔️ **`MCP Server`** ↔️ `Unity Plugin`

</div>

## Topology

1.  **Client Connection**: The AI Client connects to the Server using either `stdio` (standard input/output pipe) or `streamableHttp`.
2.  **Plugin Connection**: The Unity Plugin connects to the Server via TCP/WebSockets on a specified port (default: `8080`).

## Deployment Options

### 1. Local Automatic (Recommended)
The **Unity Plugin** automatically downloads and runs the appropriate server binary for your OS. No manual setup required. Configuration is done via the Unity Editor window.

### 2. Docker
See **[Docker Deployment](DOCKER_DEPLOYMENT.md)**. Best for cloud hosting or isolated environments.

### 3. Manual Binary
You can run the server manually if you need advanced control or debugging.

Download from **[Releases](https://github.com/IvanMurzak/Unity-MCP/releases)**.

```bash
# HTTP mode (default transport)
./unity-mcp-server --port 8080 --client-transport streamableHttp

# STDIO mode (for piping to MCP clients like Claude Desktop)
./unity-mcp-server --port 8080 --client-transport stdio
```

## CLI Arguments

All arguments can be provided as CLI flags or equivalent environment variables:

| Environment Variable          | CLI Argument         | Description                                                                                       | Default          |
| :---------------------------- | :------------------- | :------------------------------------------------------------------------------------------------ | :--------------- |
| `MCP_PLUGIN_PORT`             | `--port`             | Port for both the AI Client (HTTP) and Unity Plugin (SignalR) connections.                        | `8080`           |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | Protocol for AI Client connection: `streamableHttp` or `stdio`.                                   | `streamableHttp` |
| `MCP_PLUGIN_CLIENT_TIMEOUT`   | `--plugin-timeout`   | Timeout in ms for plugin responses.                                                               | `10000`          |
| `MCP_AUTHORIZATION`           | `--authorization`    | Authentication mode for incoming Client connections: `none` or `required`.                        | `none`           |
| `MCP_PLUGIN_TOKEN`            | `--token`            | Bearer token required from the Client when `--authorization=required`. Ignored when `none`.       | *(unset)*        |

## Architecture

The server is built on **.NET 9**, utilizing:
- **ASP.NET Core** for HTTP/WebSockets.
- **SignalR** for communication between Server and Plugin.
- **[Model Context Protocol SDK](https://github.com/modelcontextprotocol/csharp-sdk)** for implementing MCP protocol.
- **[ReflectorNet](https://github.com/IvanMurzak/ReflectorNet)** for dynamic assembly analysis (used by the plugin).
- **[MCP Plugin .NET](https://github.com/IvanMurzak/MCP-Plugin-dotnet)** for the MCP proxy implementation.
