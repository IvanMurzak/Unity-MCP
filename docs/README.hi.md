<div align="center" width="100%">

<p align="center"><img src="img/readme/hero.svg" alt="Unity के लिए AI Game Developer: Skills, MCP टूल और एक CLI जो AI एजेंटों को Unity Editor और आपके चलते हुए गेम से जोड़ते हैं" width="100%"></p>

[![OpenUPM version](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) [![OpenUPM downloads per month](https://img.shields.io/badge/dynamic/json?label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) [![unity-mcp-cli on npm](https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D)](https://www.npmjs.com/package/unity-mcp-cli) [![Unity 2022.3 or newer](https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://unity.com/releases/editor/archive)\
[![Release workflow status](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/release.yml?label=release&logo=github&color=15803D&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://discord.gg/cfbdMZX99G) [![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) [![Stand with Ukraine](https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-badge.svg)](https://stand-with-ukraine.pp.ua)

  <p>
    <a href="https://claude.ai/download"><img src="img/mcp-clients/claude-64.png" alt="Claude" title="Claude" height="36"></a>&nbsp;&nbsp;
    <a href="https://openai.com/index/introducing-codex/"><img src="img/mcp-clients/codex-64.png" alt="Codex" title="Codex" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.cursor.com/"><img src="img/mcp-clients/cursor-64.png" alt="Cursor" title="Cursor" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/docs/copilot/overview"><img src="img/mcp-clients/github-copilot-64.png" alt="GitHub Copilot" title="GitHub Copilot" height="36"></a>&nbsp;&nbsp;
    <a href="https://gemini.google.com/"><img src="img/mcp-clients/gemini-64.png" alt="Gemini" title="Gemini" height="36"></a>&nbsp;&nbsp;
    <a href="https://antigravity.google/"><img src="img/mcp-clients/antigravity-64.png" alt="Antigravity" title="Antigravity" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/"><img src="img/mcp-clients/vs-code-64.png" alt="VS Code" title="VS Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.jetbrains.com/rider/"><img src="img/mcp-clients/rider-64.png" alt="Rider" title="Rider" height="36"></a>&nbsp;&nbsp;
    <a href="https://visualstudio.microsoft.com/"><img src="img/mcp-clients/visual-studio-64.png" alt="Visual Studio" title="Visual Studio" height="36"></a>&nbsp;&nbsp;
    <a href="https://opencode.ai/"><img src="img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.fa.md) | [العربية](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ar.md)</b>

</div>

**AI Game Developer** (Unity-MCP) आपके AI एजेंट को Unity से जोड़ता है। Claude Code, Codex, Cursor, GitHub Copilot, Gemini या कोई भी दूसरा [MCP](https://modelcontextprotocol.io/) क्लाइंट **Unity Editor** में सीन, प्रीफ़ैब, एसेट और C# कोड पढ़ और बदल सकता है, टेस्ट चला सकता है और स्क्रीनशॉट ले सकता है, और **आपके कंपाइल किए गए गेम के अंदर** आपके लिखे टूल कॉल कर सकता है। यह एक मुफ़्त Unity पैकेज (`com.ivanmurzak.unity.mcp`), एक कमांड-लाइन टूल (`unity-mcp-cli`) और दस वैकल्पिक एक्सटेंशन के रूप में आता है।

## यह कैसे काम करता है

<p align="center"><img src="img/readme/architecture.svg" alt="एक AI एजेंट या unity-mcp-cli एक सर्वर से MCP पर बात करता है (डिफ़ॉल्ट रूप से ai-game.dev Cloud, या एक लोकल gamedev-mcp-server), जो Unity Editor प्लगइन और आपके चलते हुए गेम के टूल कॉल करता है" width="100%"></p>

प्लगइन अपने टूल एक MCP सर्वर में रजिस्टर करता है; आपका एजेंट उन्हें उसी सर्वर के ज़रिए कॉल करता है। डिफ़ॉल्ट रूप से प्लगइन `https://ai-game.dev/mcp` पर **Cloud** सर्वर का उपयोग करता है। **Custom** मोड में यह लोकल [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) बाइनरी शुरू करता है, या आपके होस्ट किए सर्वर (बाइनरी या Docker) से जुड़ता है। MCP टूल की जगह एजेंट जनरेट किए गए **Skills** भी इस्तेमाल कर सकता है।

## विशेषताएँ

✅ **RQL** — एक ही क्वेरी भाषा से सीन, ऑब्जेक्ट, एसेट और सेटिंग्स पढ़ें और बदलें\
✅ **C# और टेस्ट** — Roslyn से C# कंपाइल और रन करें; EditMode और PlayMode टेस्ट चलाएँ\
✅ **Skills या MCP** — एक क्लिक में Skills बनाएँ, या MCP क्लाइंट के रूप में जोड़ें\
✅ **कोई भी एजेंट** — Claude Code, Codex, Cursor, Copilot, Gemini और दूसरे MCP क्लाइंट\
✅ **Runtime** — बने हुए गेम के अंदर वही टूल: NPC लॉजिक, लाइव डीबगिंग\
✅ **अपने टूल** — कोई भी C# मेथड एक `[AiTool]` एट्रिब्यूट से टूल बन जाता है\
✅ **CLI** — टर्मिनल या CI से इंस्टॉल, ओपन, क्वेरी, कंपाइल और टेस्ट करें\
✅ **एक्सटेंशन** — Animation से Timeline तक दस वैकल्पिक टूल पैक

## जल्दी शुरू करें

**Unity 2022.3** या नया चाहिए। प्लगइन इंस्टॉल करने का एक तरीका चुनें:

[![इंस्टॉलर डाउनलोड करें](img/button/button_download_hi.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

- **इंस्टॉलर**: ऊपर से `.unitypackage` डाउनलोड करें और अपने Unity प्रोजेक्ट में खोलें।
- **OpenUPM**: `openupm add com.ivanmurzak.unity.mcp`
- **CLI** (Node.js 22.14+), Editor की ज़रूरत नहीं:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin ./MyUnityProject   # प्लगइन को Packages/manifest.json में जोड़ें
unity-mcp-cli login                             # ब्राउज़र में ai-game.dev पर साइन इन करें
unity-mcp-cli open ./MyUnityProject             # Unity खोलें; यह जुड़ता है और skills बनाता है
```

<p align="center"><img src="img/readme/terminal.svg" alt="टर्मिनल सत्र: unity-mcp-cli इंस्टॉल करना, प्लगइन जोड़ना, साइन इन करना, Claude Code कॉन्फ़िगर करना और प्रोजेक्ट खोलना" width="100%"></p>

फिर अपना एजेंट जोड़ें: Unity में **Window ▸ AI Game Dev** खोलें और **Auto-generate** Skills (सुझाया गया) या **Configure** MCP पर क्लिक करें। या टर्मिनल से:

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject
```

![AI Game Developer विंडो](img/editor/ai-game-developer-windows.png)

अपने एजेंट से कहें *"त्रिज्या 2 वाले वृत्त पर 3 क्यूब बनाओ"* और इसे होते देखें। पूरे चरण, मैनुअल MCP कॉन्फ़िगरेशन और अनइंस्टॉल: **[इंस्टॉलेशन गाइड](installation.hi.md)**।

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

## उपयोग

**अपने एजेंट से बात करें।** जो नतीजा चाहिए उसका वर्णन करें; जितना सटीक, उतना बेहतर।

```text
मेरे सीन की हायरार्की समझाओ
धातु जैसा सुनहरा मटीरियल बनाओ और उसे एक नए स्फ़ीयर पर लगाओ
Rigidbody के बिना हर दुश्मन प्रीफ़ैब ढूँढो और उसमें एक जोड़ो
```

**टूल।** डिफ़ॉल्ट रूप से दिखते हैं: `rql-query`, `rql-mutate`, `rql-schema`, `script-execute`, `tests-run`, `screenshot-isolated`, `package-*` और `assetstore-*` टूल और `unity-tool-list`। और टूल इंस्टॉल हैं पर जब तक आप उन्हें न दिखाएँ, छिपे रहते हैं। पूरी सूची: **[डिफ़ॉल्ट MCP टूल](default-mcp-tools.hi.md)** या [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools)।

**टर्मिनल से** (कोई एडिटर न चल रहा हो तो एक हेडलेस एडिटर शुरू करता है):

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame
unity-mcp-cli test ./MyGame --mode PlayMode
```

**आपका अपना टूल।** `[AiToolType]` वाली एक क्लास और `[AiTool]` वाला एक मेथड काफ़ी है:

```csharp
[AiToolType]
public class Tool_Score
{
    [AiTool("score-reset", Title = "Reset the score")]
    [Description("Sets the player's score to zero.")]
    public string Reset()
    {
        return MainThread.Instance.Run(() =>
        {
            ScoreManager.Instance.Score = 0;
            return "[Success] Score reset.";
        });
    }
}
```

प्रॉम्प्ट, रनटाइम (गेम के अंदर) टूल और पूरा उदाहरण: **[कस्टम टूल और रनटाइम](custom-tools.hi.md)**।

## एक्सटेंशन

AI Game Developer विंडो के **Extensions** सेक्शन से, या `unity-mcp-cli install-extension <name>` से इंस्टॉल करें।

[Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) · [Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) · [InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) · [Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) · [ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) · [ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) · [Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) · [Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) · [Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) · [Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md)

## दस्तावेज़

| पेज | इसमें क्या है |
| --- | --- |
| [इंस्टॉलेशन गाइड](installation.hi.md) | इंस्टॉल के विकल्प, एजेंट जोड़ना, मैनुअल MCP कॉन्फ़िग, टीम सेटिंग्स, अनइंस्टॉल |
| [डिफ़ॉल्ट MCP टूल](default-mcp-tools.hi.md) | हर बिल्ट-इन टूल, RQL सोर्स, 1.0.0 में हटाए गए टूल और उनके RQL विकल्प |
| [कस्टम टूल और रनटाइम](custom-tools.hi.md) | टूल और प्रॉम्प्ट लिखें; अपने गेम के अंदर MCP इस्तेमाल करें |
| [MCP सर्वर और कनेक्शन](mcp-server.hi.md) | Cloud और Custom मोड, प्लगइन एनवायरनमेंट वेरिएबल, लोकल सर्वर |
| [Docker डिप्लॉयमेंट](DOCKER_DEPLOYMENT.hi.md) | MCP सर्वर को कंटेनर में चलाएँ |
| [CI में बंद करें](ci-disable.hi.md) | प्लगइन को सामान्य CI टेस्ट रन से बाहर रखें |
| [CLI संदर्भ](../cli/docs/README.hi.md) | `unity-mcp-cli` के सभी कमांड और विकल्प |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | ट्यूटोरियल, गाइड और FAQ |
| [डेवलपमेंट गाइड](dev/Development.hi.md) | योगदानकर्ताओं के लिए रिपॉज़िटरी संरचना, कोड स्टाइल, टेस्ट और CI |

संबंधित: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (साझा MCP सर्वर) · [ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

## योगदान

विचार, बग रिपोर्ट और पुल रिक्वेस्ट का स्वागत है। [डेवलपमेंट गाइड](dev/Development.hi.md) पढ़ें, रिपॉज़िटरी को fork करें, Unity में `./Unity-MCP-Plugin` खोलें और `main` पर पुल रिक्वेस्ट भेजें। अगर प्रोजेक्ट आपके काम आए, तो इसे स्टार दें।

## लाइसेंस

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
