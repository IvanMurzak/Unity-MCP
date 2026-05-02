# System Architecture

```
MCP Client (Claude/Cursor/etc.)
      ↕ stdio or streamableHttp
Unity-MCP-Server  (ASP.NET Core + MCP SDK)
      ↕ SignalR
Unity-MCP-Plugin  (Unity Editor/Runtime)
      ↕ Unity API (main thread)
Unity Engine
```

- The **MCP Server** is a standalone binary downloaded automatically by the plugin to `Library/mcp-server/{platform}/`. It is also published to Docker Hub and NuGet.
- The **MCP Plugin** auto-starts the server binary on Unity Editor load (`[InitializeOnLoad]`). Port is deterministic: SHA256 hash of project path, mapped to 20000–29999.
- Communication inside Unity always runs on the **main thread** via `MainThread.Instance.Run()`.
