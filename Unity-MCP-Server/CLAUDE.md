# CLAUDE.md

## What this is

C# ASP.NET Core MCP server that bridges MCP clients and the Unity Plugin via SignalR. Standalone binary, also packaged as a NuGet global tool and Docker image.

## Build / run

```bash
dotnet build com.IvanMurzak.Unity.MCP.Server.csproj
dotnet run --project com.IvanMurzak.Unity.MCP.Server.csproj -- --client-transport stdio --port 8080
```

## Find detail in

- [docs/claude/build.md](docs/claude/build.md) — Build & Run: cross-platform publish, NuGet tool, Docker image, STDIO/HTTP transports
- [docs/claude/config.md](docs/claude/config.md) — Configuration: CLI args & env vars (Core, Analytics Webhooks, Authorization Webhooks)
- [docs/claude/structure.md](docs/claude/structure.md) — Core Components: Program.cs, McpServerService, Hub/RemoteApp, Routing, Client, Extension
