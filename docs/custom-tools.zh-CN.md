[English](custom-tools.md) | <b>中文</b> | [日本語](custom-tools.ja.md) | [Español](custom-tools.es.md) | [فارسی](custom-tools.fa.md) | [हिन्दी](custom-tools.hi.md) | [العربية](custom-tools.ar.md)

# 自定义工具与运行时

在项目代码中添加你自己的 **MCP 工具**和**提示词**，并在构建出的游戏中使用 MCP。返回 [README](README.zh-CN.md)。

插件通过特性（attribute）找到它们并发送给 MCP 服务器，因此每个已连接的智能体都能看到它们。读取 Skills 的智能体会在下一次生成 Skills 时获得它们。

## 概念

| | 是什么 | 用途 |
| --- | --- | --- |
| **Tool（工具）** | 智能体可以调用的 C# 方法，带有类型化且有描述的参数和返回结果 | 重复性或多步骤的编辑器工作、项目特定的工作流、校验、对外暴露游戏系统 |
| **Prompt（提示词）** | 以 User 或 Assistant 消息形式注入到对话中的模板 | 项目约定、架构说明、分步操作流程 |
| **Resource（资源）** | 智能体可以查看的只读数据 | 智能体无需调用工具就应知道的项目状态 |

## 添加工具

1. 一个带有 `[AiToolType]` 特性的类。
2. 一个带有 `[AiTool]` 特性的方法（id，可选的 `Title`）。
3. 可选：在方法和每个参数上添加 `[Description]`，让模型知道何时以及如何调用它。
4. 可选：带默认值的可空参数（`string? name = null`）对模型来说是可选参数。

Unity API 必须在主线程上运行：请将这部分代码包裹在 `MainThread.Instance.Run(...)` 中。不涉及 Unity API 的工作可以留在后台线程上执行。

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

在 `[AiTool]` 上设置 `Visible = false` 会默认将工具从智能体的工具列表中隐藏（按名称调用时仍会运行）。可以在 AI Game Developer 窗口中、使用 `tool-set-visibility` 工具，或使用 `unity-mcp-cli configure <path> --enable-tools <ids>` 显示或隐藏任意工具。

## 添加提示词

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

## 运行时（游戏内）

插件也可以在构建出的游戏中运行。运行时没有默认工具；你需要注册自己的工具。在编辑器中（Edit 和 Play 模式），编辑器插件通过 `UserSettings/AI-Game-Developer-Config.json` 进行配置；在构建版本中，请通过代码进行配置：

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

### 示例：国际象棋机器人

让模型在国际象棋游戏中扮演机器人一方。用 `[AiToolType(Target = ToolTarget.Runtime)]`（`using com.IvanMurzak.McpPlugin.Common;`）标记仅限游戏使用的工具：只有游戏会注册它们，即使编辑器已连接，MCP 服务器也会将对它们的调用发送到正在运行的游戏。

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

`UNITY_MCP_DISABLE` 会关闭初始化；此时 `Initialize` 会抛出异常。参见[在 CI 中禁用](ci-disable.zh-CN.md)。

## 更多

- 内置工具：[默认 MCP 工具](default-mcp-tools.zh-CN.md)
- 作为工具包示例的扩展：[`extensions/`](https://github.com/IvanMurzak/Unity-MCP/tree/main/extensions)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
