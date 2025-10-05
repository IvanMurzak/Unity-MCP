
# Unity-MCP Common

[![nuget](https://img.shields.io/nuget/v/com.IvanMurzak.Unity.MCP.Common)](https://www.nuget.org/packages/com.IvanMurzak.Unity.MCP.Common/) ![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP) [![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

![netstandard2.1](https://img.shields.io/badge/.NET-netstandard2.1-blue?logoColor=white)

Shared source code for [Unity-MCP Plugin](https://github.com/IvanMurzak/Unity-MCP) and [Unity-MCP Server](https://github.com/IvanMurzak/Unity-MCP) projects.

### Topology subjects

- **Client** is the MCP client, such as VS Code, Cursor, Claude Desktop, Claude Code etc.
- **Server** is the MCP server, this is Unity-MCP server implementation which works closely in pair with Unity MCP Plugin
- **Plugin** is the Unity-MCP Plugin, this is deeply connected with Unity Editor and runtime game build SDK, that exposes API for the Server and lets the AI magic to happen.

### Connection chain

**Client** <-> **Server** <-> **Plugin** (Unity Editor / Game)

## Variables

| Environment Variable        | Command Line Variable | Description                                                                 |
|-----------------------------|-----------------------|-----------------------------------------------------------------------------|
| `UNITY_MCP_PORT`            | `--port`              | **Client** -> **Server** <- **Plugin** connection port (default: 8080)      |
| `UNITY_MCP_PLUGIN_TIMEOUT`  | `--plugin-timeout`    | **Plugin** -> **Server** connection timeout (ms) (default: 10000)           |
| `UNITY_MCP_CLIENT_TRANSPORT`| `--client-transport`  | **Client** -> **Server** transport type: `stdio` or `http` (default: `http`) |
