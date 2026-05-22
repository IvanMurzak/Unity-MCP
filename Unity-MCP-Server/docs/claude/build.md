# Build & Run

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
