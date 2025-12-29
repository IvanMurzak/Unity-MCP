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

1.  **Client Connection**: The AI Client connects to the Server using either `stdio` (standard input/output pipe) or `http` (SSE/Post).
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
# Basic run (HTTP mode)
./unity-mcp-server --port 8080

# STDIO mode (for piping)
./unity-mcp-server --client-transport stdio
```

## CLI Arguments

The server executable accepts the following arguments:

| Argument | Description | Default |
| :--- | :--- | :--- |
| `--port` | Port for the Unity Plugin connection. | `8080` |
| `--client-transport` | Protocol for AI Client connection (`http`, `stdio`). | `http` |
| `--plugin-timeout` | Timeout in ms for plugin responses. | `10000` |

> **Note**: These can also be set via Environment Variables (e.g., `UNITY_MCP_PORT`).

## Architecture

The server is built on **.NET 9**, utilizing:
- **Model Context Protocol SDK** for AI communication.
- **ASP.NET Core** for HTTP/WebSockets.
- **ReflectorNet** for dynamic assembly analysis (used by the plugin).
