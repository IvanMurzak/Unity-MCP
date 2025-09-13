# Custom Tools Development

Unity-MCP is designed to be extensible, allowing developers to create custom AI tools tailored to their specific project needs. This guide covers everything you need to know about developing, implementing, and deploying custom tools.

## 🎯 Overview

Custom tools in Unity-MCP are C# methods that can be called by AI models to interact with your Unity project. They provide a way to expose project-specific functionality to AI assistants.

### Key Benefits
- **Project-Specific Logic** - Expose your game's unique systems to AI
- **Workflow Automation** - Create shortcuts for common development tasks  
- **Enhanced AI Capabilities** - Extend what AI can do in your project
- **Dynamic Integration** - Tools are automatically discovered and exposed

## 🏗️ Architecture

### Tool Components
1. **Tool Class** - Container marked with `[McpPluginToolType]`
2. **Tool Method** - Function marked with `[McpPluginTool]` 
3. **Parameters** - Method arguments with optional `[Description]` attributes
4. **Return Value** - Structured response back to AI

### Communication Flow
```
AI Request → MCP Server → Unity Plugin → Your Tool → Unity API → Response
```

## 🚀 Quick Start

### Basic Tool Example

Create a simple tool that adds objects to your scene:

```csharp
using Unity.MCP.Common.Attributes;
using UnityEngine;
using System.ComponentModel;

[McpPluginToolType]
public class CustomGameplayTools
{
    [McpPluginTool(
        "CreatePowerup",
        Title = "Create a powerup item in the scene"
    )]
    [Description("Creates a rotating powerup object at the specified position with the given type and effect strength.")]
    public string CreatePowerup(
        [Description("World position where the powerup should be created")]
        Vector3 position,
        
        [Description("Type of powerup: 'health', 'speed', 'jump', or 'weapon'")]
        string powerupType,
        
        [Description("Effect strength multiplier (1.0 = normal strength)")]
        float strength = 1.0f,
        
        [Description("Optional custom name for the powerup GameObject")]
        string? customName = null
    )
    {
        return MainThread.Instance.Run(() =>
        {
            // Create the powerup GameObject
            GameObject powerup = new GameObject(customName ?? $"{powerupType}_Powerup");
            powerup.transform.position = position;
            
            // Add visual representation
            var renderer = powerup.AddComponent<MeshRenderer>();
            var meshFilter = powerup.AddComponent<MeshFilter>();
            
            // Set mesh based on type
            switch (powerupType.ToLower())
            {
                case "health":
                    meshFilter.mesh = CreateCrossMesh();
                    renderer.material.color = Color.red;
                    break;
                case "speed":
                    meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                    renderer.material.color = Color.blue;
                    break;
                case "jump":
                    meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                    renderer.material.color = Color.green;
                    break;
                case "weapon":
                    meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    renderer.material.color = Color.yellow;
                    break;
                default:
                    return $"[Error] Unknown powerup type: {powerupType}";
            }
            
            // Add rotation behavior
            var rotator = powerup.AddComponent<PowerupRotator>();
            rotator.rotationSpeed = 90f;
            
            // Add powerup component
            var powerupComponent = powerup.AddComponent<PowerupItem>();
            powerupComponent.powerupType = powerupType;
            powerupComponent.effectStrength = strength;
            
            return $"[Success] Created {powerupType} powerup at {position} with strength {strength}";
        });
    }
}
```

### Tool Class Requirements

1. **Class Attribute**: Mark with `[McpPluginToolType]`
2. **Public Class**: Must be accessible to reflection
3. **Method Attribute**: Mark methods with `[McpPluginTool]`
4. **Thread Safety**: Use `MainThread.Instance.Run()` for Unity API calls

## 🔧 Detailed Implementation

### Attributes Reference

#### `[McpPluginToolType]`
Marks a class as containing MCP tools.

```csharp
[McpPluginToolType]
public class MyCustomTools
{
    // Tool methods go here
}
```

#### `[McpPluginTool]`
Defines a method as an AI-callable tool.

```csharp
[McpPluginTool(
    "ToolIdentifier",           // Unique tool ID
    Title = "Human Readable Title",
    Category = "Optional Category"
)]
```

#### `[Description]`
Provides AI with context about parameters and methods.

```csharp
[Description("Detailed explanation of what this tool does and when to use it.")]
public string MyTool(
    [Description("Explanation of this parameter")]
    string parameter
)
```

### Parameter Types

Unity-MCP supports a wide range of parameter types:

#### Basic Types
```csharp
public string BasicTypeExample(
    string text,           // Text input
    int number,           // Integer values  
    float precision,      // Floating point
    bool enabled,         // True/false
    Vector3 position,     // 3D coordinates
    Color tint           // RGBA colors
)
```

#### Optional Parameters
Use nullable types with default values:

```csharp
public string OptionalParametersExample(
    string required,                    // Required parameter
    string? optional = null,           // Optional string
    int? optionalNumber = null,        // Optional integer
    bool autoSave = true              // Optional with default
)
```

#### Complex Types
```csharp
public string ComplexTypesExample(
    GameObject target,                 // Unity objects
    Transform[] transforms,           // Arrays
    List<string> tags,               // Generic collections
    Dictionary<string, float> values  // Dictionaries
)
```

### Thread Management

#### Main Thread Operations
Unity API calls must run on the main thread:

```csharp
[McpPluginTool("CreateObject")]
public string CreateObject(Vector3 position)
{
    // Background processing (optional)
    var data = ProcessData();
    
    // Unity API calls on main thread
    return MainThread.Instance.Run(() =>
    {
        var obj = new GameObject("MyObject");
        obj.transform.position = position;
        return $"Created object at {position}";
    });
}
```

#### Background Processing
For CPU-intensive tasks that don't require Unity API:

```csharp
[McpPluginTool("ProcessData")]
public string ProcessData(string[] data)
{
    // This runs in background thread - no Unity API calls!
    var processed = data.Select(item => item.ToUpper()).ToArray();
    return $"Processed {processed.Length} items";
}
```

### Return Value Conventions

Use consistent return formats for better AI understanding:

```csharp
// Success responses
return "[Success] Operation completed successfully";
return $"[Success] Created {count} objects";

// Error responses  
return "[Error] Invalid parameter: position cannot be null";
return $"[Error] GameObject '{name}' not found";

// Info responses
return $"[Info] Found {results.Count} matching objects";

// Data responses (JSON recommended for complex data)
return JsonUtility.ToJson(new { objects = foundObjects, count = foundObjects.Length });
```

## 🎨 Advanced Examples

### Scene Analysis Tool

```csharp
[McpPluginToolType]
public class SceneAnalysisTools
{
    [McpPluginTool(
        "AnalyzeScene", 
        Title = "Analyze current scene performance and structure"
    )]
    [Description("Provides detailed analysis of the current scene including object counts, performance metrics, and optimization suggestions.")]
    public string AnalyzeScene()
    {
        return MainThread.Instance.Run(() =>
        {
            var analysis = new SceneAnalysis
            {
                TotalGameObjects = GameObject.FindObjectsOfType<GameObject>().Length,
                ActiveGameObjects = GameObject.FindObjectsOfType<GameObject>()
                    .Count(go => go.activeInHierarchy),
                RendererCount = GameObject.FindObjectsOfType<Renderer>().Length,
                LightCount = GameObject.FindObjectsOfType<Light>().Length,
                ColliderCount = GameObject.FindObjectsOfType<Collider>().Length
            };
            
            // Add performance warnings
            var warnings = new List<string>();
            if (analysis.LightCount > 8)
                warnings.Add("Too many lights - consider baking lighting");
            if (analysis.RendererCount > 100)
                warnings.Add("High renderer count - consider object pooling");
                
            analysis.Warnings = warnings.ToArray();
            
            return JsonUtility.ToJson(analysis, true);
        });
    }
}

[System.Serializable]
public class SceneAnalysis
{
    public int TotalGameObjects;
    public int ActiveGameObjects;
    public int RendererCount;
    public int LightCount;
    public int ColliderCount;
    public string[] Warnings;
}
```

### Asset Management Tool

```csharp
[McpPluginToolType]
public class CustomAssetTools
{
    [McpPluginTool(
        "OrganizeAssets",
        Title = "Organize project assets by type"
    )]
    [Description("Automatically organizes loose assets into proper folder structure based on asset types.")]
    public string OrganizeAssets(
        [Description("Root folder to organize (e.g., 'Assets/Imported')")]
        string rootFolder,
        
        [Description("Whether to create backup before organizing")]
        bool createBackup = true
    )
    {
        return MainThread.Instance.Run(() =>
        {
            try
            {
                var assetsPath = Application.dataPath + "/" + rootFolder.Replace("Assets/", "");
                
                if (!Directory.Exists(assetsPath))
                    return $"[Error] Folder not found: {rootFolder}";
                
                var moved = 0;
                var folders = new Dictionary<string, string>
                {
                    { ".png", "Textures" },
                    { ".jpg", "Textures" },
                    { ".wav", "Audio" },
                    { ".mp3", "Audio" },
                    { ".fbx", "Models" },
                    { ".obj", "Models" },
                    { ".cs", "Scripts" }
                };
                
                foreach (var file in Directory.GetFiles(assetsPath, "*", SearchOption.TopDirectoryOnly))
                {
                    var extension = Path.GetExtension(file).ToLower();
                    if (folders.ContainsKey(extension))
                    {
                        var targetFolder = Path.Combine(assetsPath, folders[extension]);
                        Directory.CreateDirectory(targetFolder);
                        
                        var fileName = Path.GetFileName(file);
                        var targetPath = Path.Combine(targetFolder, fileName);
                        
                        File.Move(file, targetPath);
                        moved++;
                    }
                }
                
                UnityEditor.AssetDatabase.Refresh();
                return $"[Success] Organized {moved} assets into categorized folders";
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to organize assets: {ex.Message}";
            }
        });
    }
}
```

## 🔍 Best Practices

### Tool Design Principles

#### 1. Single Responsibility
Each tool should have one clear purpose:
```csharp
// Good - focused tool
[McpPluginTool("CreateHealthPickup")]
public string CreateHealthPickup(Vector3 position) { /* ... */ }

// Better - separate tools for different actions
[McpPluginTool("CreatePickup")]
public string CreatePickup(Vector3 position, string type) { /* ... */ }

[McpPluginTool("DestroyPickup")]  
public string DestroyPickup(GameObject pickup) { /* ... */ }
```

#### 2. Clear Naming
Use descriptive names that clearly indicate functionality:
```csharp
// Good
[McpPluginTool("SpawnEnemyWave")]
[McpPluginTool("ConfigurePlayerMovement")]
[McpPluginTool("AnalyzeScenePerformance")]

// Avoid
[McpPluginTool("DoStuff")]
[McpPluginTool("Helper")]
[McpPluginTool("Tool1")]
```

#### 3. Rich Descriptions
Provide context for AI understanding:
```csharp
[Description("Creates a patrol route for AI enemies by placing waypoint markers. The route will be automatically connected in sequence, and enemies assigned to this route will move between waypoints in order.")]
public string CreatePatrolRoute(
    [Description("Array of world positions where waypoints should be placed")]
    Vector3[] waypoints,
    
    [Description("Name identifier for this patrol route (used by AI scripts)")]
    string routeName,
    
    [Description("Movement speed multiplier for enemies on this route")]
    float speedMultiplier = 1.0f
)
```

#### 4. Error Handling
Always handle potential failures gracefully:
```csharp
[McpPluginTool("SafeOperation")]
public string SafeOperation(string input)
{
    try
    {
        if (string.IsNullOrEmpty(input))
            return "[Error] Input cannot be null or empty";
            
        return MainThread.Instance.Run(() =>
        {
            // Unity operations here
            if (someCondition)
                return "[Success] Operation completed";
            else
                return "[Warning] Operation completed with warnings";
        });
    }
    catch (System.Exception ex)
    {
        return $"[Error] Unexpected error: {ex.Message}";
    }
}
```

### Performance Optimization

#### Minimize Main Thread Usage
```csharp
[McpPluginTool("OptimizedTool")]
public string OptimizedTool(string[] data)
{
    // Heavy processing in background
    var processedData = data
        .Where(item => !string.IsNullOrEmpty(item))
        .Select(item => item.Trim().ToUpper())
        .ToArray();
    
    // Only Unity API calls on main thread
    return MainThread.Instance.Run(() =>
    {
        // Minimal Unity operations
        var obj = GameObject.Find("Target");
        obj.name = processedData[0];
        return "[Success] Updated object name";
    });
}
```

#### Batch Operations
```csharp
[McpPluginTool("BatchCreateObjects")]
public string BatchCreateObjects(Vector3[] positions, string[] names)
{
    if (positions.Length != names.Length)
        return "[Error] Position and name arrays must be same length";
    
    return MainThread.Instance.Run(() =>
    {
        var created = new List<GameObject>();
        
        for (int i = 0; i < positions.Length; i++)
        {
            var obj = new GameObject(names[i]);
            obj.transform.position = positions[i];
            created.Add(obj);
        }
        
        return $"[Success] Created {created.Count} objects in batch operation";
    });
}
```

## 📋 Tool Categories

Organize your tools into logical categories:

### Gameplay Tools
```csharp
[McpPluginToolType]
public class GameplayTools
{
    // Player-related tools
    [McpPluginTool("ConfigurePlayerStats")]
    [McpPluginTool("CreatePlayerSpawnPoint")]
    
    // Enemy-related tools  
    [McpPluginTool("SpawnEnemyGroup")]
    [McpPluginTool("SetEnemyDifficulty")]
    
    // Item-related tools
    [McpPluginTool("CreateCollectible")]
    [McpPluginTool("SetupInventorySystem")]
}
```

### Level Design Tools
```csharp
[McpPluginToolType]
public class LevelDesignTools
{
    [McpPluginTool("GenerateTerrainChunk")]
    [McpPluginTool("PlacePlatforms")]
    [McpPluginTool("CreateCheckpoint")]
    [McpPluginTool("SetupLighting")]
}
```

### Debug & Testing Tools
```csharp
[McpPluginToolType]  
public class DebugTools
{
    [McpPluginTool("RunPerformanceTest")]
    [McpPluginTool("ValidateSceneSetup")]
    [McpPluginTool("GenerateTestData")]
}
```

## 🧪 Testing Custom Tools

### Manual Testing
1. **Create Tool Class** in your project
2. **Enter Play Mode** in Unity
3. **Connect AI Client** and ask it to use your tool
4. **Verify Behavior** matches expectations

### Unit Testing
Create tests for your tool logic:
```csharp
[UnityTest]
public IEnumerator TestCustomTool()
{
    var tool = new CustomGameplayTools();
    var result = tool.CreatePowerup(Vector3.zero, "health", 1.0f);
    
    Assert.IsTrue(result.Contains("[Success]"));
    
    var powerup = GameObject.Find("health_Powerup");
    Assert.IsNotNull(powerup);
    
    yield return null;
}
```

## 🚀 Deployment

### Development Deployment
Tools are automatically discovered when:
1. Class has `[McpPluginToolType]` attribute
2. Unity compiles the script successfully  
3. MCP Plugin is active in the project

### Production Considerations
- **Performance**: Test tools under load
- **Security**: Validate all inputs
- **Backwards Compatibility**: Maintain stable tool interfaces
- **Documentation**: Keep AI descriptions updated

## 🔧 Debugging Tools

### Common Issues

#### Tool Not Detected
```csharp
// Check these requirements:
[McpPluginToolType]  // ✓ Class attribute present
public class MyTools  // ✓ Public class
{
    [McpPluginTool("MyTool")]  // ✓ Method attribute  
    public string MyTool()     // ✓ Public method with return value
    {
        return "result";
    }
}
```

#### Thread Errors
```csharp
// Wrong - Unity API on background thread
[McpPluginTool("BadExample")]
public string BadExample()
{
    var obj = new GameObject(); // ❌ Will cause thread error
    return "done";
}

// Correct - Unity API on main thread  
[McpPluginTool("GoodExample")]
public string GoodExample()
{
    return MainThread.Instance.Run(() =>
    {
        var obj = new GameObject(); // ✓ Safe on main thread
        return "done";
    });
}
```

### Debug Logging
Add logging to understand tool execution:
```csharp
[McpPluginTool("DebugTool")]
public string DebugTool(string input)
{
    Debug.Log($"Tool called with input: {input}");
    
    try
    {
        var result = MainThread.Instance.Run(() =>
        {
            Debug.Log("Executing on main thread");
            // Tool logic here
            return "success";
        });
        
        Debug.Log($"Tool completed: {result}");
        return result;
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"Tool failed: {ex.Message}");
        return $"[Error] {ex.Message}";
    }
}
```

## 📚 Next Steps

### Advanced Topics
- **[API Reference](API-Reference)** - Complete technical documentation
- **[Configuration](Configuration)** - Optimize tool performance  
- **[Server Setup](Server-Setup)** - Deploy custom tools in production

### Learning Resources
- **[Examples & Tutorials](Examples-and-Tutorials)** - More hands-on examples
- **[AI Tools Reference](AI-Tools-Reference)** - Study existing tool implementations
- **[Troubleshooting](Troubleshooting)** - Debug common tool issues

### Community
- **[Contributing](Contributing)** - Share your tools with the community
- **GitHub Issues** - Get help with tool development
- **Discussions** - Connect with other tool developers

---

**Ready to build your first custom tool?** Start with the basic example above, then explore our [Examples & Tutorials](Examples-and-Tutorials) for more complex implementations!