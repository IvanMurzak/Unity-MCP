<div dir="rtl">

[English](installation.md) | [中文](installation.zh-CN.md) | [日本語](installation.ja.md) | [Español](installation.es.md) | <b>فارسی</b> | [हिन्दी](installation.hi.md) | [العربية](installation.ar.md)

# راهنمای نصب

پلاگین **AI Game Developer** را در یک پروژه Unity نصب کنید، یک عامل هوش مصنوعی را به آن وصل کنید و در صورت نیاز دوباره حذفش کنید. بازگشت به [README](README.fa.md).

## پیش‌نیازها

- **Unity 2022.3** یا جدیدتر روی **Windows، macOS یا Linux** (هر سه آزمایش شده‌اند و پشتیبانی می‌شوند).
- مسیر پروژه **بدون فاصله**. اگر مسیر فاصله داشته باشد، پلاگین هنگام راه‌اندازی یک خطا ثبت می‌کند.
  - ‏✅ `C:/MyProjects/MyProject`
  - ‏❌ `C:/My Projects/MyProject`
- برای CLI: [Node.js](https://nodejs.org/) نسخه 22.14 یا جدیدتر.

## 1. نصب پلاگین

### گزینه 1: نصب‌کننده

1. فایل **[AI-Game-Dev-Installer.unitypackage](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)** را دانلود کنید.
2. در حالی که پروژه باز است روی آن دوبار کلیک کنید، یا در Unity از **Assets ▸ Import Package ▸ Custom Package** استفاده کنید.

### گزینه 2: OpenUPM

</div>

```bash
openupm add com.ivanmurzak.unity.mcp
```

<div dir="rtl">

### گزینه 3: CLI

‏[`unity-mcp-cli`](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) فایل `Packages/manifest.json` را برای شما ویرایش می‌کند، بنابراین لازم نیست ویرایشگر باز باشد:

</div>

```bash
npm install -g unity-mcp-cli

unity-mcp-cli install-unity                      # optional: install a Unity Editor
unity-mcp-cli create-project ./MyUnityProject    # optional: create a project
unity-mcp-cli install-plugin ./MyUnityProject    # add the plugin
unity-mcp-cli login                              # sign in to ai-game.dev (browser device flow)
unity-mcp-cli open ./MyUnityProject              # open Unity; it connects and generates skills
unity-mcp-cli wait-for-ready ./MyUnityProject    # wait until tools can be called
```

<div dir="rtl">

‏`npx unity-mcp-cli <command>` بدون نصب سراسری هم کار می‌کند.

**ورود و پین کردن پروژه.** دستور `login` اعتبارنامه را در `~/.ai-game-dev/credentials.json` ذخیره می‌کند؛ هیچ توکنی برای چسباندن وجود ندارد. به‌طور پیش‌فرض، پیکربندی عاملی که `setup-mcp` می‌نویسد به یک endpoint **پین‌شده** مخصوص هر پروژه (`https://ai-game.dev/mcp/p/<pin>`) اشاره می‌کند؛ برای endpoint مشترک `https://ai-game.dev/mcp` گزینه `--no-pin` را بدهید. تیم‌ها می‌توانند دسترسی را با یک کد ثبت‌نام به اشتراک بگذارند: `unity-mcp-cli install-plugin --enroll <code>`.

## 2. انتخاب عامل هوش مصنوعی

به یک عامل نیاز دارید؛ همان پنجره چتی است که در آن کار می‌کنید. این عامل باید از Skills یا ابزارهای MCP که هنگام اجرا به‌روز می‌شوند پشتیبانی کند. Claude Code توصیه می‌شود. پنجره AI Game Developer و `unity-mcp-cli setup-mcp` پیکربندی را برای Claude Code، Claude Desktop، Codex، Cursor، Gemini، GitHub Copilot (CLI، VS Code، Visual Studio)، Antigravity، Cline، Open Code، Rider (Junie)، Unity AI و دیگران برای شما می‌نویسند؛ `unity-mcp-cli setup-mcp --list` فهرست کامل را چاپ می‌کند. هر کلاینت MCP دیگری هم با پیکربندی JSON موجود در پنجره کار می‌کند.

## 3. اتصال عامل

### خودکار

1. پروژه را در Unity باز کنید.
2. منوی **Window ▸ AI Game Dev** را باز کنید.
3. روی **Auto-generate** Skills (توصیه‌شده) یا **Configure** MCP برای عامل خود کلیک کنید.

</div>

![AI Game Developer window](img/ai-connector-window.gif)

<div dir="rtl">

همین کار از ترمینال:

</div>

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject             # http (default)
unity-mcp-cli setup-mcp cursor ./MyUnityProject --transport stdio
```

<div dir="rtl">

### دستی

اگر عامل شما در فهرست نیست، JSON نمایش‌داده‌شده در پنجره AI Game Developer را در پیکربندی MCP آن کپی کنید.

برای عاملی که سرورها را از خط فرمان خودش اضافه می‌کند، `<command>`ی را بسازید که سرور محلی را اجرا می‌کند (حالت Custom). `<unityProjectPath>` را با مسیر کامل پروژه و `<port>` را با پورت نمایش‌داده‌شده در پنجره AI Game Developer جایگزین کنید:

</div>

| پلتفرم | `<command>` |
| --- | --- |
| Windows x64 | `"<unityProjectPath>/Library/mcp-server/win-x64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows x86 | `"<unityProjectPath>/Library/mcp-server/win-x86/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows arm64 | `"<unityProjectPath>/Library/mcp-server/win-arm64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| macOS Apple Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| macOS Intel | `"<unityProjectPath>/Library/mcp-server/osx-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux x64 | `"<unityProjectPath>/Library/mcp-server/linux-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux arm64 | `"<unityProjectPath>/Library/mcp-server/linux-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |

<div dir="rtl">

سپس آن را اضافه کنید:

</div>

```bash
claude mcp add ai-game-developer <command>    # Claude Code
gemini mcp add ai-game-developer <command>    # Gemini CLI
```

<div dir="rtl">

‏GitHub Copilot CLI: دستور `copilot` را اجرا کنید، سپس `/mcp add` را با نام سرور `ai-game-developer`، نوع `local` و `<command>` بالا اجرا کنید.

> در VS Code همراه با Copilot، مطمئن شوید حالت **Agent** فعال است.

## تنظیمات تیم: اعلان‌های به‌روزرسانی

وقتی OpenUPM نسخه جدیدتری داشته باشد، پلاگین هنگام راه‌اندازی ویرایشگر یک پنجره به‌روزرسانی نشان می‌دهد. گزینه *"Do not show again"* یک تنظیم مخصوص هر کاربر است. برای خاموش کردن این پنجره برای **کل تیم**، **Edit ▸ Project Settings ▸ AI Game Developer** را باز کنید و *"Disable update notifications for the entire team"* را فعال کنید. این تنظیم در `ProjectSettings/AI-Game-Developer-UpdateSettings.asset` ذخیره می‌شود؛ آن فایل را commit کنید. همین گزینه در **Tools ▸ AI Game Developer ▸ Updates ▸ Disable Update Notifications (Team)** هم وجود دارد.

## حذف نصب

1. **Window ▸ Package Manager** را باز کنید، **AI Game Developer — MCP** را انتخاب کنید و روی **Remove** کلیک کنید (یا `unity-mcp-cli remove-plugin ./MyUnityProject` را اجرا کنید).
2. Unity را ببندید، سپس پوشه `Assets/Plugins/NuGet` و فایل `Assets/Plugins/NuGet.meta` آن را حذف کنید.

## بعدی

- [ابزارهای پیش‌فرض MCP](default-mcp-tools.fa.md)
- [ابزارهای سفارشی و runtime](custom-tools.fa.md)
- [سرور MCP و اتصال](mcp-server.fa.md)
- [ویکی](https://github.com/IvanMurzak/Unity-MCP/wiki)

</div>
