<b>English</b> | [中文](mcp-server.zh-CN.md) | [日本語](mcp-server.ja.md) | [Español](mcp-server.es.md) | [فارسی](mcp-server.fa.md) | [हिन्दी](mcp-server.hi.md) | [العربية](mcp-server.ar.md)

# MCP server and connection

How the plugin reaches an MCP server, the environment variables that override its settings, and how to run a server yourself. Back to the [README](https://github.com/IvanMurzak/Unity-MCP#readme).

<div align="center">

`AI agent` ↔ **`MCP server`** ↔ `Unity plugin (Editor or game)`

</div>

The server is not in this repository. It is the shared, engine-agnostic **[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)** (binary `gamedev-mcp-server`, Docker image `aigamedeveloper/mcp-server`), also used by Godot-MCP and Unreal-MCP. Its README is the reference for every server option. The agent connects to the server over `stdio` or `streamableHttp`; the plugin connects to it with SignalR.

## Connection modes

The mode is chosen in the AI Game Developer window (**Window ▸ AI Game Dev**) and saved in `UserSettings/AI-Game-Developer-Config.json`.

| Mode | Server | Notes |
| --- | --- | --- |
| **Cloud** (default) | `https://ai-game.dev/mcp` | Sign in with the window or `unity-mcp-cli login`. Never starts a local server, even if `keepServerRunning` is `true`. |
| **Custom** | Local `gamedev-mcp-server`, or any URL you host | The saved `host` field is used only in this mode. To use a local server, select **Custom** in the window, or set `"connectionMode": "Custom"` while Unity is closed. |

In Custom mode with a local server, the plugin downloads the server release pinned by `ServerVersion` in `McpServerManager.cs` into `Library/mcp-server/<rid>/` and starts it. **Tools ▸ AI Game Developer ▸ Server** has *Download Binaries* and *Open Server Logs*.

## Plugin environment variables

Read at Editor startup. They override the saved config **in memory only** and are never written back, so the window still shows the saved choice. Command-line arguments win over environment variables. Each variable is also accepted as a command-line argument `-NAME=value` (for example `-UNITY_MCP_TRANSPORT=stdio`); `-url`, `-auth` and `-token` are short aliases. Rows marked *env only* have no argument.

| Variable | Values | Effect |
| --- | --- | --- |
| `UNITY_MCP_CLOUD_URL` | URL | MCP server URL (`UNITY_MCP_HOST` is a legacy alias) |
| `UNITY_MCP_CONNECTION_MODE` | `Cloud` / `Custom` | Connection mode (a loopback URL implies `Custom`) |
| `UNITY_MCP_KEEP_CONNECTED` | `true` / `false` | Keep the connection active |
| `UNITY_MCP_AUTH_OPTION` | `none` / `oauth` / `token` | Authentication mode (legacy `required` becomes `token`) |
| `UNITY_MCP_TOKEN` | string | Authentication token |
| `UNITY_MCP_TRANSPORT` | `stdio` / `streamableHttp` | Client transport the plugin configures |
| `UNITY_MCP_START_SERVER` | `true` / `false` | Keep a local server process running |
| `UNITY_MCP_TOOLS` | comma-separated tool ids | Show only these tools; the rest are hidden (still callable by name). Unknown ids are logged as errors. |
| `UNITY_MCP_CREDENTIALS_DIR` | absolute folder path | *env only.* Read the machine credential store from this folder instead of `~/.ai-game-dev` (development and test Editors). A relative value is ignored. |
| `UNITY_MCP_SERVER_PATH` | absolute path to a `gamedev-mcp-server` | *env only.* Launch this binary instead of the pinned release (see below) |
| `UNITY_MCP_DISABLE` | `1` / `true` | *env only.* Skip plugin initialization; see [Disable in CI](ci-disable.md) |

Example, batch mode against a local server with a token:

```bash
Unity.exe -batchmode -nographics \
  -UNITY_MCP_CLOUD_URL=http://localhost:8080 \
  -UNITY_MCP_KEEP_CONNECTED=true \
  -UNITY_MCP_AUTH_OPTION=token \
  -UNITY_MCP_TOKEN=my-secret-token
```

### `UNITY_MCP_SERVER_PATH` (server development and CI)

The Editor launches the named file instead of the pinned release and skips the download and the version check. Use it when developing the server or when CI builds the server from source.

- Read from the process environment, then from a `.env` file in the Unity project root, so an Editor started from the GUI or an IDE can pick it up.
- Use an **absolute** path. If the file does not exist, the plugin silently behaves as if the variable were unset; the console line `Starting MCP server: <path> …` names the binary that was really launched.
- The override also becomes the `command` in generated agent configs, and *Open Server Logs* follows it. *Download Binaries* still downloads into `Library/mcp-server/<rid>/`.

## Run a server yourself

Use this for a self-hosted or remote setup. Download a release zip (`gamedev-mcp-server-<rid>.zip`) from [GameDev-MCP-Server releases](https://github.com/IvanMurzak/GameDev-MCP-Server/releases), or use [Docker](DOCKER_DEPLOYMENT.md).

```bash
# stdio: the agent starts the server itself
./gamedev-mcp-server --port 8080 --client-transport stdio

# HTTP: run it as a standalone process
./gamedev-mcp-server --port 8080 --client-transport streamableHttp
```

Agent config for stdio (replace the path; on Windows the file ends in `.exe`):

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["--port=8080", "--client-transport=stdio"]
    }
  }
}
```

The plugin must use the same port: set **Custom** mode and the same host and port in the AI Game Developer window.

### Common server options

Command-line arguments override environment variables. Arguments also work with one `-` or none (`port=8080`). Defaults and the full list: [GameDev-MCP-Server configuration](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration).

| Environment variable | Argument | Meaning |
| --- | --- | --- |
| `MCP_PLUGIN_PORT` | `--port` | Port for both the agent (HTTP) and the plugin (default `8080`) |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | Agent transport: `stdio` or `streamableHttp` |
| `MCP_PLUGIN_CLIENT_TIMEOUT` | `--plugin-timeout` | Timeout for catalog, prompt and resource requests to the plugin (ms) |
| `MCP_PLUGIN_TOOL_TIMEOUT` | `--tool-timeout` | How long one tool call may run (ms). A call is sent once and never re-sent. |
| `MCP_AUTH` (or `MCP_AUTHORIZATION`) | `--auth` (or `--authorization`) | `none`, `token` or `oauth` |
| `MCP_PLUGIN_TOKEN` | `--token` | Shared secret for `token` mode; the agent and the plugin must both send it |
| `MCP_BIND` | `--bind` | Bind address: `loopback` (default), `any`, or an IP |
| `MCP_PLUGIN_IDLE_TIMEOUT_SECONDS` | `--idle-timeout-seconds` | Close idle streamableHttp sessions after this many seconds |

Hosted deployments can also call webhooks on tool, prompt, resource and connection events and authorize incoming connections: `MCP_PLUGIN_WEBHOOK_TOOL_URL`, `MCP_PLUGIN_WEBHOOK_PROMPT_URL`, `MCP_PLUGIN_WEBHOOK_RESOURCE_URL`, `MCP_PLUGIN_WEBHOOK_CONNECTION_URL`, `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_URL`, `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_FAIL_OPEN`, `MCP_PLUGIN_WEBHOOK_TOKEN`, `MCP_PLUGIN_WEBHOOK_HEADER` and `MCP_PLUGIN_WEBHOOK_TIMEOUT` (all optional, unset by default).
