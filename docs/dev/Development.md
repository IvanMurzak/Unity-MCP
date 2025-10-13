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

**Contribute**
Any contribution to the project is highly appreciated. Please follow this document to see out goals, vision and project structure. All of that should help to let you participate in the new technological era of game development

**Project goals**

- To deliver high quality AI game development solution for free to everyone
- To maintain and support cutting edge AI technologies in Unity Engine and beyond the engine itself
- To provide a highly customizable platform for other developer to let them customize AI features for their needs
- To

**Vision**
We believe that AI will be an important part of the game development. There are many companies trying to charge for using AI. They


**This document**
This document is explaining the internal project structure and design, code style, and main principals. Please use it if you are a contributor or if you like to understand the project in depth.

> **[💬 Join our Discord Server](https://discord.gg/cfbdMZX99G)** - Ask questions, showcase your work, and connect with other developers!

## Content

- [Project structure](#project-structure)
  - [Add custom `MCP Prompt`](#add-custom-mcp-prompt)
- [Runtime usage (in-game)](#runtime-usage-in-game)
  - [Sample: AI powered Chess game bot](#sample-ai-powered-chess-game-bot)

# Project structure

`AI Game Developer` contains multiple different project and work as collaboration of multiple system working together. Here is the main structure of the project:

```mermaid
graph LR

  A(MCP-Client)
  B(Unity-MCP-Server)
  C(Unity-MCP-Plugin)

  %% Relationships
  A <--> B
  B <--> C

```

`Unity-MCP-Server` and `Unity-MCP-Plugin` are written with C# and they both use SignalR to communicate.

```mermaid
graph TD

  A(Unity-MCP-Server)
  B(Unity-MCP-Plugin)
  C(Unity-MCP-Common)

  %% Relationships
  A --> C
  B --> C

```

- `Unity-MCP-Server`
- `Unity-MCP-Plugin`
- `Unity-MCP-Common`
- `Installer`


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

