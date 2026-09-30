# Unity MCP Server

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/aigamedeveloper/mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/aigamedeveloper/mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

The **MCP Server** acts as the bridge between the **AI Client** (Claude, Cursor, etc.) and the **Unity Editor/Game**.

> The server lives in its own shared repo: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (binary `gamedev-mcp-server`, Docker `aigamedeveloper/mcp-server`) — one engine-agnostic server consumed by Unity-MCP, Godot-MCP, and Unreal-MCP. The Unity plugin downloads the release pinned by the `ServerVersion` constant in `McpServerManager.cs`.

<div align="center">

`AI Client` ↔️ **`MCP Server`** ↔️ `Unity Plugin`

</div>

## Topology

1.  **Client Connection**: The AI Client connects to the Server using either `stdio` (standard input/output pipe) or `streamableHttp`.
2.  **Plugin Connection**: The Unity Plugin connects to the Server via TCP/WebSockets on a specified port (default: `8080`).

## Deployment Options

### 1. Local Automatic (Recommended)
The **Unity Plugin** automatically downloads and runs the appropriate server binary for your OS. No manual setup required. Configuration is done via the Unity Editor window.

#### Overriding the launched binary — `UNITY_MCP_SERVER_PATH` (dev / CI only)

| Environment Variable    | Value                                                                        | Effect                                                                                                                              |
| :---------------------- | :--------------------------------------------------------------------------- | :---------------------------------------------------------------------------------------------------------------------------------- |
| `UNITY_MCP_SERVER_PATH` | Absolute path to an **existing** `gamedev-mcp-server` (`.exe` on Windows)     | The Editor launches **that** file instead of the release pinned by `ServerVersion`, and skips both the download and the version match. |

Intended for developing the server itself and for CI chains that build the server from source — not for everyday use. Details:

- The value is read through the same `process env` → `<projectRoot>/.env` → unset chain as `UNITY_MCP_DEV_CONTROL`, so an Editor launched from the GUI or an IDE (which inherits no shell exports) can still pick it up from a `.env` file at the Unity project root.
- **Set-but-missing falls through**: if the path does not exist, the plugin behaves exactly as if the variable were unset. This matches Unreal-MCP's `UNREAL_MCP_SERVER_PATH`. That fall-through is silent, so confirm from the Editor console rather than assuming: the `Starting MCP server: <path> …` line names the binary that was actually launched, every time. The `UNITY_MCP_SERVER_PATH override active: …` notice is emitted only when the override already resolves as the domain loads, so it is absent if you write the `.env` afterwards — the launch line is the one to trust.
- Use an **absolute** path. A relative value is resolved against the Editor process's working directory, not against the Unity project root. The filename itself is not constrained: whatever file the path names is the file that gets launched.
- The override also becomes the `command` written into generated AI-agent configs, so the agent launches the same binary the Editor does.
- `Tools/AI Game Developer/Server/Download Binaries` (the manual menu item) still downloads into `Library/mcp-server/<rid>/` regardless of the override; the override still wins at launch.
- `Tools/AI Game Developer/Server/Open Server Logs` follows the override, because the server's working directory is the folder its binary sits in — so the logs you open are the ones the overridden server actually wrote.

### 2. Docker
See **[Docker Deployment](DOCKER_DEPLOYMENT.md)**. Best for cloud hosting or isolated environments.

### 3. Manual Binary
You can run the server manually if you need advanced control or debugging.

Download from the shared **[GameDev-MCP-Server Releases](https://github.com/IvanMurzak/GameDev-MCP-Server/releases)**.

```bash
# HTTP mode (default transport)
./gamedev-mcp-server --port 8080 --client-transport streamableHttp

# STDIO mode (for piping to MCP clients like Claude Desktop)
./gamedev-mcp-server --port 8080 --client-transport stdio
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
| `MCP_PLUGIN_IDLE_TIMEOUT_SECONDS` | `--idle-timeout-seconds` | Shut the server down after this many seconds with no active connections.                  | `600`            |

### Analytics & Authorization Webhooks

For cloud/hosted deployments the server can call out to external webhooks. All are optional and unset by default:

| Environment Variable                       | CLI Argument                       | Description                                                          |
| :----------------------------------------- | :--------------------------------- | :------------------------------------------------------------------ |
| `MCP_PLUGIN_WEBHOOK_TOOL_URL`              | `--webhook-tool-url`               | Notified on tool calls.                                             |
| `MCP_PLUGIN_WEBHOOK_PROMPT_URL`            | `--webhook-prompt-url`             | Notified on prompt usage.                                          |
| `MCP_PLUGIN_WEBHOOK_RESOURCE_URL`          | `--webhook-resource-url`           | Notified on resource access.                                       |
| `MCP_PLUGIN_WEBHOOK_CONNECTION_URL`        | `--webhook-connection-url`         | Notified on plugin connect/disconnect.                             |
| `MCP_PLUGIN_WEBHOOK_TOKEN`                 | `--webhook-token`                  | Bearer token sent with webhook requests.                           |
| `MCP_PLUGIN_WEBHOOK_HEADER`                | `--webhook-header`                 | Extra header sent with webhook requests.                           |
| `MCP_PLUGIN_WEBHOOK_TIMEOUT`               | `--webhook-timeout`                | Webhook request timeout in ms (default `10000`).                   |
| `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_URL`     | `--webhook-authorization-url`      | Endpoint that authorizes incoming Client connections.              |
| `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_FAIL_OPEN` | `--webhook-authorization-fail-open` | Allow connections when the authorization webhook is unreachable (default `false`). |

## Architecture

The server is built on **.NET 9**, utilizing:
- **ASP.NET Core** for HTTP/WebSockets.
- **SignalR** for communication between Server and Plugin.
- **[Model Context Protocol SDK](https://github.com/modelcontextprotocol/csharp-sdk)** for implementing MCP protocol.
- **[ReflectorNet](https://github.com/IvanMurzak/ReflectorNet)** for dynamic assembly analysis (used by the plugin).
- **[MCP Plugin .NET](https://github.com/IvanMurzak/MCP-Plugin-dotnet)** for the MCP proxy implementation.
