# Docker deployment

Run the MCP server in a container, for cloud hosting or an isolated environment.
Back to the [README](https://github.com/IvanMurzak/Unity-MCP#readme) ·
[MCP server and connection](mcp-server.md).

- **Image:** [`aigamedeveloper/mcp-server`](https://hub.docker.com/r/aigamedeveloper/mcp-server), built from the shared
  [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) repository
- **Tags:** `latest` and release versions `X.Y.Z`
- **Port:** `8080` (exposed)

You still need the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP#quick-start) plugin in your Unity
project, set to **Custom** mode with the server's URL (see [connection modes](mcp-server.md#connection-modes)).

![Docker launch](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/editor/docker-launch.gif)

## HTTP (self-hosted, reachable from outside the container)

The server binds to loopback by default, which is not reachable through a published port. Set `MCP_BIND=any`:

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp \
  -e MCP_PLUGIN_PORT=8080 \
  -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

Agent config:

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

Replace the URL with your real endpoint when the server runs elsewhere.

### With a shared token

`token` mode requires one bearer secret from both the agent and the Unity plugin (`UNITY_MCP_TOKEN`, or the token
field in the AI Game Developer window). The secret crosses plain HTTP unless TLS terminates in front of the server.

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=8080 -e MCP_BIND=any \
  -e MCP_AUTH=token -e MCP_PLUGIN_TOKEN=<secret> \
  aigamedeveloper/mcp-server
```

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080",
      "headers": { "Authorization": "Bearer <secret>" }
    }
  }
}
```

### Custom port

Publish the same port you set in `MCP_PLUGIN_PORT`:

```bash
docker run -p 9090:9090 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=9090 -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

## stdio (the agent starts the container)

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-e", "MCP_PLUGIN_CLIENT_TRANSPORT=stdio",
        "-e", "MCP_BIND=any",
        "-p", "8080:8080",
        "aigamedeveloper/mcp-server"
      ]
    }
  }
}
```

The agent talks to the server over the container's stdin and stdout; the published port is for the Unity plugin.

## Configuration

All server options are environment variables (`-e NAME=value`). Common ones are listed in
[MCP server and connection](mcp-server.md#common-server-options); the full list, including OAuth mode and Redis-backed
sessions, is in the [GameDev-MCP-Server README](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration).
