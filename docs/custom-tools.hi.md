[English](custom-tools.md) | [中文](custom-tools.zh-CN.md) | [日本語](custom-tools.ja.md) | [Español](custom-tools.es.md) | [فارسی](custom-tools.fa.md) | <b>हिन्दी</b> | [العربية](custom-tools.ar.md)

# कस्टम टूल्स और रनटाइम

प्रोजेक्ट कोड में अपने स्वयं के **MCP टूल्स** और **प्रॉम्प्ट्स** जोड़ें, और बिल्ड किए गए गेम के अंदर MCP का उपयोग करें। [README](README.hi.md) पर वापस जाएँ।

प्लगइन इन्हें एट्रिब्यूट के ज़रिए ढूँढता है और MCP सर्वर को भेजता है, इसलिए हर कनेक्टेड एजेंट इन्हें देखता है। जो एजेंट Skills पढ़ते हैं, उन्हें ये अगली स्किल जनरेशन में मिलते हैं।

## अवधारणाएँ

| | यह क्या है | इसका उपयोग किसके लिए करें |
| --- | --- | --- |
| **Tool** | एक C# मेथड जिसे एजेंट कॉल कर सकता है, जिसमें टाइप किए गए और वर्णित पैरामीटर तथा एक परिणाम होता है | दोहराव वाला या कई चरणों वाला Editor कार्य, प्रोजेक्ट-विशिष्ट वर्कफ़्लो, वैलिडेशन, गेम सिस्टम्स को उजागर करना |
| **Prompt** | एक टेम्पलेट जो बातचीत में User या Assistant संदेश के रूप में डाला जाता है | प्रोजेक्ट की परिपाटियाँ, आर्किटेक्चर नोट्स, चरण-दर-चरण प्रक्रियाएँ |
| **Resource** | केवल-पढ़ने योग्य डेटा जिसे एजेंट जाँच सकता है | प्रोजेक्ट की वह स्थिति जो एजेंट को कोई टूल कॉल किए बिना पता होनी चाहिए |

## एक टूल जोड़ें

1. `[AiToolType]` एट्रिब्यूट वाली एक क्लास।
2. `[AiTool]` एट्रिब्यूट (id, वैकल्पिक `Title`) वाला एक मेथड।
3. वैकल्पिक: मेथड और हर पैरामीटर पर `[Description]`, ताकि मॉडल जाने कि इसे कब और कैसे कॉल करना है।
4. वैकल्पिक: डिफ़ॉल्ट मान वाले nullable पैरामीटर (`string? name = null`) मॉडल के लिए वैकल्पिक होते हैं।

Unity API को मुख्य थ्रेड पर चलना चाहिए: उस हिस्से को `MainThread.Instance.Run(...)` में लपेटें। जो कार्य Unity API को नहीं छूता, वह बैकग्राउंड थ्रेड पर रह सकता है।

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

`[AiTool]` पर `Visible = false` डिफ़ॉल्ट रूप से किसी टूल को एजेंट की टूल सूची से छिपा देता है (नाम से कॉल किए जाने पर यह फिर भी चलता है)। किसी भी टूल को AI Game Developer विंडो में, `tool-set-visibility` टूल से, या `unity-mcp-cli configure <path> --enable-tools <ids>` से दिखाएँ या छिपाएँ।

## एक प्रॉम्प्ट जोड़ें

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

## रनटाइम (गेम के अंदर)

प्लगइन बिल्ड किए गए गेम के अंदर भी चलता है। रनटाइम पर कोई डिफ़ॉल्ट टूल्स नहीं होते; आप अपने स्वयं के टूल्स रजिस्टर करते हैं। Editor में (Edit और Play मोड) एडिटर प्लगइन `UserSettings/AI-Game-Developer-Config.json` से कॉन्फ़िगर होता है; बिल्ड में, इसे कोड से कॉन्फ़िगर करें:

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

### उदाहरण: एक शतरंज बॉट

मॉडल को शतरंज के खेल में बॉट की ओर से खेलने दें। केवल-गेम वाले टूल्स को `[AiToolType(Target = ToolTarget.Runtime)]` (`using com.IvanMurzak.McpPlugin.Common;`) से चिह्नित करें: केवल गेम उन्हें रजिस्टर करता है, और MCP सर्वर उनके कॉल्स को चल रहे गेम तक भेजता है, भले ही Editor कनेक्टेड हो।

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

`UNITY_MCP_DISABLE` इनिशियलाइज़ेशन बंद कर देता है; तब `Initialize` एक्सेप्शन फेंकता है। देखें [CI में अक्षम करें](ci-disable.hi.md)।

## और

- बिल्ट-इन टूल्स: [डिफ़ॉल्ट MCP टूल्स](default-mcp-tools.hi.md)
- टूल पैक के उदाहरण के रूप में एक्सटेंशन: [`extensions/`](https://github.com/IvanMurzak/Unity-MCP/tree/main/extensions)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
