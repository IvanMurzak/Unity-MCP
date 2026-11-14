<div align="center">

<p align="center"><img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/cli-hero.svg" alt="unity-mcp-cli: ثبّت Unity وإضافة AI Game Developer، وصِل وكلاء الذكاء الاصطناعي، وشغّل الأدوات، وصرّف واختبر من الطرفية" width="100%"></p>

[![unity-mcp-cli on npm](https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D)](https://www.npmjs.com/package/unity-mcp-cli) [![Node.js 22.14 or newer](https://img.shields.io/badge/Node.js-22.14%2B-0E7490?logo=nodedotjs&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://nodejs.org/) [![Unity 2022.3 or newer](https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://unity.com/releases/editor/archive)\
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://discord.gg/cfbdMZX99G) [![Stand with Ukraine](https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-badge.svg)](https://stand-with-ukraine.pp.ua)

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.fa.md) | [हिन्दी](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.hi.md)</b>

</div>

<div dir="rtl">

‏`unity-mcp-cli` هي أداة سطر الأوامر لـ **[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)** (Unity-MCP). تثبّت محررات Unity، وتنشئ المشاريع، وتضيف الإضافة، وتصل وكلاء الذكاء الاصطناعي، وتشغّل أدوات MCP، وتصرّف مشاريع Unity وتختبرها من الطرفية أو CI.

## الميزات

‏✅ ‏**التثبيت** — محررات Unity ومشاريع جديدة والإضافة، مع إعداد سجل OpenUPM\
‏✅ ‏**الاتصال** — سجّل الدخول مرة واحدة؛ إعدادات MCP أو Skills لـ Claude Code وCodex وCursor…\
‏✅ ‏**تشغيل الأدوات** — استعلامات RQL وأي أداة MCP وشيفرة C# داخل المحرر\
‏✅ ‏**التصريف والاختبار** — أخطاء المصرّف مع الملف والسطر؛ اختبارات EditMode وPlayMode\
‏✅ ‏**قابلة للأتمتة** — `--format json`، رموز خروج ثابتة، مخرجات مناسبة لـ CI\
‏✅ ‏**مكتبة** — الأوامر نفسها كواجهة Node.js مُنمَّطة\
‏✅ ‏**متعددة المنصات** — Windows وmacOS وLinux

## البدء السريع

</div>

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-unity                  # optional: install a Unity Editor
unity-mcp-cli create-project ./MyGame        # optional: create a project
unity-mcp-cli install-plugin ./MyGame        # add the plugin
unity-mcp-cli login                          # sign in to ai-game.dev in the browser
unity-mcp-cli open ./MyGame                  # open Unity; it connects and generates skills
unity-mcp-cli wait-for-ready ./MyGame        # until tools can be called
```

<div dir="rtl">

دون تثبيت عام: `npx unity-mcp-cli <command>`. يتطلب Node.js 22.14 أو أحدث؛ يُثبَّت Unity Hub تلقائيًا إن لم يكن موجودًا.

</div>

<p align="center"><img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/terminal.svg" alt="جلسة طرفية: تثبيت unity-mcp-cli وإضافة الإضافة وتسجيل الدخول وإعداد Claude Code وفتح المشروع" width="100%"></p>

<div dir="rtl">

## الاستخدام

</div>

```bash
unity-mcp-cli setup-skills claude-code ./MyGame                  # Skills for your agent
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame --strict --format github
unity-mcp-cli test ./MyGame --mode PlayMode --junit-output junit.xml
unity-mcp-cli install-extension Tilemap ./MyGame
```

<div dir="rtl">

يشغّل `rql` و`compile` محررًا بلا واجهة إن لم يكن هناك محرر يعمل؛ ويشغّل `test` محررًا في وضع batch كما في CI.

## الأوامر

| المجال | الأوامر |
| --- | --- |
| الإعداد | `install-unity` · `create-project` · `install-plugin` |
| الحزم | `install-extension` · `update` · `remove-plugin` |
| الحساب | `login` · `unity-account` |
| وكلاء الذكاء الاصطناعي | `setup-mcp` · `setup-skills` · `configure` |
| المحرر | `open` · `close` · `wait-for-ready` · `status` · `dialog` |
| الأدوات | `tools` · `rql` · `run-tool` · `exec` |
| البناء والاختبار | `compile` · `test` |
| الاستكشاف | `commands` |

كل الخيارات ورموز الخروج ومخرجات JSON: **[مرجع الأوامر](https://github.com/IvanMurzak/Unity-MCP/blob/docs/readme-refresh/cli/docs/commands.ar.md)**، أو `unity-mcp-cli <command> --help`.

## المكتبة

الأوامر متاحة أيضًا كمكتبة مُنمَّطة بلا آثار جانبية؛ النتائج من النوع `kind: 'success' | 'failure'`.

</div>

```ts
import { installPlugin } from 'unity-mcp-cli';

const result = await installPlugin({ unityProjectPath: './MyGame' });
if (result.kind === 'success') console.log(`Installed v${result.installedVersion}`);
```

<div dir="rtl">

## روابط

[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) · [دليل التثبيت](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.ar.md) · [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) · [Changelog](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/CHANGELOG.md) · [Discord](https://discord.gg/cfbdMZX99G)

## الترخيص

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)

</div>
