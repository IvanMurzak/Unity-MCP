# Custom tools and runtime

Add your own **MCP tools** and **prompts** in project code, and use MCP inside a built game. Back to the [README](https://github.com/IvanMurzak/Unity-MCP#readme).

The plugin finds these by attribute and sends them to the MCP server, so every connected agent sees them. Agents that read Skills get them in the next skill generation.

## Concepts

| | What it is | Use it for |
| --- | --- | --- |
| **Tool** | A C# method the agent can call, with typed, described parameters and a result | Repetitive or multi-step Editor work, project-specific workflows, validation, exposing game systems |
| **Prompt** | A template injected into the conversation as a User or Assistant message | Project conventions, architecture notes, step-by-step procedures |
| **Resource** | Read-only data the agent can inspect | Project state the agent should know without calling a tool |

## Add a tool

1. A class with the `[AiToolType]` attribute.
2. A method with the `[AiTool]` attribute (id, optional `Title`).
3. Optional: `[Description]` on the method and on each parameter, so the model knows when and how to call it.
4. Optional: nullable parameters with default values (`string? name = null`) are optional for the model.

Unity APIs must run on the main thread: wrap that part in `MainThread.Instance.Run(...)`. Work that does not touch Unity APIs can stay on the background thread.

```csharp
[AiToolType]
public class Tool_GameObject
{
    [AiTool("my-custom-task", Title = "Create a new GameObject")]
    [Description("Explain to the model what this does and when to call it.")]
    public string CustomTask
    (
        [Description("Explain to the model what this parameter is.")]
        string inputData
    )
    {
        // background thread work

        return MainThread.Instance.Run(() =>
        {
            // main thread work (Unity API)
            return "[Success] Operation completed.";
        });
    }
}
```

`Visible = false` on `[AiTool]` hides a tool from the agent's tool list by default (it still runs when called by name). Show or hide any tool in the AI Game Developer window, with the `tool-set-visibility` tool, or with `unity-mcp-cli configure <path> --enable-tools <ids>`.

## Add a prompt

```csharp
[AiPromptType]
public static class Prompt_ScriptingCode
{
    [AiPrompt(Name = "add-event-system", Role = Role.User)]
    [Description("Implement UnityEvent-based communication system between GameObjects.")]
    public static string AddEventSystem()
    {
        return "Create event system using UnityEvents, UnityActions, or custom event delegates for decoupled communication between game systems and components.";
    }
}
```

## Runtime (in-game)

The plugin also runs inside a built game. There are no default tools at runtime; you register your own. In the Editor (Edit and Play mode) the editor plugin is configured from `UserSettings/AI-Game-Developer-Config.json`; in a build, configure it from code:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void SetupMcp()
{
    var mcpPlugin = UnityMcpPluginRuntime.Initialize(builder =>
        {
            builder.WithConfig(c =>
            {
                c.Host = "http://localhost:8080";
                c.CredentialProvider = () => System.Threading.Tasks.Task.FromResult<string?>("my-token");
            });
            // register every tool in this assembly
            builder.WithToolsFromAssembly(Assembly.GetExecutingAssembly());
        })
        .Build();

    mcpPlugin.Connect();     // connect to the MCP server (async, retries)
    // mcpPlugin.Disconnect();
}
```

### Example: a chess bot

Let the model play the bot side of a chess game. Mark game-only tools with `[AiToolType(Target = ToolTarget.Runtime)]` (`using com.IvanMurzak.McpPlugin.Common;`): only the game registers them, and the MCP server sends their calls to the running game even while the Editor is connected.

```csharp
[AiToolType(Target = ToolTarget.Runtime)]
public static class ChessGameAI
{
    [AiTool("chess-do-turn", Title = "Do the turn")]
    [Description("Do the turn in the chess game. Returns true if the turn was accepted, false otherwise.")]
    public static Task<bool> DoTurn(int figureId, Vector2Int position)
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.DoTurn(figureId, position));
    }

    [AiTool("chess-get-board", Title = "Get the board")]
    [Description("Get the current state of the chess board.")]
    public static Task<BoardData> GetBoard()
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.GetBoardData());
    }
}
```

`UNITY_MCP_DISABLE` turns initialization off; `Initialize` then throws. See [Disable in CI](ci-disable.md).

## More

- Built-in tools: [Default MCP tools](default-mcp-tools.md)
- Extensions as examples of tool packs: [`extensions/`](https://github.com/IvanMurzak/Unity-MCP/tree/main/extensions)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
