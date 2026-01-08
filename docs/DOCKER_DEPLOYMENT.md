# Docker Deployment

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

The Unity-MCP Server is available as a lightweight Docker container, ideal for cloud deployments or isolating the AI server environment.

- **Image**: `ivanmurzakdev/unity-mcp-server`
- **Tags**: `latest`, `X.Y.Z` (e.g., `0.35.0`)
- **Architectures**: `linux/amd64`, `linux/arm64` (Apple Silicon compatible)

## 🚀 Quick Start

Run the server on port `8080`:

```bash
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest
```

## ⚙️ Configuration

The server can be configured using environment variables.

| Variable                     | Default | Description                                              |
| :--------------------------- | :------ | :------------------------------------------------------- |
| `MCP_PLUGIN_PORT`            | `8080`  | The port the server listens on for Plugin connections.   |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `streamableHttp`  | Transport for the Client connection (`streamableHttp` or `stdio`). |
| `MCP_PLUGIN_CLIENT_TIMEOUT`   | `10000` | Connection timeout in milliseconds.                      |

### Example: Custom Port

Run on port `9090`:

```bash
docker run \
  -e MCP_PLUGIN_PORT=9090 \
  -p 9090:9090 \
  ivanmurzakdev/unity-mcp-server:latest
```

### Example: STDIO Mode
STDIO mode is used when the MCP Client manages the Docker process directly.

```bash
docker run -i \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=stdio \
  -p 8080:8080 \
  ivanmurzakdev/unity-mcp-server:latest
```

## 💻 Client Configuration

To use the Dockerized server with your AI Client (e.g., Claude):

### HTTP Mode (Recommended for Remote/Cloud)
```json
{
  "mcpServers": {
    "ai-game-developer: {
      "url": "http://localhost:8080"
    }
  }
}
```

### STDIO Mode (Managed by Client)
```json
{
  "mcpServers": {
    "ai-game-developer: {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "-e", "MCP_PLUGIN_CLIENT_TRANSPORT=stdio",
        "-p", "8080:8080",
        "ivanmurzakdev/unity-mcp-server:latest"
      ]
    }
  }
}
```