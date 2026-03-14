# CLAUDE.md

## Build & Run

```bash
dotnet build com.IvanMurzak.Unity.MCP.Server.csproj
dotnet run --project com.IvanMurzak.Unity.MCP.Server.csproj

# Cross-platform publish (creates publish/ dir)
./build-all.sh          # Linux/macOS
.\build-all.ps1         # Windows PowerShell
```

Also available as NuGet global tool (`dotnet tool install -g com.IvanMurzak.Unity.MCP.Server`) and Docker image (`ivanmurzakdev/unity-mcp-server`).

## Running the Server

### STDIO Transport (for MCP clients)
```bash
dotnet run -- --client-transport stdio --port 8080
```

### HTTP Transport (for web-based clients)
```bash
dotnet run -- --client-transport streamableHttp --port 8080
```

## Configuration

CLI arguments override environment variables.

### Core

| Argument | Environment Variable | Purpose | Default |
|----------|---------------------|---------|---------|
| `--port` | `MCP_PLUGIN_PORT` | Client & Plugin connection port | `8080` |
| `--client-transport` | `MCP_PLUGIN_CLIENT_TRANSPORT` | Transport: `stdio` or `streamableHttp` | `streamableHttp` |
| `--plugin-timeout` | `MCP_PLUGIN_CLIENT_TIMEOUT` | Plugin connection timeout (ms) | `10000` |
| `--token` | `MCP_PLUGIN_TOKEN` | Bearer token required from connecting plugins | none |
| `--authorization` | `MCP_AUTHORIZATION` | Authorization enforcement mode | `none` |

### Analytics Webhooks

| Argument | Environment Variable | Purpose |
|----------|---------------------|---------|
| `--webhook-tool-url` | `MCP_PLUGIN_WEBHOOK_TOOL_URL` | Tool call event endpoint |
| `--webhook-prompt-url` | `MCP_PLUGIN_WEBHOOK_PROMPT_URL` | Prompt retrieval endpoint |
| `--webhook-resource-url` | `MCP_PLUGIN_WEBHOOK_RESOURCE_URL` | Resource access endpoint |
| `--webhook-connection-url` | `MCP_PLUGIN_WEBHOOK_CONNECTION_URL` | Client connect/disconnect endpoint |
| `--webhook-token` | `MCP_PLUGIN_WEBHOOK_TOKEN` | Security token sent in webhook header |
| `--webhook-header` | `MCP_PLUGIN_WEBHOOK_HEADER` | Custom header name for token (default: `X-Webhook-Token`) |
| `--webhook-timeout` | `MCP_PLUGIN_WEBHOOK_TIMEOUT` | HTTP delivery timeout in ms (default: `10000`) |

### Authorization Webhooks

| Argument | Environment Variable | Purpose |
|----------|---------------------|---------|
| `--webhook-authorization-url` | `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_URL` | Connection authorization endpoint |
| `--webhook-authorization-fail-open` | `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_FAIL_OPEN` | Allow connections if webhook fails (default: `false`) |

## Core Components

- **Program.cs**: Main entry point, configures ASP.NET Core web host with MCP server and SignalR hub
- **McpServerService.cs**: Hosted service that manages MCP server lifecycle and tool change notifications
- **Hub/RemoteApp.cs**: SignalR hub for Unity Plugin communication
- **Routing/**: MCP protocol handlers for tools and resources
- **Client/**: Utilities for remote tool and resource execution
- **Extension/**: Builder extensions for MCP server configuration
