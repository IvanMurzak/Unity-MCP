
# Unity-MCP Common

## Maintenance

### 1. Build package

```bash
dotnet pack -c Release
```

### 2. Get package

Get it from the location

```txt
\Unity-MCP-Common\bin~\Release\com.IvanMurzak.Unity.MCP.Common.1.0.5.nupkg
```

## Variables

| Environment Variable        | Command Line Variable | Description                                                                 |
|-----------------------------|-----------------------|-----------------------------------------------------------------------------|
| `UNITY_MCP_PORT`            | `--port`              | **Client** -> **Server** <- **Plugin** connection port (default: 8080)      |
| `UNITY_MCP_PLUGIN_TIMEOUT`  | `--plugin-timeout`    | **Plugin** -> **Server** connection timeout (ms) (default: 10000)           |
| `UNITY_MCP_CLIENT_TRANSPORT`| `--client-transport`  | **Client** -> **Server** transport type: `stdio` or `http` (default: `http`) |
