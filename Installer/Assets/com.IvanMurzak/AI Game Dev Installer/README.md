<div align="center">
  <h1>✨ AI Game Developer — <i>Unity MCP</i></h1>

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![MCP](https://badge.mcpx.dev?type=server 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)
[![Unity Asset Store](https://img.shields.io/badge/Asset%20Store-View-blue?logo=unity&labelColor=333A41 'Asset Store')](https://u3d.as/3wsw)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

AI helper which does wide range of tasks in Unity Editor and even in a running game compiled to any platform. It connects to AI using TCP connection, that is why it is so flexible.

💬 **Join our community:** [Discord Server](https://discord.gg/cfbdMZX99G) - Ask questions, showcase your work, and connect with other developers!

![AI work](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/level-building.gif 'Level building')

</div>

<details>
  <summary><b>Made with AI — samples (click to see)</b></summary>

  <table>
    <tr>
      <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/flying-orbs.gif" alt="Animation" title="Animation" /></td>
      <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/golden-sphere.gif" alt="Animation" title="Animation" /></td>
    </tr>
    <tr>
      <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/runner.gif" alt="Runner Game" title="Runner Game" /></td>
      <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/procedural-terrain.gif" alt="Procedural Terrain" title="Procedural Terrain" /></td>
    </tr>
    <tr>
      <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/create-material.gif" alt="Material creating" title="Material creating" /></td>
      <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/playing-maze.gif" alt="Maze Game" title="Maze Game" /></td>
    </tr>
  </table>

</details>

## Features for a human

- ✅ Chat with AI like with a human
- ✅ Local and Remote usage supported
- ✅ `stdio` and `streamableHttp` protocols supported
- ✅ Wide range of default [AI tools](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/ai-tools.md)
- ✅ Use `Description` attribute in C# code to provide detailed information for `class`, `field`, `property` or `method`.
- ✅ Customizable reflection convertors, inspired by `System.Text.Json` convertors
  - do you have something extremely custom in your project? Make custom reflection convertor to let LLM be able to read and write into that data
- ✅ Remote AI units setup using docker containers,
  - make a team of AI workers which work on your project simultaneously

## Features for LLM

- ✅ Agent ready tools, find anything you need in 1-2 steps
- ✅ Instant C# code compilation & execution using `Roslyn`, iterate faster
- ✅ Assets access (read / write), C# scripts access (read / write)
- ✅ Well described positive and negative feedback for proper understanding of an issue
- ✅ Provide references to existed objects for the instant C# code using `Reflection`
- ✅ Get full access to entire project data in a readable shape using `Reflection`
- ✅ Populate & Modify any granular piece of data in the project using `Reflection`
- ✅ Find any `method` in the entire codebase, including compiled DLL files using `Reflection`
- ✅ Call any `method` in the entire codebase using `Reflection`
- ✅ Provide any property into `method` call, even if it is a reference to existed object in memory using `Reflection` and advanced reflection convertors
- ✅ Unity API instantly available for usage, even if Unity changes something you will get fresh API using `Reflection`.
- ✅ Get access to human readable description of any `class`, `method`, `field`, `property` by reading it's `Description` attribute.

### Stability status

| Unity Version | Editmode                                                                                                                                                                               | Playmode                                                                                                                                                                               | Standalone                                                                                                                                                                               |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  |

## Requirements

> [!IMPORTANT]
> **Project path cannot contain spaces**
>
> - ✅ `C:/MyProjects/Project`
> - ❌ `C:/My Projects/Project`

### Install `MCP Client`

Choose `MCP Client` you prefer, don't need to install all of them. This is will be your main chat window to talk with LLM.

- [Claude Code](https://github.com/anthropics/claude-code)
- [Claude Desktop](https://claude.ai/download)
- [GitHub Copilot in VS Code](https://code.visualstudio.com/docs/copilot/overview)
- [Cursor](https://www.cursor.com/)
- [Windsurf](https://windsurf.com)
- Any other supported

> MCP protocol is quite universal, that is why you may any MCP client you prefer, it will work as smooth as anyone else. The only important thing, that the MCP client has to support dynamic tool update.

# Installation

## Step 1: Install `Unity Plugin`

- **[⬇️ Download Installer](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.17.2/AI-Game-Dev-Installer.unitypackage)**
- **📂 Import installer into Unity project**
  > - You may use double click on the file - Unity will open it
  > - OR: You may open Unity Editor first, then click on `Assets/Import Package/Custom Package`, then choose the file

<details>
  <summary><b>Alternative: Install <code>Unity Plugin</code> via OpenUPM</b></summary>

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open command line in Unity project folder
- Run the command

```bash
openupm add com.ivanmurzak.unity.mcp
```

</details>

## Step 2: Configure `MCP Client`

### Automatic configuration

- Open Unity project
- Open `Window/AI Connector (Unity-MCP)`
- Click `Configure` at your MCP client

![Unity_AI](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/ai-connector-window.gif)

> If MCP client is not in the list, use the raw JSON below in the window, to inject it into your MCP client. Read instructions for your MCP client how to do that.

### Manual configuration

If Automatic configuration doesn't work for you for any reason. Use JSON from `AI Connector (Unity-MCP)` window to configure any `MCP Client` on your own.

<details>
  <summary>Add Unity-MCP to <code>Claude Code</code> (Windows)</summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" client-transport=stdio
  ```

</details>

<details>
  <summary>Add Unity-MCP to <code>Claude Code</code> (MacOS Apple-Silicon)</summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary>Add Unity-MCP to <code>Claude Code</code> (MacOS Apple-Intel)</summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary>Add Unity-MCP to <code>Claude Code</code> (Linux x64)</summary>

  Replace `unityProjectPath` with your real project path

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary>Add Unity-MCP to <code>Claude Code</code> (Linux arm64)</summary>

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

---

# How it works

**[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)** is a bridge between LLM and Unity. It exposes and explains to LLM Unity's tools. LLM understands the interface and utilizes the tools in the way a user asks.

Connect **[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)** to LLM client such as [Claude](https://claude.ai/download) or [Cursor](https://www.cursor.com/) using integrated `AI Connector` window. Custom clients are supported as well.

The project is designed to let developers to add custom tools soon. After that the next goal is to enable the same features in player's build. For not it works only in Unity Editor.

The system is extensible: you can define custom `tool`s directly in your Unity project codebase, exposing new capabilities to the AI or automation clients. This makes Unity-MCP a flexible foundation for building advanced workflows, rapid prototyping, or integrating AI-driven features into your development process.

---

# Advanced MCP server setup

Unity-MCP server supports many different launch options and docker docker deployment. Both transport protocol are supported `streamableHttp` and `stdio`. [Read more...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

# Add custom `tool`

> ⚠️ It only works with MCP client that supports dynamic tool list update.

Unity-MCP is designed to support custom `tool` development by project owner. MCP server takes data from Unity plugin and exposes it to a Client. So anyone in the MCP communication chain would receive the information about a new `tool`. Which LLM may decide to call at some point.

To add a custom `tool` you need:

1. To have a class with attribute `McpPluginToolType`.
2. To have a method in the class with attribute `McpPluginTool`.
3. [optional] Add `Description` attribute to each method argument to let LLM to understand it.
4. [optional] Use `string? optional = null` properties with `?` and default value to mark them as `optional` for LLM.

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

# Add custom in-game `tool`

> ⚠️ Not yet supported. The work is in progress

---

# Contribution 💙💛

Contribution is highly appreciated. Brings your ideas and lets make the game development as simple as never before! Do you have an idea of a new `tool`, feature or did you spot a bug and know how to fix it.

1. 👉 [Fork the project](https://github.com/IvanMurzak/Unity-MCP/fork)
2. Clone the fork and open the `./Unity-MCP-Plugin` folder in Unity
3. Implement new things in the project, commit, push it to GitHub
4. Create Pull Request targeting original [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) repository, `main` branch.
