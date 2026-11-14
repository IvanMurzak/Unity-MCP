[English](installation.md) | [中文](installation.zh-CN.md) | [日本語](installation.ja.md) | [Español](installation.es.md) | [فارسی](installation.fa.md) | <b>हिन्दी</b> | [العربية](installation.ar.md)

# इंस्टॉलेशन गाइड

Unity प्रोजेक्ट में **AI Game Developer** प्लगइन इंस्टॉल करें, एक AI एजेंट कनेक्ट करें, और ज़रूरत हो तो उसे फिर से हटा दें। [README](README.hi.md) पर वापस जाएँ।

## आवश्यकताएँ

- **Windows, macOS या Linux** पर **Unity 2022.3** या उससे नया (तीनों टेस्ट किए गए हैं और समर्थित हैं)।
- एक प्रोजेक्ट पाथ **जिसमें स्पेस न हों**। पाथ में स्पेस होने पर प्लगइन स्टार्टअप पर एक एरर लॉग करता है।
  - ✅ `C:/MyProjects/MyProject`
  - ❌ `C:/My Projects/MyProject`
- CLI के लिए: [Node.js](https://nodejs.org/) 22.14 या उससे नया।

## 1. प्लगइन इंस्टॉल करें

### विकल्प 1: इंस्टॉलर

1. **[AI-Game-Dev-Installer.unitypackage](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)** डाउनलोड करें।
2. प्रोजेक्ट खुला रखते हुए उस पर डबल-क्लिक करें, या Unity में **Assets ▸ Import Package ▸ Custom Package** का उपयोग करें।

### विकल्प 2: OpenUPM

```bash
openupm add com.ivanmurzak.unity.mcp
```

### विकल्प 3: CLI

[`unity-mcp-cli`](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) आपके लिए `Packages/manifest.json` को एडिट करता है, इसलिए Editor का खुला होना ज़रूरी नहीं है:

```bash
npm install -g unity-mcp-cli

unity-mcp-cli install-unity                      # optional: install a Unity Editor
unity-mcp-cli create-project ./MyUnityProject    # optional: create a project
unity-mcp-cli install-plugin ./MyUnityProject    # add the plugin
unity-mcp-cli login                              # sign in to ai-game.dev (browser device flow)
unity-mcp-cli open ./MyUnityProject              # open Unity; it connects and generates skills
unity-mcp-cli wait-for-ready ./MyUnityProject    # wait until tools can be called
```

`npx unity-mcp-cli <command>` ग्लोबल इंस्टॉल के बिना भी काम करता है।

**साइन-इन और प्रोजेक्ट पिनिंग।** `login` क्रेडेंशियल को `~/.ai-game-dev/credentials.json` में सहेजता है; पेस्ट करने के लिए कोई टोकन नहीं है। डिफ़ॉल्ट रूप से `setup-mcp` द्वारा लिखा गया एजेंट कॉन्फ़िग प्रति-प्रोजेक्ट **पिन किए गए** एंडपॉइंट (`https://ai-game.dev/mcp/p/<pin>`) की ओर इशारा करता है; साझा `https://ai-game.dev/mcp` के लिए `--no-pin` पास करें। टीमें एनरोलमेंट कोड के ज़रिए एक्सेस साझा कर सकती हैं: `unity-mcp-cli install-plugin --enroll <code>`।

## 2. एक AI एजेंट चुनें

आपको एक एजेंट चाहिए; यही वह चैट विंडो है जिसमें आप काम करते हैं। उसे Skills या ऐसे MCP टूल्स का समर्थन करना चाहिए जो उसके चलते समय अपडेट होते हैं। Claude Code की सिफ़ारिश की जाती है। AI Game Developer विंडो और `unity-mcp-cli setup-mcp` आपके लिए Claude Code, Claude Desktop, Codex, Cursor, Gemini, GitHub Copilot (CLI, VS Code, Visual Studio), Antigravity, Cline, Open Code, Rider (Junie), Unity AI और अन्य के लिए कॉन्फ़िग लिखते हैं; `unity-mcp-cli setup-mcp --list` पूरी सूची प्रिंट करता है। कोई भी अन्य MCP क्लाइंट विंडो से मिले JSON कॉन्फ़िग के साथ काम करता है।

## 3. एजेंट कनेक्ट करें

### स्वचालित

1. Unity में प्रोजेक्ट खोलें।
2. **Window ▸ AI Game Dev** खोलें।
3. **Auto-generate** Skills (अनुशंसित) पर क्लिक करें, या अपने एजेंट के लिए MCP को **Configure** करें।

![AI Game Developer window](img/ai-connector-window.gif)

टर्मिनल से यही काम:

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject             # http (default)
unity-mcp-cli setup-mcp cursor ./MyUnityProject --transport stdio
```

### मैनुअल

यदि आपका एजेंट सूची में नहीं है, तो AI Game Developer विंडो में दिखाया गया JSON उसके MCP कॉन्फ़िगरेशन में कॉपी करें।

जो एजेंट अपनी कमांड लाइन से सर्वर जोड़ता है, उसके लिए वह `<command>` बनाएँ जो लोकल सर्वर (Custom मोड) शुरू करती है। `<unityProjectPath>` को प्रोजेक्ट के पूरे पाथ से और `<port>` को AI Game Developer विंडो के पोर्ट से बदलें:

| प्लेटफ़ॉर्म | `<command>` |
| --- | --- |
| Windows x64 | `"<unityProjectPath>/Library/mcp-server/win-x64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows x86 | `"<unityProjectPath>/Library/mcp-server/win-x86/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows arm64 | `"<unityProjectPath>/Library/mcp-server/win-arm64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| macOS Apple Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| macOS Intel | `"<unityProjectPath>/Library/mcp-server/osx-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux x64 | `"<unityProjectPath>/Library/mcp-server/linux-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux arm64 | `"<unityProjectPath>/Library/mcp-server/linux-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |

फिर इसे जोड़ें:

```bash
claude mcp add ai-game-developer <command>    # Claude Code
gemini mcp add ai-game-developer <command>    # Gemini CLI
```

GitHub Copilot CLI: `copilot` चलाएँ, फिर सर्वर नाम `ai-game-developer`, टाइप `local` और ऊपर दी गई `<command>` के साथ `/mcp add` चलाएँ।

> VS Code में Copilot के साथ, सुनिश्चित करें कि **Agent** मोड चालू है।

## टीम सेटिंग्स: अपडेट सूचनाएँ

जब OpenUPM पर नया वर्ज़न उपलब्ध होता है, तो प्लगइन Editor स्टार्टअप पर एक अपडेट पॉपअप दिखाता है। *"Do not show again"* एक प्रति-उपयोगकर्ता सेटिंग है। **पूरी टीम** के लिए पॉपअप बंद करने हेतु **Edit ▸ Project Settings ▸ AI Game Developer** खोलें और *"Disable update notifications for the entire team"* चालू करें। यह सेटिंग `ProjectSettings/AI-Game-Developer-UpdateSettings.asset` में सहेजी जाती है; उस फ़ाइल को कमिट करें। यही टॉगल **Tools ▸ AI Game Developer ▸ Updates ▸ Disable Update Notifications (Team)** में भी है।

## अनइंस्टॉल करें

1. **Window ▸ Package Manager** खोलें, **AI Game Developer — MCP** चुनें, और **Remove** पर क्लिक करें (या `unity-mcp-cli remove-plugin ./MyUnityProject` चलाएँ)।
2. Unity बंद करें, फिर `Assets/Plugins/NuGet` फ़ोल्डर और उसकी `Assets/Plugins/NuGet.meta` फ़ाइल हटा दें।

## आगे

- [डिफ़ॉल्ट MCP टूल्स](default-mcp-tools.hi.md)
- [कस्टम टूल्स और रनटाइम](custom-tools.hi.md)
- [MCP सर्वर और कनेक्शन](mcp-server.hi.md)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
