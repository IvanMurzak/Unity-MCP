# Troubleshooting Guide

This guide helps you diagnose and resolve common issues with Unity-MCP. Issues are organized by category with step-by-step solutions.

## 🚨 Emergency Quick Fixes

### Unity-MCP Not Working At All?
1. **Check Unity Console** for error messages
2. **Verify Server is Running**: Visit `http://localhost:8080/health`
3. **Restart Everything**: Unity Editor → MCP Server → AI Client
4. **Check Port 8080** isn't blocked by firewall

### Can't Connect to Server?
```bash
# Test server connectivity
curl -f http://localhost:8080/health

# Check what's using port 8080
netstat -tulpn | grep 8080

# Restart server with different port
unity-mcp-server --port 8081
```

## 🔧 Installation Issues

### Unity Plugin Installation Problems

#### Plugin Not Appearing in Menu
**Symptoms**: No "Unity MCP" menu in Window menu

**Solution**:
1. **Check Console for Errors**:
   - Look for compilation errors
   - Check for missing dependencies
2. **Verify Installation**:
   ```bash
   # Check if files exist
   ls -la Assets/Unity-MCP/
   
   # Or in Package Manager location
   ls -la Packages/com.ivanmurzak.unity.mcp/
   ```
3. **Reimport Package**:
   - Right-click plugin folder → Reimport
   - Or Assets → Reimport All
4. **Check Unity Version**:
   - Unity 2022.3+ required
   - Update Unity if necessary

#### Package Manager Installation Fails
**Symptoms**: "Package not found" or Git URL errors

**Solutions**:
```bash
# Try different Git URL formats
https://github.com/IvanMurzak/Unity-MCP.git?path=/Unity-MCP-Plugin/Assets/root

# Or specific branch/commit
https://github.com/IvanMurzak/Unity-MCP.git?path=/Unity-MCP-Plugin/Assets/root#main

# For SSH access issues, use HTTPS instead
```

#### Asset Store Installation Issues
**Symptoms**: Download fails or import errors

**Solutions**:
1. **Clear Asset Store Cache**:
   - Windows: `%APPDATA%\Unity\Asset Store-5.x\`
   - macOS: `~/Library/Unity/Asset Store-5.x/`
   - Delete cache folders and try again
2. **Retry Download**:
   - Go to Window → Package Manager
   - Select "My Assets"
   - Re-download Unity MCP

### MCP Server Installation Problems

#### .NET Tool Installation Fails
**Symptoms**: "Package not found" errors

**Solutions**:
```bash
# Update .NET to latest version
dotnet --version  # Should be 8.0+

# Clear NuGet cache
dotnet nuget locals all --clear

# Try alternative installation
dotnet tool install -g Unity.MCP.Server --version 1.0.0

# Install from local package
dotnet tool install -g --add-source ./nupkg Unity.MCP.Server
```

#### Docker Installation Issues
**Symptoms**: Image pull failures or container won't start

**Solutions**:
```bash
# Check Docker is running
docker version

# Pull image manually with verbose output
docker pull ivanmurzakdev/unity-mcp-server:latest --progress=plain

# Try alternative registries
docker pull ghcr.io/ivanmurzak/unity-mcp-server:latest

# Check disk space
docker system df

# Clean up if needed
docker system prune -f
```

## 🔌 Connection Issues

### Plugin Can't Connect to Server

#### Connection Refused Errors
**Symptoms**: "Connection refused" or timeout errors in Unity Console

**Diagnosis**:
```bash
# Check if server is running
curl -f http://localhost:8080/health

# Check server logs
docker logs unity-mcp-server

# Verify port is open
telnet localhost 8080
```

**Solutions**:
1. **Start MCP Server**:
   ```bash
   # Docker
   docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest
   
   # .NET Tool
   unity-mcp-server
   ```

2. **Check Port Configuration**:
   - Unity Plugin: Window → Unity MCP → Settings → Port: 8080
   - Server: Ensure same port in configuration

3. **Firewall Issues**:
   ```bash
   # Windows
   netsh advfirewall firewall add rule name="Unity-MCP" dir=in action=allow protocol=TCP localport=8080
   
   # macOS
   sudo pfctl -d  # Disable firewall temporarily for testing
   
   # Linux
   sudo ufw allow 8080/tcp
   ```

#### Wrong Port Configuration
**Symptoms**: Connection attempts on wrong port

**Solution**:
1. **Unity Plugin Configuration**:
   - Window → Unity MCP → Settings
   - Set Port to match server (default: 8080)

2. **Server Configuration**:
   ```bash
   # Start server on specific port
   unity-mcp-server --port 8080
   
   # Or with environment variable
   UNITY_MCP_PORT=8080 unity-mcp-server
   ```

#### Network/Proxy Issues
**Symptoms**: Connection works locally but not remotely

**Solutions**:
```bash
# Test remote connectivity
curl -f http://your-server-ip:8080/health

# Configure proxy if needed
export HTTP_PROXY=http://proxy.company.com:8080
export HTTPS_PROXY=https://proxy.company.com:8080

# Bind server to all interfaces
unity-mcp-server --host 0.0.0.0 --port 8080
```

### AI Client Connection Issues

#### Claude Desktop Not Connecting
**Symptoms**: Claude doesn't show Unity-MCP tools

**Solutions**:
1. **Check Configuration File**:
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - Linux: `~/.config/Claude/claude_desktop_config.json`

2. **Validate JSON Format**:
   ```bash
   # Validate JSON syntax
   python -m json.tool claude_desktop_config.json
   ```

3. **Correct Configuration**:
   ```json
   {
     "mcpServers": {
       "unity-mcp": {
         "command": "unity-mcp-server",
         "args": ["--client-transport", "stdio"],
         "env": {
           "UNITY_MCP_PORT": "8080"
         }
       }
     }
   }
   ```

4. **Restart Claude Desktop** after configuration changes

#### VS Code MCP Extension Issues
**Symptoms**: Extension doesn't recognize Unity-MCP server

**Solutions**:
1. **Install MCP Extension**: Search for "MCP" in VS Code extensions
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
3. **Reload Window**: Cmd/Ctrl + Shift + P → "Developer: Reload Window"

## 🎮 Unity Editor Issues

### Performance Problems

#### Unity Editor Freezing
**Symptoms**: Unity becomes unresponsive when using AI tools

**Solutions**:
1. **Enable Threading** in Unity-MCP settings:
   - Window → Unity MCP → Settings
   - Enable "Use Background Threading"

2. **Reduce Batch Size**:
   - Lower batch size in settings (default: 100 → 25)

3. **Check System Resources**:
   ```bash
   # Monitor memory usage
   top -p $(pgrep Unity)
   
   # Windows Task Manager
   tasklist | findstr Unity
   ```

#### Slow Tool Responses
**Symptoms**: AI tools take too long to execute

**Solutions**:
1. **Optimize Server Settings**:
   ```bash
   # Increase timeout
   unity-mcp-server --plugin-timeout 30000
   ```

2. **Check Network Latency**:
   ```bash
   # Test local latency
   ping localhost
   
   # Test server response time
   time curl -f http://localhost:8080/health
   ```

3. **Unity Editor Optimization**:
   - Close unnecessary windows
   - Reduce scene complexity for testing
   - Clear console logs

### Scene and GameObject Issues

#### GameObjects Not Created
**Symptoms**: AI reports success but no objects appear

**Solutions**:
1. **Check Active Scene**:
   - Verify you're looking at the correct scene
   - Switch to Scene view if in Game view

2. **Check Object Position**:
   - Objects might be created outside visible area
   - Search for objects by name in Hierarchy

3. **Verify Parent Hierarchy**:
   - Objects might be created under inactive parents
   - Check if parent GameObjects are active

#### Component Addition Fails
**Symptoms**: "Component not found" or "Cannot add component" errors

**Solutions**:
1. **Check Component Name**:
   ```csharp
   // Correct component names
   "Rigidbody" (not "RigidBody")
   "MeshRenderer" (not "Mesh Renderer")
   "BoxCollider" (not "Box Collider")
   ```

2. **Assembly Issues**:
   - Some components require specific assemblies
   - Check if component is available in current context

3. **Component Conflicts**:
   - Some components can't coexist (e.g., multiple colliders)
   - Remove conflicting components first

### Asset and Material Issues

#### Materials Not Applied
**Symptoms**: Material changes don't appear on objects

**Solutions**:
1. **Check Renderer Component**:
   - Ensure GameObject has MeshRenderer or SkinnedMeshRenderer
   - Verify renderer is enabled

2. **Material Assignment**:
   ```csharp
   // Check material is properly assigned
   var renderer = gameObject.GetComponent<MeshRenderer>();
   if (renderer != null && renderer.material != null)
   {
       Debug.Log("Material assigned: " + renderer.material.name);
   }
   ```

#### Asset Loading Failures
**Symptoms**: "Asset not found" errors

**Solutions**:
1. **Refresh Asset Database**:
   - Assets → Refresh (Ctrl/Cmd + R)

2. **Check Asset Paths**:
   ```csharp
   // Use relative paths from Assets folder
   "Assets/Materials/MyMaterial.mat"  // ✓ Correct
   "/Users/name/Project/Assets/..."   // ✗ Incorrect
   ```

3. **Asset Import Issues**:
   - Check Console for import errors
   - Reimport problematic assets

## 🖥️ Server-Side Issues

### Server Startup Problems

#### Server Won't Start
**Symptoms**: Server exits immediately or shows error messages

**Solutions**:
1. **Check Port Availability**:
   ```bash
   # Find what's using port 8080
   lsof -i :8080  # macOS/Linux
   netstat -ano | findstr :8080  # Windows
   
   # Use different port
   unity-mcp-server --port 8081
   ```

2. **Check Logs**:
   ```bash
   # Docker logs
   docker logs unity-mcp-server
   
   # .NET tool logs
   unity-mcp-server --log-level debug
   ```

3. **Permission Issues**:
   ```bash
   # Ensure user can bind to port
   # Ports < 1024 require root on Linux/macOS
   unity-mcp-server --port 8080  # Regular user
   sudo unity-mcp-server --port 80  # Root required
   ```

#### Docker Container Issues
**Symptoms**: Container starts but immediately exits

**Solutions**:
```bash
# Check container status
docker ps -a

# View container logs
docker logs unity-mcp-server

# Run container interactively for debugging
docker run -it --rm -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest /bin/bash

# Check resource limits
docker stats unity-mcp-server
```

### Server Performance Issues

#### High CPU/Memory Usage
**Symptoms**: Server consuming excessive resources

**Solutions**:
1. **Monitor Resource Usage**:
   ```bash
   # System monitoring
   htop  # Linux/macOS
   
   # Docker monitoring
   docker stats unity-mcp-server
   ```

2. **Optimize Configuration**:
   ```bash
   # Reduce concurrent connections
   unity-mcp-server --max-connections 5
   
   # Increase timeouts to reduce retries
   unity-mcp-server --plugin-timeout 15000
   ```

3. **Memory Leaks**:
   - Restart server periodically
   - Monitor for increasing memory usage over time
   - Check for long-running operations

#### Server Becomes Unresponsive
**Symptoms**: Server stops responding to requests

**Solutions**:
1. **Health Check**:
   ```bash
   # Test server health
   curl -f http://localhost:8080/health
   
   # Force restart if unresponsive
   docker restart unity-mcp-server
   ```

2. **Check Logs for Errors**:
   ```bash
   # Look for error patterns
   docker logs unity-mcp-server | grep -i error
   
   # Check for deadlocks or timeouts
   docker logs unity-mcp-server | grep -i timeout
   ```

## 🤖 AI Client Issues

### Tool Discovery Problems

#### AI Doesn't See Unity Tools
**Symptoms**: AI responds "I don't have access to Unity tools"

**Solutions**:
1. **Verify MCP Connection**:
   - Check AI client shows MCP server as connected
   - Look for Unity-MCP in available tools list

2. **Check Server Status**:
   ```bash
   # Verify server is responding
   curl -f http://localhost:8080/mcp/tools
   ```

3. **Restart AI Client**:
   - Completely close and restart Claude Desktop/VS Code
   - Wait for MCP connection to establish

#### Outdated Tool Information
**Symptoms**: AI tries to use tools that don't exist

**Solutions**:
1. **Refresh Tool List**:
   - Restart AI client
   - Ask AI: "What Unity tools do you currently have access to?"

2. **Check Server Version**:
   ```bash
   # Update to latest server version
   docker pull ivanmurzakdev/unity-mcp-server:latest
   dotnet tool update -g Unity.MCP.Server
   ```

### Command Execution Issues

#### Tools Return Errors
**Symptoms**: AI tools consistently fail with error messages

**Solutions**:
1. **Check Unity Console** for detailed error messages

2. **Verify Unity State**:
   - Ensure Unity Editor is in correct mode (Edit vs Play)
   - Check if scene is properly loaded
   - Verify required objects exist

3. **Test Tool Manually**:
   ```csharp
   // Test in Unity Console window
   var tool = new Unity.MCP.Tools.GameObjectTool();
   var result = tool.CreateGameObject("Test", Vector3.zero);
   Debug.Log(result);
   ```

#### Partial Command Execution
**Symptoms**: Some parts of commands work, others fail

**Solutions**:
1. **Break Down Complex Commands**:
   - Ask AI to perform one operation at a time
   - Example: Create objects first, then modify them

2. **Check Operation Order**:
   - Some operations depend on others completing first
   - Ask AI to wait between operations

## 🔍 Debugging Techniques

### Enable Debug Logging

#### Unity Plugin Debug Logs
```csharp
// Enable verbose logging in Unity
Unity.MCP.Plugin.McpPluginSettings.Instance.VerboseLogging = true;

// Or via settings UI
// Window → Unity MCP → Settings → Enable Debug Logging
```

#### Server Debug Logs
```bash
# Enable debug logging
unity-mcp-server --log-level debug

# Docker with debug logging
docker run -p 8080:8080 -e UNITY_MCP_LOG_LEVEL=debug ivanmurzakdev/unity-mcp-server:latest
```

#### Network Traffic Logging
```bash
# Monitor HTTP traffic
tcpdump -i lo port 8080

# Or use Wireshark for detailed analysis
# Filter: tcp.port == 8080
```

### Testing Tools

#### Manual Server Testing
```bash
# Test server health endpoint
curl -v http://localhost:8080/health

# Test MCP tools endpoint
curl -v http://localhost:8080/mcp/tools

# Test specific tool (requires proper MCP format)
curl -X POST http://localhost:8080/mcp/call \
  -H "Content-Type: application/json" \
  -d '{"method": "tools/call", "params": {...}}'
```

#### Unity Integration Testing
```csharp
[UnityTest]
public IEnumerator TestMcpConnection()
{
    // Test basic connection
    var isConnected = Unity.MCP.Plugin.McpClient.Instance.IsConnected;
    Assert.IsTrue(isConnected, "MCP Client should be connected");
    
    yield return null;
}

[UnityTest]  
public IEnumerator TestToolExecution()
{
    // Test tool execution
    var tool = new Unity.MCP.Tools.GameObjectTool();
    var result = tool.CreateGameObject("TestObject", Vector3.zero);
    
    Assert.IsTrue(result.Contains("Success"), $"Tool execution failed: {result}");
    
    var testObject = GameObject.Find("TestObject");
    Assert.IsNotNull(testObject, "Created object should exist");
    
    yield return null;
}
```

## 📋 Common Error Messages

### Unity Console Errors

#### "MCP Plugin failed to connect to server"
**Cause**: Server not running or wrong port configuration
**Solution**: Start server and verify port matches Unity plugin settings

#### "Tool execution timeout"
**Cause**: Operation takes longer than configured timeout
**Solution**: Increase timeout in Unity MCP settings or server configuration

#### "GameObject not found: [name]"
**Cause**: Referenced GameObject doesn't exist or was destroyed
**Solution**: Verify object exists and use correct name/ID

### Server Log Errors

#### "Address already in use"
**Cause**: Another process is using port 8080
**Solution**: 
```bash
# Find and kill process using port
lsof -ti:8080 | xargs kill -9

# Or use different port
unity-mcp-server --port 8081
```

#### "Unity plugin connection timeout"
**Cause**: Unity Editor not responding or not running
**Solution**: Ensure Unity Editor is running and plugin is properly installed

### AI Client Errors

#### "MCP server not responding"
**Cause**: Server crashed or network connectivity issues
**Solution**: Check server status and restart if necessary

#### "Tool not found: [tool_name]"
**Cause**: Tool doesn't exist or server not properly connected
**Solution**: Verify server connection and tool availability

## 🆘 Getting Help

### Before Asking for Help

1. **Check This Guide** for your specific issue
2. **Search Existing Issues** on [GitHub](https://github.com/IvanMurzak/Unity-MCP/issues)
3. **Collect Debug Information**:
   - Unity version
   - Unity-MCP plugin version
   - Server version and deployment method
   - Operating system
   - Complete error messages
   - Steps to reproduce

### Where to Get Help

1. **GitHub Issues**: [Report bugs and request features](https://github.com/IvanMurzak/Unity-MCP/issues)
2. **Discussions**: [Community Q&A](https://github.com/IvanMurzak/Unity-MCP/discussions)
3. **Documentation**: [Complete documentation](https://github.com/IvanMurzak/Unity-MCP/wiki)

### Creating Effective Bug Reports

```markdown
**Unity-MCP Version**: [e.g., 1.0.0]
**Unity Version**: [e.g., 2023.2.5f1]
**Operating System**: [e.g., Windows 11, macOS 13.0, Ubuntu 22.04]
**Deployment**: [e.g., Docker, .NET Tool, Local Build]

**Expected Behavior**:
[What you expected to happen]

**Actual Behavior**: 
[What actually happened]

**Steps to Reproduce**:
1. [First step]
2. [Second step]
3. [And so on...]

**Error Messages**:
```
[Paste complete error messages here]
```

**Additional Context**:
[Any other relevant information]
```

## 🔄 Emergency Recovery

### Complete Reset Procedure

If nothing else works, try a complete reset:

1. **Stop All Services**:
   ```bash
   # Stop server
   docker stop unity-mcp-server
   # Or kill .NET process
   pkill -f unity-mcp-server
   ```

2. **Clean Unity**:
   - Close Unity Editor
   - Delete `Library/` folder in project
   - Restart Unity Editor

3. **Clean Server**:
   ```bash
   # Remove Docker containers
   docker rm -f unity-mcp-server
   
   # Pull fresh image
   docker pull ivanmurzakdev/unity-mcp-server:latest
   
   # Or reinstall .NET tool
   dotnet tool uninstall -g Unity.MCP.Server
   dotnet tool install -g Unity.MCP.Server
   ```

4. **Restart Everything**:
   - Start server
   - Open Unity project
   - Connect AI client
   - Test basic functionality

---

**Still having issues?** Create a detailed bug report on our [GitHub Issues page](https://github.com/IvanMurzak/Unity-MCP/issues) with the information template above. The community and maintainers are here to help!