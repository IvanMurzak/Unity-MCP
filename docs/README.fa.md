<div align="center" width="100%">

<p align="center"><img src="img/readme/hero.svg" alt="AI Game Developer برای Unity: Skills، ابزارهای MCP و یک CLI که عامل‌های هوش مصنوعی را به ویرایشگر Unity و بازیِ در حال اجرا وصل می‌کنند" width="100%"></p>

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

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md) | [हिन्दी](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.hi.md) | [العربية](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ar.md)</b>

</div>

<div dir="rtl">

‏**AI Game Developer** (Unity-MCP) عامل هوش مصنوعی شما را به Unity وصل می‌کند. Claude Code، Codex، Cursor، GitHub Copilot، Gemini یا هر کلاینت [MCP](https://modelcontextprotocol.io/) دیگری می‌تواند در **ویرایشگر Unity** صحنه‌ها، پریفب‌ها، دارایی‌ها و کد C# را بخواند و تغییر دهد، تست اجرا کند و اسکرین‌شات بگیرد، و **درون بازیِ کامپایل‌شده** ابزارهایی را که خودتان نوشته‌اید فراخوانی کند. این پروژه به صورت یک پکیج رایگان Unity (`com.ivanmurzak.unity.mcp`)، یک ابزار خط فرمان (`unity-mcp-cli`) و افزونه‌های اختیاری ارائه می‌شود.

**وضعیت:** [آخرین نسخه](https://github.com/IvanMurzak/Unity-MCP/releases/latest) و [فهرست تغییرات](https://github.com/IvanMurzak/Unity-MCP/blob/main/Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/CHANGELOG.md). پلاگین و `unity-mcp-cli` نسخه مشترکی دارند. روی **Windows، macOS و Linux** آزمایش شده و پشتیبانی می‌شود. به Unity **2022.3** یا جدیدتر نیاز دارد؛ CI تست‌های EditMode، PlayMode و بیلد مستقل (standalone) را روی Unity 2022.3، 6000.0، 6000.3 و 6000.6 اجرا می‌کند.

## چگونه کار می‌کند

<p align="center"><img src="img/readme/architecture.svg" alt="یک عامل هوش مصنوعی یا unity-mcp-cli با پروتکل MCP با یک سرور صحبت می‌کند (به طور پیش‌فرض ai-game.dev Cloud یا یک gamedev-mcp-server محلی) و سرور ابزارهای پلاگین ویرایشگر Unity و بازیِ در حال اجرای شما را فراخوانی می‌کند" width="100%"></p>

پلاگین ابزارهایش را در یک سرور MCP ثبت می‌کند و عامل شما از طریق همان سرور آن‌ها را فراخوانی می‌کند. عامل به جای ابزارهای MCP می‌تواند از **Skills** تولیدشده هم استفاده کند. شما انتخاب می‌کنید سرور کجا اجرا شود:

| | **محلی** (حالت Custom، بدون حساب کاربری) | **Cloud** (تنظیم پیش‌فرض) |
| --- | --- | --- |
| سرور | [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) که پلاگین روی `127.0.0.1` اجرا می‌کند، یا سروری که خودتان میزبانی می‌کنید | `https://ai-game.dev/mcp` |
| حساب کاربری | لازم نیست | ورود به ai-game.dev |
| داده‌های پروژه شما | روی دستگاه شما می‌ماند | فراخوانی‌های ابزار از یک رله بدون حالت (stateless) عبور می‌کنند که محتوای آن‌ها را بررسی یا ذخیره نمی‌کند؛ رله فراداده رویدادهای MCP را ثبت می‌کند که در یک بازه چرخشی ۹۰ روزه استفاده می‌شود ([سیاست حریم خصوصی](https://ai-game.dev/legal/privacy)) |
| هزینه | رایگان | رایگان |

**چرا Cloud پیش‌فرض است:** به هیچ پورت بازی نیاز ندارد، از تداخل پورت با برنامه‌های دیگر جلوگیری می‌کند و رایگان است. حالت محلی گزینه‌ای هم‌ارز برای کار آفلاین یا در محیط‌های ایزوله (air-gapped) است. جزئیات: [امنیت و حریم خصوصی](#امنیت-و-حریم-خصوصی).

## ویژگی‌ها

‏✅ ‏**پوشش ویرایشگر** — عامل تقریباً هر کاری را که شما در ویرایشگر Unity انجام می‌دهید می‌تواند انجام دهد\
‏✅ ‏**فشرده و کارآمد** — ابزارهایی ساخته‌شده برای عامل‌ها: فراخوانی‌های کمتر و توکن‌های کمتر، پس عامل سریع‌تر و ارزان‌تر کار می‌کند ([اندازه‌گیری‌شده در ویرایشگر Unity](benchmark.fa.md))\
‏✅ ‏**RQL** — خواندن و تغییر صحنه‌ها، اشیا، دارایی‌ها و تنظیمات با یک زبان کوئری\
‏✅ ‏**C# و تست** — کامپایل و اجرای C# با Roslyn؛ اجرای تست‌های EditMode و PlayMode\
‏✅ ‏**Skills یا MCP** — ساخت Skills با یک کلیک، یا اتصال به عنوان کلاینت MCP\
‏✅ ‏**هر عاملی** — Claude Code، Codex، Cursor، Copilot، Gemini و دیگر کلاینت‌های MCP\
‏✅ ‏**Runtime** — همان ابزارها درون بازیِ ساخته‌شده: منطق NPC، دیباگ زنده\
‏✅ ‏**ابزارهای خودتان** — هر متد C# با اتریبیوت `[AiTool]` به ابزار تبدیل می‌شود\
‏✅ ‏**CLI** — نصب، باز کردن، کوئری، کامپایل و تست از ترمینال یا CI\
‏✅ ‏**افزونه‌ها** — بسته‌های ابزار اختیاری، از Animation تا Timeline

## شروع سریع

به **Unity 2022.3** یا جدیدتر نیاز دارد.

**۱. پلاگین را نصب کنید** (توصیه‌شده: نصب‌کننده، بدون نیاز به نصب چیز دیگری):

[![دانلود نصب‌کننده](img/button/button_download_fa.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

فایل `.unitypackage` دانلودشده را در حالی که پروژه‌تان باز است باز کنید (روی آن دوبار کلیک کنید، یا از **Assets ▸ Import Package ▸ Custom Package** استفاده کنید).

</div>

<details>
<summary>روش‌های دیگر نصب: OpenUPM / Package Manager، یا CLI برای اسکریپت‌نویسی و CI</summary>

<div dir="rtl">

- **OpenUPM** — `openupm add com.ivanmurzak.unity.mcp`، یا بدون هیچ CLI: در **Edit ▸ Project Settings ▸ Package Manager** یک scoped registry اضافه کنید (URL `https://package.openupm.com`، scopeهای `com.ivanmurzak` و `extensions.unity`)، سپس **Window ▸ Package Manager ▸ + ▸ Add package by name** → `com.ivanmurzak.unity.mcp`.
- **unity-mcp-cli** (Node.js 22.14+) — برای اسکریپت‌ها و CI، بدون نیاز به ویرایشگر:

</div>

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin ./MyUnityProject   # add the plugin to Packages/manifest.json
unity-mcp-cli open ./MyUnityProject             # open the project in Unity
```

<p align="center"><img src="img/readme/terminal.svg" alt="نمونه ترمینال: نصب unity-mcp-cli، افزودن پلاگین، ورود، پیکربندی Claude Code و باز کردن پروژه" width="100%"></p>

</details>

<div dir="rtl">

**۲. عامل خود را وصل کنید** — یکی را انتخاب کنید؛ هر دو به طور کامل پشتیبانی می‌شوند:

| | **محلی** (بدون حساب کاربری) | **Cloud** (تنظیم پیش‌فرض) |
| --- | --- | --- |
| در **Window ▸ AI Game Dev** | اتصال **Custom** | اتصال **Cloud**، سپس **Authorize** (ورود در مرورگر) |
| پیکربندی عامل | **Configure** برای عامل شما | **Configure** برای عامل شما |
| همین کار از CLI | `unity-mcp-cli setup-mcp claude-code ./MyUnityProject --transport stdio` | `unity-mcp-cli login`، سپس `unity-mcp-cli setup-mcp claude-code ./MyUnityProject` |

**Auto-generate** Skills در همین پنجره جایگزینی برای MCP است. عامل‌های دیگر: [پیکربندی کلاینت](#پیکربندی-کلاینت).

**۳. فقط برای حالت محلی: تعیین کنید چه کسی می‌تواند ابزارهای شما را فراخوانی کند.** سرور محلی با احراز هویت `none` شروع می‌شود: فقط روی `127.0.0.1` گوش می‌دهد و originهای ناآشنای مرورگر را رد می‌کند، اما هر برنامه‌ای روی دستگاه شما می‌تواند به آن دسترسی داشته باشد. اگر افراد دیگر یا برنامه‌های غیرقابل اعتماد از این دستگاه استفاده می‌کنند، در یک قدم به `token` تغییر دهید: **Window ▸ AI Game Dev** → **Authorization Token** → `token`. سرور با یک راز تولیدشده دوباره راه‌اندازی می‌شود؛ دوباره **Configure** را بزنید تا عامل شما آن را بفرستد.

![پنجره AI Game Developer](img/editor/ai-game-developer-windows.png)

از عامل بخواهید *«۳ مکعب روی دایره‌ای به شعاع ۲ بساز»* و نتیجه را ببینید. مراحل کامل، پیکربندی دستی MCP و حذف پلاگین: **[راهنمای نصب](installation.fa.md)**.

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

**عامل شما چه کارهایی می‌تواند انجام دهد** (ابزارهایی که به طور پیش‌فرض نمایش داده می‌شوند):

| قابلیت | ابزار | نمونه پرامپت |
| --- | --- | --- |
| صحنه‌ها و GameObjectها | `rql-query`، `rql-mutate` | *«همه GameObjectهای غیرفعال در صحنه باز را فهرست کن»* |
| دارایی‌ها و پریفب‌ها | `rql-query`، `rql-mutate` | *«یک متریال قرمز بساز و آن را به همه مکعب‌ها اختصاص بده»* |
| کنسول و وضعیت ویرایشگر | `rql-query` | *«چه خطاهایی در کنسول هست؟»* |
| C# در ویرایشگر | `script-execute` | *«کد C#ی اجرا کن که ۱۰۰ درخت را در نقاط تصادفی روی زمین قرار دهد»* |
| تست‌ها | `tests-run` | *«تست‌های EditMode را اجرا کن و هر چه شکست خورد را درست کن»* |
| اسکرین‌شات‌ها | `screenshot-isolated` | *«مدل Player را از روبه‌رو به من نشان بده»* |
| پکیج‌ها و Asset Store | `package-*`، `assetstore-*` | *«Cinemachine را نصب کن»* |
| فهرست ابزارها | `unity-tool-list` | *«اینجا از چه ابزارهایی می‌توانی استفاده کنی؟»* |

لازم نیست RQL را یاد بگیرید: عامل آن را می‌نویسد و برای هر منبع، نوع و فراخوانی که لازم دارد از `rql-schema` می‌پرسد. اندازه‌گیری‌شده در ویرایشگر Unity: همان کارها با حدود ۵ برابر توکن پرامپت کمتر و فراخوانی‌های ابزار کمتر نسبت به ابزارهای تک‌کنشی (یک ابزار برای هر کنش) انجام شدند، با همان نرخ موفقیت ([بنچمارک](benchmark.fa.md)). روی یک دنیای بازی آزمایشی (sandbox)، با RQL عامل برای هر کار حدود ۲ تا ۳ فراخوانی ابزار و ۷ تا ۸ هزار توکن لازم دارد و ۱۰۰٪ موفق است، در حالی که با ابزارهای ساده‌ی reflection به ۳۰ تا ۳۷ فراخوانی و ۱۲۵ تا ۱۷۰ هزار توکن نیاز دارد و ۶۷ تا ۹۱٪ موفق است ([نتایج](https://github.com/IvanMurzak/RQL/blob/main/RESULTS.md#33-rql-vs-conventional-reflection-tools)). ابزارهای دیگر نصب شده‌اند ولی تا زمانی که نمایششان ندهید پنهان هستند. فهرست کامل: **[ابزارهای پیش‌فرض MCP](default-mcp-tools.fa.md)** یا [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools).

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

پرامپت‌ها، ابزارهای Runtime (درون بازی) و یک نمونه کامل: **[ابزارهای سفارشی و Runtime](custom-tools.fa.md)**.

## پیکربندی کلاینت

دکمه **Configure** در پنجره AI Game Developer، یا `unity-mcp-cli setup-mcp <agent> ./MyUnityProject`، این فایل‌ها را برای شما می‌نویسد (به طور پیش‌فرض Cloud، و محلی با `--transport stdio`). برای پیکربندی دستی با سرور محلی: پلاگین را روی **Custom** بگذارید، `<port>` را از پنجره بردارید و فایل اجرایی مخصوص پلتفرم خود را از `<project>/Library/mcp-server/<rid>/` استفاده کنید (`win-x64/gamedev-mcp-server.exe`، `osx-arm64/gamedev-mcp-server`، `linux-x64/gamedev-mcp-server`، …).

</div>

<details>
<summary><b>Claude Code</b> — <code>.mcp.json</code> در ریشه پروژه</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["port=<port>", "client-transport=stdio"]
    }
  }
}
```

</details>

<details>
<summary><b>Cursor</b> — <code>.cursor/mcp.json</code></summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "type": "stdio",
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["port=<port>", "client-transport=stdio"]
    }
  }
}
```

</details>

<details>
<summary><b>Codex</b> — <code>.codex/config.toml</code></summary>

```toml
[mcp_servers.ai-game-developer]
enabled = true
command = "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe"
args = ["port=<port>", "client-transport=stdio"]
tool_timeout_sec = 300
```

</details>

<div dir="rtl">

عامل‌های دیگر و پیکربندی دستی: [راهنمای نصب](installation.fa.md).

## امنیت و حریم خصوصی

**خصوصی‌ترین پیکربندی:** اتصال **Custom** (سرور محلی روی `127.0.0.1`، بدون حساب کاربری، داده‌های پروژه روی دستگاه شما می‌ماند) با **Authorization Token** روی `token` در **Window ▸ AI Game Dev**، سپس دوباره **Configure** تا عامل شما راز را بفرستد.

‏✅ ‏**پلاگین هیچ پورتی باز نمی‌کند** — خودش به سرور MCP وصل می‌شود.\
‏✅ ‏**سرور محلی روی loopback** — `gamedev-mcp-server` به `127.0.0.1` bind می‌شود، مگر اینکه برای میزبانی `MCP_BIND` را تنظیم کنید.\
‏✅ ‏**بررسی Origin** — درخواست‌های مرورگر با `Origin` غیرمجاز در همه حالت‌های احراز هویت رد می‌شوند (403).\
‏✅ ‏**حالت‌های احراز هویت** — `none` (پیش‌فرض محلی)، `token` (یک راز مشترک که عامل و پلاگین هر دو باید بفرستند)، `oauth` (حساب ai-game.dev).\
‏✅ ‏**رله Cloud** — یک رله بدون حالت (stateless): فراخوانی‌های ابزار را بین عامل شما و ویرایشگر مسیریابی می‌کند، بدون اینکه محتوای آن‌ها را بررسی یا ذخیره کند، و گفتگوها، کد منبع، فایل‌های پروژه، دارایی‌ها یا داده‌های صحنه را جمع‌آوری نمی‌کند. فراداده رویدادهای MCP (نوع رویداد، زمان‌بندی، وضعیت، شناسه‌های نشست، نام ابزار، پرامپت یا منبع، جزئیات خطا) را ثبت می‌کند که در یک بازه چرخشی ۹۰ روزه استفاده می‌شود ([سیاست حریم خصوصی](https://ai-game.dev/legal/privacy)).\
‏⚠️ ‏**محافظت‌نشده** — در حالت `none` هر پردازه‌ای روی دستگاه شما که به پورت دسترسی داشته باشد می‌تواند ابزارها را فراخوانی کند، و ابزارها با دسترسی‌های ویرایشگر اجرا می‌شوند (`script-execute` کد C# اجرا می‌کند). روی دستگاه‌های مشترک از حالت `token` استفاده کنید.

گزینه‌های سرور و Docker: [سرور MCP و اتصال](mcp-server.fa.md).

## افزونه‌ها

افزونه‌ها پکیج‌های اختیاری UPM هستند که برای قابلیت‌های مشخصی از Unity ابزار اضافه می‌کنند.

</div>

```bash
unity-mcp-cli install-extension com.ivanmurzak.unity.mcp.cinemachine ./MyUnityProject
```

<div dir="rtl">

| افزونه | چه اضافه می‌کند | پیش‌نیاز |
| --- | --- | --- |
| [Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) | AnimationClip و AnimatorController |  |
| [Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) | دوربین‌های مجازی، هدف‌ها و خط لوله دوربین | `com.unity.cinemachine` 3.1.6+ |
| [InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) | دارایی‌های اکشن ورودی، نقشه‌ها، اکشن‌ها و اتصال‌ها | `com.unity.inputsystem` 1.8.2+ |
| [Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) | سطوح NavMesh، بیک، عامل‌ها و لینک‌ها | `com.unity.ai.navigation` 1.1.7+ |
| [ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) | انتشار، شکل، منحنی‌ها و گرادیان‌های ParticleSystem |  |
| [ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) | مش‌های قابل ویرایش: شکل‌ها، اکستروژن، بِوِل، متریال | `com.unity.probuilder` 5.2.4+ |
| [Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) | کانتینرهای اسپلاین، گره‌ها و مماس‌ها | `com.unity.splines` 2.8.4+ |
| [Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) | زمین‌ها: نقشه ارتفاع، حجاری و نمونه‌برداری |  |
| [Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) | Grid و Tilemap: نقاشی، پر کردن و خواندن تایل‌ها | `com.unity.2d.tilemap.extras` 3.1.3+ |
| [Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md) | دارایی‌های Timeline، ترک‌ها، کلیپ‌ها و نشانگرها | `com.unity.timeline` 1.8.12+ |

پکیج‌های لازم Unity همراه افزونه نصب می‌شوند. افزونه‌ها را از بخش **Extensions** در پنجره AI Game Developer هم می‌توانید نصب کنید.

## مستندات

| صفحه | محتوا |
| --- | --- |
| [راهنمای نصب](installation.fa.md) | روش‌های نصب، اتصال عامل، پیکربندی دستی MCP، تنظیمات تیمی، حذف |
| [بنچمارک](benchmark.fa.md) | اندازه‌گیری‌های ویرایشگر Unity و دنیای بازی آزمایشی (sandbox): توکن‌ها، فراخوانی‌های ابزار و موفقیت با ابزارهای RQL |
| [ابزارهای پیش‌فرض MCP](default-mcp-tools.fa.md) | همه ابزارهای داخلی، منابع RQL، ابزارهای حذف‌شده در 1.0.0 و معادل RQL آن‌ها |
| [ابزارهای سفارشی و Runtime](custom-tools.fa.md) | نوشتن ابزار و پرامپت؛ استفاده از MCP درون بازی |
| [سرور MCP و اتصال](mcp-server.fa.md) | حالت‌های Cloud و Custom، متغیرهای محیطی پلاگین، سرور محلی |
| [استقرار با Docker](DOCKER_DEPLOYMENT.fa.md) | اجرای سرور MCP در کانتینر |
| [غیرفعال کردن در CI](ci-disable.fa.md) | خارج نگه داشتن پلاگین از اجرای معمولی تست‌ها در CI |
| [مرجع CLI](../cli/docs/README.fa.md) | همه دستورها و گزینه‌های `unity-mcp-cli` |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | آموزش‌ها، راهنماها و پرسش‌های متداول |
| [راهنمای توسعه](dev/Development.fa.md) | ساختار مخزن، سبک کد، تست‌ها و CI برای مشارکت‌کنندگان |

مرتبط: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (سرور MCP مشترک) · [ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

## مشارکت

ایده‌ها، گزارش باگ و Pull Request ها خوش‌آمدند. [راهنمای توسعه](dev/Development.fa.md) را بخوانید، مخزن را fork کنید، `./Unity-MCP-Plugin` را در Unity باز کنید و یک Pull Request به `main` بفرستید. اگر پروژه به شما کمک کرد، به آن ستاره بدهید.

## مجوز

**برای توسعه بازی رایگان است، از جمله بازی‌های تجاری.** مجوز پلاگین [Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) است. پلاگین از [RQL](https://github.com/IvanMurzak/RQL) و [Code-Exec](https://github.com/IvanMurzak/Code-Exec) استفاده می‌کند که تحت مجوز PolyForm Shield 1.0.0 هستند: این مجوز فقط شرکت‌هایی را محدود می‌کند که ابزارهای هوش مصنوعی رقیب برای توسعه بازی می‌سازند. توسعه‌دهندگان بازی می‌توانند آزادانه از همه چیز استفاده کنند.

</div>
