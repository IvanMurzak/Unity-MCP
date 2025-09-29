<div align="center" width="100%">
  <h1>✨ AI Game Developer — <i>Unity MCP</i></h1>

[![MCP](https://badge.mcpx.dev?type=server 'MCP Server')](https://modelcontextprotocol.io/introduction)
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

</div>

`Unity MCP` is the best game developer helper powered by AI. It works as a bridge between `MCP Client` and `Unity`. Type a message in chat, and get the work done using any advanced LLM model by your choice. Have an issue to fix? Ask AI to fix it. [Watch demo videos](https://www.youtube.com/watch?v=kQUOCQ-c0-M&list=PLyueiUu0xU70uzNoOaanGQD2hiyJmqHtK).

## Features

- ✔️ Chat with AI like with a human
- ✔️ Ask AI to **write code** and **run tests**
- ✔️ Ask AI to **get logs** and to **fix an error**
- ✔️ Use **the best agents** from Anthropic, OpenAI, Microsoft or anyone else, no limits.
- ✔️ Works locally (stdio) and remotely (http) by configuration
- ✔️ Wide range of default [MCP Tools](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)
- ✔️ Create [custom `MCP Tool` in your project code](#custom-mcp-tool)

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
  - [Features for LLM](#features-for-llm)
- [Add custom `MCP Tool`](#add-custom-mcp-tool)
- [Add custom runtime (in-game) `MCP Tool`](#add-custom-runtime-in-game-mcp-tool)
- [How it works](#how-it-works)
- [Advanced MCP server setup](#advanced-mcp-server-setup)
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

- **[⬇️ Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.18.0/AI-Game-Dev-Installer.unitypackage)**
- **📂 Import installer into Unity project**
  > - You may use double click on the file - Unity will open it
  > - OR: You may open Unity Editor first, then click on `Assets/Import Package/Custom Package`, then choose the file

### Option 2 - OpenUPM-CLI

- [⬇️ Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 Open command line in Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp
```

## Step 2: Install `MCP Client`

Choose a single `MCP Client` you prefer, don't need to install all of them. This is will be your main chat window to talk with LLM.

- [Claude Code](https://github.com/anthropics/claude-code) (highly recommended)
- [Claude Desktop](https://claude.ai/download)
- [GitHub Copilot in VS Code](https://code.visualstudio.com/docs/copilot/overview)
- [Cursor](https://www.cursor.com/)
- [Windsurf](https://windsurf.com)
- Any other supported

> MCP protocol is quite universal, that is why you may any MCP client you prefer, it will work as smooth as anyone else. The only important thing, that the MCP client has to support dynamic MCP Tool update.

## Step 3: Configure `MCP Client`

### Automatic configuration

- Open Unity project
- Open `Window/AI Game Developer (Unity-MCP)`
- Click `Configure` at your MCP client

![Unity_AI](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/ai-connector-window.gif)

> If MCP client is not in the list, use the raw JSON below in the window, to inject it into your MCP client. Read instructions for your MCP client how to do that.

### Manual configuration

If Automatic configuration doesn't work for you for any reason. Use JSON from `AI Game Developer (Unity-MCP)` window to configure any `MCP Client` on your own.

<details>
  <summary>Add Unity MCP to <b><code>Claude Code</code></b> for <b>Windows</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" client-transport=stdio
  ```

</details>

<details>
  <summary>Add Unity MCP to <b><code>Claude Code</code></b> for <b>MacOS Apple-Silicon</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary>Add Unity MCP to <b><code>Claude Code</code></b> for <b>MacOS Apple-Intel</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary>Add Unity MCP to <b><code>Claude Code</code></b> for <b>Linux x64</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary>Add Unity MCP to <b><code>Claude Code</code></b> for <b>Linux arm64</b></summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/linux-arm64/unity-mcp-server" client-transport=stdio
  ```

</details>

---

# Use AI

Talk with AI (LLM) in your `MCP Client`. Ask it to do anything you want. As better you describe your task / idea - as better it will do the job.

Some `MCP Clients` allow to chose different LLM models. Take an eye on it, some model may work much better.

  ```text
  Explain my scene hierarchy
  ```

  ```text
  Create 3 cubes in a circle with radius 2
  ```

  ```text
  Create metallic golden material and attach it to a sphere gameObject
  ```

> Make sure `Agent` mode is turned on in MCP client

## Features for LLM

It provides advanced tools for LLM to let it work faster, better, avoiding doing mistakes or correcting itself if any mistake. Everything for achieving the final goal that user needs.

- ✔️ Agent ready tools, find anything you need in 1-2 steps
- ✔️ Instant C# code compilation & execution using `Roslyn`, iterate faster
- ✔️ Assets access (read / write), C# scripts access (read / write)
- ✔️ Well described positive and negative feedback for proper understanding of an issue
- ✔️ Provide references to existed objects for the instant C# code using `Reflection`
- ✔️ Get full access to entire project data in a readable shape using `Reflection`
- ✔️ Populate & Modify any granular piece of data in the project using `Reflection`
- ✔️ Find any `method` in the entire codebase, including compiled DLL files using `Reflection`
- ✔️ Call any `method` in the entire codebase using `Reflection`
- ✔️ Provide any property into `method` call, even if it is a reference to existed object in memory using `Reflection` and advanced reflection convertors
- ✔️ Unity API instantly available for usage, even if Unity changes something you will get fresh API using `Reflection`.
- ✔️ Get access to human readable description of any `class`, `method`, `field`, `property` by reading it's `Description` attribute.

---

# Add custom `MCP Tool`

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** is designed to support custom `MCP Tool` development by project owner. MCP server takes data from Unity plugin and exposes it to a Client. So anyone in the MCP communication chain would receive the information about a new `MCP Tool`. Which LLM may decide to call at some point.

To add a custom `MCP Tool` you need:

1. To have a class with attribute `McpPluginToolType`.
2. To have a method in the class with attribute `McpPluginTool`.
3. *optional:* Add `Description` attribute to each method argument to let LLM to understand it.
4. *optional:* Use `string? optional = null` properties with `?` and default value to mark them as `optional` for LLM.

> Take a look that the line `MainThread.Instance.Run(() =>` it allows to run the code in Main thread which is needed to interact with Unity API. If you don't need it and running the tool in background thread is fine for the tool, don't use Main thread for efficiency purpose.

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

# Add custom runtime (in-game) `MCP Tool`

> ⚠️ Not yet supported. The work is in progress

---

# How it works

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** is a bridge between LLM and Unity. It exposes and explains to LLM Unity's tools. LLM understands the interface and utilizes the tools in the way a user asks.

Connect **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** to LLM client such as [Claude](https://claude.ai/download) or [Cursor](https://www.cursor.com/) using integrated `AI Connector` window. Custom clients are supported as well.

The project is designed to let developers to add custom tools soon. After that the next goal is to enable the same features in player's build. For not it works only in Unity Editor.

The system is extensible: you can define custom `MCP Tool`s directly in your Unity project codebase, exposing new capabilities to the AI or automation clients. This makes Unity-MCP a flexible foundation for building advanced workflows, rapid prototyping, or integrating AI-driven features into your development process.

---

# Advanced MCP server setup

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** server supports many different launch options and docker docker deployment. Both transport protocol are supported `http` and `stdio`. [Read more...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

---

# Contribution 💙💛

Contribution is highly appreciated. Brings your ideas and lets make the game development as simple as never before! Do you have an idea of a new `MCP Tool`, feature or did you spot a bug and know how to fix it.

1. 👉 [Fork the project](https://github.com/IvanMurzak/Unity-MCP/fork)
2. Clone the fork and open the `./Unity-MCP-Plugin` folder in Unity
3. Implement new things in the project, commit, push it to GitHub
4. Create Pull Request targeting original [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) repository, `main` branch.
