<div dir="rtl">

[English](custom-tools.md) | [中文](custom-tools.zh-CN.md) | [日本語](custom-tools.ja.md) | [Español](custom-tools.es.md) | <b>فارسی</b> | [हिन्दी](custom-tools.hi.md) | [العربية](custom-tools.ar.md)

# ابزارهای سفارشی و runtime

**ابزارهای MCP** و **پرامپت‌های** خودتان را در کد پروژه اضافه کنید و از MCP درون یک بازیِ ساخته‌شده استفاده کنید. بازگشت به [README](README.fa.md).

پلاگین این‌ها را از روی اتریبیوت پیدا می‌کند و به سرور MCP می‌فرستد، بنابراین هر عامل متصلی آن‌ها را می‌بیند. عامل‌هایی که Skills را می‌خوانند، آن‌ها را در تولید بعدی Skills دریافت می‌کنند.

## مفاهیم

| | چیست | کاربرد |
| --- | --- | --- |
| **Tool** | یک متد C# که عامل می‌تواند آن را با پارامترهای نوع‌دار و توصیف‌شده فراخوانی کند و نتیجه‌ای برمی‌گرداند | کارهای تکراری یا چندمرحله‌ای در ویرایشگر، گردش‌کارهای مخصوص پروژه، اعتبارسنجی، در دسترس قرار دادن سیستم‌های بازی |
| **Prompt** | قالبی که به‌صورت پیام User یا Assistant در گفتگو تزریق می‌شود | قراردادهای پروژه، یادداشت‌های معماری، روال‌های گام‌به‌گام |
| **Resource** | داده فقط‌خواندنی که عامل می‌تواند بررسی کند | وضعیت پروژه که عامل باید بدون فراخوانی ابزار بداند |

## افزودن یک ابزار

1. یک کلاس با اتریبیوت `[AiToolType]`.
2. یک متد با اتریبیوت `[AiTool]` (شناسه و `Title` اختیاری).
3. اختیاری: `[Description]` روی متد و روی هر پارامتر، تا مدل بداند چه زمانی و چگونه آن را فراخوانی کند.
4. اختیاری: پارامترهای nullable با مقدار پیش‌فرض (`string? name = null`) برای مدل اختیاری هستند.

APIهای Unity باید روی main thread اجرا شوند: آن بخش را در `MainThread.Instance.Run(...)` قرار دهید. کاری که به APIهای Unity دست نمی‌زند می‌تواند روی thread پس‌زمینه بماند.

</div>

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

<div dir="rtl">

‏`Visible = false` روی `[AiTool]` به‌طور پیش‌فرض یک ابزار را از فهرست ابزارهای عامل پنهان می‌کند (اگر با نامش فراخوانی شود همچنان اجرا می‌شود). هر ابزاری را در پنجره AI Game Developer، با ابزار `tool-set-visibility`، یا با `unity-mcp-cli configure <path> --enable-tools <ids>` نمایش دهید یا پنهان کنید.

## افزودن یک پرامپت

</div>

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

<div dir="rtl">

## Runtime (درون بازی)

پلاگین درون یک بازیِ ساخته‌شده هم اجرا می‌شود. در runtime هیچ ابزار پیش‌فرضی وجود ندارد؛ ابزارهای خودتان را ثبت می‌کنید. در ویرایشگر (حالت‌های Edit و Play) پلاگین ویرایشگر از `UserSettings/AI-Game-Developer-Config.json` پیکربندی می‌شود؛ در یک build، آن را از کد پیکربندی کنید:

</div>

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

<div dir="rtl">

### مثال: یک ربات شطرنج

بگذارید مدل نقش ربات را در یک بازی شطرنج بازی کند. ابزارهای مخصوص بازی را با `[AiToolType(Target = ToolTarget.Runtime)]` (`using com.IvanMurzak.McpPlugin.Common;`) علامت بزنید: فقط بازی آن‌ها را ثبت می‌کند، و سرور MCP فراخوانی‌های آن‌ها را به بازیِ در حال اجرا می‌فرستد، حتی زمانی که ویرایشگر هم متصل است.

</div>

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

<div dir="rtl">

‏`UNITY_MCP_DISABLE` مقداردهی اولیه را خاموش می‌کند؛ در این صورت `Initialize` استثنا پرتاب می‌کند. [غیرفعال‌سازی در CI](ci-disable.fa.md) را ببینید.

## بیشتر

- ابزارهای داخلی: [ابزارهای پیش‌فرض MCP](default-mcp-tools.fa.md)
- افزونه‌ها به‌عنوان نمونه‌هایی از بسته‌های ابزار: [`extensions/`](https://github.com/IvanMurzak/Unity-MCP/tree/main/extensions)
- [ویکی](https://github.com/IvanMurzak/Unity-MCP/wiki)

</div>
