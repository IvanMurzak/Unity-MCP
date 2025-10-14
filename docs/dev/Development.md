<div align="center" width="100%">
  <h1>🛠️ Contribute to AI Game Developer</h1>

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

</div>

**Vision**

We believe that AI will be (if not already) an important part of the game development. There are amazing AI interfaces such as `Claude`, `Copilot`, `Cursor` and many others. They provide amazing agents and features and the most important - they keep improving it. These projects have huge budgets and probably will be the best AI platforms for professionals. We connect game development with these tools, this project works in a pair with them, not against them. We will grow with them. That is why this project won't implement internal isolated chat window. We want to build simple and elegant solution which became a foundation for AI systems in game development with Unity Engine ecosystem.

**Project goals**

- Deliver high quality AI game development solution for **free** to everyone
- Provide a highly customizable platform for game developers for customizing AI features for their needs
- Allow to utilize the best AI instruments for game development, all in one place
- Maintain and support cutting edge AI technologies for game development especially in Unity Engine and beyond the engine

**Contribute**

Any contribution to the project is highly appreciated. Please follow this document to see out goals, vision and project structure. All of that should help to let you participate in the new technological era of game development

**This document**

This document explains the internal project structure, design, code style, and main principals. Please use it if you are a contributor or if you like to understand the project in depth.

> **[💬 Join our Discord Server](https://discord.gg/cfbdMZX99G)** - Ask questions, showcase your work, and connect with other developers!

## Content

- [Contribute](#contribute)
- [Projects structure](#projects-structure)
  - [🔹Unity-MCP-Server](#unity-mcp-server)
    - [Docker Image](#docker-image)
  - [🔸Unity-MCP-Plugin](#unity-mcp-plugin)
    - [UPM Package](#upm-package)
    - [Editor](#editor)
    - [Runtime](#runtime)
    - [MCP features](#mcp-features)
      - [Add `MCP Tool`](#add-mcp-tool)
      - [Add `MCP Prompt`](#add-mcp-prompt)
  - [🔺Unity-MCP-Common](#unity-mcp-common)
  - [◾Installer (Unity)](#installer-unity)
- [Code style](#code-style)
- [CI/CD](#cicd)
  - [Workflows Overview](#workflows-overview)
    - [🚀 release.yml](#-releaseyml)
    - [🧪 test\_pull\_request.yml](#-test_pull_requestyml)
    - [🔧 test\_unity\_plugin.yml](#-test_unity_pluginyml)
    - [📦 deploy.yml](#-deployyml)
    - [🎯 deploy\_server\_executables.yml](#-deploy_server_executablesyml)
  - [Technology Stack](#technology-stack)
  - [Security Considerations](#security-considerations)
  - [Deployment Targets](#deployment-targets)

# Contribute

Lets build the bright game development future together, contribute to the project. Use this document to understand the project structure and how exactly it works.

1. [Fork the project](https://github.com/IvanMurzak/Unity-MCP/fork)
2. Make your improvements, follow code style
3. [Create Pull Request](https://github.com/IvanMurzak/Unity-MCP/compare)

# Projects structure

```mermaid
graph LR
  A(◽MCP-Client)
  B(🔹Unity-MCP-Server)
  C(🔸Unity-MCP-Plugin)
  D(🎮Unity)

  %% Relationships
  A <--> B
  B <--> C
  C <--> D
```

◽**MCP Client** - Any AI interface such as: *Claude*, *Copilot*, *Cursor* or any other, it is not part of these project, but it is an important element of the architecture.

🔹**Unity-MCP-Server** - `MCP Server` that connects to `MCP Client` and operates with it. In the same `Unity-MCP-Server` communicates with `Unity-MCP-Plugin` over SignalR. May run locally or in a cloud with HTTP transport. Tech stack: `C#`, `ASP.NET Core`, `SignalR`

🔸**Unity-MCP-Plugin** - `Unity Plugin` which is integrated into a Unity project, has access to Unity's API. Communicates with `Unity-MCP-Server` and executes commands from the server. Tech stack: `C#`, `Unity`, `SignalR`

🎮**Unity** - Unity Engine, game engine.

---

## 🔹Unity-MCP-Server

A C# ASP.NET Core application that acts as a bridge between MCP clients (AI interfaces like Claude, Cursor) and Unity Editor instances. The server implements the [Model Context Protocol](https://github.com/modelcontextprotocol) using the [csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk).

> Project location: `Unity-MCP-Server`

**Main Responsibilities:**

1. **MCP Protocol Implementation** ([ExtensionsMcpServer.cs](Unity-MCP-Server/src/Extension/ExtensionsMcpServer.cs))
   - Implements MCP server with support for Tools, Prompts, and Resources
   - Supports both STDIO and HTTP transport methods
   - Handles MCP client requests: `CallTool`, `GetPrompt`, `ReadResource`, and their list operations
   - Sends notifications to MCP clients when capabilities change (tool/prompt list updates)

2. **SignalR Hub Communication** ([RemoteApp.cs](Unity-MCP-Server/src/Hub/RemoteApp.cs), [BaseHub.cs](Unity-MCP-Server/src/Hub/BaseHub.cs))
   - Manages real-time bidirectional communication with Unity-MCP-Plugin via SignalR
   - Handles version handshake to ensure API compatibility between server and plugin
   - Tracks client connections and manages disconnections
   - Routes tool/prompt/resource update notifications from Unity to MCP clients

3. **Request Routing & Execution** ([ToolRouter.Call.cs](Unity-MCP-Server/src/Routing/Tool/ToolRouter.Call.cs), [PromptRouter.Get.cs](Unity-MCP-Server/src/Routing/Prompt/PromptRouter.Get.cs), [ResourceRouter.ReadResource.cs](Unity-MCP-Server/src/Routing/Resource/ResourceRouter.ReadResource.cs))
   - Routes MCP client requests to the appropriate Unity-MCP-Plugin instance
   - Handles Tool calls, Prompt requests, and Resource reads
   - Performs error handling and validation
   - Converts between MCP protocol formats and internal data models

4. **Remote Execution Service** ([RemoteToolRunner.cs](Unity-MCP-Server/src/Client/RemoteToolRunner.cs), [RemotePromptRunner.cs](Unity-MCP-Server/src/Client/RemotePromptRunner.cs), [RemoteResourceRunner.cs](Unity-MCP-Server/src/Client/RemoteResourceRunner.cs))
   - Invokes remote procedures on Unity-MCP-Plugin through SignalR
   - Tracks asynchronous requests and manages timeouts
   - Implements request/response patterns with cancellation support
   - Handles request completion callbacks from Unity instances

5. **Server Lifecycle Management** ([Program.cs](Unity-MCP-Server/src/Program.cs), [McpServerService.cs](Unity-MCP-Server/src/McpServerService.cs))
   - Configures and starts ASP.NET Core web server with Kestrel
   - Initializes MCP server, SignalR hub, and dependency injection
   - Manages logging with NLog (redirects logs to stderr in STDIO mode)
   - Handles graceful shutdown and resource cleanup
   - Subscribes to Unity tool/prompt list change events

### Docker Image

`Unity-MCP-Server` is deployable into a docker image. It contains `Dockerfile` and `.dockerignore` files in the folder of the project.

---

## 🔸Unity-MCP-Plugin

Integrates into Unity environment. Uses `Unity-MCP-Common` for searching for MCP *Tool*, *Resource* and *Prompt* in the local codebase using reflection. Communicates with `Unity-MCP-Server` for sending updates about MCP *Tool*, *Resource* and *Prompt*. Takes commands from `Unity-MCP-Server` and executes it.

> Project location: `Unity-MCP-Plugin`

### UPM Package

`Unity-MCP-Plugin` is a UPM package, the root folder of the package is located at . It contains `package.json`. Which is used for uploading the package directly from GitHub release to [OpenUPM](https://openupm.com/).

> Location `Unity-MCP-Plugin/Assets/root`

### Editor

The Editor component of `Unity-MCP-Plugin` provides the Unity Editor integration that enables AI-driven game development capabilities. It runs in Unity's Edit mode and handles all communication with the `Unity-MCP-Server`.

> Location `Unity-MCP-Plugin/Assets/root/Editor`

**Main Responsibilities:**

1. **Plugin Initialization & Lifecycle** ([Startup.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/Startup.cs), [Startup.Editor.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/Startup.Editor.cs))
   - Auto-starts on Unity Editor load via `[InitializeOnLoad]` attribute
   - Initializes the plugin by calling `UnityMcpPlugin.BuildAndStart()`
   - Manages connection lifecycle across Editor events (assembly reload, play mode changes, domain reload)
   - Subscribes to Unity Editor lifecycle events (application unloading, quitting, assembly reload, play mode state changes)
   - Automatically reconnects to MCP Server after exiting Play mode or assembly reload
   - Initializes subsystems: log collector and test runner

2. **MCP Server Binary Management** ([Startup.Server.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/Startup.Server.cs))
   - Automatically downloads `Unity-MCP-Server` executable from GitHub releases
   - Detects operating system (Windows, macOS, Linux) and CPU architecture (x86, x64, ARM, ARM64)
   - Manages server binary version compatibility with plugin version
   - Stores server executables in `../Library/mcp-server/{platform}/` directory
   - Sets executable permissions on Unix-based systems (macOS, Linux)
   - Provides configuration generation for MCP clients (JSON format with executable paths and port settings)
   - Handles server binary cleanup and re-download when version mismatch detected

3. **MCP Tools Implementation** ([Scripts/API/Tool/](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/))
   - Implements MCP Tools categorized by functionality:
   - **GameObject Operations**: Create, destroy, find, modify, duplicate, set parent, add/remove components ([GameObject.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/GameObject.cs))
   - **Scene Management**: Load, unload, create, save scenes, get hierarchy ([Scene.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Scene.cs))
   - **Asset Operations**: Find, read, create, modify, delete, copy, move, refresh assets ([Assets.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Assets.cs))
   - **Prefab Operations**: Create, instantiate, open, close, save prefabs ([Assets.Prefab.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Assets.Prefab.cs))
   - **Material & Shader Operations**: Create materials, list shaders ([Assets.Material.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Assets.Material.cs))
   - **Script Management**: Read, create, update, delete, execute C# scripts ([Script.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Script.cs))
   - **Component Operations**: Get all components, modify component properties ([Component.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Component.cs))
   - **Editor Control**: Get/set editor state, manage selection ([Editor.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Editor.cs))
   - **Test Runner**: Execute EditMode and PlayMode tests with filtering ([TestRunner.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/TestRunner.cs))
   - **Console Integration**: Retrieve Unity Console logs ([Console.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Console.cs))
   - **Reflection API**: Find and call methods dynamically ([Reflection.*.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Reflection.cs))
   - All tools use `[McpPluginTool]` attribute and return string responses
   - Tools execute on Unity's main thread via `MainThread.Instance.Run()`

4. **MCP Prompts Implementation** ([Scripts/API/Prompt/](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Prompt/))
   - Provides pre-built prompt templates for common Unity tasks
   - Categories: Scene Management, GameObject/Component, Scripting/Code, Asset Management, Animation/Timeline, Debugging/Testing
   - Examples: "setup-basic-scene", "organize-scene-hierarchy", "add-event-system", "implement-singleton-pattern"
   - Prompts use `[McpPluginPrompt]` attribute with User or Assistant role

5. **MCP Resources Implementation** ([Scripts/API/Resource/](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Resource/))
   - Exposes Unity Editor data as MCP Resources with URI-based access
   - Provides dynamic resource listing via URI schemes (e.g., `gameObject://currentScene/{path}`)
   - Serializes GameObject hierarchy with components and property values to JSON
   - Uses reflection to extract component data for AI context

6. **Editor UI** ([Scripts/UI/](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/UI/))
   - Main configuration window accessible via `Window > AI Game Developer (Unity-MCP)` menu ([MenuItems.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/UI/MenuItems.cs))
   - Provides connection management interface ([MainWindowEditor.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/UI/Window/MainWindowEditor.cs))
   - Menu items for server binary management (download, delete) and log file access
   - Window content generation and client configuration UI ([MainWindowEditor.CreateGUI.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/UI/Window/MainWindowEditor.CreateGUI.cs))
   - Handles user interaction for connecting/disconnecting from MCP Server

7. **Utility Systems** ([Scripts/Utils/](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/Utils/))
   - Unix permission management for executable files ([UnixUtils.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/Utils/UnixUtils.cs))
   - Script template utilities for C# code generation ([ScriptUtils.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/Utils/ScriptUtils.cs))
   - Link extraction from documentation ([LinkExtractor.cs](../Unity-MCP-Plugin/Assets/root/Editor/Scripts/Utils/LinkExtractor.cs))

**Architecture Pattern:**

- Tools are organized as partial classes (e.g., `Tool_GameObject`, `Tool_Scene`, `Tool_Assets`)
- Each tool operation is implemented in a separate file (e.g., `GameObject.Create.cs`, `Scene.Load.cs`)
- Error handling is centralized in nested `Error` classes within each tool type
- All Editor operations run on Unity's main thread for thread safety
- Uses attributes from Unity-MCP-Common for MCP feature discovery via reflection

### Runtime

The Runtime component provides the core infrastructure and shared utilities that work in both Unity Editor and Runtime modes. It establishes the connection to Unity-MCP-Server, manages serialization, and provides thread-safe utilities.

> Location `Unity-MCP-Plugin/Assets/root/Runtime`

**Main Responsibilities:**

1. **Plugin Core & Lifecycle Management** ([UnityMcpPlugin.cs](../Unity-MCP-Plugin/Assets/root/Runtime/UnityMcpPlugin.cs), [UnityMcpPlugin.Startup.cs](../Unity-MCP-Plugin/Assets/root/Runtime/UnityMcpPlugin.Startup.cs))
   - Singleton pattern implementation with thread-safe initialization
   - Main entry point via `BuildAndStart()` method
   - Manages plugin version (`0.20.0`) and API version compatibility
   - Configuration management: host, port, timeout, connection state
   - Creates and configures `McpPlugin` instance from Unity-MCP-Common
   - Discovers MCP Tools, Prompts, and Resources from all loaded assemblies using reflection
   - Establishes SignalR connection to Unity-MCP-Server
   - Provides reactive properties for connection state monitoring via R3 library
   - Handles connection/disconnection lifecycle with mutex-based synchronization

2. **Main Thread Dispatcher** ([MainThreadDispatcher.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Utils/MainThreadDispatcher.cs), [MainThread.Editor.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Utils/MainThread.Editor.cs))
   - Provides thread-safe execution of Unity API calls from background threads
   - Implements `MainThread.Instance` singleton for executing actions/functions on Unity's main thread
   - Queues actions from any thread and processes them in Unity's Update loop
   - Editor-specific implementation using `EditorApplication.update` callbacks
   - Identifies main thread via `Thread.ManagedThreadId` comparison
   - Supports both `Task`-based and `Func<T>`-based async execution patterns
   - Essential for MCP operations triggered by SignalR callbacks from background threads

3. **Reflection Converters** ([ReflectionConverters/](../Unity-MCP-Plugin/Assets/root/Runtime/ReflectionConverters/))
   - Custom serialization/deserialization for Unity types to/from JSON
   - **Unity Object Types**: GameObject, Component, Transform, Material, Sprite, Renderer, MeshFilter ([UnityEngine_*_ReflectionConvertor.cs](../Unity-MCP-Plugin/Assets/root/Runtime/ReflectionConverters/))
   - **Struct Types**: Vector2/3/4, Vector2Int/3Int, Quaternion, Color, Color32, Bounds, BoundsInt, Rect, RectInt, Matrix4x4 ([Struct/](../Unity-MCP-Plugin/Assets/root/Runtime/ReflectionConverters/Struct/))
   - Converts Unity objects to reference format (`GameObjectRef`, `ComponentRef`, `ObjectRef`) with instanceID
   - Serializes GameObject with all attached components recursively
   - Handles component lookup by instanceID, index, or type name
   - Implements `UnityGenericReflectionConvertor<T>` base class for Unity-specific serialization
   - Integrates with ReflectorNet library for object introspection

4. **JSON Converters** ([JsonConverters/](../Unity-MCP-Plugin/Assets/root/Runtime/JsonConverters/))
   - System.Text.Json converters for Unity-specific types
   - Converts Unity structs to/from JSON with proper schema definitions
   - Types covered: Vector2/3/4, Vector2Int/3Int, Quaternion, Color, Color32, Bounds, BoundsInt, Rect, RectInt, Matrix4x4
   - Implements `IJsonSchemaConverter` for generating JSON schemas
   - Example: Vector3 serialized as `{"x": 1.0, "y": 2.0, "z": 3.0}`
   - Used by Reflector's JsonSerializer for type-safe serialization

5. **Unity Utilities** ([Utils/](../Unity-MCP-Plugin/Assets/root/Runtime/Utils/))
   - **GameObjectUtils**: Find GameObjects by path/name/instanceID, hierarchy traversal, bounds calculation, transform manipulation ([GameObjectUtils.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Utils/GameObjectUtils.cs))
   - **SceneUtils**: Scene loading/unloading, scene metadata extraction ([SceneUtils.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Utils/SceneUtils.cs))
   - **ShaderUtils**: Shader discovery and management ([ShaderUtils.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Utils/ShaderUtils.cs))
   - **EnvironmentUtils**: CI environment detection ([EnvironmentUtils.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Utils/EnvironmentUtils.cs))
   - **Safe**: Safe execution wrapper with error handling ([Safe.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Utils/Safe.cs))
   - **WeakAction**: Weak reference action wrapper for event handling ([WeakAction.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Utils/WeakAction.cs))

6. **Data Models** ([Data/](../Unity-MCP-Plugin/Assets/root/Runtime/Data/))
   - **GameObjectMetadata**: Hierarchical GameObject representation with instanceID, path, active state, tag, children ([GameObjectMetadata.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Data/GameObjectMetadata.cs))
   - **SceneMetadata**: Scene information including name, loaded status, root GameObjects ([SceneMetadata.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Data/SceneMetadata.cs))
   - Provides `Print()` methods for formatted text output with configurable line limits
   - Used by MCP Resources and Tools for returning structured scene/hierarchy data

7. **Logging Integration** ([Logger/](../Unity-MCP-Plugin/Assets/root/Runtime/Logger/))
   - Bridges Microsoft.Extensions.Logging with Unity's Debug.Log system
   - **UnityLogger**: Custom ILogger implementation that outputs to Unity Console ([UnityLogger.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Logger/UnityLogger.cs))
   - **UnityLoggerProvider**: Factory for creating UnityLogger instances ([UnityLoggerProvider.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Logger/UnityLoggerProvider.cs))
   - Color-coded log levels: Trace, Debug, Info, Warning, Error, Critical
   - Log filtering based on configured log level
   - Formatted output with tags: `[AI-Editor]`, `[trce]`, `[info]`, `[fail]`, etc.

8. **Console Log Collection** ([Unity/Logs/](../Unity-MCP-Plugin/Assets/root/Runtime/Unity/Logs/))
   - **LogUtils**: Collects Unity Console logs for AI context ([LogUtils.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Unity/Logs/LogUtils.cs))
   - **LogEntry**: Structured log entry with timestamp, type, message, stackTrace ([LogEntry.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Unity/Logs/LogEntry.cs))
   - Subscribes to `Application.logMessageReceived` events
   - Provides log history for MCP Tools to retrieve recent errors/warnings
   - Configurable max entries limit

9. **Extension Methods** ([Extensions/](../Unity-MCP-Plugin/Assets/root/Runtime/Extensions/))
   - Helper extension methods for working with Unity-MCP data types
   - **GameObjectRef extensions**: Find GameObject from reference ([ExtensionsRuntimeGameObjectRef.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Extensions/ExtensionsRuntimeGameObjectRef.cs))
   - **ComponentRef extensions**: Find Component from reference ([ExtensionsComponentRef.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Extensions/ExtensionsComponentRef.cs))
   - **SerializedMember extensions**: Extract instanceID and type information ([ExtensionsSerializedMember.cs](../Unity-MCP-Plugin/Assets/root/Runtime/Extensions/ExtensionsSerializedMember.cs))
   - Simplifies reference resolution and type conversion

**Architecture Pattern:**

- Shared between Editor and Runtime via conditional compilation (`#if UNITY_EDITOR`)
- Uses partial classes to separate concerns across multiple files
- Thread-safe singleton pattern with mutex synchronization
- Reactive programming using R3 library for state changes
- Dependency injection for logging via Microsoft.Extensions.Logging
- Custom reflection/serialization system integrated with ReflectorNet
- All Unity API calls marshaled to main thread via MainThreadDispatcher

### MCP features

#### Add `MCP Tool`

```csharp
[McpPluginToolType]
public class Tool_GameObject
{
    [McpPluginTool
    (
        "MyCustomTask",
        Title = "Create a new GameObject"
    )]
    [Description("Explain here to LLM what is this, when it should be called.")]
    public string CustomTask
    (
        [Description("Explain to LLM what is this.")]
        string inputData
    )
    {
        // do anything in background thread

        return MainThread.Instance.Run(() =>
        {
            // do something in main thread if needed

            return $"[Success] Operation completed.";
        });
    }
}
```

#### Add `MCP Prompt`

`MCP Prompt` allows you to inject custom prompts into the conversation with the LLM. It supports two sender roles: User and Assistant. This is a quick way to instruct the LLM to perform specific tasks. You can generate prompts using custom data, providing lists or any other relevant information.

```csharp
[McpPluginPromptType]
public static class Prompt_ScriptingCode
{
    [McpPluginPrompt(Name = "add-event-system", Role = Role.User)]
    [Description("Implement UnityEvent-based communication system between GameObjects.")]
    public string AddEventSystem()
    {
        return "Create event system using UnityEvents, UnityActions, or custom event delegates for decoupled communication between game systems and components.";
    }
}
```

---

## 🔺Unity-MCP-Common

```mermaid
graph TD
  A(🔹Unity-MCP-Server)
  B(🔸Unity-MCP-Plugin)
  C(🔺Unity-MCP-Common)

  %% Relationships
  A --> C
  B --> C
```

**Unity-MCP-Common** - shared code base between `Unity-MCP-Server` and `Unity-MCP-Plugin`. It is needed to simplify the data model and API sharing between projects. It is an independent dotnet library project.

> Project location: `Unity-MCP-Plugin/Assets/root/Unity-MCP-Common`

---

## ◾Installer (Unity)

```mermaid
graph LR
  A(◾Installer)
  subgraph Installation
    B(🎮Unity)
    C(🔸Unity-MCP-Plugin)
  end

  %% Relationships
  A --> B
  B -.- C
```

**Installer** installs `Unity-MCP-Plugin` and dependencies as an NPM packages into a Unity project.

> Project location: `Installer`

---

# Code style

---

# CI/CD

The project implements a comprehensive CI/CD pipeline using GitHub Actions with multiple workflows orchestrating the build, test, and deployment processes.

## Workflows Overview

> Location: `.github/workflows`

### 🚀 [release.yml](.github/workflows/release.yml)

**Trigger:** Push to `main` branch
**Purpose:** Main release workflow that orchestrates the entire release process

**Process:**

1. **Version Check** - Extracts version from [package.json](Unity-MCP-Plugin/Assets/root/package.json) and checks if release tag already exists
2. **Build Unity Installer** - Tests and exports Unity package installer (`AI-Game-Dev-Installer.unitypackage`)
3. **Build MCP Server** - Compiles cross-platform executables (Windows, macOS, Linux) using [build-all.sh](Unity-MCP-Server/build-all.sh)
4. **Unity Plugin Testing** - Runs comprehensive tests across:
   - 3 Unity versions: `2022.3.61f1`, `2023.2.20f1`, `6000.2.3f1`
   - 3 test modes: `editmode`, `playmode`, `standalone`
   - 2 operating systems: `windows-latest`, `ubuntu-latest`
   - Total: **18 test matrix combinations**
5. **Release Creation** - Generates release notes from commits and creates GitHub release with tag
6. **Publishing** - Uploads Unity installer package and MCP Server executables to the release
7. **Discord Notification** - Sends formatted release notes to Discord channel
8. **Deploy** - Triggers deployment workflow for NuGet and Docker
9. **Cleanup** - Removes build artifacts after successful publishing

### 🧪 [test_pull_request.yml](.github/workflows/test_pull_request.yml)

**Trigger:** Pull requests to `main` or `dev` branches
**Purpose:** Validates PR changes before merging

**Process:**

1. Builds MCP Server executables for all platforms
2. Runs the same 18 Unity test matrix combinations as the release workflow
3. All tests must pass before PR can be merged

### 🔧 [test_unity_plugin.yml](.github/workflows/test_unity_plugin.yml)

**Type:** Reusable workflow
**Purpose:** Parameterized Unity testing workflow used by both release and PR workflows

**Features:**

- Accepts parameters: `projectPath`, `unityVersion`, `testMode`
- Runs on matrix of operating systems (Windows, Ubuntu)
- Uses Game CI Unity Test Runner with custom Docker images
- Implements security checks for PR contributors (requires `ci-ok` label for untrusted PRs)
- Aborts if workflow files are modified in PRs
- Caches Unity Library for faster subsequent runs
- Uploads test artifacts for debugging

### 📦 [deploy.yml](.github/workflows/deploy.yml)

**Trigger:** Called by release workflow OR manual dispatch OR on release published
**Purpose:** Deploys MCP Server to NuGet and Docker Hub

**Jobs:**

**1. Deploy to NuGet:**

- Builds and tests the MCP Server
- Packs NuGet package
- Publishes to [nuget.org](https://www.nuget.org/packages/com.IvanMurzak.Unity.MCP.Server)

**2. Deploy Docker Image:**

- Builds multi-platform Docker image (linux/amd64, linux/arm64)
- Pushes to [Docker Hub](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
- Tags with version number and `latest`
- Uses GitHub Actions cache for build optimization

### 🎯 [deploy_server_executables.yml](.github/workflows/deploy_server_executables.yml)

**Trigger:** GitHub release published
**Purpose:** Builds and uploads cross-platform server executables to release

**Process:**

- Runs on macOS for cross-compilation support
- Builds executables for Windows, macOS, Linux using [build-all.sh](Unity-MCP-Server/build-all.sh)
- Creates ZIP archives for each platform
- Uploads to the GitHub release

## Technology Stack

- **CI Platform:** GitHub Actions
- **Unity Testing:** [Game CI](https://game.ci/) with Unity Test Runner
- **Containerization:** Docker with multi-platform builds
- **Package Management:** NuGet, OpenUPM, Docker Hub
- **Build Tools:** .NET 9.0, bash scripts
- **Artifact Storage:** GitHub Actions artifacts (temporary), GitHub Releases (permanent)

## Security Considerations

- Unity license, email, and password stored as GitHub secrets
- NuGet API key and Docker credentials secured
- PR workflow includes safety checks for workflow file modifications
- Untrusted PR contributions require maintainer approval via `ci-ok` label

## Deployment Targets

1. **GitHub Releases** - Unity installer package and MCP Server executables
2. **NuGet** - MCP Server package for .NET developers
3. **Docker Hub** - Containerized MCP Server for cloud deployments
4. **OpenUPM** - Unity plugin package (automatically synced from GitHub releases)

