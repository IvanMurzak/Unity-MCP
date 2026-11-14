<div dir="rtl">

[English](commands.md) | [中文](commands.zh-CN.md) | [日本語](commands.ja.md) | [Español](commands.es.md) | <b>فارسی</b> | [हिन्दी](commands.hi.md) | [العربية](commands.ar.md)

# مرجع unity-mcp-cli

همه دستورها و گزینه‌های [`unity-mcp-cli`](../README.md)، کدهای خروج، خروجی قابل خواندن برای ماشین و API کتابخانه.
برای دیدن همین جزئیات در ترمینال، `unity-mcp-cli <command> --help` را اجرا کنید.

**دستورها:** [`configure`](#configure) · [`create-project`](#create-project) · [`update`](#update) · [`install-extension`](#install-extension) · [`install-plugin`](#install-plugin) · [`install-unity`](#install-unity) · [`login`](#login) · [`unity-account`](#unity-account) · [`open`](#open) · [`close`](#close) · [`tools`](#tools) · [`rql`](#rql) · [`run-tool`](#run-tool) · [`exec`](#exec) · [`test`](#test) · [`compile`](#compile) · [`dialog`](#dialog) · [`commands`](#commands-1) · [`wait-for-ready`](#wait-for-ready) · [`setup-mcp`](#setup-mcp) · [`setup-skills`](#setup-skills) · [`remove-plugin`](#remove-plugin) · [`status`](#status)

**همچنین:** [گزینه‌های سراسری](#global-options) · [کدهای خروج و خروجی قابل خواندن برای ماشین](#exit-codes-and-machine-readable-output) · [مثال کامل خودکارسازی](#full-automation-example) · [نحوه کار](#how-it-works) · [API کتابخانه](#library-api)

# Commands

## `configure`

پیکربندی ابزارها، پرامپت‌ها و منابع MCP در `UserSettings/AI-Game-Developer-Config.json`.

</div>

```bash
unity-mcp-cli configure ./MyGame --list
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | بله | مسیر پروژه Unity (موقعیتی یا `--path`) |
| `--list` | خیر | فهرست پیکربندی فعلی و خروج |
| `--enable-tools <names>` | خیر | نمایش ابزارهای مشخص به کلاینت‌های MCP (جداشده با کاما) |
| `--disable-tools <names>` | خیر | پنهان کردن ابزارهای مشخص از کلاینت‌های MCP (جداشده با کاما؛ ابزار پنهان اگر با نامش فراخوانی شود همچنان اجرا می‌شود) |
| `--enable-all-tools` | خیر | نمایش همه ابزارها |
| `--disable-all-tools` | خیر | پنهان کردن همه ابزارها |
| `--enable-prompts <names>` | خیر | نمایش پرامپت‌های مشخص به کلاینت‌های MCP (جداشده با کاما) |
| `--disable-prompts <names>` | خیر | پنهان کردن پرامپت‌های مشخص از کلاینت‌های MCP (جداشده با کاما؛ ابزار پنهان اگر با نامش فراخوانی شود همچنان اجرا می‌شود) |
| `--enable-all-prompts` | خیر | نمایش همه پرامپت‌ها |
| `--disable-all-prompts` | خیر | پنهان کردن همه پرامپت‌ها |
| `--enable-resources <names>` | خیر | نمایش منابع مشخص به کلاینت‌های MCP (جداشده با کاما) |
| `--disable-resources <names>` | خیر | پنهان کردن منابع مشخص از کلاینت‌های MCP (جداشده با کاما؛ ابزار پنهان اگر با نامش فراخوانی شود همچنان اجرا می‌شود) |
| `--enable-all-resources` | خیر | نمایش همه منابع |
| `--disable-all-resources` | خیر | پنهان کردن همه منابع |

**مثال — فعال کردن ابزارهای مشخص و غیرفعال کردن همه پرامپت‌ها:**

</div>

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools screenshot-scene-view,script-read \
  --disable-all-prompts
```

<div dir="rtl">

**مثال — فعال کردن همه چیز:**

</div>

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

<div dir="rtl">

## `create-project`

ساخت یک پروژه جدید Unity با استفاده از ویرایشگر Unity.

</div>

```bash
unity-mcp-cli create-project /path/to/new/project
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | بله | مسیری که پروژه در آن ساخته می‌شود (موقعیتی یا `--path`) |
| `--unity <version>` | خیر | نسخه ویرایشگر Unity مورد استفاده (پیش‌فرض: بالاترین نسخه نصب‌شده) |

**مثال — ساخت پروژه با یک نسخه مشخص از ویرایشگر:**

</div>

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

<div dir="rtl">

## `update`

به‌روزرسانی همه چیز: پلاگین MCP و افزونه‌های نصب‌شده یک پروژه Unity به آخرین نسخه‌هایشان، سپس خود CLI.

</div>

```bash
unity-mcp-cli update              # the project around the current folder, then the CLI
unity-mcp-cli update ./MyGame      # a project by path
unity-mcp-cli update --check       # only report; exit code 2 when something is outdated
unity-mcp-cli update --no-self     # leave the CLI as it is
unity-mcp-cli update --no-project  # update only the CLI
unity-mcp-cli update --no-extensions
```

<div dir="rtl">

بیرون از یک پروژه، فقط CLI را به‌روز می‌کند. نصب‌هایی که از `file:` یا git انجام شده‌اند دست‌نخورده می‌مانند و هیچ چیزی به نسخه پایین‌تر برگردانده نمی‌شود.

## `install-extension`

یکی از افزونه‌های Unity-MCP — بسته‌های ابزار اختیاری که ابزارهای MCP برای Animation، Cinemachine، Input System، Navigation، Particle System، ProBuilder، Splines، Terrain، Tilemap و Timeline اضافه می‌کنند — را در `Packages/manifest.json` یک پروژه Unity نصب می‌کند. این دستور معادل خط فرمانِ بخش **Extensions** در پنجره ویرایشگر *AI Game Developer* است و از همان کاتالوگ مشترک نصب می‌کند.

</div>

```bash
# See what is installable (no project and no network required)
unity-mcp-cli install-extension --list

# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-extension Tilemap
```

<div dir="rtl">

| آرگومان / گزینه | الزامی | توضیحات |
|---|---|---|
| `[id]` | بله (مگر با `--list`) | افزونه‌ای که نصب می‌شود — شناسه بسته OpenUPM (`com.ivanmurzak.unity.mcp.tilemap`) یا نام کاتالوگ (`Tilemap`). بدون حساسیت به بزرگی و کوچکی حروف تطبیق داده می‌شود. یک شناسه ناشناخته همه افزونه‌های قابل نصب را فهرست می‌کند تا بتوانید خودتان اصلاح کنید. |
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی یا `--path`). پیش‌فرض پوشه فعلی است و به همان روش `install-plugin` بررسی می‌شود. |
| `--extension-version <version>` | خیر | نسخه افزونه برای نصب (پیش‌فرض: آخرین نسخه از OpenUPM). **نه `--version`** — آن پرچم سراسری نسخه خود CLI است. |
| `--list` | خیر | فهرست افزونه‌های قابل نصب و خروج. |

این دستور:
1. **رجیستری scoped در OpenUPM** را با همه scopeهای لازم اضافه می‌کند (scope با نام `com.ivanmurzak` همان چیزی است که اصلاً افزونه را قابل resolve می‌کند)
2. شناسه بسته افزونه را به `dependencies` اضافه می‌کند و `latest` در OpenUPM را هنگام نصب resolve می‌کند
3. برای نسخه‌ای که خودکار resolve شده **هرگز به نسخه پایین‌تر برنمی‌گرداند** — اگر نسخه بالاتری از قبل نصب باشد، حفظ می‌شود و یک هشدار چاپ می‌شود. برای اجبار یک مقدار مشخص، از جمله نسخه پایین‌تر، `--extension-version` را بدهید.
4. **idempotent** است — اجرای دوباره‌ای که ببیند افزونه از قبل به‌روز است همین را گزارش می‌دهد و چیزی نمی‌نویسد

**مثال — نصب یک نسخه مشخص از افزونه:**

</div>

```bash
unity-mcp-cli install-extension Cinemachine ./MyGame --extension-version 1.0.17
```

<div dir="rtl">

**مثال — درون فرایند، از کتابخانه:**

</div>

```ts
import { installExtension, EXTENSIONS_CATALOG } from 'unity-mcp-cli';

const result = await installExtension({
  unityProjectPath: './MyGame',
  extensionId: 'Tilemap',
});
if (result.kind === 'success') {
  console.log(result.outcome, result.fromVersion, '->', result.toVersion);
}
```

<div dir="rtl">

> پس از اجرای این دستور، ویرایشگر Unity را باز کنید (یا به آن فوکوس کنید) تا Package Manager وابستگی جدید را resolve کند.

## `install-plugin`

پلاگین Unity-MCP را در `Packages/manifest.json` یک پروژه Unity نصب می‌کند. به‌صورت اختیاری در همان اجرا فایل اجرایی سرور MCP متناسب با RID را دانلود می‌کند و یک کد ثبت‌نام تیمی را بازخرید می‌کند.

</div>

```bash
# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-plugin
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی یا `--path`). پیش‌فرض پوشه فعلی است؛ بررسی می‌شود که پوشه resolveشده یک پروژه واقعی Unity باشد (نشانگر `Packages/manifest.json`) و در صورت عدم تطابق، خطا دقیقاً آنچه را بررسی شده فهرست می‌کند. |
| `--plugin-version <version>` | خیر | نسخه پلاگین برای نصب (پیش‌فرض: آخرین نسخه از [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)) |
| `--with-server` | خیر | فایل اجرایی [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) متناسب با RID را هم در پوشه تحت مدیریت CLI دانلود می‌کند |
| `--server-version <version>` | خیر | نسخه سرور برای دانلود همراه با `--with-server` (پیش‌فرض: نسخه سرور پین‌شده در CLI) |
| `--server-source <path-or-url>` | خیر | بازنویسی برای حالت آفلاین/CI — نصب سرور از مسیر یک zip محلی یا یک URL (از بررسی checksum صرف‌نظر می‌کند) |
| `--enroll <code>` | خیر | بازخرید یک کد ثبت‌نام برای دریافت اعتبارنامه پلاگین (در مخزن مشترک ماشین قرار می‌گیرد و به این پروژه پین می‌شود) |
| `--enroll-stdin` | خیر | خواندن کد ثبت‌نام از stdin به‌جای argv (آن را از تاریخچه shell دور نگه می‌دارد) |

این دستور:
1. **رجیستری scoped در OpenUPM** را با همه scopeهای لازم اضافه می‌کند
2. ‏`com.ivanmurzak.unity.mcp` را به `dependencies` اضافه می‌کند
3. **هرگز به نسخه پایین‌تر برنمی‌گرداند** — اگر نسخه بالاتری از قبل نصب باشد، حفظ می‌شود

**مثال — نصب یک نسخه مشخص از پلاگین:**

</div>

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

<div dir="rtl">

**مثال — بازخرید یک کد ثبت‌نام تیمی (مسیر پیش‌فرض cwd است):**

</div>

```bash
cd ./MyGame && unity-mcp-cli install-plugin --enroll <code>
```

<div dir="rtl">

> پس از اجرای این دستور، پروژه را در ویرایشگر Unity باز کنید تا نصب بسته کامل شود.

## `install-unity`

نصب یک نسخه از ویرایشگر Unity از طریق Unity Hub CLI.

</div>

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

<div dir="rtl">

| آرگومان / گزینه | الزامی | توضیحات |
|---|---|---|
| `[version]` | خیر | نسخه ویرایشگر Unity برای نصب (مثلاً `6000.3.1f1`) |
| `--path <path>` | خیر | خواندن نسخه لازم از یک پروژه موجود |

اگر نه آرگومان و نه گزینه داده شود، دستور آخرین نسخه پایدار را از فهرست نسخه‌های Unity Hub نصب می‌کند.

**مثال — نصب نسخه‌ای از ویرایشگر که یک پروژه نیاز دارد:**

</div>

```bash
unity-mcp-cli install-unity --path ./MyGame
```

<div dir="rtl">

## `login`

ورود به **ai-game.dev** و ذخیره اعتبارنامه در مخزن مشترک اعتبارنامه ماشین (`~/.ai-game-dev/credentials.json`). **device flow مبتنی بر OAuth 2.1** را در مرورگر اجرا می‌کند — هیچ توکن دسترسی شخصی‌ای برای ساختن یا چسباندن وجود ندارد. این ورود کل ماشین را مجاز می‌کند: یک اعتبارنامه **agent** صادر می‌کند و از آن یک اعتبارنامه ابزارهای موتور مشتق می‌کند، و هر مصرف‌کننده (دستورهای ابری CLI، پلاگین ویرایشگر Unity، اپلیکیشن دسکتاپ) آن را به‌طور خودکار تازه می‌کند — توکن‌ها در پس‌زمینه چرخانده می‌شوند، بنابراین یک بار برای هر ماشین وارد می‌شوید، نه یک بار برای هر نشست.

</div>

```bash
unity-mcp-cli login
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `--project <path>` | خیر | ذخیره اعتبارنامه در یک مخزن محلی پروژه (`<path>/.ai-game-dev/`) به‌جای مخزن مشترک ماشین |
| `--force` | خیر | احراز هویت دوباره حتی اگر اعتبارنامه‌ای از قبل وجود داشته باشد |
| `--tools-only` | خیر | فقط **ابزارهای موتور** را مجاز می‌کند (هیچ اعتبارنامه agent ذخیره نمی‌شود، بنابراین برداشتن آن توسط اپلیکیشن دسکتاپ ممکن نیست). برای runnerهای CI / خودکارسازی در نظر گرفته شده که در این صورت به‌عنوان گروه دستگاه مستقل و قابل لغو خود نمایش داده می‌شوند |
| `--yes` | خیر | پاسخ «بله» به پرسش‌ها — برای تأیید غیرتعاملی **تغییر حساب** لازم است (ورود با حسابی غیر از حسابی که ماشین در حال حاضر دارد) |

دستور یک کد کوتاه کاربر و یک URL تأیید چاپ می‌کند، مرورگر شما را باز می‌کند و تا زمانی که ورود را تأیید کنید polling می‌کند. اگر اعتبارنامه‌ای از قبل وجود داشته باشد، زودتر با *"Already signed in"* خارج می‌شود — برای جایگزینی آن `--force` را بدهید. ورود با **حسابی دیگر** غیر از حسابی که ماشین دارد ابتدا تأیید می‌خواهد (رد کردن همه چیز را بدون تغییر می‌گذارد)؛ برای تأیید در اسکریپت‌ها `--yes` را بدهید.

**مثال — ورود (مخزن مشترک ماشین):**

</div>

```bash
unity-mcp-cli login
```

<div dir="rtl">

**مثال — runner در CI، فقط ابزارهای موتور:**

</div>

```bash
unity-mcp-cli login --tools-only
```

<div dir="rtl">

**مثال — ذخیره اعتبارنامه کنار یک پروژه مشخص:**

</div>

```bash
unity-mcp-cli login --project ./MyGame
```

<div dir="rtl">

## `unity-account`

**حساب Unity** که ویرایشگر Unity در حال اجرا با آن وارد شده است را نشان می‌دهد و ورود به آن را آغاز می‌کند. بخش "My Assets" در Asset Store (منبع RQL با نام `assetStore`) به آن نیاز دارد؛ لایسنس Personal بدون آن کار می‌کند، بنابراین یک ویرایشگر می‌تواند بدون ورود اجرا شود. این حساب ai-game.dev نیست: آن [`login`](#login) است.

‏`--sign-in` صفحه ورود Unity را در آن ویرایشگر باز می‌کند؛ **کاربر** ورود را همان‌جا، از طریق Unity Hub، کامل می‌کند. ویرایشگری که مستقیماً باز شده باشد (نه از طریق Hub، مثلاً با یک `open` قدیمی‌تر) نمی‌تواند وارد شود: فقط "Install Unity Hub" را پیشنهاد می‌دهد، بنابراین پاسخ می‌گوید پروژه را با [`close`](#close) و [`open`](#open) دوباره باز کنید، که آن را از طریق Hub اجرا می‌کنند. ویرایشگری که این‌گونه باز شود بدون هیچ ورودی، حسابی را که Hub با آن وارد شده است می‌گیرد. هیچ اعتبارنامه‌ای از CLI عبور نمی‌کند. این دستور به پروژه‌ای نیاز دارد که در یک ویرایشگر با پنجره باز باشد ([`open`](#open)) و هرگز یک ویرایشگر headless را اجرا نمی‌کند، چون آن نمی‌تواند صفحه ورود را نشان دهد. عامل‌ها می‌توانند همین کوئری‌ها را با `rql` اجرا کنند: `assetStore | select signedIn userName` و `assetStore | call SignIn()`.

</div>

```bash
unity-mcp-cli unity-account ./MyGame            # signedIn|userName
unity-mcp-cli unity-account ./MyGame --sign-in  # opens Unity's sign-in for the user
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی یا `--path`؛ پیش‌فرض: پوشه فعلی) |
| `--sign-in` | خیر | باز کردن صفحه ورود Unity وقتی ویرایشگر وارد نشده است (اگر وارد شده باشد: توکن دسترسی را تازه می‌کند) |
| `--format <format>` | خیر | `text`، `json` یا `ndjson` |

با پاسخ ویرایشگر با کد `0` خارج می‌شود، و وقتی هیچ ویرایشگری پاسخ ندهد با `7`. پلاگینی که برای این دستور خیلی قدیمی است راهنمایی برای اجرای [`update`](#update) دریافت می‌کند.

## `open`

یک پروژه Unity را در ویرایشگر Unity باز می‌کند. به‌طور پیش‌فرض، اگر گزینه‌های اتصال داده شده باشند، متغیرهای محیطی اتصال MCP را تنظیم می‌کند. برای باز کردن بدون اتصال MCP از `--no-connect` استفاده کنید.

وقتی Unity Hub نصب باشد، ویرایشگر همان‌طور که خود Hub انجام می‌دهد از طریق آن باز می‌شود (`-useHub -hubIPC`، بدون اعتبارنامه: ویرایشگر نشست خود را از Hub می‌خواهد و اگر Hub در حال اجرا نباشد آن را در پس‌زمینه اجرا می‌کند). سپس ویرایشگر حساب Unity را که Hub با آن وارد شده است می‌گیرد، که "My Assets" در Asset Store به آن نیاز دارد ([`unity-account`](#unity-account) را ببینید). ویرایشگر headless (`--headless`) مانند قبل باز می‌شود.

</div>

```bash
# Explicit path
unity-mcp-cli open ./MyGame

# From inside the Unity project folder — path defaults to the current directory
cd ./MyGame && unity-mcp-cli open
```

<div dir="rtl">

| گزینه | متغیر محیطی | الزامی | توضیحات |
|---|---|---|---|
| `[path]` | — | خیر | مسیر پروژه Unity (موقعیتی یا `--path`). پیش‌فرض پوشه کاری فعلی است. |
| `--unity <version>` | — | خیر | نسخه مشخص ویرایشگر Unity مورد استفاده (پیش‌فرض نسخه موجود در تنظیمات پروژه است، و در غیر این صورت بالاترین نسخه نصب‌شده) |
| `--editor-path <path>` | — | خیر | مسیر صریح فایل اجرایی ویرایشگر Unity. از کشف از طریق Unity Hub صرف‌نظر می‌کند؛ برای محل‌های نصب سفارشی مفید است. |
| `--no-connect` | — | خیر | باز کردن بدون متغیرهای محیطی اتصال MCP |
| `--url <url>` | `UNITY_MCP_CLOUD_URL` | خیر | URL سرور MCP برای اتصال (CLI همچنان نام مستعار قدیمی `UNITY_MCP_HOST` را export می‌کند که پلاگین هم آن را می‌پذیرد) |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | خیر | اجبار به زنده نگه داشتن اتصال |
| `--token <token>` | `UNITY_MCP_TOKEN` | خیر | توکن احراز هویت |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | خیر | حالت احراز هویت: `none`، `oauth` یا `token` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | خیر | فهرست ابزارهای فعال، جداشده با کاما |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | خیر | روش transport: `streamableHttp` یا `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | خیر | برای کنترل اجرای خودکار سرور MCP روی `true` یا `false` تنظیم کنید |
| `--headless` | — | خیر | ویرایشگر را بدون پنجره اجرا می‌کند (`-batchmode`؛ گرافیک روشن می‌ماند، بنابراین دوربین‌ها همچنان رندر می‌کنند) و منتظر می‌ماند تا سرور MCP آن پاسخ دهد. به معنای `--keep-connected` است. از ویرایشگرِ در حال اجرای پروژه دوباره استفاده می‌شود؛ ویرایشگری که این‌جا اجرا شود متعلق به خود CLI است ([`close --owned-only`](#close) را ببینید)، و فراخوانی‌های موازی فقط یکی را اجرا می‌کنند. |
| `--idle-timeout <seconds>` | `AIGD_SESSION_IDLE_SECONDS` (headless) / `UNITY_MCP_IDLE_TIMEOUT` (window) | خیر | خروج از ویرایشگر پس از این تعداد ثانیه بدون فراخوانی ابزار (ابتدا صحنه‌ها و assetهای تغییرکرده را ذخیره می‌کند) |
| `--ready-timeout <ms>` | — | خیر | همراه با `--headless`: مدت انتظار برای آماده شدن ویرایشگر (پیش‌فرض: `300000`) |
| `--no-auto-dismiss-launch-errors` | — | خیر | غیرفعال کردن بستن خودکار پنجره "compile errors at launch" ویرایشگر Unity (پیش‌فرض: فعال) |
| `--launch-dismiss-timeout-ms <ms>` | — | خیر | مهلت کلی (میلی‌ثانیه) برای حلقه polling بستن خودکار خطاهای هنگام اجرا (پیش‌فرض: `30000`) |
| `--launch-dismiss-poll-interval-ms <ms>` | — | خیر | فاصله هر تیک polling (میلی‌ثانیه) برای حلقه بستن خودکار خطاهای هنگام اجرا (پیش‌فرض: `1500`) |

بدون `--headless`، از یک ویرایشگر headless که CLI برای پروژه اجرا کرده است (`exec`، `open --headless`) ابتدا خواسته می‌شود ذخیره کند و خارج شود، تا پنجره بتواند باز شود.

فرایند ویرایشگر در حالت detached اجرا می‌شود. به‌طور پیش‌فرض، پس از اجرای ویرایشگر، `open` به‌دنبال پنجره "compile errors at launch" یونیتی polling می‌کند (`"Enter Safe Mode?"` در Unity 2020.2+، `"Hold On" / "Compiler Errors"` در نسخه‌های قدیمی‌تر) و روی `Ignore` کلیک می‌کند تا ویرایشگر مقداردهی اولیه را تمام کند — بدون این کار، هیچ خودکارسازی درون ویرایشگر که باید پس از وضعیتی اجرا شود که خود Unity نمی‌تواند کامپایل کند (مثلاً resolver وابستگی NuGet) نمی‌تواند خودش را ترمیم کند. این پنجره پس از اینکه Unity بالا آمد، به Package Manager وصل شد و کامپایل را شروع کرد ظاهر می‌شود — به‌طور تجربی حدود ۶ ثانیه روی یک ماشین سریع و بیشتر روی ماشین کند — بنابراین حلقه polling یک پنجره زمانی مهلت دارد که پس از آن، اگر هیچ پنجره‌ای دیده نشده باشد، زودتر خارج می‌شود. این پنجره مهلت باید کل مرحله راه‌اندازی Unity را پوشش دهد، وگرنه حلقه پیش از ظاهر شدن پنجره رها می‌شود (issue #737)؛ در حالتی که پنجره‌ای نیست هرگز کل `--launch-dismiss-timeout-ms` را اجرا نمی‌کند. اگر پنجره دیده شود (و با موفقیت بسته شود)، polling تا مهلت کلی ادامه می‌یابد تا پنجره‌ای که دوباره ظاهر می‌شود (resolver یک خطا را رفع می‌کند ← پنجره با خطای بعدی دوباره ظاهر می‌شود) دوباره بسته شود. فراخوان‌های حالت کتابخانه می‌توانند یک `AbortSignal` (`launchDismissAbortSignal` روی `OpenProjectOptions`) بدهند تا حلقه را در همان لحظه‌ای که سیگنال آمادگی خودشان فعال می‌شود متوقف کنند.

### الزامات پلتفرم برای بستن خودکار

| پلتفرم | الزام | توضیحات |
|---|---|---|
| **Windows** | داخلی (Win32 API) | از `EnumWindows` / `EnumChildWindows` / `SendMessageW(BM_CLICK)` که از PowerShell هدایت می‌شوند استفاده می‌کند. نیازی به راه‌اندازی اضافه نیست. |
| **macOS** | **مجوز Accessibility** باید به ترمینال (یا فایل اجرایی `unity-mcp-cli`) داده شود. System Settings → Privacy & Security → Accessibility. | با AppleScript / `osascript` پیاده‌سازی شده است. بدون این مجوز، `osascript` در هر تیک polling خطا گزارش می‌کند و پنجره بسته نمی‌شود. |
| **Linux/X11** | ‏`xdotool` روی `PATH` (مثلاً `sudo apt-get install xdotool`). | Wayland در نسخه اول **پشتیبانی نمی‌شود** — برای پشتیبانی از Wayland، issueهای upstream را دنبال کنید. |

برای انصراف کامل، `--no-auto-dismiss-launch-errors` را بدهید.

**مثال — باز کردن با اتصال MCP:**

</div>

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

<div dir="rtl">

**مثال — باز کردن بدون اتصال MCP (باز کردن ساده):**

</div>

```bash
unity-mcp-cli open ./MyGame --no-connect
```

<div dir="rtl">

**مثال — باز کردن با احراز هویت و ابزارهای مشخص:**

</div>

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth token \
  --tools rql-query,rql-mutate,rql-schema
```

<div dir="rtl">

## `close`

نمونه ویرایشگر Unity در حال اجرا برای یک مسیر پروژه مشخص را به‌آرامی خاتمه می‌دهد. همتای متقارن [`open`](#open) — برای گردش‌کارهای اسکریپتی (عامل‌های CI، اجراکننده‌های pipeline، fixtureهای تست یکپارچگی) که بدون توسل به kill کردن فرایند در سطح سیستم‌عامل، به یک پایان تمیز نیاز دارند.

</div>

```bash
unity-mcp-cli close ./MyGame
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی، پیش‌فرض پوشه فعلی) |
| `--timeout <seconds>` | خیر | مهلت خروج مؤدبانه به ثانیه (پیش‌فرض: 30) |
| `--force` | خیر | اگر ویرایشگر در مدت `--timeout` خارج نشود، آن را به‌زور kill می‌کند |
| `--owned-only` | خیر | فقط ویرایشگری را می‌بندد که CLI اجرا کرده است (`exec`، `open --headless`)؛ به ویرایشگری که کاربر باز کرده هرگز دست نمی‌زند |

**نحوه کار:**

0. ویرایشگری که CLI اجرا کرده است در session lease پروژه (`.ai-game-dev/session.json`: PID، زمان شروع فرایند، nonce) ثبت می‌شود. اگر lease به یک ویرایشگر زنده اشاره کند، `close` از پلاگین آن می‌خواهد ذخیره کند و خارج شود (ابزار سیستمی `editor-session-quit`، که با nonce موجود در lease بررسی می‌شود، در URL سروری که lease هنگام اجرای ویرایشگر ثبت کرده است) — این کار به ویرایشگر headless هم می‌رسد، که `WM_CLOSE` به آن نمی‌رسد — و از مراحل زیر صرف‌نظر می‌کند. با `--owned-only` همین‌جا متوقف می‌شود: بدون lease، چیزی بسته نمی‌شود. اگر آن ویرایشگر پس از `--timeout` (بدون `--force`) همچنان در حال اجرا باشد و یک پنجره modal باز باشد، `close` با کد 18 خارج می‌شود و نام آن را می‌برد، مانند مرحله 3.
1. PID ویرایشگر در حال اجرا را با خواندن `<project>/Temp/UnityLockfile` (uint32 چهاربایتی little-endian) پیدا می‌کند و برای مدیریت lock fileهای کهنه آن را با فهرست فرایندها مقایسه می‌کند.
2. یک سیگنال خروج مؤدبانه می‌فرستد — `SIGTERM` در Linux/macOS، `taskkill` (بدون `/F`) در Windows — و اجازه می‌دهد Unity ذخیره خودکار / import assetها را تمام کند.
3. هر 250ms تا خروج فرایند یا سپری شدن `--timeout` polling می‌کند. در همین حین به‌دنبال پنجره modal ویرایشگر می‌گردد (Windows، macOS، Linux): خروج مؤدبانه با تغییرات ذخیره‌نشده پنجره "Scene(s) Have Been Modified" [Save | Don't Save | Cancel] را باز می‌کند و ویرایشگر سپس منتظر یک انسان می‌ماند. بدون `--force`، به‌محض اینکه دو اسکن پشت‌سرهم پنجره را ببینند (حدود 3 ثانیه پس از خروج)، `close` با کد خروج 18 متوقف می‌شود و نام پنجره و دکمه‌هایش را می‌برد (در ویرایشگر یا با [`dialog --press`](#dialog) به آن پاسخ دهید)؛ با `--force` همچنان پیش از kill اجباری تا پایان `--timeout` صبر می‌کند.
4. اگر مهلت تمام شود و `--force` تنظیم شده باشد، به `SIGKILL` / `taskkill /F` روی می‌آورد.
5. ‏idempotent — بستن ویرایشگری که از قبل بسته است (یا پروژه‌ای که ویرایشگرش هرگز اجرا نشده) با کد 0 و پیام `no running Editor for project at <path>` خارج می‌شود.
6. از عمل روی هر مسیری که ریشه پروژه Unity نباشد امتناع می‌کند (`ProjectSettings/ProjectVersion.txt` باید وجود داشته باشد) — در برابر فراخوانی‌های تصادفی که همه Unityهای میزبان را kill می‌کنند محافظت می‌کند.

> **هشدار headless در Windows:** مرحله خروج مؤدبانه از `taskkill` (بدون `/F`) استفاده می‌کند که `WM_CLOSE` را تحویل می‌دهد. این پیام فقط به فرایندهایی می‌رسد که یک پنجره سطح بالا روی **همان desktop/session** با CLI دارند. اگر Unity توسط یک سرویس Windows در session 0 (یا هر desktop غیرتعاملی دیگری) اجرا شده باشد، خروج مؤدبانه بی‌صدا نادیده گرفته می‌شود، `--timeout` سپری می‌شود و `--force` تنها راهی است که ویرایشگر را پایین می‌آورد. در runnerهای CI بدون رابط گرافیکی متناسب با آن برنامه‌ریزی کنید. ویرایشگری که CLI اجرا کرده است (مرحله 0) تحت تأثیر نیست: از طریق پلاگین خود خارج می‌شود.

**مثال — بستن، و پس از 60 ثانیه روی آوردن به force:**

</div>

```bash
unity-mcp-cli close ./MyGame --timeout 60 --force
```

<div dir="rtl">

**مثال — پایان تمیز در انتهای یک اسکریپت خودکارسازی:**

</div>

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
unity-mcp-cli close ./MyGame
```

<div dir="rtl">

## `tools`

ابزارهای هوش مصنوعی‌ای را که ویرایشگر Unity پروژه ارائه می‌دهد فهرست می‌کند — صحنه، assetها، تست‌ها، اسکرین‌شات‌ها و هر چیزی که افزونه‌های نصب‌شده اضافه می‌کنند — همراه با کاری که هر کدام انجام می‌دهد و ورودی‌هایش، یا یک ابزار را با جزئیات نشان می‌دهد (توضیح کامل، ورودی‌ها، یک فراخوانی نمونه، JSON schema ورودی آن). وقتی هیچ ویرایشگری در حال اجرا نباشد، مانند [`exec`](#exec) یک ویرایشگر headless اجرا می‌کند. فهرست از طریق MCP (`tools/list`) خوانده و در پروژه ذخیره می‌شود (git-ignored)، بنابراین `--cached` وقتی ویرایشگر بسته است فوراً پاسخ می‌دهد. ابزارهای پنهان (که به کلاینت‌های MCP ارائه نمی‌شوند) با نامشان فهرست می‌شوند؛ `run-tool` همچنان آن‌ها را فراخوانی می‌کند.

</div>

```bash
unity-mcp-cli tools                 # every tool, one line each, with its inputs
unity-mcp-cli tools rql-mutate      # one tool: description, inputs, example call, schema
unity-mcp-cli tools screenshot      # a regex filter over names and descriptions
unity-mcp-cli tools --format json   # full schemas, machine-readable
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[name]` | خیر | نام یک ابزار (جزئیات) یا یک regex (فیلتر) |
| `--path <path>` | خیر | مسیر پروژه Unity (پیش‌فرض: پوشه فعلی) |
| `--cached` | خیر | پاسخ از فهرست ذخیره‌شده بدون تماس با ویرایشگر |
| `--no-start` | خیر | هرگز ویرایشگری اجرا نکن؛ وقتی هیچ ویرایشگری پاسخ نمی‌دهد از فهرست ذخیره‌شده استفاده کن |
| `--once` | خیر | اگر این فراخوانی ویرایشگر را اجرا کرده، پس از آن ویرایشگر را ببند |
| `--ready-timeout <ms>` | خیر | انتظار برای ویرایشگر اجراشده (پیش‌فرض: 300000) |
| `--url <url>` / `--token <token>` | خیر | بازنویسی‌های سرور |
| `--format <format>` | خیر | `text`، `json` یا `ndjson` |

## `rql`

اجرای RQL از خط فرمان، بدون نقل‌قول‌گذاری JSON: یک کوئری، یا چند کوئری که با `;` از هم جدا شده‌اند. کوئری‌ای که مرحله آخرش `set` یا `call` باشد به `rql-mutate` می‌رود (در حالت چندتایی: اگر هر یک از آن‌ها چنین باشد، همه باید چنین باشند؛ هر کدام تغییری مستقل و همه‌یا‌هیچ است و اولین کوئری‌ای که شکست بخورد بقیه را متوقف می‌کند) و هر کوئری دیگری به `rql-query`؛ `--schema [type]` ابزار `rql-schema` را اجرا می‌کند. مانند `run-tool` یک ویرایشگر headless اجرا می‌کند. Unity هر تغییر را بلافاصله روی دیسک ذخیره می‌کند، بنابراین پس از یک `set` / `call`، CLI فایل‌های صحنه و assetی را که نوشته است چاپ می‌کند (`Saved to disk: Assets/Scenes/Forest.unity`). عملگری که به املای زبانی دیگر نوشته شده باشد (`startsWith`، `like`) راهنمایی با املای RQL دریافت می‌کند. `rql --help` دستورالعمل‌های آماده را فهرست می‌کند.

</div>

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli rql 'objects | where name starts-with "Rock" | set transform.localScale = transform.localScale * 1.5'
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[query]` | بله، مگر با `--schema` / `-f` | کوئری RQL (آن را در نقل‌قول تکی بگذارید؛ رشته‌های درون آن نقل‌قول دوتایی می‌گیرند) |
| `-f, --file <file>` | خیر | خواندن کوئری از یک فایل (`-`: stdin)، برای shellی که نقل‌قول‌های داخلی را حذف می‌کند |
| `--schema [type]` | خیر | فهرست منابع RQL، یا اعضای یک نوع |
| `--dry-run` | خیر | برای `set` / `call`: نشان بده چه چیزی تغییر می‌کند، چیزی را تغییر نده |
| `--max-targets <n>` | خیر | برای `set` / `call`: بیش از 200 هدف (تا 1000) |
| `--path <path>` | خیر | مسیر پروژه Unity (پیش‌فرض: پوشه فعلی) |
| `--no-start` / `--once` | خیر | مانند `run-tool` (`--url`، `--token`، `--raw`، `--timeout`، `--ready-timeout` هم کار می‌کنند) |
| `--format <format>` | خیر | `text`، `json` یا `ndjson` |

## `run-tool`

یک ابزار MCP را مستقیماً از طریق HTTP API اجرا می‌کند. URL سرور و توکن مجوز به‌طور **خودکار** از فایل پیکربندی پروژه (`UserSettings/AI-Game-Developer-Config.json`) و بر اساس حالت اتصال فعلی (Custom یا Cloud) **resolve می‌شوند**.

</div>

```bash
unity-mcp-cli run-tool rql-mutate ./MyGame --input '{"query":"editor | call CreateGameObject({name: \"Cube\", primitive: \"Cube\"})"}'
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `<tool-name>` | بله | نام ابزار MCP برای اجرا |
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی یا `--path`) — برای خواندن پیکربندی و تشخیص پورت استفاده می‌شود |
| `--url <url>` | خیر | بازنویسی مستقیم URL سرور (پیکربندی را دور می‌زند) |
| `--token <token>` | خیر | بازنویسی توکن Bearer (پیکربندی را دور می‌زند) |
| `--input <json>` | خیر | رشته JSON آرگومان‌های ابزار (پیش‌فرض: `{}`) |
| `--input-file <file>` | خیر | خواندن آرگومان‌های JSON از یک فایل |
| `--raw` | خیر | خروجی JSON خام (بدون قالب‌بندی، بدون spinner) |
| `--format <format>` | خیر | `text` (پیش‌فرض)، `json` یا `ndjson` — [خروجی قابل خواندن برای ماشین](#exit-codes-and-machine-readable-output) را ببینید |
| `--timeout <ms>` | خیر | مهلت درخواست به میلی‌ثانیه (پیش‌فرض: 60000) |
| `--no-start` | خیر | هرگز ویرایشگری اجرا نکن؛ وقتی هیچ ویرایشگری پاسخ نمی‌دهد شکست بخور (خروج با 7) |
| `--once` | خیر | اگر این فراخوانی ویرایشگر را اجرا کرده، پس از فراخوانی آن را ببند (استفاده یک‌باره در CI) |
| `--ready-timeout <ms>` | خیر | مدت انتظار برای آماده شدن ویرایشگر اجراشده (پیش‌فرض: 300000) |

**اجرای ویرایشگر.** وقتی هیچ ویرایشگری از پروژه پاسخ ندهد، `run-tool` دقیقاً مانند [`exec`](#exec) یک ویرایشگر headless اجرا می‌کند (session lease مشترک، هرگز ویرایشگری را که خودش اجرا نکرده نمی‌بندد) — اما فقط برای پروژه‌ای که نسخه Unity خود را در `ProjectSettings/ProjectVersion.txt` نام می‌برد، تا هرگز ویرایشگری را حدس نزند. `--no-start` این کار را خاموش می‌کند؛ `--once` فراخوانی را یک‌باره می‌کند: ویرایشگری که این فراخوانی اجرا کرده پس از فراخوانی دوباره بسته می‌شود (و همچنین وقتی هرگز آماده نشود). `run-system-tool` هم همین رفتار را دارد.

ابزاری که با خطا پاسخ دهد با کد 6 خارج می‌شود؛ ابزار ناشناخته با 2 (`NOT_FOUND`)، مهلت تمام‌شده با 14، ویرایشگر غیرقابل دسترس با 7، ویرایشگر مشغول با 17، و فراخوانی‌ای که توسط یک پنجره modal مسدود شده با 18 — جدول کامل، همراه با وضعیت‌های HTTP، در [کدهای خروج](#exit-codes-and-machine-readable-output) آمده است.

**اولویت resolve کردن URL:**
1. ‏`--url` ← مستقیماً استفاده می‌شود
2. فایل پیکربندی ← `host` (حالت Custom) یا endpoint ابری ثابت (حالت Cloud)
3. پورت قطعی بر اساس مسیر پروژه

**مجوز** به‌طور خودکار از پیکربندی پروژه خوانده می‌شود (`token` در حالت Custom، `cloudToken` در حالت Cloud). برای بازنویسی صریح توکنی که از پیکربندی به‌دست آمده از `--token` استفاده کنید.

**مثال — فراخوانی یک ابزار (URL و احراز هویت از پیکربندی):**

</div>

```bash
unity-mcp-cli run-tool rql-query ./MyGame --input '{"query":"objects | where name == \"Player\" | select path name active"}'
```

<div dir="rtl">

**مثال — بازنویسی صریح URL:**

</div>

```bash
unity-mcp-cli run-tool rql-mutate --url http://localhost:8080 --input '{"query":"editor | call SaveScenes()"}'
```

<div dir="rtl">

**مثال — هدایت خروجی JSON خام با pipe:**

</div>

```bash
unity-mcp-cli run-tool unity-tool-list ./MyGame --raw | jq .
```

<div dir="rtl">

**مثال — فراخوانی یک‌باره در CI (یک ویرایشگر headless اجرا می‌کند و پس از آن می‌بندد):**

</div>

```bash
unity-mcp-cli run-tool package-list ./MyGame --url http://localhost:28343 --once --format json
```

<div dir="rtl">

## `exec`

کد C# را در ویرایشگر Unity اجرا می‌کند و نتیجه را چاپ می‌کند. اگر پروژه از قبل باز باشد (با پنجره یا headless)، `exec` از همان ویرایشگر استفاده می‌کند و هرگز آن را نمی‌بندد. در غیر این صورت یک ویرایشگر headless اجرا می‌کند که برای فراخوانی‌های بعدی گرم می‌ماند و پس از `--idle` ثانیه بدون فراخوانی ابزار خودش خارج می‌شود (صحنه‌ها و assetهای تغییرکرده را ذخیره می‌کند). ویرایشگری که اجرا می‌کند در session lease پروژه (`.ai-game-dev/session.json`) ثبت می‌شود و تحت یک file lock اجرا می‌شود، بنابراین فراخوانی‌های موازی `exec` یک ویرایشگر را به اشتراک می‌گذارند؛ `close --owned-only` آن را می‌بندد، و `open` (با پنجره) پیش از باز کردن پنجره آن را می‌بندد. این فایل‌های نشست مخصوص همان ماشین هستند: `.ai-game-dev/.gitignore` آن‌ها را بیرون از git نگه می‌دارد. اگر پروژه در ویرایشگری باز باشد که CLI اجرا نکرده و پلاگین آن ظرف 60 ثانیه پاسخ ندهد (قطع است، یا به ابر ai-game.dev وصل است)، `exec` به‌جای انتظار با یک راهنمایی متوقف می‌شود: پلاگین را وصل کنید، اتصال آن را به Custom (یک سرور محلی) تغییر دهید، یا `--url` را بدهید.

</div>

```bash
# The last expression is the result
unity-mcp-cli exec 'GameObject.FindObjectsOfType<Camera>().Length'

# From a file or stdin, with extra namespaces and helper files
unity-mcp-cli exec -f ./snippet.cs --using MyGame.Gameplay --include Helpers/
echo 'Application.unityVersion' | unity-mcp-cli exec
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[code]` | خیر | قطعه کد C#: دستورهای ساده، آخرین عبارت نتیجه است. یا به‌جای آن `-f` یا stdin. |
| `--path <path>` | خیر | مسیر پروژه Unity (پیش‌فرض: پوشه فعلی) |
| `-f, --file <file>` | خیر | خواندن کد از یک فایل (`-` برای stdin) |
| `--using <namespace>` | خیر | namespace اضافه، قابل تکرار |
| `--include <path>` | خیر | فایل یا پوشه کمکی `.cs` (بازگشتی)، نسبت به `<Project>/.ai-game-dev/snippets`، قابل تکرار |
| `--window` | خیر | اجرای ویرایشگر با پنجره به‌جای headless (فقط وقتی هنوز در حال اجرا نیست) |
| `--idle <seconds>` | خیر | خروج ویرایشگری که `exec` اجرا کرده پس از این تعداد ثانیه بیکاری (پیش‌فرض: 300) |
| `--ready-timeout <ms>` | خیر | مدت انتظار برای آماده شدن ویرایشگر (پیش‌فرض: 300000) |
| `--timeout <ms>` | خیر | مهلت فراخوانی ابزار (پیش‌فرض: 310000) |
| `--raw` | خیر | چاپ JSON خام پاسخ ابزار |
| `--format <format>` | خیر | `text` (پیش‌فرض)، `json` یا `ndjson` |
| `--once` | خیر | اگر این فراخوانی ویرایشگر را اجرا کرده، پس از فراخوانی آن را ببند |
| `--url <url>` | خیر | بازنویسی URL سرور؛ یک URL محلی همچنین باعث می‌شود ویرایشگری که تازه اجرا می‌شود آن سرور را اجرا کند |
| `--token <token>` | خیر | بازنویسی توکن Bearer |

‏`exec` ابزار `script-execute` را فراخوانی می‌کند، بنابراین قطعه‌کدها، `usings` و `include` به نسخه‌ای از پلاگین با پشتیبانی Code-Exec نیاز دارند. خطای کامپایل یا یک استثنا با کد 6 خارج می‌شود و به خطوط خود شما اشاره می‌کند.

> **وارد نشده‌اید؟** یک پروژه تازه در حالت اتصال **Cloud** شروع می‌شود. بدون ورود به ai-game.dev، دستورهای `exec`،
> `run-tool`، `compile` و `tools` به‌جای آن از سرور MCP **محلی** پروژه استفاده می‌کنند (این را در stderr می‌گویند): به ویرایشگری
> که اجرا می‌کنند گفته می‌شود آن را در حافظه سرو کند — پیکربندی پروژه تغییر نمی‌کند. برای کار از طریق ابر `login` را اجرا کنید.
>
> ویرایشگر headless که CLI اجرا کرده است وقتی اسکریپت‌های پروژه کامپایل نمی‌شوند همچنان در حال اجرا می‌ماند
> (`-ignorecompilererrors`)، تا `compile` بتواند خطاها را گزارش کند؛ لاگ آن `Logs/unity-mcp-cli-editor.log` است.

## `test`

تست‌های Unity پروژه را به همان روشی که CI اجرا می‌کند اجرا می‌کند: یک **ویرایشگر در حالت batch با `-runTests` خود Unity** — هیچ پلاگین AI Game Developer و هیچ سرور MCP درگیر نیست، بنابراین روی پروژه‌ای که پلاگینش خراب یا غایب است هم کار می‌کند. حکم از گزارش NUnit که اجرا می‌نویسد گرفته می‌شود (گزارش قبلی در همان مسیر ابتدا حذف می‌شود)، و هرگز فقط از کد خروج ویرایشگر نیست.

</div>

```bash
unity-mcp-cli test ./MyGame                                  # EditMode, report in ./test-results.xml
unity-mcp-cli test ./MyGame --mode PlayMode --filter "MyGame.Tests.Combat" --junit-output junit.xml
unity-mcp-cli test ./MyGame --format github                  # ::error annotations at the failing test's source line
unity-mcp-cli test ./MyGame --format json --timeout 900 -- -nographics
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی یا `--path`، پیش‌فرض: پوشه فعلی) |
| `--mode <mode>` | خیر | ‏`EditMode` (پیش‌فرض) یا `PlayMode` |
| `--filter <pattern>` | خیر | ‏`-testFilter` در Unity: نام کامل تست‌ها یا یک regex، `;` چند مورد را جدا می‌کند |
| `--category <names>` | خیر | ‏`-testCategory` در Unity |
| `--assembly <names>` | خیر | ‏`-assemblyNames` در Unity |
| `--output <file>` | خیر | مسیر گزارش NUnit (پیش‌فرض: `<project>/Logs/test-results.xml`) |
| `--junit-output <file>` | خیر | یک گزارش JUnit هم بنویس |
| `--log-file <file>` | خیر | لاگ ویرایشگر (پیش‌فرض: مسیر گزارش با `.log`) |
| `--timeout <seconds>` | خیر | پس از این مدت ویرایشگر (و فرایندهای فرزند آن) را kill کن (پیش‌فرض: 3600) |
| `--nographics` | خیر | اجرا بدون GPU |
| `--unity <version>` / `--editor-path <path>` | خیر | ویرایشگر مورد استفاده (پیش‌فرض: نسخه پروژه) |
| `--format <format>` | خیر | `text`، `json`، `ndjson` یا `github` |
| `-- <args>` | خیر | آرگومان‌های اضافه ویرایشگر؛ آرگومان‌هایی که `test` مدیریت می‌کند (`-batchmode`، `-runTests`، `-quit`، ...) رد می‌شوند |

**کدهای خروج:** `0` همه قبول شدند (اجرایی با صفر تست همراه با یک هشدار قبول می‌شود)، `8` تست‌ها رد شدند، `6` بدون حکم (خطاهای کامپایل — به‌صورت `file(line,col): error CS...` فهرست می‌شوند — مشکل لایسنس، crash، یا پروژه **از قبل در یک ویرایشگر باز است**: ویرایشگر دوم نمی‌تواند آن را باز کند، پس به‌جای آن از `run-tool tests-run` روی همان ویرایشگر استفاده کنید)، `14` مهلت تمام شد، `2` آرگومان‌های نادرست. Ctrl+C یا لغو یک job در CI هم ویرایشگر را متوقف می‌کند.

## `compile`

«آیا C# من کامپایل می‌شود؟» در برابر ویرایشگر پروژه: assetها را refresh می‌کند تا اسکریپت‌های تغییرکرده کامپایل شوند، منتظر کامپایل و domain reload آن می‌ماند، و خطاها و هشدارهای کامپایلر را از Console ویرایشگر همراه با فایل، خط و ستون گزارش می‌کند. وقتی هیچ ویرایشگری در حال اجرا نباشد یک ویرایشگر headless اجرا می‌کند (مانند `exec` برای فراخوانی بعدی گرم نگه داشته می‌شود)؛ فراخوانی در برابر یک ویرایشگر گرم چند ثانیه طول می‌کشد.

</div>

```bash
unity-mcp-cli compile ./MyGame
unity-mcp-cli compile ./MyGame --strict --format github   # warnings fail too
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی یا `--path`) |
| `--strict` | خیر | با هشدارهای کامپایلر هم شکست بخور |
| `--timeout <seconds>` | خیر | انتظار برای کامپایل و domain reload (پیش‌فرض: 180) |
| `--ready-timeout <ms>` | خیر | انتظار برای ویرایشگر اجراشده (پیش‌فرض: 300000) |
| `--once` | خیر | اگر این فراخوانی ویرایشگر را اجرا کرده، پس از آن ویرایشگر را ببند |
| `--url <url>` / `--token <token>` | خیر | بازنویسی‌های سرور، مانند `exec` |
| `--format <format>` | خیر | `text`، `json`، `ndjson` یا `github` |

**کدهای خروج:** `0` کامپایل می‌شود، `6` خطاهای کامپایل (یا هشدارها با `--strict`)، `7` به هیچ ویرایشگری نمی‌توان رسید، `14` مهلت تمام شد، `18` یک پنجره modal ویرایشگر را مسدود کرده است. تشخیص‌ها در قالب خود کامپایلر `file(line,col): error CS0103: ...` به stderr می‌روند.

## `dialog`

یک پنجره modal (پرسش ذخیره، "Enter Safe Mode?"، `EditorUtility.DisplayDialog`) main thread ویرایشگر را مسدود می‌کند، بنابراین هر فراخوانی ابزار منتظر می‌ماند تا کسی به آن پاسخ دهد. `dialog` پنجره‌های ویرایشگر پروژه را فهرست می‌کند و می‌تواند یک دکمه را فشار دهد. `status`، `run-tool`، `exec`، `compile` و `wait-for-ready` چنین پنجره‌ای را به‌جای یک timeout ساده با کد خروج `18` (`AWAITING_INPUT`) گزارش می‌کنند. Windows، macOS و Linux؛ ویرایشگر headless (`-batchmode`) هرگز پنجره نشان نمی‌دهد.

| پلتفرم | روش | الزام |
|---|---|---|
| **Windows** | ‏Win32 (`EnumWindows`، `BM_CLICK`) از طریق PowerShell | هیچ |
| **macOS** | اسکریپت‌نویسی UI با System Events از طریق `osascript -l JavaScript`: پنجره‌ها و sheetهای dialog ویرایشگر، متن‌ها و دکمه‌هایشان | **مجوز Accessibility** برای اپلیکیشنی که CLI را اجرا می‌کند (ترمینال یا اپلیکیشن عامل هوش مصنوعی شما): System Settings → Privacy & Security → Accessibility. بدون آن `dialog` با همین راهنمایی شکست می‌خورد و بررسی exit-18 هیچ پنجره‌ای پیدا نمی‌کند. |
| **Linux** | گذرگاه دسترس‌پذیری AT-SPI از طریق یک helper با `python3` (PyGObject): پنجره‌های dialog و alert ویرایشگر (GTK، که `EditorUtility.DisplayDialog` یونیتی از آن استفاده می‌کند)، برچسب‌ها و دکمه‌های فشاری آن‌ها، که از طریق عمل `click` فشار داده می‌شوند. روی X11 و Wayland کار می‌کند | ‏`python3` همراه با PyGObject و typelib مربوط به AT-SPI (Debian/Ubuntu: `python3-gi gir1.2-atspi-2.0`؛ Fedora: `python3-gobject at-spi2-core`؛ Arch: `python-gobject at-spi2-core`) و یک گذرگاه دسترس‌پذیری در حال اجرا (`at-spi2-core`، که در GNOME، KDE و بیشتر desktopها در صورت نیاز اجرا می‌شود). CLI را در همان نشست desktop که ویرایشگر را اجرا می‌کند اجرا کنید. بسته غایب، گذرگاه غیرقابل دسترس یا نبود نشست گرافیکی باعث می‌شود `dialog` با راهنمایی‌ای که راه‌حل را نام می‌برد شکست بخورد، و بررسی exit-18 هیچ پنجره‌ای پیدا نمی‌کند. |

‏Windows پنجره‌های modal خود Unity را هم فهرست می‌کند، مانند "Missing signature" که Unity 6.6 پیش از import یک `.unitypackage` بدون امضا (هر بسته Asset Store) نشان می‌دهد: Unity متن و دکمه‌های آن‌ها را خودش رسم می‌کند، بنابراین فهرست فقط عنوان آن‌ها و یک دکمه، **Cancel**، را نشان می‌دهد که `--press Cancel` آن را به‌صورت کلید Escape می‌فرستد. هر پاسخ دیگری (Import Anyway) با کاربر است، در ویرایشگر.

شناسه یک dialog از فهرست می‌آید و تا زمانی که آن dialog باز است ثابت می‌ماند (Windows: handle پنجره آن؛ macOS و Linux: یک hash از pid، عنوان، متن و دکمه‌های آن). برچسب دکمه هرگز درون یک اسکریپت قرار نمی‌گیرد و فقط به‌عنوان داده فرستاده می‌شود.

روی یک ماشین Linux بدون رابط گرافیکی، ویرایشگر (بدون `-batchmode`) و CLI را زیر `Xvfb` درون یک `dbus-run-session` اجرا کنید، در حالی که `at-spi2-core` نصب است. اگر همان کاربر یک نشست desktop هم دارد، به آن نشست خصوصی `XDG_RUNTIME_DIR` جداگانه بدهید: در غیر این صورت گذرگاه دسترس‌پذیری آن، سوکت `at-spi` مربوط به desktop را جایگزین می‌کند (و هنگام خروج حذف می‌کند).

</div>

```bash
unity-mcp-cli dialog ./MyGame                          # list: title and buttons
unity-mcp-cli dialog ./MyGame --press "Don't Save"     # answer it
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی یا `--path`) |
| `--press <button>` | خیر | دکمه‌ای با این برچسب را فشار بده (بدون حساسیت به بزرگی و کوچکی حروف) |
| `--id <id>` | خیر | همراه با `--press`: dialogی که باید به آن پاسخ داد (از فهرست) |
| `--format <format>` | خیر | `text`، `json` یا `ndjson` |

## `commands`

همه دستورها همراه با آرگومان‌ها، گزینه‌ها و مقادیر پیش‌فرضشان، به‌علاوه جدول کدهای خروج — که از خود CLI خوانده می‌شود، بنابراین هرگز از واقعیت فاصله نمی‌گیرد. برای عامل‌ها و ابزارها در نظر گرفته شده است.

</div>

```bash
unity-mcp-cli commands --format json
```

<div dir="rtl">

## `wait-for-ready`

منتظر می‌ماند تا ویرایشگر Unity و سرور MCP متصل و آماده پذیرش فراخوانی‌های ابزار شوند. سرور را با فاصله زمانی قابل تنظیم polling می‌کند تا با موفقیت پاسخ دهد یا مهلت تمام شود. برای اسکریپت‌های خودکارسازی و هماهنگ‌سازی عامل‌های هوش مصنوعی مفید است، جایی که `open` یونیتی را اجرا می‌کند اما عامل باید بداند چه زمانی می‌تواند فراخوانی ابزارها را شروع کند.

</div>

```bash
unity-mcp-cli wait-for-ready ./MyGame
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی یا `--path`) — برای خواندن پیکربندی و تشخیص پورت استفاده می‌شود |
| `--url <url>` | خیر | بازنویسی مستقیم URL سرور (پیکربندی را دور می‌زند) |
| `--token <token>` | خیر | بازنویسی توکن Bearer (پیکربندی را دور می‌زند) |
| `--timeout <ms>` | خیر | حداکثر زمان انتظار به میلی‌ثانیه (پیش‌فرض: 120000) |
| `--interval <ms>` | خیر | فاصله polling به میلی‌ثانیه (پیش‌فرض: 3000) |
| `--format <format>` | خیر | `text` (پیش‌فرض)، `json` یا `ndjson` |

وقتی آماده باشد با `0` خارج می‌شود، با تمام شدن مهلت با `14`، و وقتی یک پنجره modal ویرایشگر را مسدود کرده باشد با `18`.

**مثال — انتظار با مهلت پیش‌فرض (120 ثانیه):**

</div>

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
```

<div dir="rtl">

**مثال — مهلت کوتاه‌تر برای CI:**

</div>

```bash
unity-mcp-cli wait-for-ready ./MyGame --timeout 60000 --interval 2000
```

<div dir="rtl">

**مثال — URL صریح سرور:**

</div>

```bash
unity-mcp-cli wait-for-ready --url http://localhost:8080 --timeout 30000
```

<div dir="rtl">

## `setup-mcp`

فایل‌های پیکربندی MCP را برای عامل‌های هوش مصنوعی می‌نویسد و راه‌اندازی headless/CI را بدون رابط کاربری ویرایشگر Unity ممکن می‌کند. از همه عامل‌هایی که ‏`setup-mcp --list` نشان می‌دهد (Claude Code، Cursor، Gemini، Codex و غیره) پشتیبانی می‌کند.

پیکربندی از طریق یک URL مخصوص هر پروژه (`https://ai-game.dev/mcp/p/<pin>` برای transport از نوع http، یا آرگومان `project=<pin>` برای stdio) به این پروژه **پین** می‌شود. برای transport ابری http، همچنین **کلید پروژه** این پروژه (`Authorization: Bearer agd_pk_…`) را برای هر عامل در خود دارد: یک اعتبارنامه بدون انقضا و قابل لغو که فقط به این پروژه وابسته است، که از `~/.ai-game-dev/project-keys.json` دوباره استفاده می‌شود یا با حسابی که با [`login`](#login) وارد شده‌اید صادر می‌شود. بدون ورود (یا اگر کلید به‌دست نیاید) پیکربندی فقط شامل URL است و عامل با OAuth خودش وارد می‌شود. `--oauth` را بدهید تا همیشه پیکربندی فقط-URL نوشته شود، `--regenerate-key` را برای جایگزینی کلید (پیکربندی‌های دیگر عامل‌های این پروژه که کلید قبلی را دارند به کلید جدید منتقل و چاپ می‌شوند، سپس کلید قبلی لغو می‌شود؛ اگر هر کدام از آن‌ها قابل بازنویسی نباشد، کلید قبلی فعال باقی می‌ماند و یک هشدار نام آن را می‌برد)، `--token` را برای نوشتن یک PAT صریح به‌جای آن، یا `--no-pin` را برای endpoint مشترک و بدون پین. پیکربندی‌های stdio و سرور محلی هرگز کلید پروژه ندارند. Antigravity بسته به نصب، یا `~/.gemini/config/mcp_config.json` یا `~/.gemini/antigravity/mcp_config.json` را می‌خواند، بنابراین هر دو فایل نوشته می‌شوند و هر مسیر نوشته‌شده چاپ می‌شود؛ اگر هر کدام از آن‌ها قابل نوشتن نباشد، دستور شکست می‌خورد. این خروجی با دکمه **Configure** ویرایشگر Unity مطابقت دارد.

</div>

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[agent-id]` | بله | عاملی که پیکربندی می‌شود (برای دیدن همه از `--list` استفاده کنید) |
| `[path]` | خیر | مسیر پروژه Unity (پیش‌فرض: cwd) |
| `--transport <transport>` | خیر | روش transport: `stdio` یا `http` (پیش‌فرض: `http`) |
| `--url <url>` | خیر | بازنویسی URL سرور (برای transport از نوع http) |
| `--token <token>` | خیر | PAT صریح — به‌جای کلید پروژه به‌عنوان هدر `Authorization` نوشته می‌شود |
| `--oauth` | خیر | نوشتن یک پیکربندی Cloud فقط-URL (عامل با OAuth خودش وارد می‌شود) به‌جای کلید پروژه |
| `--regenerate-key` | خیر | صدور یک کلید پروژه تازه، بازنویسی پیکربندی و لغو کلید قبلی (فقط Cloud http) |
| `--no-pin` | خیر | نوشتن یک URL بدون پین / حذف آرگومان `project=` (پیش‌فرض: پین به این پروژه از طریق `/mcp/p/<pin>`) |
| `--list` | خیر | فهرست همه شناسه‌های عامل موجود |

**مثال — فهرست همه عامل‌های پشتیبانی‌شده:**

</div>

```bash
unity-mcp-cli setup-mcp --list
```

<div dir="rtl">

**مثال — پیکربندی Cursor با transport از نوع stdio:**

</div>

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

<div dir="rtl">

**مثال — جایگزینی کلید این پروژه (کلید قدیمی دیگر کار نمی‌کند):**

</div>

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --regenerate-key
```

<div dir="rtl">

**مثال — نوشتن یک پیکربندی بدون پین (endpoint مشترک `/mcp`):**

</div>

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --no-pin
```

<div dir="rtl">

## `setup-skills`

فایل‌های skill را برای یک عامل هوش مصنوعی می‌نویسد. همیشه، به‌صورت آفلاین (بدون ویرایشگر، بدون سرور): skill با نام `unity-mcp-cli`، یعنی اینکه یک عامل چگونه از یک شروع سرد از این CLI استفاده کند (اولین دستور ویرایشگر یک ویرایشگر headless اجرا می‌کند؛ دستورالعمل‌های `rql`؛ `compile` / `test`؛ `--format json`؛ کدهای خروج و `dialog`)، همراه با `references/rql.md` و `references/install.md` برای جزئیات بیشتر، به‌علاوه `ai-game-dev-app`. دستورالعمل‌ها از همان منبع `--help` می‌آیند، بنابراین این دو نمی‌توانند از هم فاصله بگیرند. سپس، وقتی ویرایشگر پروژه پاسخ دهد، skillهای مخصوص هر ابزار را هم از پلاگین می‌خواهد؛ بسته بودن ویرایشگر خطا محسوب نمی‌شود.

</div>

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[agent-id]` | بله | عاملی که skillها برای آن ساخته می‌شوند (برای دیدن همه از `--list` استفاده کنید) |
| `[path]` | خیر | مسیر پروژه Unity (پیش‌فرض: cwd) |
| `--url <url>` | خیر | بازنویسی URL سرور |
| `--token <token>` | خیر | بازنویسی توکن احراز هویت |
| `--list` | خیر | فهرست همه عامل‌ها همراه با وضعیت پشتیبانی از skillها |
| `--offline` | خیر | فقط skill با نام `unity-mcp-cli` را بنویس؛ از ویرایشگر نپرس |
| `--timeout <ms>` | خیر | مهلت درخواست به میلی‌ثانیه (پیش‌فرض: 60000) |

**مثال — فهرست عامل‌ها همراه با پشتیبانی از skillها:**

</div>

```bash
unity-mcp-cli setup-skills --list
```

<div dir="rtl">

## `remove-plugin`

پلاگین Unity-MCP را از `Packages/manifest.json` یک پروژه Unity حذف می‌کند.

</div>

```bash
unity-mcp-cli remove-plugin ./MyGame
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | بله | مسیر پروژه Unity (موقعیتی یا `--path`) |

این دستور:
1. ‏`com.ivanmurzak.unity.mcp` را از `dependencies` حذف می‌کند
2. **رجیستری‌های scoped و scopeها را حفظ می‌کند** — ممکن است بسته‌های دیگر به آن‌ها وابسته باشند
3. اگر پلاگین نصب نباشد **هیچ کاری انجام نمی‌دهد**

> پس از اجرای این دستور، پروژه را در ویرایشگر Unity باز کنید تا تغییر اعمال شود.

## `status`

وضعیت اتصال ویرایشگر Unity و سرور MCP را بررسی می‌کند. نشان می‌دهد آیا Unity در حال اجراست، آیا سرور MCP محلی قابل دسترس است، و آیا سروری که از پیکربندی resolve شده (مثلاً ابری) قابل دسترس است.

</div>

```bash
unity-mcp-cli status ./MyGame
```

<div dir="rtl">

| گزینه | الزامی | توضیحات |
|---|---|---|
| `[path]` | خیر | مسیر پروژه Unity (موقعیتی یا `--path`) |
| `--url <url>` | خیر | بازنویسی مستقیم URL سرور (پیکربندی را دور می‌زند) |
| `--token <token>` | خیر | بازنویسی توکن Bearer (پیکربندی را دور می‌زند) |
| `--timeout <ms>` | خیر | مهلت بررسی به میلی‌ثانیه (پیش‌فرض: 5000) |
| `--format <format>` | خیر | `text` (پیش‌فرض)، `json` یا `ndjson` |

وقتی سرور MCP پاسخ دهد با `0` خارج می‌شود، وقتی پاسخ ندهد با `7`، و وقتی یک پنجره modal ویرایشگر را مسدود کرده باشد با `18` (در خروجی فهرست می‌شود؛ [`dialog`](#dialog) را ببینید).

**نمونه خروجی:**

</div>

```
Unity-MCP Status
  Project: /path/to/MyGame
──────────────────────────────────────────────────
Unity Editor Process
✔  Unity is running (PID: 53740)
Local MCP Server
  URL: http://localhost:22958
✖  Not available (connection refused)
Config Server
  URL: https://ai-game.dev/mcp
✔  Connected
──────────────────────────────────────────────────
✔  MCP server is reachable — ready for tool calls
```

<div dir="rtl">

## Global Options

این گزینه‌ها روی همه دستورها در دسترس هستند:

| گزینه | توضیحات |
|---|---|
| `-v, --verbose` | فعال کردن خروجی تشخیصی مفصل برای عیب‌یابی |
| `--version` | نمایش نسخه CLI |
| `--help` | نمایش راهنمای دستور |

**مثال — اجرای هر دستور با خروجی مفصل:**

</div>

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

<div dir="rtl">

## Exit Codes and Machine-Readable Output

‏`unity-mcp-cli`، `unreal-mcp-cli` و `godot-cli` یک جدول کد خروج مشترک دارند، بنابراین یک اسکریپت یا عامل می‌تواند بدون خواندن پیام‌ها تشخیص دهد که «خط فرمان را اصلاح کن» با «ویرایشگر شکست خورد» و با «تست‌ها رد شدند» فرق دارد:

| کد | نام | معنا |
|---|---|---|
| `0` | `SUCCESS` | دستور موفق بود |
| `1` | `ERROR` | خطای طبقه‌بندی‌نشده |
| `2` | `USAGE` | آرگومان‌های نادرست، ابزار ناشناخته؛ هیچ چیز اجرا نشد. خط فرمان را اصلاح کنید — آن را بدون تغییر دوباره امتحان نکنید |
| `6` | `COMMAND_FAILED` | دستور اجرا شد و شکست خورد (خطای ابزار، خطاهای کامپایل، اجرای تست بدون حکم، اعتبارنامه‌های ردشده) |
| `7` | `UNREACHABLE` | ویرایشگر یا سرور MCP قابل اجرا یا دسترسی نبود؛ پس از اصلاح محیط دوباره امتحان کنید |
| `8` | `TESTS_FAILED` | تست‌ها اجرا شدند و برخی رد شدند |
| `14` | `TIMEOUT` | پیش از رسیدن نتیجه مهلت تمام شد |
| `17` | `BUSY` | ویرایشگر مشغول است و فراخوانی را اجرا نکرد؛ بعداً دوباره امتحان کنید |
| `18` | `AWAITING_INPUT` | یک پنجره modal ویرایشگر را مسدود کرده است؛ یک انسان باید به آن پاسخ دهد ([`dialog`](#dialog) را ببینید) |

‏`--format json` (روی `run-tool`، `run-system-tool`، `exec`، `test`، `compile`، `dialog`، `status`، `wait-for-ready` و `commands`) دقیقاً یک envelope نتیجه روی stdout چاپ می‌کند؛ هر چیز دیگری به stderr می‌رود. `--format ndjson` همان envelope را در یک خط چاپ می‌کند و خطوط پیشرفت (`{"type":"progress",...}`) را روی stderr استریم می‌کند. یک خطای خط فرمان تحت `--format json` همچنان یک envelope (با کد `USAGE`) چاپ می‌کند.

</div>

```json
{
  "success": false,
  "command": "test",
  "data": { "verdict": "failed", "summary": { "total": 3, "passed": 2, "failed": 1 } },
  "errors": [{ "code": "TESTS_FAILED", "message": "1 test(s) failed." }],
  "warnings": []
}
```

<div dir="rtl">

‏`errors[].code` یک توکن پایدار است (`USAGE`، `NOT_FOUND`، `NOT_AUTHENTICATED`، `TOOL_FAILED`، `UNREACHABLE`، `TIMEOUT`، `BUSY`، `AWAITING_INPUT`، `TESTS_FAILED`، `TEST_RUN_ERROR`، `TEST_TIMED_OUT`، `COMPILATION_FAILED`، `PROJECT_OPEN`، `ERROR`، ...)؛ با آن تطبیق دهید، هرگز با پیام.

**فراخوانی ابزاری که موفق نبوده** (`run-tool`، `run-system-tool`، `exec`، `rql`، `tools`) در هر سه CLI به همان کد خروج و کد خطا نگاشت می‌شود (جدول در `@baizor/gamedev-cli-core` قرار دارد؛ `commands --format json` آن را به‌صورت `errorCodes` فهرست می‌کند). وقتی سرور MCP پاسخ داده باشد، وضعیت HTTP آن تعیین‌کننده است و خطای envelope آن را در خود دارد: `errors[{code, message, httpStatus}]`.

| شرط | وضعیت HTTP | خروج | `errors[].code` |
|---|---|---|---|
| آرگومان‌های نادرست: یک پرچم نادرست، آرگومان‌های ابزار که CLI یا سرور رد کرده‌اند (هر 4xx که این‌جا فهرست نشده) | 400 (یک 422 `[invalid_params]`) | `2` | `USAGE` |
| ابزاری با آن نام وجود ندارد (با `tools` فهرستشان کنید)، یا پروژه‌ای در آن مسیر نیست | 404 | `2` | `NOT_FOUND` |
| اعتبارنامه قابل استفاده‌ای نیست، یا سرور آن را رد کرد: با `login` وارد شوید | 401, 403 | `6` | `NOT_AUTHENTICATED` |
| ابزار اجرا شد و خطا گزارش کرد (`status: "error"`، هر 5xx که این‌جا فهرست نشده) | 409, 422, 500 | `6` | `TOOL_FAILED` |
| کد پروژه build نشد، بنابراین هیچ ویرایشگری اجرا نشد | — | `6` | `COMPILATION_FAILED` |
| اتصال رد یا reset شد، هیچ پلاگین موتوری به سرور وصل نیست، پلاگین پس از ارسال فراخوانی از دست رفت | 502 | `7` | `UNREACHABLE` |
| پاسخی به‌موقع نرسید: `--timeout`، یا مهلت ابزار خود سرور | 408, 504 | `14` | `TIMEOUT` |
| مشغول بود و فراخوانی را اجرا نکرد (در حال reload، محدودیت نرخ، فراخوانی CLI دیگری ویرایشگر را در اختیار دارد)؛ بعداً دوباره امتحان کنید | 503, 429 | `17` | `BUSY` |
| یک پنجره modal ویرایشگر را مسدود کرده است | — | `18` | `AWAITING_INPUT` |
| هر چیز دیگر (فراخوانی لغو شد، یک خطای غیرمنتظره) | — | `1` | `ERROR` |

# Full Automation Example

یک پروژه کامل Unity MCP را از صفر در یک اسکریپت راه‌اندازی کنید:

</div>

```bash
# 1. Create a new Unity project
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Install the Unity-MCP plugin
unity-mcp-cli install-plugin ./MyAIGame

# 3. Enable all MCP tools
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Sign in to ai-game.dev (browser OAuth; credential saved to ~/.ai-game-dev)
unity-mcp-cli login

# 5. Open the project (auto-connects and generates skills for claude-code)
unity-mcp-cli open ./MyAIGame

# 6. Wait for Unity Editor and MCP server to be ready
unity-mcp-cli wait-for-ready ./MyAIGame

# 7. Check that the scripts compile, then close the editor
unity-mcp-cli compile ./MyAIGame
unity-mcp-cli close ./MyAIGame

# 8. Run the tests in a batch-mode editor (no plugin needed); JUnit for the CI dashboard
unity-mcp-cli test ./MyAIGame --junit-output junit.xml
```

<div dir="rtl">

# How It Works

### پورت قطعی

CLI برای هر پروژه Unity بر اساس مسیر پوشه آن یک **پورت قطعی** تولید می‌کند (hash از نوع SHA256 که به بازه پورت 20000–29999 نگاشت می‌شود). این با تولید پورت در پلاگین Unity مطابقت دارد و تضمین می‌کند سرور و پلاگین بدون پیکربندی دستی به‌طور خودکار روی یک پورت توافق کنند.

### نصب پلاگین

دستور `install-plugin` فایل `Packages/manifest.json` را مستقیماً تغییر می‌دهد:
- رجیستری scoped در [OpenUPM](https://openupm.com/) (`package.openupm.com`) را اضافه می‌کند
- همه scopeهای لازم (`com.ivanmurzak`، `extensions.unity`) را ثبت می‌کند
- وابستگی `com.ivanmurzak.unity.mcp` را با به‌روزرسانی‌های آگاه از نسخه اضافه می‌کند (هرگز به نسخه پایین‌تر برنمی‌گرداند)

### فایل پیکربندی

دستور `configure` فایل `UserSettings/AI-Game-Developer-Config.json` را می‌خواند و می‌نویسد که این موارد را کنترل می‌کند:
- **ابزارها** — ابزارهای MCP در دسترس عامل‌های هوش مصنوعی
- **پرامپت‌ها** — پرامپت‌های از پیش تعریف‌شده که در گفتگوهای LLM تزریق می‌شوند
- **منابع** — داده‌های فقط‌خواندنی که در اختیار عامل‌های هوش مصنوعی قرار می‌گیرند
- **تنظیمات اتصال** — URL میزبان، توکن احراز هویت، روش transport، مهلت‌ها

### یکپارچگی با Unity Hub

دستورهایی که ویرایشگرها را مدیریت می‌کنند یا پروژه می‌سازند از **Unity Hub CLI** (حالت `--headless`) استفاده می‌کنند. اگر Unity Hub نصب نباشد، CLI **آن را به‌طور خودکار دانلود و نصب می‌کند**:
- **Windows** — نصب بی‌صدا از طریق `UnityHubSetup.exe /S` (ممکن است به دسترسی administrator نیاز داشته باشد)
- **macOS** — فایل DMG را دانلود و mount می‌کند و `Unity Hub.app` را در `/Applications` کپی می‌کند
- **Linux** — فایل `UnityHub.AppImage` را در `~/Applications/` دانلود می‌کند

# Library API

علاوه بر فایل اجرایی CLI، `unity-mcp-cli` دستورهای اصلی خود را به‌صورت یک کتابخانه نوع‌دار و بدون اثر جانبی در اختیار می‌گذارد، تا ابزارهای دیگر Node.js / TypeScript بتوانند همان روند نصب / پیکربندی را بدون فراخوانی shell در خود جای دهند.

</div>

```ts
import { installPlugin, removePlugin, configure, setupMcp } from 'unity-mcp-cli';

const result = await installPlugin({
  unityProjectPath: './MyUnityProject',
  // version: 'x.y.z',        // optional — defaults to latest from OpenUPM
  onProgress: (event) => {
    // phase is one of: 'start' | 'dependencies-resolved' | 'manifest-patched' | 'done'
    console.log(event.phase, event.message);
  },
});

if (!result.success) {
  console.error('Install failed:', result.error?.message);
  return;
}

console.log(`Installed v${result.installedVersion}`);
for (const warning of result.warnings) console.warn(warning);
for (const step of result.nextSteps) console.log(step);
```

<div dir="rtl">

هر تابع یک شیء نتیجه نوع‌دار `{ success, ... }` برمی‌گرداند؛ خطاها هرگز از مرز عمومی بیرون پرتاب نمی‌شوند. ورودی کتابخانه هیچ اثر جانبی در سطح بالا ندارد — `import 'unity-mcp-cli'` هرگز argv را تجزیه نمی‌کند و هرگز در stdout یا stderr نمی‌نویسد.

برای فهرست کامل توابع و نوع‌های export‌شده، [`CHANGELOG.md`](../CHANGELOG.md) را ببینید.

</div>
