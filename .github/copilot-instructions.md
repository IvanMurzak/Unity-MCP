# Unity MCP - AI Game Developer

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Project Overview

Unity MCP is a Model Context Protocol (MCP) implementation that bridges AI assistants with Unity Editor and Unity games. The project consists of two main components:

1. **Unity-MCP-Server** - A .NET 9.0 MCP server that communicates with AI clients
2. **Unity-MCP-Plugin** - A Unity Editor plugin that integrates with Unity projects

## Critical Requirements

- **Project path cannot contain spaces** (Unity limitation)
- **.NET 9.0 SDK required** for MCP Server builds
- **Unity Editor required** for plugin development and testing (not available in standard CI environments)
- **Docker required** for container builds

## Working Effectively

### Prerequisites and Setup

Install .NET 9.0 SDK:
```bash
# Linux/WSL - Download and install .NET 9
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest
export PATH="$HOME/.dotnet:$PATH"

# Alternative: Use package manager
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

Verify installation:
```bash
dotnet --version  # Should show 9.0.x
dotnet --list-sdks  # Should include 9.0.x
```

**CRITICAL**: The project requires .NET 9.0. Builds will fail with error NETSDK1045 if using .NET 8 or lower.

### Building the MCP Server

Navigate to server directory:
```bash
cd Unity-MCP-Server
```

**NEVER CANCEL**: Restore dependencies - takes ~11 seconds. Set timeout to 60+ seconds:
```bash
dotnet restore
```

**NEVER CANCEL**: Build the server - takes ~10 seconds. Set timeout to 60+ seconds:
```bash
dotnet build -c Release
```

**NEVER CANCEL**: Build for all platforms - takes 2-5 minutes when working. Set timeout to 10+ minutes:
```bash
chmod +x build-all.sh
./build-all.sh Release
```

**Expected behavior**:
- With .NET 9: Creates 7 platform executables successfully
- With .NET 8 or lower: Fails immediately with NETSDK1045 error

The multi-platform build creates executables for:
- Windows (x64, x86, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

Output location: `Unity-MCP-Server/publish/{runtime}/`

### Docker Build

**WARNING**: Docker builds may fail due to SSL certificate issues with NuGet in CI environments. Error message: "The remote certificate is invalid because of errors in the certificate chain: UntrustedRoot"

**NEVER CANCEL**: Docker build - may take 10+ minutes. Set timeout to 20+ minutes:
```bash
cd Unity-MCP-Server
docker build -t unity-mcp-server .
```

**Expected failures in CI**:
- SSL certificate errors during NuGet restore
- Network connectivity issues in sandboxed environments

Run the server:
```bash
docker run -p 8080:8080 unity-mcp-server
```

### Unity Plugin Development

**CRITICAL LIMITATION**: Unity Editor is not available in standard CI environments. You cannot build, test, or run Unity projects in headless environments without Unity Editor installation.

Unity versions supported:
- Unity 2022.3.62f3
- Unity 2023.2.22f1
- Unity 6000.3.1f1 (Unity 6)

The plugin is located in: `Unity-MCP-Plugin/`

Key files:
- `Assets/root/package.json` - Package configuration
- `Assets/root/Runtime/` - Runtime code
- `Assets/root/Editor/` - Editor-specific code
- `Assets/root/Tests/` - Test files

## Testing

### MCP Server Tests

Run server unit tests (if any):
```bash
cd Unity-MCP-Server
dotnet test
```

### Unity Plugin Tests

**CRITICAL**: Unity tests require Unity Editor and cannot run in standard environments.

The GitHub Actions workflow uses Unity CI containers:
- Edit Mode tests: `unityci/editor:ubuntu-{version}-base-3`
- Play Mode tests: `unityci/editor:ubuntu-{version}-base-3`
- Standalone tests: `unityci/editor:ubuntu-{version}-base-3`

Unity test locations:
- `Assets/root/Tests/Editor/` - Editor tests (run in Unity Editor)
- `Assets/root/Tests/Runtime/` - Runtime tests (run during play mode)

Key test files:
- `ConnectionManagerTests.cs` - Tests MCP connection functionality
- `TestGameObjectUtils.cs` - Tests Unity GameObject utilities
- `DemoTest.cs` - Basic runtime test example

**NEVER CANCEL**: Unity tests can take 15-30 minutes per mode. Set timeout to 45+ minutes.

Test modes available:
- `editmode` - Tests that run in Unity Editor
- `playmode` - Tests that run during play mode
- `standalone` - Tests that run in built games

### Manual Validation

Since Unity Editor is not available in CI environments, document any Unity-related changes and note that manual validation is required in a Unity project environment.

## Validation Scenarios

After making changes to the MCP Server:

1. **Basic Server Functionality**:
   ```bash
   cd Unity-MCP-Server
   dotnet run --project com.IvanMurzak.Unity.MCP.Server.csproj
   # Should start server on port 8080
   ```

2. **Docker Container**:
   ```bash
   docker run -p 8080:8080 unity-mcp-server
   # Should start and expose server on localhost:8080
   ```

3. **Multi-platform Build**:
   ```bash
   ./build-all.sh Release
   # Should create executables for all platforms without errors
   ```

After making changes to Unity Plugin:
- **Manual Unity Editor testing required** - Cannot be automated in headless environments
- Test in Unity Editor with supported Unity versions
- Verify MCP client connection works
- Test basic AI tool functionality (create GameObjects, modify scene, etc.)

## Build Timing Expectations

**Always use these timeout values to prevent premature cancellation:**

| Operation | Expected Time | Minimum Timeout |
|-----------|---------------|-----------------|
| dotnet restore | 5-15 seconds | 60 seconds |
| dotnet build | 5-15 seconds | 60 seconds |
| Multi-platform build | 2-5 minutes | 10 minutes |
| Docker build | 10-15 minutes | 20 minutes |
| Unity Editor tests | 15-30 minutes | 45 minutes |

**NEVER CANCEL ANY BUILD OR TEST COMMAND** - Wait for completion or documented failure.

## CI/CD Integration

The repository uses GitHub Actions with these key workflows:

- `release.yml` - Main release pipeline
- `test_unity_plugin.yml` - Unity plugin testing across versions
- `test_pull_request.yml` - PR validation
- `deploy_server_executables.yml` - Server deployment

Unity CI uses containers from `unityci/editor` with specific Unity versions.

## Common Tasks

### Adding New MCP Tools

1. Create a class with `[McpPluginToolType]` attribute
2. Add methods with `[McpPluginTool]` attribute
3. Use `[Description]` attributes for AI understanding
4. Test in Unity Editor with MCP client connection

### Debugging MCP Communication

1. Check server logs in console output
2. Verify Unity plugin connection status
3. Test with different MCP clients (Claude, Cursor, etc.)
4. Ensure port 8080 is available and not blocked

**MCP Inspector Tool**:
```bash
# Install and run MCP inspector for debugging
npx @modelcontextprotocol/inspector
```

This tool helps debug MCP protocol communication between clients and servers.

### Release Process

1. Update version in `Unity-MCP-Plugin/Assets/root/package.json`
2. Push to main branch triggers release workflow
3. **NEVER CANCEL**: Release builds take 20+ minutes. Set timeout to 45+ minutes
4. GitHub Actions creates releases with:
   - Unity installer package
   - Multi-platform MCP server executables
   - Docker images

## Frequently Used Commands Output

### Repository Structure
```
Unity-MCP/
├── Unity-MCP-Server/          # .NET 9 MCP Server
│   ├── src/                   # Server source code
│   ├── build-all.sh           # Multi-platform build script
│   ├── com.IvanMurzak.Unity.MCP.Server.csproj
│   └── Dockerfile
├── Unity-MCP-Plugin/          # Unity Editor Plugin
│   ├── Assets/root/           # Plugin source code
│   ├── ProjectSettings/       # Unity project settings
│   └── Packages/              # Unity packages
├── Installer/      # Unity package installer
└── .github/workflows/         # CI/CD pipelines
```

### Key Package Info
> Unity-MCP-Plugin/Assets/root/package.json
```json
{
    "name": "com.ivanmurzak.unity.mcp",
    "displayName": "AI Game Developer (Unity MCP Plugin)",
    "author": {
        "name": "IvanMurzak",
        "url": "https://github.com/IvanMurzak"
    },
    "keywords": [
        "AI",
        "AI Integration",
        "MCP",
        "Unity MCP"
    ],
    "version": "0.17.1",
    "unity": "2022.3",
    "dependencies": {
        "com.unity.test-framework": "1.1.33",
        "com.unity.modules.uielements": "1.0.0",
        "extensions.unity.playerprefsex": "2.0.2",
        "org.nuget.microsoft.bcl.memory": "9.0.7",
        "org.nuget.microsoft.aspnetcore.signalr.client": "9.0.7",
        "org.nuget.microsoft.aspnetcore.signalr.protocols.json": "9.0.7",
        "org.nuget.microsoft.codeanalysis.csharp": "4.13.0",
        "org.nuget.microsoft.extensions.caching.abstractions": "9.0.7",
        "org.nuget.microsoft.extensions.dependencyinjection.abstractions": "9.0.7",
        "org.nuget.microsoft.extensions.hosting": "9.0.7",
        "org.nuget.microsoft.extensions.hosting.abstractions": "9.0.7",
        "org.nuget.microsoft.extensions.logging.abstractions": "9.0.7",
        "org.nuget.r3": "1.3.0",
        "org.nuget.system.text.json": "9.0.7"
    },
    "scopedRegistries": [
        {
            "name": "package.openupm.com",
            "url": "https://package.openupm.com",
            "scopes": [
                "org.nuget",
                "extensions.unity"
            ]
        }
    ]
}
```

## Known Limitations

- **Unity Editor required**: Cannot test Unity plugin without Unity Editor installation
- **Docker builds may fail**: SSL certificate issues (NETSDK1045, UntrustedRoot errors) in CI environments
- **Path restrictions**: Unity project paths cannot contain spaces
- **Manual validation needed**: Unity functionality requires manual testing in Editor

Always validate your changes work correctly before committing. For Unity plugin changes, test manually in Unity Editor. For server changes, verify builds complete and basic functionality works.