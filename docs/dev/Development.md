<div align="center" width="100%">
  <h1>🛠️ Development ─ AI Game Developer</h1>

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

  <b>[中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.es.md)</b>

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

The Editor component provides Unity Editor integration, implementing MCP capabilities (Tools, Prompts, Resources) and managing the `Unity-MCP-Server` lifecycle.

> Location `Unity-MCP-Plugin/Assets/root/Editor`

**Main Responsibilities:**

1. **Plugin Lifecycle Management** ([Startup.cs](../../Unity-MCP-Plugin/Assets/root/Editor/Scripts/Startup.cs))
   - Auto-initializes on Unity Editor load via `[InitializeOnLoad]`
   - Manages connection persistence across Editor lifecycle events (assembly reload, play mode transitions)
   - Automatic reconnection after domain reload or Play mode exit

2. **MCP Server Binary Management** ([Startup.Server.cs](../../Unity-MCP-Plugin/Assets/root/Editor/Scripts/Startup.Server.cs))
   - Downloads and manages `Unity-MCP-Server` executable from GitHub releases
   - Cross-platform binary selection (Windows/macOS/Linux, x86/x64/ARM/ARM64)
   - Version compatibility enforcement between server and plugin
   - Configuration generation for MCP clients (JSON with executable paths and connection settings)

3. **MCP API Implementation** ([Scripts/API/](../../Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/))
   - **Tools** (50+): GameObject, Scene, Assets, Prefabs, Scripts, Components, Editor Control, Test Runner, Console, Reflection
   - **Prompts**: Pre-built templates for common Unity development tasks
   - **Resources**: URI-based access to Unity Editor data with JSON serialization
   - All operations execute on Unity's main thread for thread safety
   - Attribute-based discovery using `[McpPluginTool]`, `[McpPluginPrompt]`, `[McpPluginResource]`

4. **Editor UI** ([Scripts/UI/](../../Unity-MCP-Plugin/Assets/root/Editor/Scripts/UI/))
   - Configuration window for connection management (`Window > AI Game Developer`)
   - Server binary management and log access via Unity menu items

### Runtime

The Runtime component provides core infrastructure shared between Editor and Runtime modes, handling SignalR communication, serialization, and thread-safe Unity API access.

> Location `Unity-MCP-Plugin/Assets/root/Runtime`

**Main Responsibilities:**

1. **Plugin Core & SignalR Connection** ([UnityMcpPlugin.cs](../../Unity-MCP-Plugin/Assets/root/Runtime/UnityMcpPlugin.cs))
   - Thread-safe singleton managing plugin lifecycle via `BuildAndStart()`
   - Discovers MCP Tools/Prompts/Resources from assemblies using reflection
   - Establishes SignalR connection to Unity-MCP-Server with reactive state monitoring (R3 library)
   - Configuration management: host, port, timeout, version compatibility

2. **Main Thread Dispatcher** ([MainThreadDispatcher.cs](../../Unity-MCP-Plugin/Assets/root/Runtime/Utils/MainThreadDispatcher.cs))
   - Marshals Unity API calls from SignalR background threads to Unity's main thread
   - Queue-based execution in Unity's Update loop
   - Critical for thread-safe MCP operation execution

3. **Unity Type Serialization** ([ReflectionConverters/](../../Unity-MCP-Plugin/Assets/root/Runtime/ReflectionConverters/), [JsonConverters/](../../Unity-MCP-Plugin/Assets/root/Runtime/JsonConverters/))
   - Custom JSON serialization for Unity types (GameObject, Component, Transform, Vector3, Quaternion, etc.)
   - Converts Unity objects to reference format (`GameObjectRef`, `ComponentRef`) with instanceID tracking
   - Integrates with ReflectorNet for object introspection and component serialization
   - Provides JSON schemas for MCP protocol type definitions

4. **Logging & Diagnostics** ([Logger/](../../Unity-MCP-Plugin/Assets/root/Runtime/Logger/), [Unity/Logs/](../../Unity-MCP-Plugin/Assets/root/Runtime/Unity/Logs/))
   - Bridges Microsoft.Extensions.Logging to Unity Console with color-coded levels
   - Collects Unity Console logs for AI context retrieval via MCP Tools

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

This project follows consistent C# coding patterns. Below is a comprehensive example demonstrating the key conventions:

```csharp
/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

// Enable nullable reference types for better null safety
#nullable enable

// Conditional compilation for platform-specific code
#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    // Use [McpPluginToolType] for tool classes - enables MCP discovery via reflection
    [McpPluginToolType]
    // Partial classes allow splitting implementation across multiple files
    // Pattern: One file per operation (e.g., GameObject.Create.cs, GameObject.Destroy.cs)
    public partial class Tool_GameObject
    {
        // Nested Error class centralizes error messages for maintainability
        public static class Error
        {
            // Static methods for consistent error formatting
            public static string GameObjectNameIsEmpty()
                => "[Error] GameObject name is empty. Please provide a valid name.";

            public static string NotFoundGameObjectAtPath(string path)
                => $"[Error] GameObject '{path}' not found.";
        }

        // MCP Tool declaration with attribute-based metadata
        [McpPluginTool(
            "GameObject_Create",                    // Unique tool identifier
            Title = "Create a new GameObject"       // Human-readable title
        )]
        // Description attribute guides AI on when/how to use this tool
        [Description(@"Create a new GameObject in the scene.
Provide position, rotation, and scale to minimize subsequent operations.")]
        public string Create
        (
            // Parameter descriptions help AI understand expected inputs
            [Description("Name of the new GameObject.")]
            string name,

            [Description("Parent GameObject reference. If not provided, created at scene root.")]
            GameObjectRef? parentGameObjectRef = null,  // Nullable with default value

            [Description("Transform position of the GameObject.")]
            Vector3? position = null,                    // Unity struct, nullable

            [Description("Transform rotation in Euler angles (degrees).")]
            Vector3? rotation = null,

            [Description("Transform scale of the GameObject.")]
            Vector3? scale = null
        )
        // Lambda expression syntax for immediate main thread execution
        => MainThread.Instance.Run(() =>           // All Unity API calls MUST run on main thread
        {
            // Validate input parameters early
            if (string.IsNullOrEmpty(name))
                return Error.GameObjectNameIsEmpty();

            // Null-coalescing assignment for default values
            position ??= Vector3.zero;
            rotation ??= Vector3.zero;
            scale ??= Vector3.one;

            // Create GameObject using Unity API
            var go = new GameObject(name);

            // Set parent if provided
            if (parentGameObjectRef?.IsValid ?? false)
            {
                var parentGo = parentGameObjectRef.FindGameObject(out var error);
                if (error != null)
                    return $"[Error] {error}";

                go.transform.SetParent(parentGo.transform, worldPositionStays: false);
            }

            // Apply transform values
            go.transform.localPosition = position.Value;
            go.transform.localRotation = Quaternion.Euler(rotation.Value);
            go.transform.localScale = scale.Value;

            // Mark as modified for Unity Editor
            EditorUtility.SetDirty(go);

            // Return success message with structured data
            // Use string interpolation for readable formatting
            return $"[Success] Created GameObject.\ninstanceID: {go.GetInstanceID()}, path: {go.GetPath()}";
        });

        // Async method example with proper error handling
        public static async Task<string> AsyncOperation(string parameter)
        {
            try
            {
                // Background work can happen here
                await Task.Delay(100);

                // Switch to main thread for Unity API calls
                return await MainThread.Instance.RunAsync(() =>
                {
                    // Unity API calls here
                    return "[Success] Async operation completed.";
                });
            }
            catch (Exception ex)
            {
                // Log exceptions with structured logging
                Debug.LogException(ex);
                return $"[Error] Operation failed: {ex.Message}";
            }
        }
    }

    // Separate partial class file for prompts
    [McpPluginPromptType]
    public static partial class Prompt_SceneManagement
    {
        // MCP Prompt with role definition (User or Assistant)
        [McpPluginPrompt(Name = "setup-basic-scene", Role = Role.User)]
        [Description("Setup a basic scene with camera, lighting, and environment.")]
        public static string SetupBasicScene()
        {
            // Return prompt text for AI to process
            return "Create a basic Unity scene with Main Camera, Directional Light, and basic environment setup.";
        }
    }
}
```

**Key Conventions:**

1. **File Headers**: Include copyright notice in box comment format
2. **Nullable Context**: Use `#nullable enable` for null safety
3. **Attributes**: Leverage `[McpPluginTool]`, `[McpPluginPrompt]`, `[McpPluginResource]` for MCP discovery
4. **Partial Classes**: Split functionality across files (e.g., `Tool_GameObject.Create.cs`)
5. **Main Thread Execution**: Wrap Unity API calls with `MainThread.Instance.Run()`
6. **Error Handling**: Centralize error messages in nested `Error` classes
7. **Return Format**: Use `[Success]` or `[Error]` prefixes in return strings
8. **Descriptions**: Annotate all public APIs with `[Description]` for AI guidance
9. **Naming**: Use PascalCase for public members, camelCase for private/local
10. **Null Safety**: Use nullable types (`?`) and null-coalescing operators (`??`, `??=`)

---

# CI/CD

The project implements a comprehensive CI/CD pipeline using GitHub Actions with multiple workflows orchestrating the build, test, and deployment processes.

## Workflows Overview

> Location: `.github/workflows`

### 🚀 [release.yml](../../.github/workflows/release.yml)

**Trigger:** Push to `main` branch
**Purpose:** Main release workflow that orchestrates the entire release process

**Process:**

1. **Version Check** - Extracts version from [package.json](../../Unity-MCP-Plugin/Assets/root/package.json) and checks if release tag already exists
2. **Build Unity Installer** - Tests and exports Unity package installer (`AI-Game-Dev-Installer.unitypackage`)
3. **Build MCP Server** - Compiles cross-platform executables (Windows, macOS, Linux) using [build-all.sh](../../Unity-MCP-Server/build-all.sh)
4. **Unity Plugin Testing** - Runs comprehensive tests across:
   - 3 Unity versions: `2022.3.62f3`, `2023.2.22f1`, `6000.3.1f1`
   - 3 test modes: `editmode`, `playmode`, `standalone`
   - 2 operating systems: `windows-latest`, `ubuntu-latest`
   - Total: **18 test matrix combinations**
5. **Release Creation** - Generates release notes from commits and creates GitHub release with tag
6. **Publishing** - Uploads Unity installer package and MCP Server executables to the release
7. **Discord Notification** - Sends formatted release notes to Discord channel
8. **Deploy** - Triggers deployment workflow for NuGet and Docker
9. **Cleanup** - Removes build artifacts after successful publishing

### 🧪 [test_pull_request.yml](../../.github/workflows/test_pull_request.yml)

**Trigger:** Pull requests to `main` or `dev` branches
**Purpose:** Validates PR changes before merging

**Process:**

1. Builds MCP Server executables for all platforms
2. Runs the same 18 Unity test matrix combinations as the release workflow
3. All tests must pass before PR can be merged

### 🔧 [test_unity_plugin.yml](../../.github/workflows/test_unity_plugin.yml)

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

### 📦 [deploy.yml](../../.github/workflows/deploy.yml)

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

### 🎯 [deploy_server_executables.yml](../../.github/workflows/deploy_server_executables.yml)

**Trigger:** GitHub release published
**Purpose:** Builds and uploads cross-platform server executables to release

**Process:**

- Runs on macOS for cross-compilation support
- Builds executables for Windows, macOS, Linux using [build-all.sh](../../Unity-MCP-Server/build-all.sh)
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
