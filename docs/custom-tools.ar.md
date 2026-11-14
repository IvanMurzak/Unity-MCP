<div dir="rtl">

[English](custom-tools.md) | [中文](custom-tools.zh-CN.md) | [日本語](custom-tools.ja.md) | [Español](custom-tools.es.md) | [فارسی](custom-tools.fa.md) | [हिन्दी](custom-tools.hi.md) | <b>العربية</b>

# الأدوات المخصصة ووقت التشغيل

أضف **أدوات MCP** و**الموجّهات (prompts)** الخاصة بك في شيفرة المشروع، واستخدم MCP داخل لعبة مبنية. العودة إلى [README](README.ar.md).

تعثر الإضافة على هذه العناصر عبر السمات (attributes) وترسلها إلى خادم MCP، فيراها كل وكيل متصل. أما الوكلاء الذين يقرؤون Skills فيحصلون عليها في عملية توليد المهارات التالية.

## المفاهيم

| | ما هو | استخدمه من أجل |
| --- | --- | --- |
| **Tool** | دالة C# يستطيع الوكيل استدعاءها، بمعاملات ذات أنواع وأوصاف وبنتيجة | أعمال المحرر المتكررة أو متعددة الخطوات، وسير العمل الخاص بالمشروع، والتحقق، وكشف أنظمة اللعبة |
| **Prompt** | قالب يُحقن في المحادثة كرسالة من User أو Assistant | أعراف المشروع، وملاحظات البنية المعمارية، والإجراءات خطوة بخطوة |
| **Resource** | بيانات للقراءة فقط يمكن للوكيل فحصها | حالة المشروع التي ينبغي أن يعرفها الوكيل دون استدعاء أداة |

## إضافة أداة

1. صنف (class) يحمل السمة `[AiToolType]`.
2. دالة تحمل السمة `[AiTool]` (المعرّف، و`Title` اختياريًا).
3. اختياري: `[Description]` على الدالة وعلى كل معامل، كي يعرف النموذج متى وكيف يستدعيها.
4. اختياري: المعاملات القابلة لقيمة null ذات القيم الافتراضية (`string? name = null`) تكون اختيارية للنموذج.

يجب أن تعمل واجهات Unity البرمجية على الخيط الرئيسي: ضع ذلك الجزء داخل `MainThread.Instance.Run(...)`. أما العمل الذي لا يلمس واجهات Unity فيمكن أن يبقى على خيط الخلفية.

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

‏`Visible = false` على `[AiTool]` يُخفي الأداة من قائمة أدوات الوكيل افتراضيًا (مع بقائها قابلة للتشغيل عند استدعائها بالاسم). أظهر أي أداة أو أخفها من نافذة AI Game Developer، أو بالأداة `tool-set-visibility`، أو بالأمر `unity-mcp-cli configure <path> --enable-tools <ids>`.

## إضافة موجّه (prompt)

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

## وقت التشغيل (داخل اللعبة)

تعمل الإضافة أيضًا داخل لعبة مبنية. لا توجد أدوات افتراضية في وقت التشغيل؛ بل تسجّل أدواتك بنفسك. في المحرر (وضعا Edit وPlay) تُضبط إضافة المحرر من `UserSettings/AI-Game-Developer-Config.json`؛ أما في النسخة المبنية فاضبطها من الشيفرة:

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

### مثال: روبوت شطرنج

دع النموذج يلعب دور الروبوت في لعبة شطرنج. علّم الأدوات الخاصة باللعبة فقط بـ `[AiToolType(Target = ToolTarget.Runtime)]` (`using com.IvanMurzak.McpPlugin.Common;`): فاللعبة وحدها تسجّلها، ويرسل خادم MCP استدعاءاتها إلى اللعبة قيد التشغيل حتى أثناء اتصال المحرر.

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

‏`UNITY_MCP_DISABLE` يوقف التهيئة؛ وعندها يرمي `Initialize` استثناءً. راجع [التعطيل في CI](ci-disable.ar.md).

## المزيد

- الأدوات المدمجة: [أدوات MCP الافتراضية](default-mcp-tools.ar.md)
- الإضافات كأمثلة على حزم الأدوات: [`extensions/`](https://github.com/IvanMurzak/Unity-MCP/tree/main/extensions)
- ‏[Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)

</div>
