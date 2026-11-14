[English](custom-tools.md) | [中文](custom-tools.zh-CN.md) | <b>日本語</b> | [Español](custom-tools.es.md) | [فارسی](custom-tools.fa.md) | [हिन्दी](custom-tools.hi.md) | [العربية](custom-tools.ar.md)

# カスタムツールとランタイム

プロジェクトのコードに独自の **MCP ツール**と**プロンプト**を追加し、ビルドしたゲーム内で MCP を使う方法を説明します。[README](README.ja.md) に戻る。

プラグインは属性によってこれらを見つけて MCP サーバーに送信するため、接続しているすべてのエージェントから見えるようになります。Skills を読み込むエージェントは、次回のスキル生成時にそれらを取得します。

## コンセプト

| | 概要 | 用途 |
| --- | --- | --- |
| **Tool** | エージェントが呼び出せる C# メソッド。型と説明付きのパラメーターと結果を持つ | 繰り返し作業や複数ステップの Editor 作業、プロジェクト固有のワークフロー、検証、ゲームシステムの公開 |
| **Prompt** | User または Assistant メッセージとして会話に挿入されるテンプレート | プロジェクトの規約、アーキテクチャに関するメモ、手順書 |
| **Resource** | エージェントが参照できる読み取り専用データ | ツールを呼び出さなくてもエージェントが把握しておくべきプロジェクトの状態 |

## ツールを追加する

1. `[AiToolType]` 属性を持つクラス。
2. `[AiTool]` 属性 (id、省略可能な `Title`) を持つメソッド。
3. 任意: メソッドと各パラメーターに `[Description]` を付けると、モデルがいつどのように呼び出すべきかを理解できます。
4. 任意: デフォルト値を持つ null 許容パラメーター (`string? name = null`) は、モデルにとって省略可能になります。

Unity API はメインスレッドで実行する必要があります。その部分を `MainThread.Instance.Run(...)` で囲んでください。Unity API に触れない処理はバックグラウンドスレッドのままで構いません。

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

`[AiTool]` に `Visible = false` を指定すると、そのツールはデフォルトでエージェントのツール一覧から非表示になります (名前を指定して呼び出せば実行されます)。任意のツールの表示/非表示は、AI Game Developer ウィンドウ、`tool-set-visibility` ツール、または `unity-mcp-cli configure <path> --enable-tools <ids>` で切り替えられます。

## プロンプトを追加する

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

## ランタイム (ゲーム内)

プラグインはビルドしたゲームの中でも動作します。ランタイムにはデフォルトのツールはなく、独自のツールを登録します。Editor (Edit モードと Play モード) では、エディタープラグインは `UserSettings/AI-Game-Developer-Config.json` から設定されます。ビルドではコードから設定します:

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

### 例: チェスボット

チェスゲームのボット側をモデルにプレイさせます。ゲーム専用のツールには `[AiToolType(Target = ToolTarget.Runtime)]` (`using com.IvanMurzak.McpPlugin.Common;`) を付けます。これらはゲームだけが登録し、Editor が接続している間でも MCP サーバーはその呼び出しを実行中のゲームに送ります。

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

`UNITY_MCP_DISABLE` は初期化をオフにします。その場合 `Initialize` は例外をスローします。[CI での無効化](ci-disable.ja.md)を参照してください。

## その他

- 組み込みツール: [デフォルト MCP ツール](default-mcp-tools.ja.md)
- ツールパックの例としての拡張機能: [`extensions/`](https://github.com/IvanMurzak/Unity-MCP/tree/main/extensions)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
