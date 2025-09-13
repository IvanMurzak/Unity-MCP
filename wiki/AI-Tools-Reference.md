# AI Tools Reference

Unity-MCP provides a comprehensive set of AI tools that enable intelligent interaction with Unity Editor and runtime environments. Each tool serves as a bridge between AI models and Unity's API, allowing AI to perform complex game development tasks.

## 🧰 Tool Categories Overview

Unity-MCP organizes tools into logical categories based on Unity's architecture:

- **[GameObject Tools](#gameobject-tools)** - Create, modify, and manage GameObjects
- **[Component Tools](#component-tools)** - Add, configure, and remove components
- **[Scene Management](#scene-management)** - Handle scenes, hierarchies, and navigation
- **[Asset Management](#asset-management)** - Work with files, textures, materials, and resources
- **[Editor Tools](#editor-tools)** - Control Unity Editor state and settings
- **[Prefab Tools](#prefab-tools)** - Create and manage prefabs
- **[Material & Shader Tools](#material--shader-tools)** - Handle rendering materials and shaders
- **[Script Tools](#script-tools)** - Create, read, and modify C# scripts
- **[Debug Tools](#debug-tools)** - Access console logs and debugging information

## 🎮 GameObject Tools

Core tools for managing Unity GameObjects in scenes.

### Creation & Destruction
| Tool | Status | Description |
|------|--------|-------------|
| **Create GameObject** | ✅ | Create new GameObjects with specified names, positions, and properties |
| **Destroy GameObject** | ✅ | Remove GameObjects from the scene |
| **Duplicate GameObject** | ✅ | Create copies of existing GameObjects |

### GameObject Management
| Tool | Status | Description |
|------|--------|-------------|
| **Find GameObject** | ✅ | Locate GameObjects by name, tag, or component type |
| **Modify GameObject** | ✅ | Change GameObject properties (name, tag, layer, static flags) |
| **Set Parent** | ✅ | Organize hierarchy by setting parent-child relationships |

### Example Usage
```
"Create a red cube at position (0, 5, 0)"
"Find all GameObjects tagged 'Enemy'"  
"Set the Main Camera as a child of the Player GameObject"
"Duplicate the Tree prefab 10 times in a grid pattern"
```

## 🔧 Component Tools

Tools for working with Unity components and their properties.

### Component Management
| Tool | Status | Description |
|------|--------|-------------|
| **Add Component** | ✅ | Add any Unity component to GameObjects |
| **Get Components** | ✅ | List all components attached to a GameObject |
| **Modify Component** | ✅ | Change component field and property values |
| **Destroy Component** | ✅ | Remove components from GameObjects |

### Property Manipulation
| Tool | Status | Description |
|------|--------|-------------|
| **Set Field Values** | ✅ | Modify component public fields |
| **Set Property Values** | ✅ | Change component properties |
| **Link References** | ✅ | Connect component references to other objects |

### Example Usage
```
"Add a Rigidbody component to the cube"
"Set the Rigidbody mass to 5.0"
"Link the AudioSource clip to the 'explosion' sound"
"Remove all Collider components from selected objects"
```

## 🏞️ Scene Management

Tools for managing Unity scenes and scene hierarchies.

### Scene Operations
| Tool | Status | Description |
|------|--------|-------------|
| **Create Scene** | ✅ | Create new empty or templated scenes |
| **Save Scene** | ✅ | Save current scene changes |
| **Load Scene** | ✅ | Switch to different scenes |
| **Unload Scene** | ✅ | Remove scenes from memory |
| **Get Loaded Scenes** | ✅ | List all currently loaded scenes |

### Hierarchy Tools
| Tool | Status | Description |
|------|--------|-------------|
| **Get Scene Hierarchy** | ✅ | Retrieve complete scene object tree |
| **Scene Search** | 🔲 | Search for objects within scenes *(planned)* |
| **Scene Raycast** | 🔲 | Perform 3D raycasting operations *(planned)* |

### Example Usage
```
"Create a new scene called 'Level2'"
"Show me the complete hierarchy of the current scene"
"Load the MainMenu scene additively"
"Save all modified scenes"
```

## 📁 Asset Management

Comprehensive tools for handling Unity assets and project files.

### File Operations
| Tool | Status | Description |
|------|--------|-------------|
| **Create Assets** | ✅ | Create new asset files of various types |
| **Find Assets** | ✅ | Locate assets by name, type, or path |
| **Read Assets** | ✅ | Access asset content and metadata |
| **Modify Assets** | ✅ | Update asset properties and content |
| **Rename Assets** | ✅ | Change asset names and paths |
| **Delete Assets** | ✅ | Remove assets from project |
| **Move Assets** | ✅ | Relocate assets within project structure |

### Folder Management
| Tool | Status | Description |
|------|--------|-------------|
| **Create Folders** | ✅ | Organize project with new directories |
| **Refresh Asset Database** | ✅ | Update Unity's asset database |

### Example Usage
```
"Create a new Materials folder in Assets"
"Find all .png textures in the project"
"Move all audio files to Assets/Audio folder"
"Delete unused materials from the project"
```

## ⚙️ Editor Tools

Tools that control Unity Editor behavior and settings.

### Editor State
| Tool | Status | Description |
|------|--------|-------------|
| **Get Play Mode State** | ✅ | Check if editor is playing, paused, or stopped |
| **Set Play Mode State** | ✅ | Control editor play mode (play, pause, stop) |
| **Get Editor Windows** | ✅ | List all open editor windows |

### Project Settings
| Tool | Status | Description |
|------|--------|-------------|
| **Layer Management** | ✅ | Get, add, and remove layers |
| **Tag Management** | ✅ | Get, add, and remove tags |
| **Execute Menu Items** | ✅ | Trigger Unity menu commands |
| **Run Tests** | ✅ | Execute unit and integration tests |

### Selection Tools
| Tool | Status | Description |
|------|--------|-------------|
| **Get Selection** | ✅ | Retrieve currently selected objects |
| **Set Selection** | ✅ | Change editor selection |

### Example Usage
```
"Enter play mode to test the game"
"Add a new layer called 'UI Elements'"
"Select all objects with the 'Collectible' tag"
"Run all unit tests in edit mode"
```

## 🧩 Prefab Tools

Tools for working with Unity prefabs and prefab instances.

### Prefab Operations
| Tool | Status | Description |
|------|--------|-------------|
| **Instantiate Prefab** | ✅ | Create instances of prefabs in scenes |
| **Open Prefab** | ✅ | Enter prefab editing mode |
| **Save Prefab** | ✅ | Apply changes to prefab assets |
| **Close Prefab** | ✅ | Exit prefab editing mode |
| **Create Prefab** | 🔲 | Convert GameObjects to prefabs *(planned)* |

### Example Usage
```
"Instantiate the Player prefab at spawn point"
"Open the Enemy prefab for editing"
"Save changes to the current prefab"
"Create 5 instances of the Tree prefab randomly in the scene"
```

## 🎨 Material & Shader Tools

Tools for managing rendering materials and shaders.

### Material Operations
| Tool | Status | Description |
|------|--------|-------------|
| **Create Materials** | ✅ | Generate new material assets |
| **Modify Materials** | ✅ | Change material properties and textures |
| **Read Materials** | ✅ | Access material settings and values |
| **Assign Materials** | ✅ | Apply materials to renderer components |

### Shader Information
| Tool | Status | Description |
|------|--------|-------------|
| **List All Shaders** | ✅ | Get available shaders in the project |

### Example Usage
```
"Create a metallic material with blue color"
"Assign the wood texture to all terrain objects"
"List all Standard shader materials in the project"
"Change the emission color of the neon sign material"
```

## 📝 Script Tools

Tools for creating and managing C# scripts in Unity projects.

### Script Operations
| Tool | Status | Description |
|------|--------|-------------|
| **Read Scripts** | ✅ | Access C# script content and structure |
| **Create/Update Scripts** | ✅ | Write new scripts or modify existing ones |
| **Delete Scripts** | ✅ | Remove script files from project |

### ScriptableObject Support
| Tool | Status | Description |
|------|--------|-------------|
| **Create ScriptableObject** | ✅ | Generate new ScriptableObject instances |
| **Read ScriptableObject** | ✅ | Access ScriptableObject data |
| **Modify ScriptableObject** | ✅ | Update ScriptableObject properties |

### Example Usage
```
"Create a PlayerController script with basic movement"
"Read the contents of the GameManager script"
"Create a new ScriptableObject for weapon data"
"Add a Jump method to the Player script"
```

## 🐛 Debug Tools

Tools for debugging and monitoring Unity applications.

### Logging & Console
| Tool | Status | Description |
|------|--------|-------------|
| **Read Console Logs** | ✅ | Access Unity Console messages, warnings, and errors |

### Component Information
| Tool | Status | Description |
|------|--------|-------------|
| **Get All Components** | ✅ | List all available component types |

### Example Usage
```
"Show me the last 10 console messages"
"List all components available in Unity"
"Check for any error messages in the console"
```

## 📦 Package Management

*Note: Package management tools are planned for future releases.*

### Planned Package Tools
| Tool | Status | Description |
|------|--------|-------------|
| **Get Installed Packages** | 🔲 | List all installed Unity packages |
| **Install Package** | 🔲 | Add new packages to the project |
| **Remove Package** | 🔲 | Uninstall packages |
| **Update Packages** | 🔲 | Update packages to latest versions |

## 🔍 Tool Discovery

### Dynamic Tool Loading
Unity-MCP supports dynamic tool discovery, meaning:
- Custom tools are automatically detected when created
- Tool lists update in real-time as you add new functionality
- AI models receive updated tool information automatically

### Tool Metadata
Each tool provides rich metadata including:
- **Descriptions** - Clear explanations of tool functionality
- **Parameter Types** - Type-safe parameter definitions
- **Optional Parameters** - Flexible argument handling
- **Return Values** - Structured response formats

## 🚀 Performance Considerations

### Thread Safety
- Most tools execute on Unity's main thread for API compatibility
- Background processing is used where possible for performance
- Thread-safe tools are marked and optimized for concurrent usage

### Batching Operations
For efficiency, consider batching related operations:
```
"Create 10 cubes in a 3x3 grid pattern"  # Better than 10 separate requests
"Apply the same material to all selected objects"  # Batch material assignment
```

## 📚 Next Steps

### Learn More About Tools
- **[Custom Tools Development](Custom-Tools-Development)** - Create your own AI tools
- **[API Reference](API-Reference)** - Technical implementation details
- **[Examples & Tutorials](Examples-and-Tutorials)** - Hands-on tool usage

### Integration Guides
- **[Configuration](Configuration)** - Optimize tool performance
- **[Server Setup](Server-Setup)** - Deploy tools in production
- **[Troubleshooting](Troubleshooting)** - Resolve tool-related issues

---

**Ready to use these tools?** Start with our [Getting Started guide](Getting-Started) or explore [Examples & Tutorials](Examples-and-Tutorials) for practical applications!

> **Legend**: ✅ = Implemented & Available | 🔲 = Planned for Future Release