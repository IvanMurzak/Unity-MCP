# System Architecture

```
MCP Client (Claude/Cursor/etc.)
      ↕ stdio or streamableHttp
GameDev-MCP-Server  (shared repo: ASP.NET Core + MCP SDK)
      ↕ SignalR
Unity-MCP-Plugin  (Unity Editor/Runtime)
      ↕ Unity API (main thread)
Unity Engine
```

- The **MCP Server** is the shared [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (binary `gamedev-mcp-server`) — it lives in its own repo and is downloaded automatically by the plugin to `Library/mcp-server/{platform}/`, pinned by the `ServerVersion` constant in `McpServerManager.cs` (decoupled from the plugin version). It is also published to Docker Hub as `aigamedeveloper/mcp-server`.
- The **MCP Plugin** auto-starts the server binary on Unity Editor load (`[InitializeOnLoad]`). Port is deterministic: SHA256 hash of project path, mapped to 20000–29999.
- Communication inside Unity always runs on the **main thread** via `MainThread.Instance.Run()`.
