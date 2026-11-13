<div align="center" width="100%">

<p align="center"><img src="img/readme/hero.svg" alt="AI Game Developer برای Unity: Skills، ابزارهای MCP و یک CLI که عامل‌های هوش مصنوعی را به ویرایشگر Unity و بازیِ در حال اجرا وصل می‌کنند" width="100%"></p>

<p align="center">
<a href="https://openupm.com/packages/com.ivanmurzak.unity.mcp/"><img src="https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F" alt="OpenUPM version"></a> <a href="https://openupm.com/packages/com.ivanmurzak.unity.mcp/"><img src="https://img.shields.io/badge/dynamic/json?label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F" alt="OpenUPM downloads per month"></a> <a href="https://www.npmjs.com/package/unity-mcp-cli"><img src="https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D" alt="unity-mcp-cli on npm"></a> <a href="https://unity.com/releases/editor/archive"><img src="https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Unity 2022.3 or newer"></a>
<br>
<a href="https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml"><img src="https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/release.yml?label=release&logo=githubactions&color=15803D&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Release workflow status"></a> <a href="https://discord.gg/cfbdMZX99G"><img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Discord"></a> <a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE"><img src="https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857" alt="License"></a> <a href="https://stand-with-ukraine.pp.ua"><img src="https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-square.svg" height="28" alt="Stand with Ukraine" title="Stand with Ukraine"></a>
</p>

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

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md)</b>

</div>

<div dir="rtl">

‏**AI Game Developer** (Unity-MCP) عامل هوش مصنوعی شما را به Unity وصل می‌کند. Claude Code، Codex، Cursor، GitHub Copilot، Gemini یا هر کلاینت [MCP](https://modelcontextprotocol.io/) دیگری می‌تواند در **ویرایشگر Unity** صحنه‌ها، پریفب‌ها، دارایی‌ها و کد C# را بخواند و تغییر دهد، تست اجرا کند و اسکرین‌شات بگیرد، و **درون بازیِ کامپایل‌شده** ابزارهایی را که خودتان نوشته‌اید فراخوانی کند. این پروژه به صورت یک پکیج رایگان Unity (`com.ivanmurzak.unity.mcp`)، یک ابزار خط فرمان (`unity-mcp-cli`) و ده افزونه اختیاری ارائه می‌شود.

## چگونه کار می‌کند

<p align="center"><img src="img/readme/architecture.svg" alt="یک عامل هوش مصنوعی یا unity-mcp-cli با پروتکل MCP با یک سرور صحبت می‌کند (به طور پیش‌فرض ai-game.dev Cloud یا یک gamedev-mcp-server محلی) و سرور ابزارهای پلاگین ویرایشگر Unity و بازیِ در حال اجرا را فراخوانی می‌کند" width="100%"></p>

پلاگین ابزارهایش را در یک سرور MCP ثبت می‌کند و عامل شما از طریق همان سرور آن‌ها را فراخوانی می‌کند. به طور پیش‌فرض پلاگین از سرور **Cloud** در `https://ai-game.dev/mcp` استفاده می‌کند. در حالت **Custom** فایل اجرایی محلی [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) را اجرا می‌کند، یا به سروری که خودتان میزبانی می‌کنید (فایل اجرایی یا Docker) وصل می‌شود. عامل به جای ابزارهای MCP می‌تواند از **Skills** تولیدشده هم استفاده کند.

## ویژگی‌ها

‏✅ ‏**RQL** — خواندن و تغییر صحنه‌ها، اشیا، دارایی‌ها و تنظیمات با یک زبان کوئری\
‏✅ ‏**C# و تست** — کامپایل و اجرای C# با Roslyn؛ اجرای تست‌های EditMode و PlayMode\
‏✅ ‏**Skills یا MCP** — ساخت Skills با یک کلیک، یا اتصال به عنوان کلاینت MCP\
‏✅ ‏**هر عاملی** — Claude Code، Codex، Cursor، Copilot، Gemini و دیگر کلاینت‌های MCP\
‏✅ ‏**Runtime** — همان ابزارها درون بازیِ ساخته‌شده: منطق NPC، دیباگ زنده\
‏✅ ‏**ابزارهای خودتان** — هر متد C# با اتریبیوت `[AiTool]` به ابزار تبدیل می‌شود\
‏✅ ‏**CLI** — نصب، باز کردن، کوئری، کامپایل و تست از ترمینال یا CI\
‏✅ ‏**افزونه‌ها** — ده بسته ابزار اختیاری، از Animation تا Timeline

## شروع سریع

به **Unity 2022.3** یا جدیدتر نیاز دارد. یکی از روش‌های نصب پلاگین را انتخاب کنید:

[![DOWNLOAD INSTALLER](img/button/button_download.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

- **نصب‌کننده**: فایل `.unitypackage` بالا را دانلود کرده و در پروژه Unity باز کنید.
- **OpenUPM**: `openupm add com.ivanmurzak.unity.mcp`
- **CLI** (Node.js 22.14+)، بدون نیاز به ویرایشگر:

</div>

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin ./MyUnityProject   # add the plugin to Packages/manifest.json
unity-mcp-cli login                             # sign in to ai-game.dev in the browser
unity-mcp-cli open ./MyUnityProject             # open Unity; it connects and generates skills
```

<div dir="rtl">

<p align="center"><img src="img/readme/terminal.svg" alt="نمونه ترمینال: نصب unity-mcp-cli، افزودن پلاگین، ورود، پیکربندی Claude Code و باز کردن پروژه" width="100%"></p>

سپس عامل را وصل کنید: در Unity منوی **Window ▸ AI Game Dev** را باز کنید و روی **Auto-generate** Skills (توصیه‌شده) یا **Configure** MCP کلیک کنید. یا از ترمینال:

</div>

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject
```

<div dir="rtl">

![پنجره AI Game Developer](img/editor/ai-game-developer-windows.png)

از عامل بخواهید *«۳ مکعب روی دایره‌ای به شعاع ۲ بساز»* و نتیجه را ببینید. مراحل کامل، پیکربندی دستی MCP و حذف پلاگین: **[راهنمای نصب](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md)** (به انگلیسی).

</div>

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

<div dir="rtl">

## استفاده

**با عامل صحبت کنید.** نتیجه‌ای را که می‌خواهید توصیف کنید؛ هر چه دقیق‌تر، بهتر.

</div>

```text
سلسله‌مراتب صحنه من را توضیح بده
یک متریال طلایی فلزی بساز و آن را به یک کره جدید بده
همه پریفب‌های دشمن بدون Rigidbody را پیدا کن و به هر کدام یکی اضافه کن
```

<div dir="rtl">

**ابزارها.** به طور پیش‌فرض نمایش داده می‌شوند: `rql-query`، `rql-mutate`، `rql-schema`، `script-execute`، `tests-run`، `screenshot-isolated`، ابزارهای `package-*` و `assetstore-*` و `unity-tool-list`. ابزارهای دیگر نصب شده‌اند ولی تا زمانی که نمایششان ندهید پنهان هستند. فهرست کامل: **[ابزارهای پیش‌فرض MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)** یا [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools).

**از ترمینال** (اگر ویرایشگری در حال اجرا نباشد، یک ویرایشگر بدون رابط گرافیکی اجرا می‌کند):

</div>

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame
unity-mcp-cli test ./MyGame --mode PlayMode
```

<div dir="rtl">

**ابزار خودتان.** یک کلاس با `[AiToolType]` و یک متد با `[AiTool]` کافی است:

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

پرامپت‌ها، ابزارهای Runtime (درون بازی) و یک نمونه کامل: **[ابزارهای سفارشی و Runtime](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md)**.

## افزونه‌ها

از بخش **Extensions** در پنجره AI Game Developer نصب کنید، یا با `unity-mcp-cli install-extension <name>`.

[Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) · [Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) · [InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) · [Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) · [ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) · [ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) · [Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) · [Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) · [Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) · [Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md)

## مستندات

| صفحه | محتوا |
| --- | --- |
| [راهنمای نصب](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md) | روش‌های نصب، اتصال عامل، پیکربندی دستی MCP، تنظیمات تیمی، حذف |
| [ابزارهای پیش‌فرض MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md) | همه ابزارهای داخلی، منابع RQL، ابزارهای حذف‌شده در 1.0.0 و معادل RQL آن‌ها |
| [ابزارهای سفارشی و Runtime](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md) | نوشتن ابزار و پرامپت؛ استفاده از MCP درون بازی |
| [سرور MCP و اتصال](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md) | حالت‌های Cloud و Custom، متغیرهای محیطی پلاگین، سرور محلی |
| [استقرار با Docker](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/DOCKER_DEPLOYMENT.md) | اجرای سرور MCP در کانتینر |
| [غیرفعال کردن در CI](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/ci-disable.md) | خارج نگه داشتن پلاگین از اجرای معمولی تست‌ها در CI |
| [مرجع CLI](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | همه دستورها و گزینه‌های `unity-mcp-cli` |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | آموزش‌ها، راهنماها و پرسش‌های متداول |
| [راهنمای توسعه](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md) | ساختار مخزن، سبک کد، تست‌ها و CI برای مشارکت‌کنندگان |

مرتبط: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (سرور MCP مشترک) · [ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

صفحات مستندات به زبان انگلیسی هستند.

## مشارکت

ایده‌ها، گزارش باگ و Pull Request ها خوش‌آمدند. [راهنمای توسعه](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md) را بخوانید، مخزن را fork کنید، `./Unity-MCP-Plugin` را در Unity باز کنید و یک Pull Request به `main` بفرستید. اگر پروژه به شما کمک کرد، به آن ستاره بدهید.

## مجوز

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)

</div>
