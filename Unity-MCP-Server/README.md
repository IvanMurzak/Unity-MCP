# Unity MCP Server

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server) [![MCP](https://badge.mcpx.dev?type=server 'MCP Server')](https://modelcontextprotocol.io/introduction) [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) [![Unity Asset Store](https://img.shields.io/badge/Asset%20Store-View-blue?logo=unity&labelColor=333A41 'Asset Store')](https://u3d.as/3wsw) [![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive) [![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Runtime supported')](https://unity.com/releases/editor/archive) [![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G) [![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers) [![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) [![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

Model Context Protocol implementation for Unity Editor and for games made with Unity.

💬 **Join our community:** [Discord Server](https://discord.gg/cfbdMZX99G) - Ask questions, showcase your work, and connect with other developers!

Required to install [Unity MCP Plugin](https://github.com/IvanMurzak/Unity-MCP) into Unity Engine to connect it with MCP Server.

## Topology

- **Client** is the MCP client, such as VS Code, Cursor, Claude Desktop, Claude Code etc.
- **Server** is the MCP server, this is Unity-MCP server implementation which works closely in pair with Unity MCP Plugin
- **Plugin** is the Unity-MCP Plugin, this is deeply connected with Unity Editor and runtime game build SDK, that exposes API for the Server and lets the AI magic to happen. It utilizes advanced reflection by using [ReflectorNet](https://github.com/IvanMurzak/ReflectorNet)

### Connection chain

**Client** <-> **Server** <-> **Plugin** (Unity Editor / Game Build)

---

### Variables

Doesn't matter what launch option you choose, all of them support custom configuration using both Environment Variables and Command Line Arguments. It would work with default values, if you just need to launch it, don't waste your time for the variables. Just make sure Unity Plugin also has default values, especially the `--port`, they should be equal.

| Environment Variable        | Command Line Args     | Description                                                                 |
|-----------------------------|-----------------------|-----------------------------------------------------------------------------|
| `UNITY_MCP_PORT`            | `--port`              | **Client** -> **Server** <- **Plugin** connection port (default: 8080)      |
| `UNITY_MCP_PLUGIN_TIMEOUT`  | `--plugin-timeout`    | **Plugin** -> **Server** connection timeout (ms) (default: 10000)           |
| `UNITY_MCP_CLIENT_TRANSPORT`| `--client-transport`  | **Client** -> **Server** transport type: `stdio` or `http` (default: `http`) |

---

## Launch

Unity-MCP server is developed with idea of flexibility in mind, that is why it has many launch options.

### Option: Using Docker (recommended)

#### Default launch

The default the transport method is `http`. The port `8080` should be forwarded. It will be used for http transport and for **plugin** <-> **server** communication

```bash
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

MCP client config:

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "url": "http://localhost:8080"
    }
  }
}
```

#### Use STDIO

The `8080` port is not needed for STDIO, because it uses the STDIO to communicate with **Client**. It is a good setup for using in a client with automatic installation and launching. Because this docker command loads the image from docker hub and launches immediately.

```bash
docker run -t -e UNITY_MCP_CLIENT_TRANSPORT=stdio -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

MCP client config:

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "command": "docker",
      "args": [
        "run",
        "-t",
        "-e",
        "UNITY_MCP_CLIENT_TRANSPORT=stdio",
        "-p",
        "8080:8080",
        "ivanmurzakdev/unity-mcp-server"
      ]
    }
  }
}
```

#### Custom port

```bash
docker run -e UNITY_MCP_PORT=123 -p 123:123 ivanmurzakdev/unity-mcp-server
```

MCP client config:

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "url": "http://localhost:123"
    }
  }
}
```

Port forwarding is need for the launch with docker `-p 123:123`.

---

### Option: Using binary file

Download binary from the [GitHub releases page](https://github.com/IvanMurzak/Unity-MCP/releases). Unpack the zip archive and use command line to simply launch binary of the server for your target operation system and CPU architecture.

#### Default launch (STDIO)

```bash
./unity-mcp-server --client-transport stdio
```

MCP client config:

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "command": "C:/Projects/Unity/Unity-MCP/Unity-MCP-Plugin/Library/mcp-server/win-x64/unity-mcp-server.exe",
      "args": [
        "--client-transport=stdio"
      ]
    }
  }
}
```

#### Launch STDIO (Local)

Launch server with STDIO transport type for local usage on the same machine with Unity Editor.

```bash
./unity-mcp-server --port 8080 --plugin-timeout 10000 --client-transport stdio
```

MCP client config:

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "command": "C:/Projects/Unity/Unity-MCP/Unity-MCP-Plugin/Library/mcp-server/win-x64/unity-mcp-server.exe",
      "args": [
        "--port=8080",
        "--plugin-timeout=10000",
        "--client-transport=stdio"
      ]
    }
  }
}
```

#### Launch HTTP(S) (Local OR Remote)

Launch server with HTTP transport type for local OR remote usage using HTTP(S) url.

```bash
./unity-mcp-server --port 8080 --plugin-timeout 10000 --client-transport http
```

MCP client config:

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "command": "C:/Projects/Unity/Unity-MCP/Unity-MCP-Plugin/Library/mcp-server/win-x64/unity-mcp-server.exe",
      "args": [
        "--port=8080",
        "--plugin-timeout=10000",
        "--client-transport=http"
      ]
    }
  }
}
```

---
