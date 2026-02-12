# Project Guidelines

## Code Style
- **C#**: Use 4 spaces indentation. PascalCase for classes/methods/properties, `_camelCase` for private readonly fields.
    - Namespace: `com.IvanMurzak.Unity.MCP`.
    - Example: [UnityMcpPlugin.cs](Unity-MCP-Plugin/Assets/root/Runtime/UnityMcpPlugin.cs).
- **PowerShell**: Use K&R brace style.

## Architecture
- **Unity-MCP-Plugin**: Main Unity package.
    - Core logic: [Assets/root/Runtime](Unity-MCP-Plugin/Assets/root/Runtime).
    - Editor logic: `Assets/root/Editor`.
    - Tests: `Assets/root/Tests`.
- **Unity-MCP-Server**: ASP.NET Core bridging LLMs and Unity.
    - Entry point: [Program.cs](Unity-MCP-Server/src/Program.cs) (or similar in project root/src).
    - SignalR Hub: `RemoteApp` (referenced in CLAUDE.md).
- **Installer**: [Installer/](Installer/) wraps the package installation.
- **Unity-Tests**: [Unity-Tests/](Unity-Tests/) contains projects for different Unity versions (2022, 2023, 6000) linking locally to the Plugin.

## Build and Test
- **Plugin**:
    - Auto-compiles in Unity.
    - Run tests: [commands/run-unity-tests.ps1](commands/run-unity-tests.ps1).
    - Editor Tests: `Assets/root/Tests/Editor`.
- **Server**:
    - Build: `.\Unity-MCP-Server\build-all.ps1`.
    - Run: `dotnet run --project Unity-MCP-Server/com.IvanMurzak.Unity.MCP.Server.csproj`.
- **Commands**: See [commands/](commands/) for utility scripts (release, tests).

## Project Conventions
- **MCP Tools**: Implemented using attributes in the Plugin. Reflection-based access via `ReflectorNet`.
- **Documentation**:
    - [Unity-MCP.wiki](Unity-MCP.wiki/) for user docs.
    - [docs/](docs/) for translations and repo docs.
    - See `CLAUDE.md` in subdirectories for specific agent notes.
- **Versioning**: `package.json` in `Unity-MCP-Plugin/Assets/root/package.json`.

## Integration Points
- **Communication**: SignalR between Server and Plugin.
- **Dependencies**: OpenUPM for external packages.

## Security
- **Server Transport**: Configurable via `--client-transport` (`stdio` or `streamableHttp`).
