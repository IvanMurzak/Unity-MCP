# Getting Started with Unity-MCP

Welcome to Unity-MCP! This guide will help you set up and start using AI assistance in your Unity projects in just a few minutes.

## 🎯 What You'll Accomplish

By the end of this guide, you'll have:
- Unity-MCP Plugin installed in Unity Editor
- MCP Server running and connected
- AI client configured and ready to use
- Your first AI-assisted Unity task completed

## ⚡ Quick Start (5 minutes)

### Prerequisites

- **Unity 2022.3+** (LTS recommended)
- **Claude Desktop App** or **VS Code with MCP extension**
- **Docker** (recommended) or **.NET 8.0+**

### Step 1: Install Unity Plugin

Choose one of these installation methods:

#### Option A: Unity Asset Store (Easiest)
1. Open Unity Editor
2. Go to **Window → Asset Store**
3. Search for "Unity MCP"
4. Click **Download** and **Import**

#### Option B: OpenUPM (Recommended for developers)
1. Open Unity Editor
2. Go to **Window → Package Manager**
3. Click **"+" → Add package from git URL**
4. Enter: `https://github.com/IvanMurzak/Unity-MCP.git?path=/Unity-MCP-Plugin/Assets/root`

#### Option C: Git URL
1. In Package Manager, click **"+" → Add package from git URL**
2. Enter: `https://github.com/IvanMurzak/Unity-MCP.git?path=/Unity-MCP-Plugin/Assets/root`

### Step 2: Launch MCP Server

#### Using Docker (Recommended)
```bash
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest
```

#### Using .NET Tool
```bash
dotnet tool install -g Unity.MCP.Server
unity-mcp-server
```

### Step 3: Configure AI Client

#### For Claude Desktop
1. Open Claude Desktop settings
2. Add MCP server configuration:
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "docker",
      "args": ["run", "-p", "8080:8080", "ivanmurzakdev/unity-mcp-server:latest"],
      "env": {}
    }
  }
}
```

#### For VS Code
1. Install the MCP extension
2. Configure the Unity-MCP server endpoint: `http://localhost:8080`

### Step 4: Test Your Setup

1. **Open Unity Editor** with any project
2. **Start your AI client** (Claude Desktop or VS Code)
3. **Ask the AI**: "Can you create a red cube in the Unity scene?"
4. **Watch the magic happen!** ✨

## 🎮 Your First AI Tasks

Once everything is connected, try these beginner-friendly tasks:

### Create Basic Objects
```
"Create a blue sphere at position (0, 5, 0)"
"Add a directional light to the scene"
"Create a plane as the ground"
```

### Modify Existing Objects
```
"Make the Main Camera look at the cube"
"Change the cube's material to metallic"
"Add a Rigidbody to the sphere so it falls"
```

### Scene Organization
```
"Create an empty GameObject called 'Environment'"
"Move all terrain objects under the Environment parent"
"Organize the scene hierarchy"
```

## 🔧 Basic Configuration

After installation, Unity-MCP works with default settings, but you might want to customize:

### Unity Plugin Settings
1. Go to **Window → Unity MCP → Settings**
2. Configure:
   - **Server Port**: 8080 (default)
   - **Auto-connect**: Enable for automatic connection
   - **Connection Timeout**: 10 seconds (default)

### Server Configuration
The server accepts several environment variables:
- `UNITY_MCP_PORT=8080` - Connection port
- `UNITY_MCP_CLIENT_TRANSPORT=http` - Transport protocol
- `UNITY_MCP_PLUGIN_TIMEOUT=10000` - Plugin timeout in milliseconds

## 🚀 Next Steps

Congratulations! You now have Unity-MCP running. Here's what to explore next:

### Learn More
- [**AI Tools Reference**](AI-Tools-Reference) - Discover all available AI tools
- [**Configuration Guide**](Configuration) - Customize your setup
- [**Examples & Tutorials**](Examples-and-Tutorials) - Follow hands-on tutorials

### Advanced Setup
- [**Server Setup**](Server-Setup) - Docker deployment and advanced configuration
- [**Custom Tools Development**](Custom-Tools-Development) - Create your own AI tools
- [**API Reference**](API-Reference) - Technical documentation

### Get Help
- [**Troubleshooting**](Troubleshooting) - Common issues and solutions
- [**FAQ**](FAQ) - Frequently asked questions
- [**GitHub Issues**](https://github.com/IvanMurzak/Unity-MCP/issues) - Report bugs or request features

## 🛠️ Troubleshooting Quick Fixes

### Plugin Not Connecting?
1. Check that the MCP Server is running on port 8080
2. Verify no firewall is blocking the connection
3. Restart Unity Editor

### AI Not Responding?
1. Verify your AI client supports MCP
2. Check MCP server configuration in your client
3. Look for error messages in the client console

### Server Won't Start?
1. Make sure port 8080 is available
2. Check Docker is running (if using Docker)
3. Try a different port with `--port 8081`

## 💡 Tips for Success

1. **Start Simple** - Begin with basic object creation before complex tasks
2. **Be Specific** - The more detailed your requests, the better the results
3. **Use Descriptions** - Add descriptive comments to your code for better AI understanding
4. **Experiment** - Try different types of requests to discover capabilities
5. **Share Context** - Tell the AI about your project goals for better assistance

---

**Ready for more advanced features?** Check out our [Examples & Tutorials](Examples-and-Tutorials) or dive into [Custom Tools Development](Custom-Tools-Development)!

**Need help?** Visit our [Troubleshooting guide](Troubleshooting) or ask questions in [GitHub Issues](https://github.com/IvanMurzak/Unity-MCP/issues).