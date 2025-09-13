# Installation Guide

This comprehensive guide covers all installation methods for Unity-MCP, from beginners to advanced users.

## 📋 System Requirements

### Unity Requirements
- **Unity 2022.3 LTS** or newer (recommended)
- **Unity 2021.3 LTS** (supported)
- **Unity 6000.2** (latest, fully supported)

### Operating System Support
- **Windows** 10/11 (x64, ARM64)
- **macOS** 10.15+ (Intel, Apple Silicon)
- **Linux** (x64, ARM64)

### Runtime Requirements
- **.NET 8.0** or newer (for server)
- **Docker** (optional, recommended for server deployment)

## 🎯 Installation Overview

Unity-MCP consists of two main components that need to be installed:

1. **Unity Plugin** - Installed in Unity Editor
2. **MCP Server** - Runs separately and communicates with AI clients

## 🔧 Part 1: Unity Plugin Installation

### Option A: Unity Asset Store (Easiest)

1. **Open Unity Editor**
2. **Navigate to Asset Store**:
   - Go to **Window → Asset Store**
   - Or visit [Unity Asset Store](https://u3d.as/3wsw) directly
3. **Search and Install**:
   - Search for "Unity MCP" or "AI Game Developer"
   - Click **Download** then **Import**
4. **Import Package**:
   - Select all files in the import dialog
   - Click **Import**

### Option B: OpenUPM (Recommended for Developers)

1. **Open Package Manager**:
   - In Unity, go to **Window → Package Manager**
2. **Add Package from Git URL**:
   - Click the **"+"** button → **Add package from git URL**
   - Enter: `https://github.com/IvanMurzak/Unity-MCP.git?path=/Unity-MCP-Plugin/Assets/root`
3. **Wait for Installation**:
   - Unity will automatically download and install the package

### Option C: Manual Git Installation

1. **Clone Repository**:
   ```bash
   git clone https://github.com/IvanMurzak/Unity-MCP.git
   ```
2. **Copy Plugin Files**:
   - Copy `Unity-MCP-Plugin/Assets/root/` to your project's `Assets/` folder
3. **Refresh Unity**:
   - Return to Unity Editor and let it import the files

### Option D: Unity Package Manager via Git URL

1. **Open Package Manager**
2. **Add Package from Git URL**:
   - Click **"+"** → **Add package from git URL**
   - Enter: `https://github.com/IvanMurzak/Unity-MCP.git?path=/Unity-MCP-Plugin/Assets/root`

### Verify Plugin Installation

1. **Check Menu**: Look for **Window → Unity MCP** in the menu bar
2. **Console Messages**: Check Console for "Unity MCP Plugin loaded" message
3. **Package Manager**: Verify "Unity MCP" appears in Package Manager

## ⚙️ Part 2: MCP Server Installation

### Option A: Docker (Recommended)

Docker provides the easiest and most reliable deployment option.

#### Prerequisites
- Install [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Ensure Docker is running

#### Installation
```bash
# Pull the latest image
docker pull ivanmurzakdev/unity-mcp-server:latest

# Run with default settings
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest
```

#### Advanced Docker Usage
```bash
# Run with custom port
docker run -p 9090:9090 -e UNITY_MCP_PORT=9090 ivanmurzakdev/unity-mcp-server:latest

# Run with STDIO transport
docker run -e UNITY_MCP_CLIENT_TRANSPORT=stdio ivanmurzakdev/unity-mcp-server:latest

# Run in background
docker run -d -p 8080:8080 --name unity-mcp ivanmurzakdev/unity-mcp-server:latest
```

### Option B: .NET Global Tool

#### Prerequisites
- Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer

#### Installation
```bash
# Install globally
dotnet tool install -g com.IvanMurzak.Unity.MCP.Server

# Verify installation
unity-mcp-server --help

# Run server
unity-mcp-server
```

#### Update Global Tool
```bash
# Update to latest version
dotnet tool update -g com.IvanMurzak.Unity.MCP.Server
```

### Option C: Build from Source

#### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

#### Steps
```bash
# Clone repository
git clone https://github.com/IvanMurzak/Unity-MCP.git
cd Unity-MCP/Unity-MCP-Server

# Build project
dotnet build -c Release

# Run server
dotnet run --project Unity.MCP.Server
```

### Verify Server Installation

Test the server is working:
```bash
# Check server health (if running on HTTP)
curl http://localhost:8080/health

# Or check Docker container
docker ps | grep unity-mcp
```

## 🤖 Part 3: AI Client Configuration

### Claude Desktop App

1. **Locate Configuration File**:
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`  
   - **Linux**: `~/.config/Claude/claude_desktop_config.json`

2. **Add Configuration**:
   ```json
   {
     "mcpServers": {
       "unity-mcp": {
         "command": "docker",
         "args": [
           "run", "-i", "--rm", 
           "-p", "8080:8080",
           "ivanmurzakdev/unity-mcp-server:latest"
         ],
         "env": {
           "UNITY_MCP_CLIENT_TRANSPORT": "stdio"
         }
       }
     }
   }
   ```

3. **Restart Claude Desktop**

### VS Code with MCP Extension

1. **Install MCP Extension** from VS Code marketplace
2. **Configure Settings**:
   ```json
   {
     "mcp.servers": {
       "unity-mcp": {
         "command": "unity-mcp-server",
         "transport": "stdio"
       }
     }
   }
   ```

### Claude Code (CLI)

For each platform, replace `<unityProjectPath>` with your Unity project path:

#### Windows
```bash
claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" client-transport=stdio
```

#### macOS (Apple Silicon)
```bash
claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" client-transport=stdio
```

#### macOS (Intel)
```bash
claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" client-transport=stdio
```

#### Linux
```bash
claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" client-transport=stdio
```

## ✅ Installation Verification

### Complete System Test

1. **Start Unity Editor** with your project
2. **Launch MCP Server**:
   ```bash
   docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest
   ```
3. **Open AI Client** (Claude Desktop, VS Code, etc.)
4. **Test Connection**:
   - Ask AI: "Can you tell me about my Unity scene?"
   - You should receive information about your current scene

### Expected Behavior

✅ **Success Indicators**:
- Unity Console shows "MCP Plugin connected to server"
- AI client recognizes Unity-MCP tools
- AI can respond to Unity-related questions
- You can create objects through AI commands

❌ **Failure Indicators**:
- "Connection refused" errors
- AI doesn't see Unity tools
- Timeout messages in Unity Console

## 🔄 Updating Unity-MCP

### Update Unity Plugin
- **Asset Store**: Re-download from Asset Store
- **OpenUPM**: Update through Package Manager
- **Git**: Pull latest changes and re-import

### Update MCP Server
- **Docker**: `docker pull ivanmurzakdev/unity-mcp-server:latest`
- **.NET Tool**: `dotnet tool update -g com.IvanMurzak.Unity.MCP.Server`

## 🚨 Troubleshooting Installation

### Common Unity Plugin Issues

**Plugin not visible in menu**:
- Check Console for import errors
- Verify package.json exists in plugin folder
- Restart Unity Editor

**Import errors**:
- Check Unity version compatibility
- Ensure all dependencies are met
- Try clearing Library folder and reimporting

### Common Server Issues

**Port already in use**:
```bash
# Use different port
docker run -p 8081:8081 -e UNITY_MCP_PORT=8081 ivanmurzakdev/unity-mcp-server:latest
```

**Docker not found**:
- Install Docker Desktop
- Ensure Docker service is running
- Check Docker is in system PATH

**.NET tool not found**:
- Verify .NET 8.0+ is installed
- Check PATH includes .NET tools directory
- Restart terminal after installing .NET

### Getting Help

If you encounter issues:

1. **Check the logs** in Unity Console and server output
2. **Review** our [Troubleshooting guide](Troubleshooting)
3. **Search** existing [GitHub Issues](https://github.com/IvanMurzak/Unity-MCP/issues)
4. **Create** a new issue with:
   - Your operating system
   - Unity version
   - Installation method used
   - Complete error messages
   - Steps to reproduce

## 🎉 Next Steps

Once installation is complete:

- **Quick Start**: Follow our [Getting Started guide](Getting-Started)
- **Configuration**: Customize your setup with our [Configuration guide](Configuration)
- **Learn Tools**: Explore available [AI Tools](AI-Tools-Reference)
- **Examples**: Try hands-on [Examples & Tutorials](Examples-and-Tutorials)

---

**Installation complete?** Great! Head over to [Getting Started](Getting-Started) to begin using Unity-MCP with AI assistance!