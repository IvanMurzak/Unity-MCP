# Default MCP Tools

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

Unity-MCP comes with a comprehensive suite of built-in tools that allow AI models to interact with the Unity Editor and Runtime.

> **Note**: Tool names below are the *Titles* displayed in AI clients. Internal IDs are in kebab-case (e.g., `assets-create-folder`).

## 📂 Asset Management
Manage files, folders, and project resources.

| Tool | ID | Description |
| :--- | :--- | :--- |
| **Assets / Create Folder** | `assets-create-folder` | Create new directories (supports nested paths). |
| **Assets / Create** | `assets-create` | Create a new Unity Asset. |
| **Assets / Delete** | `assets-delete` | Delete a specific asset or file. |
| **Assets / Find** | `assets-find` | Find assets using search filters (e.g., `t:Texture`). |
| **Assets / Copy** | `assets-copy` | Duplicate an asset. |
| **Assets / Move** | `assets-move` | Move or rename an asset. |
| **Assets / Get Data** | `assets-get-data` | Retrieve metadata or content of an asset. |
| **Assets / Refresh** | `assets-refresh` | Force an Asset Database refresh. |

### Materials & Shaders
| Tool | ID | Description |
| :--- | :--- | :--- |
| **Assets / Material / Create** | `assets-material-create` | Create a new Material asset. |
| **Assets / Shader / List All** | `assets-shader-list-all` | List all available shaders in the project. |

### Prefabs
| Tool | ID | Description |
| :--- | :--- | :--- |
| **Assets / Prefab / Instantiate** | `assets-prefab-instantiate` | Spawn a prefab into the active scene. |
| **Assets / Prefab / Create** | `assets-prefab-create` | Create a prefab from a scene object. |
| **Assets / Prefab / Open** | `assets-prefab-open` | Open Prefab Mode. |
| **Assets / Prefab / Close** | `assets-prefab-close` | Exit Prefab Mode. |
| **Assets / Prefab / Save** | `assets-prefab-save` | Save changes in Prefab Mode. |

## 🎮 GameObject
Manage scene objects and hierarchy.

| Tool | ID | Description |
| :--- | :--- | :--- |
| **GameObject / Create** | `gameobject-create` | Create a new GameObject (Empty or Primitive). |
| **GameObject / Destroy** | `gameobject-destroy` | Remove a GameObject. |
| **GameObject / Duplicate** | `gameobject-duplicate` | Clone a GameObject. |
| **GameObject / Find** | `gameobject-find` | Find objects by Name, Tag, or Type. |
| **GameObject / Modify** | `gameobject-modify` | Update Transform, Name, Tag, Layer, Active state. |
| **GameObject / Set Parent** | `gameobject-set-parent` | Change hierarchy parent. |

### Components
| Tool | ID | Description |
| :--- | :--- | :--- |
| **Component / Add** | `gameobject-component-add` | Add a component (e.g., `Rigidbody`). |
| **Component / Destroy** | `gameobject-component-destroy` | Remove a component. |
| **Component / Get** | `gameobject-component-get` | Get details of a component. |
| **Component / Modify** | `gameobject-component-modify` | Set fields, properties, or object references. |
| **Component / List** | `component-list` | List available Component types. |

## 🎬 Scene Management
| Tool | ID | Description |
| :--- | :--- | :--- |
| **Scene / Create** | `scene-create` | Create a new Scene asset. |
| **Scene / Open** | `scene-open` | Open a scene in the Editor. |
| **Scene / Save** | `scene-save` | Save the current scene. |
| **Scene / Unload** | `scene-unload` | Unload an additive scene. |
| **Scene / Set Active** | `scene-set-active` | Set the active scene. |
| **Scene / Get Data** | `scene-get-data` | Get list of root objects in a scene. |
| **Scene / List Opened** | `scene-list-opened` | List currently open scenes. |

## 📝 Scripting
| Tool | ID | Description |
| :--- | :--- | :--- |
| **Script / Create** | `script-date-or-create` | Create or update a C# script file. |
| **Script / Read** | `script-read` | Read the content of a `.cs` file. |
| **Script / Delete** | `script-delete` | Delete a script file. |
| **Script / Execute** | `script-execute` | Compile and run C# code snippet dynamically. |

## 📦 Package Manager
| Tool | ID | Description |
| :--- | :--- | :--- |
| **Package / List** | `package-list` | List installed packages. |
| **Package / Add** | `package-add` | Install package (Registry, Git, Local). |
| **Package / Remove** | `package-remove` | Uninstall a package. |
| **Package / Search** | `package-search` | Search Unity Registry. |

## 💡 Advanced & Editor
| Tool | ID | Description |
| :--- | :--- | :--- |
| **Console / Get Logs** | `console-get-logs` | Retrieve Unity Console logs. |
| **Editor / State / Get** | `editor-application-get-state` | Check Play/Pause/Edit mode status. |
| **Editor / State / Set** | `editor-application-set-state` | Set Play/Pause status. |
| **Editor / Selection / Get** | `editor-selection-get` | Get current selection. |
| **Editor / Selection / Set** | `editor-selection-set` | Set current selection. |
| **Reflection / Method Find** | `reflection-method-find` | Find any C# method (public/private). |
| **Reflection / Method Call** | `reflection-method-call` | Execute any found method. |
