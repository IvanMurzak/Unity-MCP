# API Reference

Comprehensive technical reference for Unity-MCP APIs, including Unity Plugin interfaces, MCP Server endpoints, and integration patterns.

## 📚 Overview

Unity-MCP provides several API layers:

1. **[Unity Plugin API](#unity-plugin-api)** - C# interfaces for Unity integration
2. **[MCP Server API](#mcp-server-api)** - HTTP/STDIO endpoints for AI communication
3. **[Tool Development API](#tool-development-api)** - Framework for creating custom tools
4. **[Configuration API](#configuration-api)** - Programmatic configuration management

## 🎮 Unity Plugin API

### Core Plugin Interface

#### McpPlugin Class
Main entry point for Unity-MCP functionality.

```csharp
namespace Unity.MCP.Plugin
{
    public class McpPlugin : MonoBehaviour
    {
        public static McpPlugin Instance { get; }
        public bool IsConnected { get; }
        public string ServerUrl { get; set; }
        public int ServerPort { get; set; }
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        
        public Task<bool> ConnectAsync();
        public Task DisconnectAsync();
        public Task<string> ExecuteToolAsync(string toolName, object parameters);
    }
}
```

**Usage Example**:
```csharp
// Connect to MCP Server
var plugin = McpPlugin.Instance;
plugin.OnConnected += () => Debug.Log("Connected to MCP Server");
await plugin.ConnectAsync();

// Execute a tool
var result = await plugin.ExecuteToolAsync("CreateGameObject", new {
    name = "MyObject",
    position = Vector3.zero
});
```

#### McpClient Interface
Low-level communication interface.

```csharp
namespace Unity.MCP.Plugin
{
    public interface IMcpClient
    {
        bool IsConnected { get; }
        Task<bool> ConnectAsync(string serverUrl);
        Task DisconnectAsync();
        Task<T> SendRequestAsync<T>(McpRequest request);
        Task SendNotificationAsync(McpNotification notification);
    }
    
    public class McpClient : IMcpClient
    {
        public static McpClient Instance { get; }
        // Implementation details...
    }
}
```

### Tool Registration System

#### McpToolRegistry Class
Manages tool discovery and registration.

```csharp
namespace Unity.MCP.Plugin.Tools
{
    public class McpToolRegistry
    {
        public static McpToolRegistry Instance { get; }
        
        public IReadOnlyList<McpToolInfo> RegisteredTools { get; }
        
        public void RegisterTool(Type toolType);
        public void UnregisterTool(Type toolType);
        public McpToolInfo GetTool(string toolName);
        public Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters);
    }
}
```

#### Tool Information Structure
```csharp
namespace Unity.MCP.Plugin.Tools
{
    [Serializable]
    public class McpToolInfo
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public McpToolParameter[] Parameters { get; set; }
        public Type ImplementationType { get; set; }
    }
    
    [Serializable]
    public class McpToolParameter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Type Type { get; set; }
        public bool IsOptional { get; set; }
        public object DefaultValue { get; set; }
    }
}
```

### Threading Utilities

#### MainThread Helper
Ensures Unity API calls execute on the main thread.

```csharp
namespace Unity.MCP.Plugin.Threading
{
    public class MainThread : MonoBehaviour
    {
        public static MainThread Instance { get; }
        
        public T Run<T>(Func<T> function);
        public void Run(Action action);
        public Task<T> RunAsync<T>(Func<T> function);
        public Task RunAsync(Action action);
    }
}
```

**Usage Example**:
```csharp
// Execute Unity API call on main thread from background thread
var result = MainThread.Instance.Run(() =>
{
    var obj = new GameObject("MyObject");
    obj.transform.position = Vector3.up * 5;
    return obj.name;
});
```

### Event System

#### McpEventManager
Handles MCP-related events and notifications.

```csharp
namespace Unity.MCP.Plugin.Events
{
    public class McpEventManager
    {
        public static McpEventManager Instance { get; }
        
        public event Action<McpToolExecutedEventArgs> ToolExecuted;
        public event Action<McpConnectionEventArgs> ConnectionChanged;
        public event Action<McpErrorEventArgs> ErrorOccurred;
        
        public void RaiseToolExecuted(string toolName, object parameters, string result);
        public void RaiseConnectionChanged(bool isConnected);
        public void RaiseError(string message, Exception exception = null);
    }
}
```

## 🌐 MCP Server API

### HTTP Transport Protocol

#### Base URL Structure
```
http://localhost:8080/mcp/{endpoint}
```

#### Authentication
```http
Authorization: Bearer <api-key>  # If authentication enabled
Content-Type: application/json
```

### Core Endpoints

#### Health Check
```http
GET /health
```

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0",
  "uptime": 3600,
  "connections": {
    "unity": true,
    "clients": 2
  }
}
```

#### Server Information
```http
GET /mcp/info
```

**Response**:
```json
{
  "name": "Unity-MCP Server",
  "version": "1.0.0",
  "description": "Model Context Protocol server for Unity Engine",
  "author": "IvanMurzak",
  "homepage": "https://github.com/IvanMurzak/Unity-MCP",
  "protocolVersion": "2024-11-05"
}
```

#### List Available Tools
```http
GET /mcp/tools
```

**Response**:
```json
{
  "tools": [
    {
      "name": "CreateGameObject",
      "description": "Create a new GameObject in the Unity scene",
      "inputSchema": {
        "type": "object",
        "properties": {
          "name": {
            "type": "string",
            "description": "Name for the new GameObject"
          },
          "position": {
            "type": "object",
            "description": "World position for the GameObject"
          }
        },
        "required": ["name"]
      }
    }
  ]
}
```

#### Execute Tool
```http
POST /mcp/tools/call
```

**Request**:
```json
{
  "method": "tools/call",
  "params": {
    "name": "CreateGameObject",
    "arguments": {
      "name": "MyObject",
      "position": {"x": 0, "y": 5, "z": 0}
    }
  }
}
```

**Response**:
```json
{
  "content": [
    {
      "type": "text",
      "text": "[Success] Created GameObject 'MyObject' at position (0, 5, 0)"
    }
  ]
}
```

### STDIO Transport Protocol

#### Message Format
Unity-MCP follows the JSON-RPC 2.0 specification over STDIO.

**Request Message**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "CreateGameObject",
    "arguments": {
      "name": "TestObject"
    }
  }
}
```

**Response Message**:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "[Success] GameObject created successfully"
      }
    ]
  }
}
```

**Notification Message**:
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/initialized"
}
```

### WebSocket Support (Future)

#### Connection Establishment
```javascript
const ws = new WebSocket('ws://localhost:8080/mcp/ws');

ws.onopen = function() {
    // Send initialization message
    ws.send(JSON.stringify({
        type: 'initialize',
        clientInfo: {
            name: 'Custom MCP Client',
            version: '1.0.0'
        }
    }));
};

ws.onmessage = function(event) {
    const message = JSON.parse(event.data);
    // Handle MCP messages
};
```

## 🛠️ Tool Development API

### Attribute System

#### McpPluginToolType Attribute
Marks classes as tool containers.

```csharp
namespace Unity.MCP.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class McpPluginToolTypeAttribute : Attribute
    {
        public string Category { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; } = 0;
    }
}
```

#### McpPluginTool Attribute
Marks methods as executable tools.

```csharp
namespace Unity.MCP.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class McpPluginToolAttribute : Attribute
    {
        public string Name { get; }
        public string Title { get; set; }
        public string Category { get; set; }
        public bool IsAsync { get; set; } = false;
        public int TimeoutMs { get; set; } = 30000;
        
        public McpPluginToolAttribute(string name)
        {
            Name = name;
        }
    }
}
```

### Parameter Validation

#### Parameter Types
Supported parameter types for tool methods:

```csharp
// Basic Types
public string ToolWithBasicTypes(
    string text,           // String input
    int number,           // Integer values
    float precision,      // Floating point
    bool enabled,         // Boolean flags
    Vector3 position,     // Unity Vector3
    Color color,          // Unity Color
    Transform transform   // Unity objects
)

// Collections
public string ToolWithCollections(
    string[] textArray,           // Arrays
    List<int> numberList,        // Generic lists
    Dictionary<string, object> data  // Dictionaries
)

// Optional Parameters
public string ToolWithOptionals(
    string required,              // Required parameter
    string optional = "default", // Optional with default
    int? optionalNumber = null   // Optional nullable
)
```

#### Validation Attributes
```csharp
namespace Unity.MCP.Common.Validation
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ValidateRangeAttribute : Attribute
    {
        public double Min { get; }
        public double Max { get; }
        
        public ValidateRangeAttribute(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ValidateNotNullAttribute : Attribute { }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ValidateRegexAttribute : Attribute
    {
        public string Pattern { get; }
        
        public ValidateRegexAttribute(string pattern)
        {
            Pattern = pattern;
        }
    }
}
```

**Usage Example**:
```csharp
[McpPluginTool("CreateObjectWithValidation")]
public string CreateObjectWithValidation(
    [ValidateNotNull]
    [Description("Name of the GameObject to create")]
    string name,
    
    [ValidateRange(-100, 100)]
    [Description("X coordinate (-100 to 100)")]
    float x,
    
    [ValidateRegex(@"^[A-Za-z]+$")]
    [Description("Tag name (letters only)")]
    string tag = "Untagged"
)
{
    return MainThread.Instance.Run(() =>
    {
        var obj = new GameObject(name);
        obj.transform.position = new Vector3(x, 0, 0);
        obj.tag = tag;
        return $"[Success] Created {name} at ({x}, 0, 0) with tag {tag}";
    });
}
```

### Tool Base Classes

#### McpToolBase Abstract Class
Base class for complex tools.

```csharp
namespace Unity.MCP.Plugin.Tools
{
    public abstract class McpToolBase
    {
        protected virtual void OnBeforeExecute(string toolName, Dictionary<string, object> parameters) { }
        protected virtual void OnAfterExecute(string toolName, string result) { }
        protected virtual void OnError(string toolName, Exception exception) { }
        
        protected T GetParameter<T>(Dictionary<string, object> parameters, string name, T defaultValue = default)
        {
            if (parameters.TryGetValue(name, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            return defaultValue;
        }
        
        protected void ValidateParameter(object value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
        }
    }
}
```

**Usage Example**:
```csharp
[McpPluginToolType(Category = "Custom")]
public class CustomToolBase : McpToolBase
{
    protected override void OnBeforeExecute(string toolName, Dictionary<string, object> parameters)
    {
        Debug.Log($"Executing tool: {toolName}");
        base.OnBeforeExecute(toolName, parameters);
    }
    
    [McpPluginTool("CustomOperation")]
    public string CustomOperation(string input)
    {
        return MainThread.Instance.Run(() =>
        {
            // Custom Unity operations
            return "[Success] Operation completed";
        });
    }
}
```

## ⚙️ Configuration API

### Settings Management

#### McpPluginSettings Class
Manages Unity Plugin configuration.

```csharp
namespace Unity.MCP.Plugin.Configuration
{
    public class McpPluginSettings : ScriptableObject
    {
        public static McpPluginSettings Instance { get; }
        
        [Header("Connection Settings")]
        public string ServerUrl = "http://localhost";
        public int ServerPort = 8080;
        public bool AutoConnect = true;
        public int ConnectionTimeoutMs = 10000;
        public int ReconnectIntervalMs = 5000;
        public int MaxReconnectAttempts = 5;
        
        [Header("Performance Settings")]
        public bool EnableThreading = true;
        public int BatchSize = 100;
        public int UpdateFrequencyHz = 30;
        public int MemoryLimitMB = 256;
        
        [Header("Debug Settings")]
        public bool VerboseLogging = false;
        public bool LogNetworkTraffic = false;
        public bool PerformanceMonitoring = false;
        
        public void SaveSettings();
        public void LoadSettings();
        public void ResetToDefaults();
    }
}
```

#### Server Configuration Interface
```csharp
namespace Unity.MCP.Server.Configuration
{
    public interface IServerConfiguration
    {
        int Port { get; set; }
        string Host { get; set; }
        string ClientTransport { get; set; }
        int PluginTimeout { get; set; }
        int MaxConnections { get; set; }
        string LogLevel { get; set; }
        bool EnableCors { get; set; }
        bool EnableSsl { get; set; }
        string SslCertPath { get; set; }
        string SslKeyPath { get; set; }
    }
}
```

### Environment Configuration

#### Configuration Sources
Priority order for configuration loading:
1. Command-line arguments
2. Environment variables  
3. Configuration files
4. Default values

```csharp
namespace Unity.MCP.Server.Configuration
{
    public class ConfigurationBuilder
    {
        public static IServerConfiguration Build()
        {
            var config = new ServerConfiguration();
            
            // Load from environment variables
            LoadFromEnvironment(config);
            
            // Override with command line args
            LoadFromCommandLine(config);
            
            // Load from config file if specified
            LoadFromConfigFile(config);
            
            return config;
        }
    }
}
```

## 🔍 Error Handling API

### Exception Types

#### McpException Hierarchy
```csharp
namespace Unity.MCP.Common.Exceptions
{
    public abstract class McpException : Exception
    {
        public string ErrorCode { get; }
        public Dictionary<string, object> ErrorData { get; }
        
        protected McpException(string errorCode, string message, Exception innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            ErrorData = new Dictionary<string, object>();
        }
    }
    
    public class McpConnectionException : McpException
    {
        public McpConnectionException(string message, Exception innerException = null)
            : base("CONNECTION_ERROR", message, innerException) { }
    }
    
    public class McpToolExecutionException : McpException
    {
        public string ToolName { get; }
        
        public McpToolExecutionException(string toolName, string message, Exception innerException = null)
            : base("TOOL_EXECUTION_ERROR", message, innerException)
        {
            ToolName = toolName;
        }
    }
    
    public class McpValidationException : McpException
    {
        public string ParameterName { get; }
        
        public McpValidationException(string parameterName, string message)
            : base("VALIDATION_ERROR", message)
        {
            ParameterName = parameterName;
        }
    }
}
```

### Error Response Format

#### Standard Error Response
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32603,
    "message": "Internal error",
    "data": {
      "errorType": "McpToolExecutionException",
      "errorCode": "TOOL_EXECUTION_ERROR", 
      "toolName": "CreateGameObject",
      "details": "GameObject name cannot be null or empty"
    }
  }
}
```

## 📊 Performance Monitoring API

### Metrics Collection

#### McpMetrics Class
```csharp
namespace Unity.MCP.Plugin.Monitoring
{
    public class McpMetrics
    {
        public static McpMetrics Instance { get; }
        
        public long TotalToolExecutions { get; }
        public long SuccessfulExecutions { get; }
        public long FailedExecutions { get; }
        public TimeSpan AverageExecutionTime { get; }
        public long ActiveConnections { get; }
        public long TotalMemoryUsage { get; }
        
        public void RecordToolExecution(string toolName, TimeSpan executionTime, bool success);
        public void RecordConnectionEvent(bool connected);
        public void RecordMemoryUsage(long bytes);
        
        public Dictionary<string, object> GetMetricsSnapshot();
        public void ResetMetrics();
    }
}
```

### Performance Profiling

#### Tool Performance Profiler
```csharp
[McpPluginTool("ProfiledTool")]
public string ProfiledTool(string input)
{
    using (var profiler = new McpToolProfiler("ProfiledTool"))
    {
        return MainThread.Instance.Run(() =>
        {
            profiler.BeginSection("Unity Operations");
            
            // Unity API calls
            var obj = new GameObject(input);
            
            profiler.EndSection("Unity Operations");
            profiler.BeginSection("Processing");
            
            // Additional processing
            var result = ProcessInput(input);
            
            profiler.EndSection("Processing");
            
            return result;
        });
    }
}
```

## 🧪 Testing API

### Test Utilities

#### McpTestFramework
```csharp
namespace Unity.MCP.Testing
{
    public class McpTestFramework
    {
        public static McpTestFramework Instance { get; }
        
        public async Task<string> ExecuteToolAsync(string toolName, object parameters);
        public async Task<bool> ConnectToTestServerAsync();
        public void MockUnityApi(Action mockSetup);
        public void ResetMocks();
    }
}
```

#### Integration Test Base Class
```csharp
namespace Unity.MCP.Testing
{
    public abstract class McpIntegrationTestBase
    {
        protected McpTestFramework TestFramework { get; }
        
        [SetUp]
        public virtual async Task SetUp()
        {
            await TestFramework.ConnectToTestServerAsync();
        }
        
        [TearDown]
        public virtual void TearDown()
        {
            TestFramework.ResetMocks();
        }
        
        protected async Task<string> ExecuteTool(string toolName, object parameters)
        {
            return await TestFramework.ExecuteToolAsync(toolName, parameters);
        }
    }
}
```

## 📚 Code Examples

### Complete Tool Implementation
```csharp
using Unity.MCP.Common.Attributes;
using Unity.MCP.Plugin.Threading;
using UnityEngine;
using System.ComponentModel;

[McpPluginToolType(Category = "GameObject Management")]
public class ExampleGameObjectTool
{
    [McpPluginTool(
        "CreateAndConfigureObject",
        Title = "Create and configure a GameObject with components"
    )]
    [Description("Creates a new GameObject and adds specified components with configuration")]
    public string CreateAndConfigureObject(
        [Description("Name of the GameObject to create")]
        string name,
        
        [Description("World position for the GameObject")]
        Vector3 position = default,
        
        [Description("Components to add (e.g., 'Rigidbody,BoxCollider')")]
        string components = "",
        
        [Description("Whether to add a MeshRenderer for visibility")]
        bool addRenderer = true
    )
    {
        if (string.IsNullOrEmpty(name))
            return "[Error] GameObject name cannot be null or empty";
            
        return MainThread.Instance.Run(() =>
        {
            try
            {
                // Create GameObject
                var obj = new GameObject(name);
                obj.transform.position = position;
                
                // Add renderer if requested
                if (addRenderer)
                {
                    var meshRenderer = obj.AddComponent<MeshRenderer>();
                    var meshFilter = obj.AddComponent<MeshFilter>();
                    meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    
                    // Create and assign material
                    var material = new Material(Shader.Find("Standard"));
                    material.color = Color.white;
                    meshRenderer.material = material;
                }
                
                // Add specified components
                if (!string.IsNullOrEmpty(components))
                {
                    var componentNames = components.Split(',');
                    foreach (var componentName in componentNames)
                    {
                        var trimmedName = componentName.Trim();
                        var componentType = System.Type.GetType($"UnityEngine.{trimmedName}, UnityEngine");
                        
                        if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                        {
                            obj.AddComponent(componentType);
                        }
                        else
                        {
                            Debug.LogWarning($"Component type '{trimmedName}' not found");
                        }
                    }
                }
                
                return $"[Success] Created GameObject '{name}' at {position} with {obj.GetComponents<Component>().Length} components";
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to create GameObject: {ex.Message}";
            }
        });
    }
}
```

---

**Need more technical details?** Check the source code on [GitHub](https://github.com/IvanMurzak/Unity-MCP) or explore our [Examples & Tutorials](Examples-and-Tutorials) for practical API usage examples!