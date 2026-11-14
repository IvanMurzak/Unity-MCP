<div align="center" width="100%">

<p align="center"><img src="img/readme/hero.svg" alt="AI Game Developer لـ Unity: مهارات Skills وأدوات MCP وواجهة سطر أوامر تربط وكلاء الذكاء الاصطناعي بمحرر Unity وبلعبتك أثناء تشغيلها" width="100%"></p>

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

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.fa.md) | [हिन्दी](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.hi.md)</b>

</div>

<div dir="rtl">

‏**AI Game Developer** (Unity-MCP) يربط وكيل الذكاء الاصطناعي الخاص بك بـ Unity. يستطيع Claude Code أو Codex أو Cursor أو GitHub Copilot أو Gemini أو أي عميل [MCP](https://modelcontextprotocol.io/) آخر قراءة المشاهد والـ prefabs والأصول وشيفرة C# وتعديلها، وتشغيل الاختبارات والتقاط لقطات الشاشة في **محرر Unity**، واستدعاء أدوات تكتبها أنت **داخل لعبتك المُصرَّفة**. يأتي في صورة حزمة Unity مجانية (`com.ivanmurzak.unity.mcp`) وأداة سطر أوامر (`unity-mcp-cli`) وعشر إضافات اختيارية.

## كيف يعمل

<p align="center"><img src="img/readme/architecture.svg" alt="يتواصل وكيل ذكاء اصطناعي أو unity-mcp-cli عبر MCP مع خادم (ai-game.dev Cloud افتراضيًا، أو gamedev-mcp-server محلي) يستدعي أدوات إضافة محرر Unity وأدوات لعبتك أثناء تشغيلها" width="100%"></p>

تسجّل الإضافة أدواتها في خادم MCP، ويستدعيها وكيلك عبر ذلك الخادم. تستخدم الإضافة افتراضيًا خادم **Cloud** على `https://ai-game.dev/mcp`. في وضع **Custom** تشغّل الملف التنفيذي المحلي لـ [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)، أو تتصل بخادم تستضيفه أنت (ملف تنفيذي أو Docker). ويمكن للوكيل أيضًا استخدام **Skills** مُولَّدة بدلًا من أدوات MCP.

## الميزات

‏✅ ‏**RQL** — اقرأ المشاهد والكائنات والأصول والإعدادات وعدّلها بلغة استعلام واحدة\
‏✅ ‏**C# والاختبارات** — صرّف C# وشغّله عبر Roslyn؛ شغّل اختبارات EditMode وPlayMode\
‏✅ ‏**Skills أو MCP** — ولّد Skills بنقرة واحدة، أو اتصل كعميل MCP\
‏✅ ‏**أي وكيل** — Claude Code وCodex وCursor وCopilot وGemini وعملاء MCP آخرون\
‏✅ ‏**Runtime** — الأدوات نفسها داخل لعبتك المبنية: منطق الشخصيات، تصحيح مباشر\
‏✅ ‏**أدواتك الخاصة** — أي دالة C# تصبح أداة بسمة واحدة `[AiTool]`\
‏✅ ‏**CLI** — ثبّت وافتح واستعلم وصرّف واختبر من الطرفية أو CI\
‏✅ ‏**الإضافات** — عشر حزم أدوات اختيارية، من Animation إلى Timeline

## البدء السريع

يتطلب **Unity 2022.3** أو أحدث. اختر طريقة لتثبيت الإضافة:

[![تنزيل المُثبِّت](img/button/button_download_ar.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

- **المُثبِّت**: نزّل ملف `.unitypackage` أعلاه وافتحه في مشروع Unity.
- **OpenUPM**: `openupm add com.ivanmurzak.unity.mcp`
- **CLI** (Node.js 22.14+)، دون الحاجة إلى المحرر:

</div>

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin ./MyUnityProject   # add the plugin to Packages/manifest.json
unity-mcp-cli login                             # sign in to ai-game.dev in the browser
unity-mcp-cli open ./MyUnityProject             # open Unity; it connects and generates skills
```

<div dir="rtl">

<p align="center"><img src="img/readme/terminal.svg" alt="جلسة طرفية: تثبيت unity-mcp-cli وإضافة الإضافة وتسجيل الدخول وإعداد Claude Code وفتح المشروع" width="100%"></p>

ثم صِل وكيلك: في Unity افتح **Window ▸ AI Game Dev** وانقر **Auto-generate** Skills (موصى به) أو **Configure** MCP. أو من الطرفية:

</div>

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject
```

<div dir="rtl">

![نافذة AI Game Developer](img/editor/ai-game-developer-windows.png)

اطلب من وكيلك *«أنشئ 3 مكعبات على دائرة نصف قطرها 2»* وشاهد ذلك يحدث. الخطوات الكاملة، وإعداد MCP يدويًا، وإلغاء التثبيت: **[دليل التثبيت](installation.ar.md)**.

</div>

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

<div dir="rtl">

## الاستخدام

**تحدّث مع وكيلك.** صِف النتيجة التي تريدها؛ كلما كنت أدق كان أفضل.

</div>

```text
اشرح تسلسل مشهدي الهرمي
أنشئ مادة ذهبية معدنية وطبّقها على كرة جديدة
ابحث عن كل prefab عدو بلا Rigidbody وأضف واحدًا إليه
```

<div dir="rtl">

**الأدوات.** الظاهرة افتراضيًا: `rql-query` و`rql-mutate` و`rql-schema` و`script-execute` و`tests-run` و`screenshot-isolated` وأدوات `package-*` و`assetstore-*` و`unity-tool-list`. هناك أدوات أخرى مثبتة لكنها مخفية حتى تُظهرها. القائمة الكاملة: **[أدوات MCP الافتراضية](default-mcp-tools.ar.md)** أو [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools).

**من الطرفية** (يشغّل محررًا بلا واجهة إن لم يكن هناك محرر يعمل):

</div>

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame
unity-mcp-cli test ./MyGame --mode PlayMode
```

<div dir="rtl">

**أداتك الخاصة.** يكفي صنف عليه `[AiToolType]` ودالة عليها `[AiTool]`:

</div>

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

<div dir="rtl">

الموجّهات وأدوات وقت التشغيل (داخل اللعبة) ومثال كامل: **[الأدوات المخصصة ووقت التشغيل](custom-tools.ar.md)**.

## الإضافات

الإضافات حزم UPM اختيارية تضيف أدوات لميزات محددة في Unity.

</div>

```bash
unity-mcp-cli install-extension com.ivanmurzak.unity.mcp.cinemachine ./MyUnityProject
```

<div dir="rtl">

| الإضافة | ما تضيفه | المتطلبات |
| --- | --- | --- |
| [Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) | AnimationClip وAnimatorController |  |
| [Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) | الكاميرات الافتراضية والأهداف ومسار الكاميرا | `com.unity.cinemachine` 3.1.6+ |
| [InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) | أصول إجراءات الإدخال والخرائط والإجراءات والربط | `com.unity.inputsystem` 1.8.2+ |
| [Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) | أسطح NavMesh والخَبز والوكلاء والروابط | `com.unity.ai.navigation` 1.1.7+ |
| [ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) | الانبعاث والشكل والمنحنيات والتدرجات في ParticleSystem |  |
| [ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) | شبكات قابلة للتحرير: أشكال، بثق، شطف، مواد | `com.unity.probuilder` 5.2.4+ |
| [Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) | حاويات المنحنيات والعُقد والمماسات | `com.unity.splines` 2.8.4+ |
| [Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) | التضاريس: خرائط الارتفاع والنحت وأخذ العينات |  |
| [Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) | Grid وTilemap: رسم البلاطات وتعبئتها وقراءتها | `com.unity.2d.tilemap.extras` 3.1.3+ |
| [Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md) | أصول Timeline والمسارات والمقاطع والعلامات | `com.unity.timeline` 1.8.12+ |

تُثبَّت حزم Unity المطلوبة مع الإضافة. يمكنك أيضًا تثبيت الإضافات من قسم **Extensions** في نافذة AI Game Developer.

## التوثيق

| الصفحة | المحتوى |
| --- | --- |
| [دليل التثبيت](installation.ar.md) | خيارات التثبيت، ربط وكيل، إعداد MCP يدويًا، إعدادات الفريق، إلغاء التثبيت |
| [أدوات MCP الافتراضية](default-mcp-tools.ar.md) | كل أداة مدمجة، ومصادر RQL، والأدوات المحذوفة في 1.0.0 وبدائلها في RQL |
| [الأدوات المخصصة ووقت التشغيل](custom-tools.ar.md) | اكتب أدوات وموجّهات؛ استخدم MCP داخل لعبتك |
| [خادم MCP والاتصال](mcp-server.ar.md) | وضعا Cloud وCustom، متغيرات بيئة الإضافة، الخادم المحلي |
| [النشر باستخدام Docker](DOCKER_DEPLOYMENT.ar.md) | شغّل خادم MCP داخل حاوية |
| [التعطيل في CI](ci-disable.ar.md) | أبقِ الإضافة خارج تشغيلات اختبارات CI العادية |
| [مرجع CLI](../cli/docs/README.ar.md) | كل أوامر `unity-mcp-cli` وخياراتها |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | دروس وأدلة وأسئلة شائعة |
| [دليل التطوير](dev/Development.ar.md) | بنية المستودع، أسلوب الكود، الاختبارات وCI للمساهمين |

ذو صلة: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (خادم MCP المشترك) · [ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

## المساهمة

نرحب بالأفكار وتقارير الأخطاء وطلبات الدمج. اقرأ [دليل التطوير](dev/Development.ar.md)، وأنشئ fork للمستودع، وافتح `./Unity-MCP-Plugin` في Unity، وأرسل طلب دمج إلى `main`. إن أفادك المشروع فامنحه نجمة.

## الترخيص

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)

</div>
