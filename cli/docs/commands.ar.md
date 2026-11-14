<div dir="rtl">

[English](commands.md) | [中文](commands.zh-CN.md) | [日本語](commands.ja.md) | [Español](commands.es.md) | [فارسی](commands.fa.md) | [हिन्दी](commands.hi.md) | <b>العربية</b>

# مرجع unity-mcp-cli

كل أوامر [`unity-mcp-cli`](../README.md) وخياراته، ورموز الخروج، والمخرجات القابلة للقراءة آليًا، وواجهة المكتبة البرمجية.
شغّل `unity-mcp-cli <command> --help` للحصول على التفاصيل نفسها في الطرفية.

**الأوامر:** [`configure`](#configure) · [`create-project`](#create-project) · [`update`](#update) · [`install-extension`](#install-extension) · [`install-plugin`](#install-plugin) · [`install-unity`](#install-unity) · [`login`](#login) · [`unity-account`](#unity-account) · [`open`](#open) · [`close`](#close) · [`tools`](#tools) · [`rql`](#rql) · [`run-tool`](#run-tool) · [`exec`](#exec) · [`test`](#test) · [`compile`](#compile) · [`dialog`](#dialog) · [`commands`](#commands) · [`wait-for-ready`](#wait-for-ready) · [`setup-mcp`](#setup-mcp) · [`setup-skills`](#setup-skills) · [`remove-plugin`](#remove-plugin) · [`status`](#status)

**أيضًا:** [الخيارات العامة](#الخيارات-العامة) · [رموز الخروج والمخرجات القابلة للقراءة آليًا](#رموز-الخروج-والمخرجات-القابلة-للقراءة-آليًا) · [مثال أتمتة كامل](#مثال-أتمتة-كامل) · [كيف يعمل](#كيف-يعمل) · [واجهة المكتبة البرمجية](#واجهة-المكتبة-البرمجية-v0670)

# الأوامر

## `configure`

اضبط أدوات MCP والموجّهات والموارد في `UserSettings/AI-Game-Developer-Config.json`.

</div>

```bash
unity-mcp-cli configure ./MyGame --list
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | نعم | مسار مشروع Unity (موضعي أو `--path`) |
| `--list` | لا | سرد الإعداد الحالي ثم الخروج |
| `--enable-tools <names>` | لا | إظهار أدوات محددة لعملاء MCP (مفصولة بفواصل) |
| `--disable-tools <names>` | لا | إخفاء أدوات محددة عن عملاء MCP (مفصولة بفواصل؛ الأداة المخفية تظل تعمل عند استدعائها بالاسم) |
| `--enable-all-tools` | لا | إظهار كل الأدوات |
| `--disable-all-tools` | لا | إخفاء كل الأدوات |
| `--enable-prompts <names>` | لا | إظهار موجّهات محددة لعملاء MCP (مفصولة بفواصل) |
| `--disable-prompts <names>` | لا | إخفاء موجّهات محددة عن عملاء MCP (مفصولة بفواصل؛ الأداة المخفية تظل تعمل عند استدعائها بالاسم) |
| `--enable-all-prompts` | لا | إظهار كل الموجّهات |
| `--disable-all-prompts` | لا | إخفاء كل الموجّهات |
| `--enable-resources <names>` | لا | إظهار موارد محددة لعملاء MCP (مفصولة بفواصل) |
| `--disable-resources <names>` | لا | إخفاء موارد محددة عن عملاء MCP (مفصولة بفواصل؛ الأداة المخفية تظل تعمل عند استدعائها بالاسم) |
| `--enable-all-resources` | لا | إظهار كل الموارد |
| `--disable-all-resources` | لا | إخفاء كل الموارد |

**مثال — تفعيل أدوات محددة وتعطيل كل الموجّهات:**

</div>

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools screenshot-scene-view,script-read \
  --disable-all-prompts
```

<div dir="rtl">

**مثال — تفعيل كل شيء:**

</div>

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

<div dir="rtl">

## `create-project`

أنشئ مشروع Unity جديدًا باستخدام محرر Unity.

</div>

```bash
unity-mcp-cli create-project /path/to/new/project
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | نعم | المسار الذي سيُنشأ فيه المشروع (موضعي أو `--path`) |
| `--unity <version>` | لا | إصدار محرر Unity المراد استخدامه (الافتراضي هو أعلى إصدار مثبّت) |

**مثال — إنشاء مشروع بإصدار محرر محدد:**

</div>

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

<div dir="rtl">

## `update`

حدّث كل شيء: إضافة MCP والإضافات المثبّتة في مشروع Unity إلى أحدث إصداراتها، ثم واجهة CLI نفسها.

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

خارج أي مشروع، يحدّث واجهة CLI فقط. تُترك التثبيتات من `file:` أو git كما هي ولا يُخفَّض إصدار أي شيء.

## `install-extension`

ثبّت إحدى إضافات Unity-MCP — حزم الأدوات الاختيارية التي تضيف أدوات MCP لكلٍّ من Animation وCinemachine وInput System وNavigation وParticle System وProBuilder وSplines وTerrain وTilemap وTimeline — في ملف `Packages/manifest.json` لمشروع Unity. هذا هو مكافئ سطر الأوامر لقسم **Extensions** في نافذة المحرر *AI Game Developer*، ويثبّت من الفهرس المشترك نفسه.

</div>

```bash
# See what is installable (no project and no network required)
unity-mcp-cli install-extension --list

# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-extension Tilemap
```

<div dir="rtl">

| الوسيط / الخيار | مطلوب | الوصف |
|---|---|---|
| `[id]` | نعم (ما لم يُستخدم `--list`) | الإضافة المراد تثبيتها — معرّف حزمة OpenUPM (`com.ivanmurzak.unity.mcp.tilemap`) أو الاسم في الفهرس (`Tilemap`). المطابقة لا تميّز حالة الأحرف. المعرّف غير المعروف يسرد كل الإضافات القابلة للتثبيت كي تصحّح بنفسك. |
| `[path]` | لا | مسار مشروع Unity (موضعي أو `--path`). الافتراضي هو المجلد الحالي، ويُتحقق منه بالطريقة نفسها كما في `install-plugin`. |
| `--extension-version <version>` | لا | إصدار الإضافة المراد تثبيته (الافتراضي هو الأحدث من OpenUPM). **ليس `--version`** — فذلك علم الإصدار العام لواجهة CLI نفسها. |
| `--list` | لا | سرد الإضافات القابلة للتثبيت ثم الخروج. |

هذا الأمر:
1. يضيف **سجل OpenUPM المحدّد النطاق (scoped registry)** مع كل النطاقات المطلوبة (النطاق `com.ivanmurzak` هو ما يجعل الإضافة قابلة للحل أصلًا)
2. يضيف معرّف حزمة الإضافة إلى `dependencies`، مع حل قيمة `latest` من OpenUPM وقت التثبيت
3. **لا يخفّض الإصدار أبدًا** عند الحل التلقائي للإصدار — إذا كان إصدار أعلى مثبّتًا بالفعل فإنه يُحفظ وتُطبع رسالة تحذير. مرّر `--extension-version` لفرض قيمة محددة، بما في ذلك التخفيض.
4. **متساوي الأثر (idempotent)** — إعادة التشغيل التي تجد الإضافة محدّثة بالفعل تُبلغ بذلك ولا تكتب شيئًا

**مثال — تثبيت إصدار محدد من إضافة:**

</div>

```bash
unity-mcp-cli install-extension Cinemachine ./MyGame --extension-version 1.0.17
```

<div dir="rtl">

**مثال — داخل العملية، من المكتبة:**

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

> بعد تشغيل هذا الأمر، افتح محرر Unity (أو انتقل إليه) كي يحل Package Manager الاعتمادية الجديدة.

## `install-plugin`

ثبّت إضافة Unity-MCP في ملف `Packages/manifest.json` لمشروع Unity. ويمكنك اختياريًا تنزيل الملف التنفيذي لخادم MCP المطابق لمعرّف وقت التشغيل (RID) واسترداد رمز تسجيل للفريق في التشغيل نفسه.

</div>

```bash
# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-plugin
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | لا | مسار مشروع Unity (موضعي أو `--path`). الافتراضي هو المجلد الحالي؛ ويُتحقق من أن المجلد الناتج مشروع Unity حقيقي (العلامة `Packages/manifest.json`)، وعند الإخفاق تسرد رسالة الخطأ بالضبط ما فُحص. |
| `--plugin-version <version>` | لا | إصدار الإضافة المراد تثبيته (الافتراضي هو الأحدث من [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)) |
| `--with-server` | لا | تنزيل الملف التنفيذي لـ [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) المطابق لـ RID أيضًا إلى المجلد الذي تديره CLI |
| `--server-version <version>` | لا | إصدار الخادم المراد تنزيله مع `--with-server` (الافتراضي: إصدار الخادم المثبّت في CLI) |
| `--server-source <path-or-url>` | لا | تجاوز للعمل دون اتصال أو في CI — تثبيت الخادم من مسار zip محلي أو عنوان URL (يتخطى التحقق من المجموع الاختباري) |
| `--enroll <code>` | لا | استرداد رمز تسجيل للحصول على بيانات اعتماد للإضافة (تُزرع في مخزن الجهاز المشترك وتُثبَّت لهذا المشروع) |
| `--enroll-stdin` | لا | قراءة رمز التسجيل من stdin بدلًا من argv (يبقيه خارج سجل الصدفة) |

هذا الأمر:
1. يضيف **سجل OpenUPM المحدّد النطاق** مع كل النطاقات المطلوبة
2. يضيف `com.ivanmurzak.unity.mcp` إلى `dependencies`
3. **لا يخفّض الإصدار أبدًا** — إذا كان إصدار أعلى مثبّتًا بالفعل فإنه يُحفظ

**مثال — تثبيت إصدار محدد من الإضافة:**

</div>

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

<div dir="rtl">

**مثال — استرداد رمز تسجيل للفريق (المسار الافتراضي هو المجلد الحالي):**

</div>

```bash
cd ./MyGame && unity-mcp-cli install-plugin --enroll <code>
```

<div dir="rtl">

> بعد تشغيل هذا الأمر، افتح المشروع في محرر Unity لإكمال تثبيت الحزمة.

## `install-unity`

ثبّت إصدارًا من محرر Unity عبر Unity Hub CLI.

</div>

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

<div dir="rtl">

| الوسيط / الخيار | مطلوب | الوصف |
|---|---|---|
| `[version]` | لا | إصدار محرر Unity المراد تثبيته (مثلًا `6000.3.1f1`) |
| `--path <path>` | لا | قراءة الإصدار المطلوب من مشروع موجود |

إذا لم يُقدَّم الوسيط ولا الخيار، يثبّت الأمر أحدث إصدار مستقر من قائمة إصدارات Unity Hub.

**مثال — تثبيت إصدار المحرر الذي يحتاجه مشروع:**

</div>

```bash
unity-mcp-cli install-unity --path ./MyGame
```

<div dir="rtl">

## `login`

سجّل الدخول إلى **ai-game.dev** وخزّن بيانات الاعتماد في مخزن بيانات الاعتماد المشترك للجهاز (`~/.ai-game-dev/credentials.json`). يشغّل **تدفق الجهاز OAuth 2.1** عبر المتصفح — لا يوجد رمز وصول شخصي عليك إنشاؤه أو لصقه. تسجيل الدخول يفوّض الجهاز بأكمله: فهو يصدر بيانات اعتماد **للوكيل** ويشتق منها بيانات اعتماد لأدوات المحرك، وكل مستهلك (أوامر CLI السحابية، وإضافة محرر Unity، وتطبيق سطح المكتب) يجددها تلقائيًا — تتبدّل الرموز في الخلفية، لذا تسجّل الدخول مرة واحدة لكل جهاز، لا مرة لكل جلسة.

</div>

```bash
unity-mcp-cli login
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `--project <path>` | لا | تخزين بيانات الاعتماد في مخزن محلي للمشروع (`<path>/.ai-game-dev/`) بدلًا من مخزن الجهاز المشترك |
| `--force` | لا | إعادة المصادقة حتى لو كانت بيانات الاعتماد موجودة بالفعل |
| `--tools-only` | لا | تفويض **أدوات المحرك فقط** (لا تُخزَّن بيانات اعتماد للوكيل، لذا يستحيل أن يلتقطها تطبيق سطح المكتب). مخصص لمشغّلات CI والأتمتة، التي تظهر حينها كمجموعة أجهزة مستقلة قابلة للإلغاء |
| `--yes` | لا | افتراض "نعم" في المطالبات — مطلوب لتأكيد **تبديل الحساب** دون تفاعل (تسجيل الدخول بحساب مختلف عن الحساب الذي يحمله الجهاز حاليًا) |

يطبع الأمر رمز مستخدم قصيرًا وعنوان URL للتحقق، ويفتح متصفحك، ثم ينتظر بالاستطلاع حتى توافق على تسجيل الدخول. إذا كانت بيانات الاعتماد موجودة بالفعل فإنه يخرج مبكرًا برسالة *"Already signed in"* — مرّر `--force` لاستبدالها. تسجيل الدخول **بحساب مختلف** عن الحساب الذي يحمله الجهاز يطلب التأكيد أولًا (الرفض يترك كل شيء دون تغيير)؛ مرّر `--yes` للتأكيد في السكربتات.

**مثال — تسجيل الدخول (مخزن الجهاز المشترك):**

</div>

```bash
unity-mcp-cli login
```

<div dir="rtl">

**مثال — مشغّل CI، أدوات المحرك فقط:**

</div>

```bash
unity-mcp-cli login --tools-only
```

<div dir="rtl">

**مثال — تخزين بيانات الاعتماد بجوار مشروع محدد:**

</div>

```bash
unity-mcp-cli login --project ./MyGame
```

<div dir="rtl">

## `unity-account`

اعرض **حساب Unity** الذي سجّل محرر Unity قيد التشغيل الدخول به، وابدأ تسجيل الدخول إليه. تحتاجه قائمة
‏"My Assets" في Asset Store (مصدر RQL `assetStore`)؛ أما رخصة Personal فتعمل بدونه، لذا يمكن تشغيل المحرر
دون تسجيل دخول. هذا ليس حساب ai-game.dev: فذلك هو [`login`](#login).

يفتح `--sign-in` نافذة تسجيل الدخول في Unity داخل ذلك المحرر؛ ويكملها **المستخدم** هناك، عبر Unity Hub. المحرر المفتوح
مباشرةً (لا عبر Hub، مثلًا بإصدار أقدم من `open`) لا يستطيع تسجيل الدخول: فهو يعرض فقط "Install Unity Hub"، لذا
تقول الإجابة بإعادة فتح المشروع بـ [`close`](#close) و[`open`](#open)، اللذين يشغّلانه عبر Hub. والمحرر المفتوح
بهذه الطريقة يأخذ الحساب الذي سجّل Hub الدخول به دون أي تسجيل دخول. لا تمرّ أي بيانات اعتماد عبر CLI. يحتاج الأمر إلى أن يكون المشروع مفتوحًا في محرر بنافذة
‏([`open`](#open)) ولا يشغّل أبدًا محررًا دون واجهة، لأنه لا يستطيع عرض نافذة تسجيل الدخول. يمكن للوكلاء تشغيل الاستعلامات نفسها بـ
‏`rql`: `assetStore | select signedIn userName` و`assetStore | call SignIn()`.

</div>

```bash
unity-mcp-cli unity-account ./MyGame            # signedIn|userName
unity-mcp-cli unity-account ./MyGame --sign-in  # opens Unity's sign-in for the user
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | لا | مسار مشروع Unity (موضعي أو `--path`؛ الافتراضي: المجلد الحالي) |
| `--sign-in` | لا | فتح نافذة تسجيل الدخول في Unity عندما لا يكون المحرر مسجّل الدخول (إذا كان مسجّلًا: يجدد رمز الوصول) |
| `--format <format>` | لا | `text` أو `json` أو `ndjson` |

يخرج بالرمز `0` مع إجابة المحرر، و`7` عندما لا يجيب أي محرر. الإضافة الأقدم من أن تدعم هذا الأمر تحصل على تلميح بتشغيل
‏[`update`](#update).

## `open`

افتح مشروع Unity في محرر Unity. افتراضيًا، يضبط متغيرات بيئة اتصال MCP إذا قُدّمت خيارات الاتصال. استخدم `--no-connect` للفتح دون اتصال MCP.

عندما يكون Unity Hub مثبّتًا، يُفتح المحرر عبره كما يفعل Hub نفسه (`-useHub -hubIPC`، بلا بيانات اعتماد:
يطلب المحرر جلسته من Hub، ويشغّل Hub في الخلفية إذا لم يكن قيد التشغيل). ثم يأخذ المحرر
حساب Unity الذي سجّل Hub الدخول به، والذي تحتاجه قائمة "My Assets" في Asset Store (راجع
‏[`unity-account`](#unity-account)). أما المحرر دون واجهة (`--headless`) فيُفتح كما في السابق.

</div>

```bash
# Explicit path
unity-mcp-cli open ./MyGame

# From inside the Unity project folder — path defaults to the current directory
cd ./MyGame && unity-mcp-cli open
```

<div dir="rtl">

| الخيار | متغير البيئة | مطلوب | الوصف |
|---|---|---|---|
| `[path]` | — | لا | مسار مشروع Unity (موضعي أو `--path`). الافتراضي هو مجلد العمل الحالي. |
| `--unity <version>` | — | لا | إصدار محدد من محرر Unity (الافتراضي هو الإصدار من إعدادات المشروع، وإلا فأعلى إصدار مثبّت) |
| `--editor-path <path>` | — | لا | مسار صريح إلى الملف التنفيذي لمحرر Unity. يتخطى اكتشاف Unity Hub، ومفيد لمواقع التثبيت المخصصة. |
| `--no-connect` | — | لا | الفتح دون متغيرات بيئة اتصال MCP |
| `--url <url>` | `UNITY_MCP_CLOUD_URL` | لا | عنوان URL لخادم MCP المراد الاتصال به (ما زالت CLI تصدّر الاسم البديل القديم `UNITY_MCP_HOST`، الذي تقبله الإضافة أيضًا) |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | لا | فرض إبقاء الاتصال قائمًا |
| `--token <token>` | `UNITY_MCP_TOKEN` | لا | رمز المصادقة |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | لا | وضع المصادقة: `none` أو `oauth` أو `token` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | لا | قائمة أدوات مفصولة بفواصل لتفعيلها |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | لا | طريقة النقل: `streamableHttp` أو `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | لا | اضبطه على `true` أو `false` للتحكم في التشغيل التلقائي لخادم MCP |
| `--headless` | — | لا | تشغيل المحرر بلا نافذة (`-batchmode`؛ تبقى الرسوميات مفعّلة، لذا تظل الكاميرات تعرض) والانتظار حتى يجيب خادم MCP الخاص به. يتضمن ضمنًا `--keep-connected`. يُعاد استخدام محرر قيد التشغيل للمشروع؛ والمحرر الذي يُشغَّل هنا ملك لـ CLI (راجع [`close --owned-only`](#close))، والاستدعاءات المتوازية تشغّل محررًا واحدًا فقط. |
| `--idle-timeout <seconds>` | `AIGD_SESSION_IDLE_SECONDS` (headless) / `UNITY_MCP_IDLE_TIMEOUT` (window) | لا | إغلاق المحرر بعد هذا العدد من الثواني دون أي استدعاء أداة (يحفظ المشاهد والأصول المعدّلة أولًا) |
| `--ready-timeout <ms>` | — | لا | مع `--headless`: مدة انتظار جاهزية المحرر (الافتراضي: `300000`) |
| `--no-auto-dismiss-launch-errors` | — | لا | تعطيل الإغلاق التلقائي لنافذة "compile errors at launch" في محرر Unity (الافتراضي: مفعّل) |
| `--launch-dismiss-timeout-ms <ms>` | — | لا | المهلة الإجمالية (بالمللي ثانية) لحلقة استطلاع الإغلاق التلقائي لأخطاء التشغيل (الافتراضي: `30000`) |
| `--launch-dismiss-poll-interval-ms <ms>` | — | لا | الفاصل الزمني لنبضة الاستطلاع (بالمللي ثانية) لحلقة الإغلاق التلقائي لأخطاء التشغيل (الافتراضي: `1500`) |

بدون `--headless`، يُطلب من المحرر دون واجهة الذي شغّلته CLI للمشروع (`exec`، `open --headless`) أن يحفظ ويغلق أولًا، كي تُفتح النافذة.

تُنشأ عملية المحرر في وضع منفصل (detached). افتراضيًا، بعد إنشاء المحرر، يستطلع `open` ظهور نافذة "compile errors at launch" في Unity (`"Enter Safe Mode?"` على Unity 2020.2+، و`"Hold On" / "Compiler Errors"` على الإصدارات الأقدم) وينقر `Ignore` كي يُكمل المحرر تهيئته — فبدون ذلك، لا تستطيع أي أتمتة داخل المحرر تحتاج إلى العمل بعد حالة لا يستطيع فيها Unity نفسه الترجمة (مثل محلّل اعتماديات NuGet) أن تُصلح نفسها. تظهر النافذة بعد أن يُقلع Unity ويتصل بـ Package Manager ويبدأ الترجمة — تجريبيًا نحو 6 ثوانٍ على جهاز سريع وأطول على جهاز بطيء — لذا لحلقة الاستطلاع نافذة سماح تخرج بعدها مبكرًا إذا لم تُرَ أي نافذة. يجب أن تغطي نافذة السماح مرحلة بدء تشغيل Unity كاملة وإلا خرجت الحلقة قبل أن تظهر النافذة أصلًا (issue #737)؛ ولا تستغرق أبدًا كامل `--launch-dismiss-timeout-ms` في حالة عدم ظهور نافذة. إذا رُصدت النافذة (وأُغلقت بنجاح)، يستمر الاستطلاع حتى المهلة الإجمالية كي تُغلق مجددًا نافذة تعاود الظهور (المحلّل يصلح خطأً ← تعاود النافذة الظهور مع الخطأ التالي). يمكن للمستدعين في وضع المكتبة تمرير `AbortSignal` (`launchDismissAbortSignal` في `OpenProjectOptions`) لإيقاف الحلقة لحظة إطلاق إشارة الجاهزية الخاصة بهم.

### متطلبات الإغلاق التلقائي حسب المنصة

| المنصة | المتطلب | ملاحظات |
|---|---|---|
| **Windows** | مدمج (Win32 API) | يستخدم `EnumWindows` / `EnumChildWindows` / `SendMessageW(BM_CLICK)` من خلال PowerShell. لا حاجة إلى إعداد إضافي. |
| **macOS** | يجب منح **إذن Accessibility** للطرفية (أو للملف التنفيذي `unity-mcp-cli`). System Settings → Privacy & Security → Accessibility. | مُنفَّذ عبر AppleScript / `osascript`. بدون هذا الإذن، يُبلغ `osascript` عن خطأ في كل نبضة استطلاع ولا يمكن إغلاق النافذة. |
| **Linux/X11** | `xdotool` على `PATH` (مثلًا `sudo apt-get install xdotool`). | Wayland **غير** مدعوم في هذه النسخة الأولى — تابع المشكلات في المصدر الأصلي لدعم Wayland. |

لإلغاء الاشتراك كليًا، مرّر `--no-auto-dismiss-launch-errors`.

**مثال — الفتح مع اتصال MCP:**

</div>

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

<div dir="rtl">

**مثال — الفتح دون اتصال MCP (فتح بسيط):**

</div>

```bash
unity-mcp-cli open ./MyGame --no-connect
```

<div dir="rtl">

**مثال — الفتح مع المصادقة وأدوات محددة:**

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

أنهِ بلطف نسخة محرر Unity العاملة لمسار مشروع معيّن. هو النظير المتماثل لـ [`open`](#open) — لسير العمل المبرمج (وكلاء CI، ومنفّذو خطوط الأنابيب، وتجهيزات اختبارات التكامل) الذي يحتاج إلى إنهاء نظيف دون اللجوء إلى قتل العمليات على مستوى نظام التشغيل.

</div>

```bash
unity-mcp-cli close ./MyGame
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | لا | مسار مشروع Unity (موضعي، الافتراضي هو المجلد الحالي) |
| `--timeout <seconds>` | لا | مهلة الإنهاء اللطيف بالثواني (الافتراضي: 30) |
| `--force` | لا | قتل المحرر قسرًا إذا لم يخرج خلال `--timeout` |
| `--owned-only` | لا | إغلاق المحرر الذي شغّلته CLI فقط (`exec`، `open --headless`)؛ أما المحرر الذي فتحه المستخدم فلا يُمسّ أبدًا |

**كيف يعمل:**

0. يُسجَّل المحرر الذي شغّلته CLI في عقد جلسة المشروع (`.ai-game-dev/session.json`: معرّف العملية PID، ووقت بدء العملية، وقيمة nonce). إذا كان العقد يسمّي محررًا حيًا، يطلب `close` من إضافته أن تحفظ وتغلق (أداة النظام `editor-session-quit`، مع التحقق من nonce العقد، على عنوان الخادم الذي سجّله العقد عند تشغيل المحرر) — وهذا يصل أيضًا إلى المحرر دون واجهة، الذي لا يصله `WM_CLOSE` — ويتخطى الخطوات أدناه. مع `--owned-only` يتوقف هنا: إذا لم يوجد عقد، لا يُغلق شيء. إذا ظل ذلك المحرر يعمل بعد `--timeout` (دون `--force`) وكانت هناك نافذة مشروطة (modal) مفتوحة، يخرج `close` بالرمز 18 ويسمّيها، كما في الخطوة 3.
1. يحدد PID المحرر العامل بقراءة `<project>/Temp/UnityLockfile` (عدد uint32 من 4 بايتات بترتيب little-endian) ومقاطعته مع تعداد العمليات للتعامل مع ملفات القفل القديمة.
2. يرسل إشارة إنهاء لطيف — `SIGTERM` على Linux/macOS، و`taskkill` (دون `/F`) على Windows — ليتيح لـ Unity إنهاء الحفظ التلقائي واستيراد الأصول.
3. يستطلع كل 250ms حتى تخرج العملية أو تنقضي `--timeout`. وفي الأثناء يبحث عن نافذة مشروطة للمحرر (Windows وmacOS وLinux): فالإنهاء اللطيف مع وجود تغييرات غير محفوظة يفتح "Scene(s) Have Been Modified" [Save | Don't Save | Cancel]، ثم ينتظر المحرر شخصًا. بدون `--force`، يتوقف `close` بمجرد أن يرى فحصان متتاليان النافذة (بعد نحو 3 ثوانٍ من الإنهاء) برمز الخروج 18 ويسمّي النافذة وأزرارها (أجب عنها في المحرر أو بـ [`dialog --press`](#dialog))؛ ومع `--force` ينتظر حتى تنقضي `--timeout` قبل القتل القسري.
4. إذا انقضت المهلة وكان `--force` مضبوطًا، يلجأ إلى `SIGKILL` / `taskkill /F`.
5. متساوي الأثر — إغلاق محرر مغلق بالفعل (أو مشروع لم يكن محرره يعمل أصلًا) يخرج بالرمز 0 مع `no running Editor for project at <path>`.
6. يرفض العمل على أي مسار ليس جذر مشروع Unity (يجب أن يوجد `ProjectSettings/ProjectVersion.txt`) — حماية من الاستدعاءات العرضية التي تقتل كل نسخ Unity على المضيف.

> **تنبيه Windows دون واجهة:** تستخدم خطوة الإنهاء اللطيف `taskkill` (دون `/F`)، الذي يرسل `WM_CLOSE`. ولا تصل هذه الرسالة إلا إلى العمليات التي تملك نافذة علوية على **سطح المكتب/الجلسة نفسها** التي تعمل فيها CLI. إذا شُغّل Unity بواسطة خدمة Windows في الجلسة 0 (أو أي سطح مكتب آخر غير تفاعلي)، فستُسقط إشارة الإنهاء اللطيف بصمت، وستنقضي `--timeout`، ويصبح `--force` الطريق الوحيد لإيقاف المحرر. خطّط لذلك في مشغّلات CI دون واجهة. المحرر الذي شغّلته CLI (الخطوة 0) لا يتأثر: فهو يغلق عبر إضافته.

**مثال — الإغلاق، مع اللجوء إلى القسر بعد 60 ثانية:**

</div>

```bash
unity-mcp-cli close ./MyGame --timeout 60 --force
```

<div dir="rtl">

**مثال — إنهاء نظيف في نهاية سكربت أتمتة:**

</div>

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
unity-mcp-cli close ./MyGame
```

<div dir="rtl">

## `tools`

اسرد أدوات الذكاء الاصطناعي التي يوفرها محرر Unity الخاص بالمشروع — المشهد، والأصول، والاختبارات، ولقطات الشاشة، وكل ما تضيفه الإضافات
المثبّتة — مع ما تفعله كل أداة ومدخلاتها، أو اعرض أداة واحدة بالتفصيل (الوصف الكامل، والمدخلات، ومثال
استدعاء، ومخطط JSON لمدخلاتها). يشغّل محررًا دون واجهة إذا لم يكن أي محرر يعمل، مثل [`exec`](#exec). تُقرأ القائمة عبر MCP
‏(`tools/list`) وتُحفظ في المشروع (متجاهَلة في git)، لذا يجيب `--cached` فورًا بينما المحرر مغلق.
الأدوات المخفية (غير المعروضة لعملاء MCP) تُسرد بأسمائها؛ ويظل `run-tool` قادرًا على استدعائها.

</div>

```bash
unity-mcp-cli tools                 # every tool, one line each, with its inputs
unity-mcp-cli tools rql-mutate      # one tool: description, inputs, example call, schema
unity-mcp-cli tools screenshot      # a regex filter over names and descriptions
unity-mcp-cli tools --format json   # full schemas, machine-readable
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[name]` | لا | اسم أداة (للتفاصيل) أو تعبير نمطي (للتصفية) |
| `--path <path>` | لا | مسار مشروع Unity (الافتراضي: المجلد الحالي) |
| `--cached` | لا | الإجابة من القائمة المحفوظة دون الاتصال بالمحرر |
| `--no-start` | لا | عدم تشغيل محرر أبدًا؛ واستخدام القائمة المحفوظة عندما لا يجيب أي محرر |
| `--once` | لا | إغلاق المحرر بعد ذلك إذا كان هذا الاستدعاء هو من شغّله |
| `--ready-timeout <ms>` | لا | انتظار المحرر المشغَّل (الافتراضي: 300000) |
| `--url <url>` / `--token <token>` | لا | تجاوزات الخادم |
| `--format <format>` | لا | `text` أو `json` أو `ndjson` |

## `rql`

استعلام RQL واحد من سطر الأوامر، دون علامات اقتباس JSON. الاستعلام الذي تكون مرحلته الأخيرة `set` أو `call` يذهب إلى
‏`rql-mutate`، وأي استعلام آخر إلى `rql-query`؛ ويشغّل `--schema [type]` الأداة `rql-schema`. يشغّل محررًا دون واجهة مثل `run-tool`.
يحفظ Unity التغيير على القرص فورًا، لذا بعد `set` / `call` تطبع CLI ملفات المشاهد والأصول التي كتبها
‏(`Saved to disk: Assets/Scenes/Forest.unity`). المعامل المكتوب كما في لغة أخرى (`startsWith`، `like`) يحصل على
تلميح بالتهجئة الخاصة بـ RQL. ويسرد `rql --help` وصفات جاهزة.

</div>

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli rql 'objects | where name starts-with "Rock" | set transform.localScale = transform.localScale * 1.5'
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[query]` | نعم، ما لم يُستخدم `--schema` / `-f` | استعلام RQL (ضعه بين علامتي اقتباس مفردتين؛ والنصوص داخله بعلامات اقتباس مزدوجة) |
| `-f, --file <file>` | لا | قراءة الاستعلام من ملف (`-`: stdin)، للصدفة التي تحذف علامات الاقتباس الداخلية |
| `--schema [type]` | لا | سرد مصادر RQL، أو أعضاء نوع واحد |
| `--dry-run` | لا | لـ `set` / `call`: عرض ما سيتغير، دون تغيير أي شيء |
| `--max-targets <n>` | لا | لـ `set` / `call`: أكثر من 200 هدف (حتى 1000) |
| `--path <path>` | لا | مسار مشروع Unity (الافتراضي: المجلد الحالي) |
| `--no-start` / `--once` | لا | كما في `run-tool` (تعمل أيضًا `--url` و`--token` و`--raw` و`--timeout` و`--ready-timeout`) |
| `--format <format>` | لا | `text` أو `json` أو `ndjson` |

## `run-tool`

نفّذ أداة MCP مباشرةً عبر HTTP API. يُحَل عنوان URL للخادم ورمز التفويض **تلقائيًا** من ملف إعداد المشروع (`UserSettings/AI-Game-Developer-Config.json`)، بناءً على وضع الاتصال الحالي (Custom أو Cloud).

</div>

```bash
unity-mcp-cli run-tool rql-mutate ./MyGame --input '{"query":"editor | call CreateGameObject({name: \"Cube\", primitive: \"Cube\"})"}'
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `<tool-name>` | نعم | اسم أداة MCP المراد تنفيذها |
| `[path]` | لا | مسار مشروع Unity (موضعي أو `--path`) — يُستخدم لقراءة الإعداد واكتشاف المنفذ |
| `--url <url>` | لا | تجاوز مباشر لعنوان URL للخادم (يتجاوز الإعداد) |
| `--token <token>` | لا | تجاوز رمز Bearer (يتجاوز الإعداد) |
| `--input <json>` | لا | سلسلة JSON بوسيطات الأداة (الافتراضي `{}`) |
| `--input-file <file>` | لا | قراءة وسيطات JSON من ملف |
| `--raw` | لا | إخراج JSON خام (بلا تنسيق، وبلا مؤشر دوّار) |
| `--format <format>` | لا | `text` (افتراضي) أو `json` أو `ndjson` — راجع [المخرجات القابلة للقراءة آليًا](#رموز-الخروج-والمخرجات-القابلة-للقراءة-آليًا) |
| `--timeout <ms>` | لا | مهلة الطلب بالمللي ثانية (الافتراضي: 60000) |
| `--no-start` | لا | عدم تشغيل محرر أبدًا؛ والفشل (الخروج 7) عندما لا يجيب أي محرر |
| `--once` | لا | إغلاق المحرر بعد الاستدعاء إذا كان هذا الاستدعاء هو من شغّله (استخدام لمرة واحدة في CI) |
| `--ready-timeout <ms>` | لا | مدة انتظار جاهزية المحرر المشغَّل (الافتراضي: 300000) |

**تشغيل المحرر.** عندما لا يجيب أي محرر للمشروع، يشغّل `run-tool` محررًا دون واجهة تمامًا مثل
‏[`exec`](#exec) (عقد جلسة مشترك، ولا يغلق أبدًا محررًا لم يشغّله) — لكن فقط لمشروع يذكر
إصدار Unity الخاص به في `ProjectSettings/ProjectVersion.txt`، كي لا يخمّن المحرر أبدًا. يوقف `--no-start` هذا السلوك؛
ويجعل `--once` الاستدعاء لمرة واحدة: المحرر الذي شغّله هذا الاستدعاء يُغلق مجددًا بعد الاستدعاء (وكذلك إذا لم
يصبح جاهزًا أبدًا). ويتصرف `run-system-tool` بالطريقة نفسها.

الأداة التي تجيب بخطأ تخرج بالرمز 6؛ والأداة غير المعروفة 2 (`NOT_FOUND`)، والمهلة 14، والمحرر الذي يتعذّر الوصول إليه 7، والمحرر
المشغول 17، والاستدعاء الذي تحجبه نافذة مشروطة 18 — والجدول الكامل، مع حالات HTTP، موجود في
[رموز الخروج](#رموز-الخروج-والمخرجات-القابلة-للقراءة-آليًا).

**أولوية حل عنوان URL:**
1. `--url` ← يُستخدم مباشرةً
2. ملف الإعداد ← `host` (وضع Custom) أو نقطة النهاية السحابية الثابتة (وضع Cloud)
3. منفذ حتمي مشتق من مسار المشروع

**التفويض** يُقرأ تلقائيًا من إعداد المشروع (`token` في وضع Custom، و`cloudToken` في وضع Cloud). استخدم `--token` لتجاوز الرمز المشتق من الإعداد صراحةً.

**مثال — استدعاء أداة (عنوان URL والمصادقة من الإعداد):**

</div>

```bash
unity-mcp-cli run-tool rql-query ./MyGame --input '{"query":"objects | where name == \"Player\" | select path name active"}'
```

<div dir="rtl">

**مثال — تجاوز صريح لعنوان URL:**

</div>

```bash
unity-mcp-cli run-tool rql-mutate --url http://localhost:8080 --input '{"query":"editor | call SaveScenes()"}'
```

<div dir="rtl">

**مثال — تمرير مخرجات JSON خام عبر أنبوب:**

</div>

```bash
unity-mcp-cli run-tool unity-tool-list ./MyGame --raw | jq .
```

<div dir="rtl">

**مثال — استدعاء لمرة واحدة في CI (يشغّل محررًا دون واجهة ثم يغلقه):**

</div>

```bash
unity-mcp-cli run-tool package-list ./MyGame --url http://localhost:28343 --once --format json
```

<div dir="rtl">

## `exec`

شغّل شيفرة C# في محرر Unity واطبع النتيجة. إذا كان المشروع مفتوحًا بالفعل (بنافذة أو دون واجهة) يستخدم `exec` ذلك المحرر ولا يغلقه أبدًا. وإلا فإنه يشغّل محررًا دون واجهة يبقى جاهزًا للاستدعاءات التالية ويغلق من تلقاء نفسه بعد `--idle` ثانية دون استدعاءات أدوات (مع حفظ المشاهد والأصول المعدّلة). يُسجَّل المحرر الذي يشغّله في عقد جلسة المشروع (`.ai-game-dev/session.json`) ويُشغَّل تحت قفل ملف، لذا تتشارك استدعاءات `exec` المتوازية محررًا واحدًا؛ ويغلقه `close --owned-only`، ويغلقه `open` (بنافذة) قبل فتح النافذة. ملفات الجلسة هذه محلية للجهاز: يُبقيها `.ai-game-dev/.gitignore` خارج git. إذا كان المشروع مفتوحًا في محرر لم تشغّله CLI ولم تُجب إضافته خلال 60 ثانية (غير متصلة، أو متصلة بسحابة ai-game.dev)، يتوقف `exec` مع تلميح بدلًا من الانتظار: اربط الإضافة، أو بدّل اتصالها إلى Custom (خادم محلي)، أو مرّر `--url`.

</div>

```bash
# The last expression is the result
unity-mcp-cli exec 'GameObject.FindObjectsOfType<Camera>().Length'

# From a file or stdin, with extra namespaces and helper files
unity-mcp-cli exec -f ./snippet.cs --using MyGame.Gameplay --include Helpers/
echo 'Application.unityVersion' | unity-mcp-cli exec
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[code]` | لا | مقتطف C#: عبارات عادية، وآخر تعبير هو النتيجة. أو بدلًا من ذلك `-f` أو stdin. |
| `--path <path>` | لا | مسار مشروع Unity (الافتراضي: المجلد الحالي) |
| `-f, --file <file>` | لا | قراءة الشيفرة من ملف (`-` لـ stdin) |
| `--using <namespace>` | لا | فضاء أسماء إضافي، قابل للتكرار |
| `--include <path>` | لا | ملف `.cs` مساعد أو مجلد (بشكل تكراري)، نسبةً إلى `<Project>/.ai-game-dev/snippets`، قابل للتكرار |
| `--window` | لا | تشغيل المحرر بنافذة بدلًا من دون واجهة (فقط إذا لم يكن يعمل بعد) |
| `--idle <seconds>` | لا | إغلاق المحرر الذي شغّله `exec` بعد هذا العدد من ثواني الخمول (الافتراضي: 300) |
| `--ready-timeout <ms>` | لا | مدة انتظار جاهزية المحرر (الافتراضي: 300000) |
| `--timeout <ms>` | لا | مهلة استدعاء الأداة (الافتراضي: 310000) |
| `--raw` | لا | طباعة استجابة الأداة كـ JSON خام |
| `--format <format>` | لا | `text` (افتراضي) أو `json` أو `ndjson` |
| `--once` | لا | إغلاق المحرر بعد الاستدعاء إذا كان هذا الاستدعاء هو من شغّله |
| `--url <url>` | لا | تجاوز عنوان URL للخادم؛ والعنوان المحلي يجعل أيضًا المحرر المشغَّل حديثًا يشغّل ذلك الخادم |
| `--token <token>` | لا | تجاوز رمز Bearer |

يستدعي `exec` الأداة `script-execute`، لذا تحتاج المقتطفات و`usings` و`include` إلى إصدار إضافة يدعم Code-Exec. خطأ الترجمة أو الاستثناء يخرج بالرمز 6 ويشير إلى أسطرك أنت.

> **لم تسجّل الدخول؟** يبدأ المشروع الجديد في وضع الاتصال **Cloud**. بدون تسجيل دخول إلى ai-game.dev، تستخدم `exec`
> و`run-tool` و`compile` و`tools` خادم MCP **المحلي** للمشروع بدلًا من ذلك (وتذكر ذلك على stderr): المحرر الذي
> تشغّله يُطلب منه تقديمه، في الذاكرة — ولا يتغير إعداد المشروع. شغّل `login` للعمل عبر السحابة.
>
> المحرر دون واجهة الذي شغّلته CLI يظل يعمل عندما لا تُترجم سكربتات المشروع
> (`-ignorecompilererrors`)، كي يتمكن `compile` من الإبلاغ عن الأخطاء؛ وسجله هو `Logs/unity-mcp-cli-editor.log`.

## `test`

شغّل اختبارات Unity للمشروع كما يفعل CI: **محرر في وضع الدُفعات مع `-runTests` الخاص بـ Unity** — دون إضافة AI Game
‏Developer ودون خادم MCP، لذا يعمل على مشروع إضافته معطوبة أو مفقودة. يأتي الحكم
من تقرير NUnit الذي يكتبه التشغيل (يُحذف أولًا أي تقرير سابق في المسار نفسه)، وليس أبدًا من
رمز خروج المحرر وحده.

</div>

```bash
unity-mcp-cli test ./MyGame                                  # EditMode, report in ./test-results.xml
unity-mcp-cli test ./MyGame --mode PlayMode --filter "MyGame.Tests.Combat" --junit-output junit.xml
unity-mcp-cli test ./MyGame --format github                  # ::error annotations at the failing test's source line
unity-mcp-cli test ./MyGame --format json --timeout 900 -- -nographics
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | لا | مسار مشروع Unity (موضعي أو `--path`، الافتراضي: المجلد الحالي) |
| `--mode <mode>` | لا | `EditMode` (افتراضي) أو `PlayMode` |
| `--filter <pattern>` | لا | `-testFilter` في Unity: أسماء اختبارات كاملة أو تعبير نمطي، ويفصل `;` بين عدة قيم |
| `--category <names>` | لا | `-testCategory` في Unity |
| `--assembly <names>` | لا | `-assemblyNames` في Unity |
| `--output <file>` | لا | مسار تقرير NUnit (الافتراضي: `<project>/Logs/test-results.xml`) |
| `--junit-output <file>` | لا | كتابة تقرير JUnit أيضًا |
| `--log-file <file>` | لا | سجل المحرر (الافتراضي: مسار التقرير مع `.log`) |
| `--timeout <seconds>` | لا | قتل المحرر (وعملياته الفرعية) بعد هذه المدة (الافتراضي: 3600) |
| `--nographics` | لا | التشغيل دون GPU |
| `--unity <version>` / `--editor-path <path>` | لا | المحرر المراد استخدامه (الافتراضي: إصدار المشروع) |
| `--format <format>` | لا | `text` أو `json` أو `ndjson` أو `github` |
| `-- <args>` | لا | وسيطات إضافية للمحرر؛ وتُرفض الوسيطات التي يديرها `test` (`-batchmode`، `-runTests`، `-quit`، ...) |

**رموز الخروج:** `0` نجح الكل (التشغيل بصفر اختبارات ينجح مع تحذير)، `8` فشلت اختبارات، `6` لا حكم (أخطاء
ترجمة — تُسرد بصيغة `file(line,col): error CS...` — أو مشكلة رخصة، أو انهيار، أو أن المشروع **مفتوح بالفعل في
محرر**: لا يستطيع محرر ثانٍ فتحه، لذا استخدم `run-tool tests-run` على ذلك المحرر بدلًا من ذلك)، `14` انتهاء المهلة، `2` وسيطات
خاطئة. كما يوقف Ctrl+C أو إلغاء مهمة CI المحرر.

## `compile`

‏"هل تُترجم شيفرة C# الخاصة بي؟" مقابل محرر المشروع: يحدّث الأصول كي تُترجم السكربتات المعدّلة، وينتظر الترجمة
وإعادة تحميل النطاق (domain reload) التالية لها، ويُبلغ عن أخطاء المترجم وتحذيراته من Console المحرر مع الملف والسطر
والعمود. يشغّل محررًا دون واجهة إذا لم يكن أي محرر يعمل (ويبقى جاهزًا للاستدعاء التالي، مثل `exec`)؛ والاستدعاء مقابل محرر
جاهز يستغرق بضع ثوانٍ.

</div>

```bash
unity-mcp-cli compile ./MyGame
unity-mcp-cli compile ./MyGame --strict --format github   # warnings fail too
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | لا | مسار مشروع Unity (موضعي أو `--path`) |
| `--strict` | لا | الفشل عند تحذيرات المترجم أيضًا |
| `--timeout <seconds>` | لا | انتظار الترجمة وإعادة تحميل النطاق (الافتراضي: 180) |
| `--ready-timeout <ms>` | لا | انتظار المحرر المشغَّل (الافتراضي: 300000) |
| `--once` | لا | إغلاق المحرر بعد ذلك إذا كان هذا الاستدعاء هو من شغّله |
| `--url <url>` / `--token <token>` | لا | تجاوزات الخادم، كما في `exec` |
| `--format <format>` | لا | `text` أو `json` أو `ndjson` أو `github` |

**رموز الخروج:** `0` تُترجم، `6` أخطاء ترجمة (أو تحذيرات مع `--strict`)، `7` تعذّر الوصول إلى أي محرر، `14`
انتهاء المهلة، `18` نافذة مشروطة تحجب المحرر. تذهب التشخيصات إلى stderr بالصيغة الخاصة بالمترجم
‏`file(line,col): error CS0103: ...`.

## `dialog`

النافذة المشروطة (مطالبة حفظ، أو "Enter Safe Mode?"، أو `EditorUtility.DisplayDialog`) تحجب الخيط الرئيسي للمحرر، لذا
ينتظر كل استدعاء أداة حتى يجيب عنها أحد. يسرد `dialog` نوافذ محرر المشروع ويستطيع الضغط على
زر. تُبلغ `status` و`run-tool` و`exec` و`compile` و`wait-for-ready` عن مثل هذه النافذة برمز الخروج `18`
‏(`AWAITING_INPUT`) بدلًا من مجرد انتهاء مهلة. يعمل على Windows وmacOS وLinux؛ والمحرر دون واجهة (`-batchmode`) لا يعرض
نوافذ أبدًا.

| المنصة | الكيفية | المتطلب |
|---|---|---|
| **Windows** | Win32 (`EnumWindows`، `BM_CLICK`) عبر PowerShell | لا شيء |
| **macOS** | برمجة واجهة System Events عبر `osascript -l JavaScript`: نوافذ الحوار والأوراق (sheets) في المحرر، ونصوصها وأزرارها | **إذن Accessibility** للتطبيق الذي يشغّل CLI (طرفيتك أو تطبيق وكيل الذكاء الاصطناعي): System Settings → Privacy & Security → Accessibility. بدونه يفشل `dialog` مع هذا التلميح، ولا يجد فحص الخروج 18 أي نوافذ. |
| **Linux** | ناقل إمكانية الوصول AT-SPI عبر مساعد `python3` (PyGObject): نوافذ الحوار والتنبيه في المحرر (GTK، الذي يستخدمه `EditorUtility.DisplayDialog` في Unity)، وتسمياتها وأزرارها، وتُضغط عبر إجراء `click` الخاص بها. يعمل على X11 وWayland | `python3` مع PyGObject وAT-SPI typelib (Debian/Ubuntu: `python3-gi gir1.2-atspi-2.0`؛ Fedora: `python3-gobject at-spi2-core`؛ Arch: `python-gobject at-spi2-core`) وناقل إمكانية وصول قيد التشغيل (`at-spi2-core`، يبدأ عند الطلب في GNOME وKDE ومعظم بيئات سطح المكتب). شغّل CLI في جلسة سطح المكتب التي تشغّل المحرر. الحزمة المفقودة، أو الناقل الذي يتعذّر الوصول إليه، أو غياب جلسة رسومية، تجعل `dialog` يفشل مع تلميح يسمّي الحل، ولا يجد فحص الخروج 18 أي نوافذ. |

يسرد Windows أيضًا نوافذ Unity المشروطة الخاصة، مثل "Missing signature" التي يعرضها Unity 6.6 قبل استيراد
ملف `.unitypackage` غير موقّع (كل حزمة من Asset Store): يرسم Unity نصها وأزرارها بنفسه، لذا لا تعرض القائمة سوى
عنوانها وزر واحد، **Cancel**، الذي يرسله `--press Cancel` كمفتاح Escape. أي إجابة أخرى (Import Anyway)
تعود إلى المستخدم، في المحرر.

يأتي معرّف النافذة من القائمة ويبقى كما هو ما دامت تلك النافذة مفتوحة (Windows: مقبض نافذتها؛ macOS و
‏Linux: تجزئة لمعرّف العملية والعنوان والنص والأزرار). لا تُوضع تسمية الزر أبدًا داخل سكربت، بل تُمرَّر كبيانات فقط.

على جهاز Linux دون واجهة، شغّل المحرر (دون `-batchmode`) وCLI تحت `Xvfb` داخل
‏`dbus-run-session` واحدة، مع تثبيت `at-spi2-core`. إذا كان للمستخدم نفسه جلسة سطح مكتب أيضًا، فامنح تلك الجلسة
الخاصة `XDG_RUNTIME_DIR` مستقلًا: وإلا فإن ناقل إمكانية الوصول الخاص بها سيستبدل (ويحذف عند الخروج) مقبس
‏`at-spi` الخاص بسطح المكتب.

</div>

```bash
unity-mcp-cli dialog ./MyGame                          # list: title and buttons
unity-mcp-cli dialog ./MyGame --press "Don't Save"     # answer it
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | لا | مسار مشروع Unity (موضعي أو `--path`) |
| `--press <button>` | لا | الضغط على الزر الذي يحمل هذه التسمية (دون تمييز حالة الأحرف) |
| `--id <id>` | لا | مع `--press`: النافذة المراد الإجابة عنها (من القائمة) |
| `--format <format>` | لا | `text` أو `json` أو `ndjson` |

## `commands`

كل أمر مع وسيطاته وخياراته وقيمه الافتراضية، إضافةً إلى جدول رموز الخروج — مقروءة من CLI نفسها، لذا لا
تنحرف أبدًا عن الواقع. مخصص للوكلاء والأدوات.

</div>

```bash
unity-mcp-cli commands --format json
```

<div dir="rtl">

## `wait-for-ready`

انتظر حتى يتصل محرر Unity وخادم MCP ويصبحا جاهزين لقبول استدعاءات الأدوات. يستطلع الخادم على فاصل زمني قابل للضبط حتى يستجيب بنجاح أو تنقضي المهلة. مفيد لسكربتات الأتمتة وتنسيق وكلاء الذكاء الاصطناعي حيث يشغّل `open` برنامج Unity لكن الوكيل يحتاج إلى معرفة متى يمكنه البدء في استدعاء الأدوات.

</div>

```bash
unity-mcp-cli wait-for-ready ./MyGame
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | لا | مسار مشروع Unity (موضعي أو `--path`) — يُستخدم لقراءة الإعداد واكتشاف المنفذ |
| `--url <url>` | لا | تجاوز مباشر لعنوان URL للخادم (يتجاوز الإعداد) |
| `--token <token>` | لا | تجاوز رمز Bearer (يتجاوز الإعداد) |
| `--timeout <ms>` | لا | أقصى مدة انتظار بالمللي ثانية (الافتراضي: 120000) |
| `--interval <ms>` | لا | الفاصل الزمني للاستطلاع بالمللي ثانية (الافتراضي: 3000) |
| `--format <format>` | لا | `text` (افتراضي) أو `json` أو `ndjson` |

يخرج بالرمز `0` عند الجاهزية، و`14` عند انتهاء المهلة، و`18` عندما تحجب نافذة مشروطة المحرر.

**مثال — الانتظار بالمهلة الافتراضية (120 ثانية):**

</div>

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
```

<div dir="rtl">

**مثال — مهلة أقصر لـ CI:**

</div>

```bash
unity-mcp-cli wait-for-ready ./MyGame --timeout 60000 --interval 2000
```

<div dir="rtl">

**مثال — عنوان URL صريح للخادم:**

</div>

```bash
unity-mcp-cli wait-for-ready --url http://localhost:8080 --timeout 30000
```

<div dir="rtl">

## `setup-mcp`

اكتب ملفات إعداد MCP لوكلاء الذكاء الاصطناعي، مما يتيح الإعداد دون واجهة أو في CI دون واجهة محرر Unity. يدعم الوكلاء الـ 14 جميعهم (Claude Code وCursor وGemini وCodex وغيرها).

يكون الإعداد **مثبّتًا** على هذا المشروع عبر عنوان URL خاص بكل مشروع (`https://ai-game.dev/mcp/p/<pin>` لنقل http، أو الوسيط `project=<pin>` لـ stdio). ولنقل http السحابي يحمل أيضًا **مفتاح المشروع** الخاص بهذا المشروع (`Authorization: Bearer agd_pk_…`) لكل وكيل: بيانات اعتماد لا تنتهي صلاحيتها وقابلة للإلغاء ومرتبطة بهذا المشروع وحده، يُعاد استخدامها من `~/.ai-game-dev/project-keys.json` أو تُصدر بالحساب الذي سجّلت الدخول به عبر [`login`](#login). بدون تسجيل دخول (أو إذا تعذّر الحصول على المفتاح) يكون الإعداد عنوان URL فقط ويسجّل الوكيل الدخول عبر OAuth الخاص به. مرّر `--oauth` لكتابة إعداد عنوان URL فقط دائمًا، و`--regenerate-key` لاستبدال المفتاح (تُنقل إعدادات الوكلاء الأخرى لهذا المشروع التي تحمل المفتاح السابق إلى المفتاح الجديد وتُطبع، ثم يُلغى المفتاح السابق؛ وإذا تعذّرت إعادة كتابة أيٍّ منها، يُترك المفتاح السابق نشطًا ويسمّيه تحذير)، و`--token` لكتابة PAT صريح بدلًا من ذلك، أو `--no-pin` لنقطة النهاية المشتركة غير المثبّتة. لا تحمل إعدادات stdio والخادم المحلي مفتاح مشروع أبدًا. يقرأ Antigravity إما `~/.gemini/config/mcp_config.json` أو `~/.gemini/antigravity/mcp_config.json` بحسب التثبيت، لذا يُكتب الملفان كلاهما ويُطبع كل مسار مكتوب؛ وإذا تعذّرت كتابة أيٍّ منهما يفشل الأمر. تطابق هذه المخرجات زر **Configure** في محرر Unity.

</div>

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[agent-id]` | نعم | الوكيل المراد إعداده (استخدم `--list` لرؤية الكل) |
| `[path]` | لا | مسار مشروع Unity (الافتراضي هو المجلد الحالي) |
| `--transport <transport>` | لا | طريقة النقل: `stdio` أو `http` (الافتراضي: `http`) |
| `--url <url>` | لا | تجاوز عنوان URL للخادم (لنقل http) |
| `--token <token>` | لا | PAT صريح — يُكتب كترويسة `Authorization` بدلًا من مفتاح المشروع |
| `--oauth` | لا | كتابة إعداد سحابي بعنوان URL فقط (يسجّل الوكيل الدخول عبر OAuth الخاص به) بدلًا من مفتاح المشروع |
| `--regenerate-key` | لا | إصدار مفتاح مشروع جديد، وإعادة كتابة الإعداد، وإلغاء المفتاح السابق (Cloud http فقط) |
| `--no-pin` | لا | كتابة عنوان URL غير مثبّت / حذف الوسيط `project=` (الافتراضي: التثبيت على هذا المشروع عبر `/mcp/p/<pin>`) |
| `--list` | لا | سرد كل معرّفات الوكلاء المتاحة |

**مثال — سرد كل الوكلاء المدعومين:**

</div>

```bash
unity-mcp-cli setup-mcp --list
```

<div dir="rtl">

**مثال — إعداد Cursor بنقل stdio:**

</div>

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

<div dir="rtl">

**مثال — استبدال مفتاح هذا المشروع (يتوقف المفتاح القديم عن العمل):**

</div>

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --regenerate-key
```

<div dir="rtl">

**مثال — كتابة إعداد غير مثبّت (نقطة النهاية المشتركة `/mcp`):**

</div>

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --no-pin
```

<div dir="rtl">

## `setup-skills`

اكتب ملفات المهارات (skills) لوكيل ذكاء اصطناعي. دائمًا، ودون اتصال (بلا محرر، وبلا خادم): مهارة `unity-mcp-cli`، أي كيف يستخدم الوكيل
هذه CLI من بداية باردة (أول أمر للمحرر يشغّل محررًا دون واجهة؛ ووصفات `rql`؛ و`compile` / `test`؛
و`--format json`؛ ورموز الخروج و`dialog`)، مع `references/rql.md` و`references/install.md` للتعمق، إضافةً إلى
‏`ai-game-dev-app`. تأتي الوصفات من المصدر نفسه الذي يأتي منه `--help`، لذا لا يمكن أن ينحرف أحدهما عن الآخر. ثم، عندما يجيب
محرر المشروع، يطلب أيضًا من الإضافة مهاراتها الخاصة بكل أداة؛ والمحرر المغلق ليس خطأً.

</div>

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[agent-id]` | نعم | الوكيل المراد توليد المهارات له (استخدم `--list` لرؤية الكل) |
| `[path]` | لا | مسار مشروع Unity (الافتراضي هو المجلد الحالي) |
| `--url <url>` | لا | تجاوز عنوان URL للخادم |
| `--token <token>` | لا | تجاوز رمز المصادقة |
| `--list` | لا | سرد كل الوكلاء مع حالة دعم المهارات |
| `--offline` | لا | كتابة مهارة `unity-mcp-cli` فقط؛ دون سؤال المحرر |
| `--timeout <ms>` | لا | مهلة الطلب بالمللي ثانية (الافتراضي: 60000) |

**مثال — سرد الوكلاء مع دعم المهارات:**

</div>

```bash
unity-mcp-cli setup-skills --list
```

<div dir="rtl">

## `remove-plugin`

أزل إضافة Unity-MCP من ملف `Packages/manifest.json` لمشروع Unity.

</div>

```bash
unity-mcp-cli remove-plugin ./MyGame
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | نعم | مسار مشروع Unity (موضعي أو `--path`) |

هذا الأمر:
1. يزيل `com.ivanmurzak.unity.mcp` من `dependencies`
2. **يحافظ على السجلات المحدّدة النطاق والنطاقات** — فقد تعتمد عليها حزم أخرى
3. **لا يفعل شيئًا** إذا لم تكن الإضافة مثبّتة

> بعد تشغيل هذا الأمر، افتح المشروع في محرر Unity لتطبيق التغيير.

## `status`

تحقق من حالة اتصال محرر Unity وخادم MCP. يعرض ما إذا كان Unity يعمل، وما إذا كان يمكن الوصول إلى خادم MCP المحلي، وما إذا كان يمكن الوصول إلى الخادم المحدد من الإعداد (مثل الخادم السحابي).

</div>

```bash
unity-mcp-cli status ./MyGame
```

<div dir="rtl">

| الخيار | مطلوب | الوصف |
|---|---|---|
| `[path]` | لا | مسار مشروع Unity (موضعي أو `--path`) |
| `--url <url>` | لا | تجاوز مباشر لعنوان URL للخادم (يتجاوز الإعداد) |
| `--token <token>` | لا | تجاوز رمز Bearer (يتجاوز الإعداد) |
| `--timeout <ms>` | لا | مهلة الفحص بالمللي ثانية (الافتراضي: 5000) |
| `--format <format>` | لا | `text` (افتراضي) أو `json` أو `ndjson` |

يخرج بالرمز `0` عندما يجيب خادم MCP، و`7` عندما لا يجيب، و`18` عندما تحجب نافذة مشروطة المحرر (تُسرد في
المخرجات؛ راجع [`dialog`](#dialog)).

**مثال على المخرجات:**

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

## الخيارات العامة

هذه الخيارات متاحة في كل الأوامر:

| الخيار | الوصف |
|---|---|
| `-v, --verbose` | تفعيل مخرجات تشخيصية مفصّلة لاستكشاف الأخطاء وإصلاحها |
| `--version` | عرض إصدار CLI |
| `--help` | عرض المساعدة للأمر |

**مثال — تشغيل أي أمر بمخرجات مفصّلة:**

</div>

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

<div dir="rtl">

## رموز الخروج والمخرجات القابلة للقراءة آليًا

تتشارك `unity-mcp-cli` و`unreal-mcp-cli` و`godot-cli` جدول رموز خروج واحدًا، كي يميّز سكربت أو وكيل بين "أصلح
سطر الأوامر" و"فشل المحرر" و"فشلت الاختبارات" دون قراءة الرسائل:

| الرمز | الاسم | المعنى |
|---|---|---|
| `0` | `SUCCESS` | نجح الأمر |
| `1` | `ERROR` | خطأ غير مصنّف |
| `2` | `USAGE` | وسيطات خاطئة، أو أداة غير معروفة؛ لم يُشغَّل شيء. أصلح سطر الأوامر — ولا تُعِد المحاولة دون تغيير |
| `6` | `COMMAND_FAILED` | شُغّل الأمر وفشل (خطأ أداة، أو أخطاء ترجمة، أو تشغيل اختبارات دون حكم، أو بيانات اعتماد مرفوضة) |
| `7` | `UNREACHABLE` | تعذّر تشغيل المحرر أو خادم MCP أو الوصول إليه؛ أعد المحاولة بعد إصلاح البيئة |
| `8` | `TESTS_FAILED` | شُغّلت الاختبارات وفشل بعضها |
| `14` | `TIMEOUT` | انتهت المهلة قبل الحصول على نتيجة |
| `17` | `BUSY` | المحرر مشغول ولم يشغّل الاستدعاء؛ أعد المحاولة لاحقًا |
| `18` | `AWAITING_INPUT` | نافذة مشروطة تحجب المحرر؛ يجب أن يجيب عنها إنسان (راجع [`dialog`](#dialog)) |

يطبع `--format json` (في `run-tool` و`run-system-tool` و`exec` و`test` و`compile` و`dialog` و`status` و`wait-for-ready` و
‏`commands`) غلاف نتيجة واحدًا بالضبط على stdout؛ ويذهب كل ما عداه إلى stderr. ويطبع `--format ndjson`
الغلاف نفسه في سطر واحد ويبث أسطر التقدم (`{"type":"progress",...}`) على stderr. خطأ سطر الأوامر تحت
`--format json` يطبع مع ذلك غلافًا (الرمز `USAGE`).

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

‏`errors[].code` رمز ثابت (`USAGE`، `NOT_FOUND`، `NOT_AUTHENTICATED`، `TOOL_FAILED`، `UNREACHABLE`، `TIMEOUT`،
‏`BUSY`، `AWAITING_INPUT`، `TESTS_FAILED`، `TEST_RUN_ERROR`، `TEST_TIMED_OUT`، `COMPILATION_FAILED`، `PROJECT_OPEN`،
‏`ERROR`، ...)؛ طابِقه هو، ولا تطابق الرسالة أبدًا.

**استدعاء الأداة الذي لم ينجح** (`run-tool`، `run-system-tool`، `exec`، `rql`، `tools`) يُربط برمز الخروج نفسه
ورمز الخطأ نفسه في الواجهات الثلاث (يوجد الجدول في `@baizor/gamedev-cli-core`؛ ويسرده `commands --format json` باسم
‏`errorCodes`). عندما يجيب خادم MCP، تكون حالة HTTP الخاصة به هي الحاسمة، ويحملها خطأ الغلاف:
‏`errors[{code, message, httpStatus}]`.

| الحالة | حالة HTTP | الخروج | `errors[].code` |
|---|---|---|---|
| وسيطات خاطئة: علم خاطئ، أو وسيطات أداة رفضتها CLI أو الخادم (أي 4xx غير مذكور هنا) | 400 (وأيضًا 422 `[invalid_params]`) | `2` | `USAGE` |
| لا توجد أداة بهذا الاسم (اسردها بـ `tools`)، أو لا يوجد مشروع في المسار | 404 | `2` | `NOT_FOUND` |
| لا توجد بيانات اعتماد صالحة، أو رفضها الخادم: سجّل الدخول بـ `login` | 401، 403 | `6` | `NOT_AUTHENTICATED` |
| شُغّلت الأداة وأبلغت عن خطأ (`status: "error"`، أي 5xx غير مذكور هنا) | 409، 422، 500 | `6` | `TOOL_FAILED` |
| لم تُبنَ شيفرة المشروع، لذا لم يُشغَّل أي محرر | — | `6` | `COMPILATION_FAILED` |
| رُفض الاتصال أو أُعيد ضبطه، أو لا توجد إضافة محرك متصلة بالخادم، أو فُقدت الإضافة بعد إرسال الاستدعاء | 502 | `7` | `UNREACHABLE` |
| لا إجابة في الوقت المحدد: `--timeout`، أو مهلة الأداة الخاصة بالخادم | 408، 504 | `14` | `TIMEOUT` |
| مشغول ولم يشغّل الاستدعاء (إعادة تحميل، أو تحديد معدل، أو استدعاء CLI آخر يحتجز المحرر)؛ أعد المحاولة لاحقًا | 503، 429 | `17` | `BUSY` |
| نافذة مشروطة تحجب المحرر | — | `18` | `AWAITING_INPUT` |
| أي شيء آخر (أُلغي الاستدعاء، أو خطأ غير متوقع) | — | `1` | `ERROR` |

# مثال أتمتة كامل

أعدّ مشروع Unity MCP كاملًا من الصفر في سكربت واحد:

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

# كيف يعمل

### المنفذ الحتمي

تولّد CLI **منفذًا حتميًا** لكل مشروع Unity بناءً على مسار مجلده (تجزئة SHA256 مُسقطة على نطاق المنافذ 20000–29999). يطابق هذا توليد المنفذ في إضافة Unity، مما يضمن اتفاق الخادم والإضافة تلقائيًا على المنفذ نفسه دون إعداد يدوي.

### تثبيت الإضافة

يعدّل الأمر `install-plugin` الملف `Packages/manifest.json` مباشرةً:
- يضيف سجل [OpenUPM](https://openupm.com/) المحدّد النطاق (`package.openupm.com`)
- يسجّل كل النطاقات المطلوبة (`com.ivanmurzak`، `extensions.unity`)
- يضيف الاعتمادية `com.ivanmurzak.unity.mcp` مع تحديثات واعية بالإصدار (لا يخفّض الإصدار أبدًا)

### ملف الإعداد

يقرأ الأمر `configure` الملف `UserSettings/AI-Game-Developer-Config.json` ويكتبه، وهو يتحكم في:
- **الأدوات (Tools)** — أدوات MCP المتاحة لوكلاء الذكاء الاصطناعي
- **الموجّهات (Prompts)** — موجّهات معرّفة مسبقًا تُحقن في محادثات LLM
- **الموارد (Resources)** — بيانات للقراءة فقط مكشوفة لوكلاء الذكاء الاصطناعي
- **إعدادات الاتصال** — عنوان URL للمضيف، ورمز المصادقة، وطريقة النقل، والمهل

### التكامل مع Unity Hub

تستخدم الأوامر التي تدير المحررات أو تنشئ المشاريع **Unity Hub CLI** (وضع `--headless`). إذا لم يكن Unity Hub مثبّتًا، فإن CLI **تنزّله وتثبّته تلقائيًا**:
- ‏**Windows** — تثبيت صامت عبر `UnityHubSetup.exe /S` (قد يتطلب صلاحيات المسؤول)
- ‏**macOS** — ينزّل ملف DMG ويركّبه وينسخ `Unity Hub.app` إلى `/Applications`
- ‏**Linux** — ينزّل `UnityHub.AppImage` إلى `~/Applications/`

# واجهة المكتبة البرمجية (v0.67.0+)

إضافةً إلى الملف التنفيذي لـ CLI، يكشف `unity-mcp-cli` أوامره الأساسية كمكتبة ذات أنواع وخالية من الآثار الجانبية، كي تتمكن أدوات Node.js / TypeScript الأخرى من تضمين تدفق التثبيت والإعداد نفسه دون استدعاء الصدفة.

</div>

```ts
import { installPlugin, removePlugin, configure, setupMcp } from 'unity-mcp-cli';

const result = await installPlugin({
  unityProjectPath: './MyUnityProject',
  // version: '0.67.0',        // optional — defaults to latest from OpenUPM
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

تُعيد كل دالة كائن نتيجة ذا أنواع `{ success, ... }`؛ ولا تُرمى الأخطاء أبدًا خارج الحدود العامة. ولا يحتوي مدخل المكتبة على آثار جانبية على المستوى الأعلى — فـ `import 'unity-mcp-cli'` لا يحلل argv أبدًا ولا يكتب أبدًا إلى stdout أو stderr.

راجع [`CHANGELOG.md`](../CHANGELOG.md#0670---2026-04-21) للاطلاع على القائمة الكاملة للدوال والأنواع المُصدَّرة.

</div>
