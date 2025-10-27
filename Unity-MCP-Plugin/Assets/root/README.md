<div align="center" width="100%">
  <h1>✨ AI Game Developer — <i>Unity MCP</i></h1>

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

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/level-building.gif" alt="AI work" title="Level building" width="100%">

  <b>[中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md)</b>

</div>

`Unity MCP` is an AI-powered game development assistant that serves as a bridge between `MCP Client` and `Unity`. Simply type a message in chat and get work done using any advanced LLM model of your choice. Have an issue to fix? Ask the AI to fix it. **[Watch demo videos](https://www.youtube.com/watch?v=kQUOCQ-c0-M&list=PLyueiUu0xU70uzNoOaanGQD2hiyJmqHtK)**.

> **[💬 Join our Discord Server](https://discord.gg/cfbdMZX99G)** - Ask questions, showcase your work, and connect with other developers!

## Features

- ✔️ **Natural conversation** - Chat with AI like you would with a human
- ✔️ **Code assistance** - Ask AI to write code and run tests
- ✔️ **Debug support** - Ask AI to get logs and fix errors
- ✔️ **Multiple LLM providers** - Use agents from Anthropic, OpenAI, Microsoft, or any other provider with no limits
- ✔️ **Flexible deployment** - Works locally (stdio) and remotely (http) by configuration
- ✔️ **Rich toolset** - Wide range of default [MCP Tools](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)
- ✔️ **Extensible** - Create [custom MCP Tools in your project code](#add-custom-mcp-tool)

### Stability status

| Unity Version | Editmode                                                                                                                                                                               | Playmode                                                                                                                                                                               | Standalone                                                                                                                                                                               |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.61f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-61f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-61f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-61f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 2023.2.20f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-20f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-20f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-20f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 6000.2.3f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-2-3f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-2-3f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-2-3f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  |

## Content

- [Installation](#installation)
  - [Step 1: Install `Unity MCP Plugin`](#step-1-install-unity-mcp-plugin)
    - [Option 1 - Installer](#option-1---installer)
    - [Option 2 - OpenUPM-CLI](#option-2---openupm-cli)
  - [Step 2: Install `MCP Client`](#step-2-install-mcp-client)
  - [Step 3: Configure `MCP Client`](#step-3-configure-mcp-client)
    - [Automatic configuration](#automatic-configuration)
    - [Manual configuration](#manual-configuration)
- [Use AI](#use-ai)
  - [Advanced Features for LLM](#advanced-features-for-llm)
    - [Core Capabilities](#core-capabilities)
    - [Reflection-Powered Features](#reflection-powered-features)
- [Customize MCP](#customize-mcp)
  - [Add custom `MCP Tool`](#add-custom-mcp-tool)
  - [Add custom `MCP Prompt`](#add-custom-mcp-prompt)
- [Runtime usage (in-game)](#runtime-usage-in-game)
  - [Sample: AI powered Chess game bot](#sample-ai-powered-chess-game-bot)
  - [Why runtime usage is needed?](#why-runtime-usage-is-needed)
- [Unity `MCP Server` setup](#unity-mcp-server-setup)
  - [Variables](#variables)
  - [Docker 📦](#docker-)
    - [`HTTP` Transport](#http-transport)
    - [`STDIO` Transport](#stdio-transport)
    - [Custom `port`](#custom-port)
  - [Binary executable](#binary-executable)
- [How it works](#how-it-works)
  - [What is `MCP`](#what-is-mcp)
  - [What is `MCP Client`](#what-is-mcp-client)
  - [What is `MCP Server`](#what-is-mcp-server)
  - [What is `MCP Tool`](#what-is-mcp-tool)
    - [When to use `MCP Tool`](#when-to-use-mcp-tool)
  - [What is `MCP Resource`](#what-is-mcp-resource)
    - [When to use `MCP Resource`](#when-to-use-mcp-resource)
  - [What is `MCP Prompt`](#what-is-mcp-prompt)
    - [When to use `MCP Prompt`](#when-to-use-mcp-prompt)
- [Contribution 💙💛](#contribution-)

# Installation

## Step 1: Install `Unity MCP Plugin`

<details>
  <summary><b>⚠️ Requirements (click)</b></summary>

> [!IMPORTANT]
> **Project path cannot contain spaces**
>
> - ✅ `C:/MyProjects/Project`
> - ❌ `C:/My Projects/Project`

</details>

### Option 1 - Installer

- **[⬇️ Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.21.0/AI-Game-Dev-Installer.unitypackage)**
- **📂 Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [⬇️ Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp
```

## Step 2: Install `MCP Client`

Choose a single `MCP Client` you prefer - you don't need to install all of them. This will be your main chat window to communicate with the LLM.

- [Claude Code](https://github.com/anthropics/claude-code) (highly recommended)
- [Claude Desktop](https://claude.ai/download)
- [GitHub Copilot in VS Code](https://code.visualstudio.com/docs/copilot/overview)
- [Cursor](https://www.cursor.com/)
- [Windsurf](https://windsurf.com)
- Any other supported

> The MCP protocol is quite universal, which is why you may use any MCP client you prefer - it will work as smoothly as any other. The only important requirement is that the MCP client must support dynamic MCP Tool updates.

## Step 3: Configure `MCP Client`

### Automatic configuration

- Open Unity project
- Open `Window/AI Game Developer (Unity-MCP)`
- Click `Configure` at your MCP client

![Unity_AI](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/ai-connector-window.gif)

> If your MCP client is not in the list, use the raw JSON shown in the window to inject it into your MCP client. Read the instructions for your specific MCP client on how to do this.

### Manual configuration

If automatic configuration doesn't work for you for any reason, use the JSON from the `AI Game Developer (Unity-MCP)` window to configure any `MCP Client` manually.

<details>
  <summary>Configure <b><code>Claude Code</code></b> for <b>Windows</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" client-transport=stdio
  ```

</details>

<details>
  <summary>Configure <b><code>Claude Code</code></b> for <b>MacOS Apple-Silicon</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary>Configure <b><code>Claude Code</code></b> for <b>MacOS Apple-Intel</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary>Configure <b><code>Claude Code</code></b> for <b>Linux x64</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary>Configure <b><code>Claude Code</code></b> for <b>Linux arm64</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/linux-arm64/unity-mcp-server" client-transport=stdio
  ```

</details>

---

# Use AI

Communicate with the AI (LLM) in your `MCP Client`. Ask it to do anything you want. The better you describe your task or idea, the better it will perform the job.

Some `MCP Clients` allow you to choose different LLM models. Pay attention to this feature, as some models may work much better than others.

**Example commands:**

```text
Explain my scene hierarchy
```

```text
Create 3 cubes in a circle with radius 2
```

```text
Create metallic golden material and attach it to a sphere gameObject
```

> Make sure `Agent` mode is turned on in your MCP client

## Advanced Features for LLM

Unity MCP provides advanced tools that enable the LLM to work faster and more effectively, avoiding mistakes and self-correcting when errors occur. Everything is designed to achieve your goals efficiently.

### Core Capabilities

- ✔️ **Agent-ready tools** - Find anything you need in 1-2 steps
- ✔️ **Instant compilation** - C# code compilation & execution using `Roslyn` for faster iteration
- ✔️ **Full asset access** - Read/write access to assets and C# scripts
- ✔️ **Intelligent feedback** - Well-described positive and negative feedback for proper issue understanding

### Reflection-Powered Features

- ✔️ **Object references** - Provide references to existing objects for instant C# code
- ✔️ **Project data access** - Get full access to entire project data in a readable format
- ✔️ **Granular modifications** - Populate & modify any piece of data in the project
- ✔️ **Method discovery** - Find any method in the entire codebase, including compiled DLL files
- ✔️ **Method execution** - Call any method in the entire codebase
- ✔️ **Advanced parameters** - Provide any property for method calls, even references to existing objects in memory
- ✔️ **Live Unity API** - Unity API instantly available - even when Unity changes, you get the fresh API
- ✔️ **Self-documenting** - Access human-readable descriptions of any `class`, `method`, `field`, or `property` via `Description` attributes

---

# Customize MCP

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** supports custom `MCP Tool`, `MCP Resource`, and `MCP Prompt` development by project owners. The MCP server takes data from the `Unity MCP Plugin` and exposes it to a client. Anyone in the MCP communication chain will receive information about new MCP features, which the LLM may decide to use at some point.

## Add custom `MCP Tool`

To add a custom `MCP Tool`, you need:

1. A class with the `McpPluginToolType` attribute
2. A method in the class with the `McpPluginTool` attribute
3. *Optional:* Add a `Description` attribute to each method argument to help the LLM understand it
4. *Optional:* Use `string? optional = null` properties with `?` and default values to mark them as `optional` for the LLM

> Note that the line `MainThread.Instance.Run(() =>` allows you to run code on the main thread, which is required for interacting with Unity's API. If you don't need this and running the tool in a background thread is acceptable, avoid using the main thread for efficiency purposes.

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

## Add custom `MCP Prompt`

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

# Runtime usage (in-game)

Use **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** in your game/app. Use Tools, Resources or Prompts. By default there are no tools, you would need to implement your custom.

```csharp
UnityMcpPlugin.BuildAndStart(); // Build and start Unity-MCP-Plugin, it is required
UnityMcpPlugin.Connect(); // Start active connection with retry to Unity-MCP-Server
UnityMcpPlugin.Disconnect(); // Stop active connection and close existed connection
```

## Sample: AI powered Chess game bot

There is a classic Chess game. Lets outsource to LLM the bot logic. Bot should do the turn using game rules.

```csharp
[McpPluginToolType]
public static class ChessGameAI
{
    [McpPluginTool("chess-do-turn", Title = "Do the turn")]
    [Description("Do the turn in the chess game. Returns true if the turn was accepted, false otherwise.")]
    public static Task<bool> DoTurn(int figureId, Vector2Int position)
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.DoTurn(figureId, position));
    }

    [McpPluginTool("chess-get-board", Title = "Get the board")]
    [Description("Get the current state of the chess board.")]
    public static Task<BoardData> GetBoard()
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.GetBoardData());
    }
}
```

## Why runtime usage is needed?

There are many use cases, lets imagine you are working on a Chess game with bot. You may outsource the bot decision making to LLM by writing few lines of code.

---

# Unity `MCP Server` setup

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** Server supports many different launch options and Docker deployment. Both transport protocols are supported: `http` and `stdio`. If you need to customize or deploy Unity MCP Server to a cloud, this section is for you. [Read more...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

## Variables

Doesn't matter what launch option you choose, all of them support custom configuration using both Environment Variables and Command Line Arguments. It would work with default values, if you just need to launch it, don't waste your time for the variables. Just make sure Unity Plugin also has default values, especially the `--port`, they should be equal.

| Environment Variable        | Command Line Args     | Description                                                                 |
|-----------------------------|-----------------------|-----------------------------------------------------------------------------|
| `UNITY_MCP_PORT`            | `--port`              | **Client** -> **Server** <- **Plugin** connection port (default: 8080)      |
| `UNITY_MCP_PLUGIN_TIMEOUT`  | `--plugin-timeout`    | **Plugin** -> **Server** connection timeout (ms) (default: 10000)           |
| `UNITY_MCP_CLIENT_TRANSPORT`| `--client-transport`  | **Client** -> **Server** transport type: `stdio` or `http` (default: `http`) |

> Command line args support also the option with a single `-` prefix (`-port`) and an option without prefix at all (`port`).

## Docker 📦

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)

Make sure Docker is installed. And please make sure Docker Desktop is launched if you are at Windows operation system.

### `HTTP` Transport

```bash
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCP Client</code> config:</summary>

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "url": "http://localhost:8080"
    }
  }
}
```

> Replace `url` with your real endpoint if it is hosted in cloud.

</details>

### `STDIO` Transport

For using this variant, `MCP Client` should launch the `MCP Server` in the docker. It is achievable through the modified `MCP Client` configuration.

```bash
docker run -t -e UNITY_MCP_CLIENT_TRANSPORT=stdio -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCP Client</code> config:</summary>

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "command": "docker",
      "args": [
        "run",
        "-t",
        "-e",
        "UNITY_MCP_CLIENT_TRANSPORT=stdio",
        "-p",
        "8080:8080",
        "ivanmurzakdev/unity-mcp-server"
      ]
    }
  }
}
```

</details>

### Custom `port`

```bash
docker run -e UNITY_MCP_PORT=123 -p 123:123 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCP Client</code> config:</summary>

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "url": "http://localhost:123"
    }
  }
}
```

> Replace `url` with your real endpoint if it is hosted in cloud
</details>

## Binary executable

You may launch Unity `MCP Server` directly from a binary file. You would need to have a binary compiled specifically for your CPU architecture. Check [GitHub Release Page](https://github.com/IvanMurzak/Unity-MCP/releases), it contains pre-compiled binaries for all CPU architectures.

```bash
./unity-mcp-server --port 8080 --plugin-timeout 10000 --client-transport stdio
```

<details>
  <summary><code>MCP Client</code> config:</summary>

> Replace `<project>` with your Unity project path.

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "command": "<project>/Library/mcp-server/win-x64/unity-mcp-server.exe",
      "args": [
        "--port=8080",
        "--plugin-timeout=10000",
        "--client-transport=stdio"
      ]
    }
  }
}
```

</details>

---

# How it works

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** serves as a bridge between LLMs and Unity. It exposes and explains Unity's tools to the LLM, which then understands the interface and utilizes the tools according to user requests.

Connect **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** to LLM clients such as [Claude](https://claude.ai/download) or [Cursor](https://www.cursor.com/) using the integrated `AI Connector` window. Custom clients are also supported.

The system is highly extensible - you can define custom `MCP Tools`, `MCP Resource` or `MCP Prompt` directly in your Unity project codebase, exposing new capabilities to AI or automation clients. This makes Unity MCP a flexible foundation for building advanced workflows, rapid prototyping, and integrating AI-driven features into your development process.

## What is `MCP`

MCP - Model Context Protocol. In a few words, that is `USB Type-C` for AI, specifically for LLM (Large Language Model). It teaches LLM how to use external features. Such as Unity Engine in this case, or even your custom C# method in your code. [Official documentation](https://modelcontextprotocol.io/).

## What is `MCP Client`

It is an application with a chat window. It may have smart agents to operate better, it may have embedded advanced MCP Tools. In general well done MCP Client is 50% of the AI success of executing a task. That is why it is very important to choose the best one for usage.

## What is `MCP Server`

It is a bridge between `MCP Client` and "something else", in this particular case it is Unity Engine. This project includes `MCP Server`.

## What is `MCP Tool`

`MCP Tool` is a function or method that the LLM can call to interact with Unity. These tools act as the bridge between natural language requests and actual Unity operations. When you ask the AI to "create a cube" or "change material color," it uses MCP Tools to execute these actions.

**Key characteristics:**

- **Executable functions** that perform specific operations
- **Typed parameters** with descriptions to help the LLM understand what data to provide
- **Return values** that give feedback about the operation's success or failure
- **Thread-aware** - can run on main thread for Unity API calls or background thread for heavy processing

### When to use `MCP Tool`

- **Automate repetitive tasks** - Create tools for common operations you do frequently
- **Complex operations** - Bundle multiple Unity API calls into a single, easy-to-use tool
- **Project-specific workflows** - Build tools that understand your project's specific structure and conventions
- **Error-prone tasks** - Create tools that include validation and error handling
- **Custom game logic** - Expose your game's systems to AI for dynamic content creation

**Examples:**

- Creating and configuring GameObjects with specific components
- Batch processing assets (textures, materials, prefabs)
- Setting up lighting and post-processing effects
- Generating level geometry or placing objects procedurally
- Configuring physics settings or collision layers

## What is `MCP Resource`

`MCP Resource` provides read-only access to data within your Unity project. Unlike MCP Tools that perform actions, Resources allow the LLM to inspect and understand your project's current state, assets, and configuration. Think of them as "sensors" that give the AI context about your project.

**Key characteristics:**

- **Read-only access** to project data and Unity objects
- **Structured information** presented in a format the LLM can understand
- **Real-time data** that reflects the current state of your project
- **Contextual awareness** helping the AI make informed decisions

### When to use `MCP Resource`

- **Project analysis** - Let AI understand your project structure, assets, and organization
- **Debugging assistance** - Provide current state information for troubleshooting
- **Intelligent suggestions** - Give AI context to make better recommendations
- **Documentation generation** - Automatically create documentation based on project state
- **Asset management** - Help AI understand what assets are available and their properties

**Examples:**

- Exposing scene hierarchy and GameObject properties
- Listing available materials, textures, and their settings
- Showing script dependencies and component relationships
- Displaying current lighting setup and render pipeline configuration
- Providing information about audio sources, animations, and particle systems

## What is `MCP Prompt`

`MCP Prompt` allows you to inject pre-defined prompts into the conversation with the LLM. These are smart templates that can provide context, instructions, or knowledge to guide the AI's behavior. Prompts can be static text or dynamically generated based on your project's current state.

**Key characteristics:**

- **Contextual guidance** that influences how the AI responds
- **Role-based** - can simulate different personas (User requests or Assistant knowledge)
- **Dynamic content** - can include real-time project data
- **Reusable templates** for common scenarios and workflows

### When to use `MCP Prompt`

- **Provide domain knowledge** - Share best practices and coding standards specific to your project
- **Set coding conventions** - Establish naming conventions, architecture patterns, and code style
- **Give context about project structure** - Explain how your project is organized and why
- **Share workflow instructions** - Provide step-by-step procedures for common tasks
- **Inject specialized knowledge** - Add information about specific Unity features, third-party assets, or custom systems

**Examples:**

- "Always use PascalCase for public methods and camelCase for private fields"
- "This project uses a custom event system located in Scripts/Events/"
- "When creating UI elements, always add them to the Canvas in Scene/UI/MainCanvas"
- "Performance is critical - prefer object pooling for frequently instantiated objects"
- "This project follows SOLID principles - explain any architecture decisions"

---

# Contribution 💙💛

Contributions are highly appreciated. Bring your ideas and let's make game development simpler than ever before! Do you have an idea for a new `MCP Tool` or feature, or did you spot a bug and know how to fix it?

1. 👉 [Read Development documentation](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md)
2. 👉 [Fork the project](https://github.com/IvanMurzak/Unity-MCP/fork)
3. Clone the fork and open the `./Unity-MCP-Plugin` folder in Unity
4. Implement new things in the project, commit, push it to GitHub
5. Create Pull Request targeting original [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP/compare) repository, `main` branch.
