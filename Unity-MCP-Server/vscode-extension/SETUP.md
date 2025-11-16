# Unity MCP Extension - Development & Deployment Guide

This guide covers setting up the development environment and deploying the Unity MCP VS Code extension.

## Prerequisites

### Required Software

1. **Node.js** (v20.x or higher)
   - Download from: https://nodejs.org/
   - Verify: `node --version`

2. **npm** (comes with Node.js)
   - Verify: `npm --version`

3. **PowerShell** (v7.0 or higher)
   - Windows: Usually pre-installed
   - macOS/Linux: Install from https://github.com/PowerShell/PowerShell

4. **.NET SDK 9.0** (for building the MCP server)
   - Download from: https://dotnet.microsoft.com/download
   - Verify: `dotnet --version`

5. **Visual Studio Code**
   - Download from: https://code.visualstudio.com/

### Optional Tools

- **vsce** - VS Code Extension Manager (will be installed automatically by the script)
  ```bash
  npm install -g @vscode/vsce
  ```

## Development Setup

### 1. Install Dependencies

Navigate to the extension directory and install npm packages:

```bash
cd vscode-extension
npm install
```

### 2. Open in VS Code

```bash
code .
```

### 3. Compile TypeScript

```bash
npm run compile
```

Or watch for changes:

```bash
npm run watch
```

### 4. Debug the Extension

1. Press `F5` or go to Run > Start Debugging
2. A new VS Code window will open with the extension loaded
3. Test the extension commands via Command Palette (`Ctrl+Shift+P`)

## Building the Extension

### Quick Build (Local Testing)

```bash
npm run compile
npm run package
```

This creates a `.vsix` file you can install locally:

```bash
code --install-extension unity-mcp-0.22.1.vsix
```

### Full Build with Binaries

Use the PowerShell deployment script:

```bash
# Build everything and create package (no publishing)
.\deploy.ps1 -SkipPublish

# Skip .NET build, use existing binaries
.\deploy.ps1 -SkipBuild -SkipPublish
```

## Publishing to VS Code Marketplace

### 1. Get a Personal Access Token (PAT)

1. Go to: https://dev.azure.com/
2. Sign in with your Microsoft account
3. Create a new organization (if needed)
4. Go to User Settings > Personal Access Tokens
5. Click "New Token"
6. Configure:
   - Name: "VS Code Extension Publishing"
   - Organization: All accessible organizations
   - Expiration: Custom (recommended: 90 days)
   - Scopes: **Marketplace > Manage**
7. Copy the token (you won't see it again!)

### 2. Set Environment Variable (Optional)

```powershell
# Windows PowerShell
$env:VSCE_PAT = "your-token-here"

# Or set it permanently via System Properties
```

### 3. Create Publisher Account

```bash
vsce create-publisher IvanMurzak
```

Or use the web interface: https://marketplace.visualstudio.com/manage

### 4. Deploy

Full deployment with publishing:

```powershell
# Full build and publish
.\deploy.ps1 -Token "your-pat-token"

# Or if VSCE_PAT is set
.\deploy.ps1
```

Deployment script options:

- `-SkipBuild` - Skip building .NET binaries (use existing)
- `-SkipPublish` - Only create .vsix package
- `-Token <PAT>` - Provide PAT token directly

## Script Workflow

The `deploy.ps1` script performs these steps:

1. ✅ **Build .NET Server** - Compiles Unity MCP Server for all platforms
2. ✅ **Copy Binaries** - Moves server executables to extension folder
3. ✅ **Install Dependencies** - Runs `npm install`
4. ✅ **Compile TypeScript** - Builds the extension code
5. ✅ **Package Extension** - Creates `.vsix` file
6. ✅ **Publish** - Uploads to VS Code Marketplace (optional)

## Version Management

### Update Version

1. Edit [package.json](package.json):
   ```json
   {
     "version": "0.22.2"
   }
   ```

2. Update [CHANGELOG.md](CHANGELOG.md):
   ```markdown
   ## [0.22.2] - 2025-01-17
   ### Changed
   - ...
   ```

3. Also update the .NET project version in [com.IvanMurzak.Unity.MCP.Server.csproj](../com.IvanMurzak.Unity.MCP.Server.csproj):
   ```xml
   <Version>0.22.2</Version>
   ```

### Semantic Versioning

- **MAJOR** (1.0.0): Breaking changes
- **MINOR** (0.1.0): New features, backward compatible
- **PATCH** (0.0.1): Bug fixes

## Testing the Extension

### Local Testing

1. **Build & Install**:
   ```bash
   .\deploy.ps1 -SkipPublish
   code --install-extension unity-mcp-0.22.1.vsix
   ```

2. **Verify Installation**:
   - Open VS Code
   - Go to Extensions panel
   - Search for "Unity MCP"
   - Should show as installed

3. **Test Commands**:
   - `Ctrl+Shift+P` → "Unity MCP: Start Server"
   - Check status bar indicator
   - View > Output > Unity MCP (for logs)

### Integration Testing

1. Open a Unity project with Unity-MCP Plugin
2. Start the extension
3. Verify connection in Unity (`Window/AI Game Developer`)
4. Test MCP tools with Claude Code or compatible client

## Troubleshooting

### Build Errors

**"npm not found"**:
- Install Node.js from https://nodejs.org/

**"tsc not found"**:
- Run `npm install` in the extension folder

**"vsce not found"**:
- Install: `npm install -g @vscode/vsce`

### Publishing Errors

**"401 Unauthorized"**:
- Check your PAT token is valid
- Ensure token has "Marketplace > Manage" scope

**"Publisher not found"**:
- Create publisher at: https://marketplace.visualstudio.com/manage

**"Extension already exists"**:
- Increment version number in `package.json`

### Platform-Specific Issues

**macOS**: Make binaries executable:
```bash
chmod +x server/osx-*/unity-mcp-server
```

**Linux**: Same as macOS:
```bash
chmod +x server/linux-*/unity-mcp-server
```

## File Structure

```
vscode-extension/
├── src/
│   └── extension.ts          # Main extension code
├── out/                       # Compiled JavaScript (generated)
├── server/                    # MCP server binaries (generated)
│   ├── win-x64/
│   ├── win-x86/
│   ├── win-arm64/
│   ├── linux-x64/
│   ├── linux-arm64/
│   ├── osx-x64/
│   └── osx-arm64/
├── node_modules/             # npm dependencies (generated)
├── package.json              # Extension manifest
├── tsconfig.json             # TypeScript config
├── .eslintrc.json           # ESLint config
├── .vscodeignore            # Files to exclude from package
├── .gitignore               # Git ignore rules
├── README.md                # User documentation
├── CHANGELOG.md             # Version history
├── SETUP.md                 # This file
└── deploy.ps1               # Deployment script
```

## Resources

- [VS Code Extension API](https://code.visualstudio.com/api)
- [Publishing Extensions](https://code.visualstudio.com/api/working-with-extensions/publishing-extension)
- [Extension Manifest](https://code.visualstudio.com/api/references/extension-manifest)
- [vsce Documentation](https://github.com/microsoft/vscode-vsce)

## Support

- GitHub Issues: https://github.com/IvanMurzak/Unity-MCP/issues
- Discord: https://discord.gg/cfbdMZX99G

---

Happy coding! 🚀
