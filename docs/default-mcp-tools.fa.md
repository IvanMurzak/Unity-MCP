<div dir="rtl">

[English](default-mcp-tools.md) | [中文](default-mcp-tools.zh-CN.md) | [日本語](default-mcp-tools.ja.md) | [Español](default-mcp-tools.es.md) | <b>فارسی</b> | [हिन्दी](default-mcp-tools.hi.md) | [العربية](default-mcp-tools.ar.md)

# ابزارهای پیش‌فرض MCP

بازگشت به [README](README.fa.md). همین رجیستری را به‌صورت آنلاین در [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools) مرور کنید.

‏Unity-MCP مجموعه کوچکی از ابزارها را برای ویرایشگر و Runtime یونیتی در اختیار مدل‌های هوش مصنوعی قرار می‌دهد. بیشتر کارهای ویرایشگر از طریق **RQL** انجام می‌شود، یک زبان کوئری روی داده‌های ویرایشگر: `rql-query` می‌خواند، `rql-mutate` تغییر می‌دهد، `rql-schema` منابع، نوع‌ها و فراخوانی‌ها را توصیف می‌کند. از نسخه 1.0.0، RQL جایگزین ابزارهای «یک ابزار برای هر عمل» نسخه‌های قبلی شده است ([#1017](https://github.com/IvanMurzak/Unity-MCP/pull/1017))؛ جدول انتهای صفحه شناسه‌های قدیمی را به RQL نگاشت می‌کند.

> **نکته**: شناسه‌ها به‌صورت kebab-case هستند. ابزارهای *پیش‌فرض* پس از نصب به عامل هوش مصنوعی نمایش داده می‌شوند؛ ابزارهای *پنهان* نصب شده‌اند و وقتی با نامشان فراخوانی شوند اجرا می‌شوند، و با `tool-set-visibility`، در پنجره AI Game Developer، یا با `unity-mcp-cli configure <path> --enable-tools <ids>` نمایش داده می‌شوند.

## 🔎 RQL
| ابزار | شناسه | پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| **RQL / Query** | `rql-query` | نمایان | خواندن داده‌های ویرایشگر، مثلاً `roots \| recurse children \| select name active`. |
| **RQL / Mutate** | `rql-mutate` | نمایان | تغییر داده‌های ویرایشگر یا فراخوانی عملیات ویرایشگر، مثلاً `editor \| call CreateGameObject({name: "Ground", primitive: "Plane"})`. |
| **RQL / Schema** | `rql-schema` | نمایان | توصیف منابع، نوع‌ها، اعضا و فراخوانی‌ها؛ `rql-schema "guide:<topic>"` یک راهنما چاپ می‌کند. |

منابع RQL: `scenes`، `roots`، `objects` (هر GameObject در صحنه‌های بارگذاری‌شده)، `selection`، `prefab` (پریفبی که در prefab mode باز است)، `editor` (ساخت GameObjectها، متریال‌ها، پوشه‌ها و صحنه‌ها؛ باز کردن و ذخیره صحنه‌ها و پریفب‌ها؛ کپی، جابه‌جایی و حذف assetها؛ اجرای آیتم‌های منو)، `settings`، `console` (ورودی‌های ثبت‌شده لاگ ویرایشگر)، `shaders`، `componentTypes`، `tests`، `testResults`، `profiler`، `lighting`، `assetStore` (بسته‌های Asset Store که مالک آن‌ها هستید و دانلود کرده‌اید، همراه با import؛ وقتی ویرایشگر از حساب خارج شده باشد، `SignIn()` صفحه ورود Unity را برای کاربر باز می‌کند). assetها با مسیر و به‌کمک `unity.asset("Assets/…")` آدرس‌دهی می‌شوند.

## 📸 اسکرین‌شات‌ها
| ابزار | شناسه | پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| **Screenshot / Isolated** | `screenshot-isolated` | نمایان | رندر یک GameObject به‌صورت ایزوله از زاویه دلخواه (نمای ترکیبی 2x2 اختیاری). |
| **Screenshot / Scene View** | `screenshot-scene-view` | پنهان | گرفتن تصویر از Scene View ویرایشگر Unity. |
| **Screenshot / Game View** | `screenshot-game-view` | پنهان | گرفتن تصویر از Game View ویرایشگر Unity. |
| **Screenshot / Camera** | `screenshot-camera` | پنهان | گرفتن نمای یک دوربین به‌صورت تصویر. |

هر چهار ابزار یک `savePath` اختیاری می‌گیرند، برای وقتی که کاربر تصویر را به‌صورت فایل می‌خواهد. فایل PNG داخل پروژه هم نوشته می‌شود و نتیجه می‌گوید `Saved to <absolute path>`؛ تصویر همچنان برگردانده می‌شود. `Screenshots/shot.png` یک فایل ساده باقی می‌ماند؛ مسیری زیر `Assets/` بلافاصله به‌عنوان یک asset یونیتی با `.meta` import می‌شود. موارد ردشده: هر چیزی بیرون از پروژه (`..`، دیگر مسیرهای مطلق، یک symlink یا junction در مسیر)، پوشه‌های خود Unity (`Library`، `Temp`، `Logs`، `obj`، `Packages`، `ProjectSettings`، `UserSettings`)، نامی غیر از `.png`، و فایلی که از قبل وجود دارد (هرگز بازنویسی نمی‌کند).

## 📜 اسکریپت‌نویسی و تست‌ها
| ابزار | شناسه | پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| **Script / Execute** | `script-execute` | نمایان | کامپایل و اجرای پویای کد C# با استفاده از Roslyn. |
| **Script / Read** | `script-read` | پنهان | خواندن محتوای یک فایل اسکریپت. |
| **Script / Update or Create** | `script-update-or-create` | پنهان | به‌روزرسانی یا ساخت یک فایل اسکریپت با کد C# داده‌شده. |
| **Script / Delete** | `script-delete` | پنهان | حذف فایل(های) اسکریپت. |
| **Tests / Run** | `tests-run` | نمایان | اجرای تست‌های Unity (EditMode/PlayMode): هر انتخاب RQL روی منبع `tests` به‌عنوان یک اجرا، با نتایجی که با RQL فیلتر می‌شوند. |

## 📦 بسته‌ها
| ابزار | شناسه | پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| **Package / List** | `package-list` | نمایان | فهرست بسته‌های نصب‌شده در پروژه. |
| **Package / Search** | `package-search` | نمایان | جستجو در رجیستری Unity Package Manager و بسته‌های نصب‌شده. |
| **Package / Add** | `package-add` | نمایان | نصب یک بسته از رجیستری، یک Git URL یا یک مسیر محلی. |
| **Package / Remove** | `package-remove` | نمایان | حذف نصب یک بسته. |

## 🛒 Asset Store
| ابزار | شناسه | پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| **Asset Store / Library** | `assetstore-library` | نمایان | فهرست بسته‌های Asset Store که مالک آن‌ها هستید (My Assets، از طریق ورود Unity در ویرایشگر) و بسته‌های موجود در کش دانلود محلی، فیلترشده با کلمات؛ هر ردیف می‌گوید آیا دانلود شده و آیا قبلاً import شده است. |
| **Asset Store / Add** | `assetstore-add` | نمایان | import یک بسته دانلودشده بدون پنجره import؛ پریفب‌ها، متریال‌ها، مدل‌ها، صحنه‌ها و یادداشت‌های آن (render pipeline، اسکریپت‌ها، بسته‌های تودرتو) را برمی‌گرداند. در Unity 6.6 کاربر پنجره "Missing signature" یونیتی را تأیید می‌کند. |

منبع RQL با نام `assetStore` بقیه موارد را در خود دارد: `Contents(filter)` فایل‌های یک بسته را بدون import فهرست می‌کند، `ImportFile(path)` یک `.unitypackage` تودرتو را import می‌کند (مثلاً یک نسخه مخصوص render pipeline)، `SignIn()` وقتی ویرایشگر از حساب خارج شده باشد ورود Unity را آغاز می‌کند (`rql-schema "assetStore"`).

## 🧰 مدیریت ابزارها
| ابزار | شناسه | پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| **Tool / List** | `unity-tool-list` | نمایان | فهرست همه ابزارها و اینکه آیا عامل آن‌ها را می‌بیند. |
| **Tool / Set Visibility** | `tool-set-visibility` | پنهان | نمایش یا پنهان کردن ابزارها در فهرست ابزارهای عامل. |
| **Tool / Set Enabled State** | `tool-set-enabled-state` | پنهان | نام مستعار منسوخ‌شده `tool-set-visibility`. |

## 📊 پروفایلینگ و عیب‌یابی
| ابزار | شناسه | توضیحات |
| :--- | :--- | :--- |
| **Profiler (RQL)** | `rql-query` / `rql-mutate` روی منبع `profiler` | اسنپ‌شات‌های حافظه، رندرینگ، اسکریپت و فریم و پرچم‌های ماژول؛ فراخوانی‌های `Start`، `Stop`، `ClearFrames`، `EnableModule`، `Save`، `Load`. |

## 💡 نورپردازی
| ابزار | شناسه | توضیحات |
| :--- | :--- | :--- |
| **Lighting (RQL)** | `rql-query` / `rql-mutate` روی منبع `lighting` | محیط صحنه فعال (ambient، fog، skybox، sun، reflections؛ قابل undo)، asset تنظیمات Lighting Settings و bake؛ فراخوانی‌های `CreateSettings`، `UseSettings`، `Bake`، `CancelBake`، `ClearBakedData`، `BakeReflectionProbes`. |
| **Light probes (RQL)** | `call PlaceGrid(size, spacing)` روی یک `LightProbeGroup` | پر کردن یک جعبه دور گروه با probeهایی با فاصله یکنواخت. |
| **Reflection probes (RQL)** | `call Bake()` روی یک `ReflectionProbe` | bake کردن یک probe از نوع Custom در یک texture کنار صحنه‌اش، رندر دوباره یک probe از نوع Realtime (probeهای Baked همراه با نورپردازی bake می‌شوند). |

## 🔁 ابزارهای حذف‌شده در 1.0.0 و معادل‌های RQL آن‌ها
آموزش‌هایی که برای نسخه‌های قبلی نوشته شده‌اند از این شناسه‌ها استفاده می‌کنند. این ابزارها دیگر وجود ندارند؛ از RQL استفاده کنید (امضاهای دقیق: `rql-schema`).

</div>

| شناسه قدیمی | اکنون |
| :--- | :--- |
| `scene-list-opened`, `scene-get-data` | `rql-query`: `scenes`, `roots`, `objects` |
| `scene-create`, `scene-open`, `scene-save` | `rql-mutate`: `editor \| call NewScene(…)`, `OpenScene(…)`, `SaveScenes()`; صحنه بدون نام: `scenes \| where … \| call SaveAs("Assets/….unity")` |
| `scene-set-active`, `scene-unload` | `rql-mutate`: `scenes \| where … \| call SetActive()`, `Close()` |
| `gameobject-create` | `rql-mutate`: `editor \| call CreateGameObject({name, primitive, position, rotation, scale})` |
| `gameobject-duplicate` | `rql-mutate`: `objects \| where name == "…" \| call Duplicate()` |
| `gameobject-destroy` | `rql-mutate`: `objects \| where name == "…" \| call Destroy()` |
| `gameobject-find` | `rql-query`: `objects \| where name == "…" \| select path name active`، یا `unity.find("Level/Player")` |
| `gameobject-set-parent` | `rql-mutate`: `… \| set transform.parent = ref(…)` |
| `gameobject-modify`, `object-modify` | `rql-mutate`: `… \| set <member> = <value>` |
| `gameobject-component-add` | `rql-mutate`: `… \| call AddComponent("Rigidbody")` |
| `gameobject-component-get`, `gameobject-component-modify`, `gameobject-component-destroy` | `… \| get components \| where $ is Rigidbody \| select …` / `set …` / `call Destroy()` |
| `gameobject-component-list-all` | `rql-query`: `componentTypes` |
| `assets-find`, `assets-get-data`, `assets-modify` | `unity.assets("t:Material")`; `unity.asset("Assets/…") \| select …` / `set …` |
| `assets-material-create` | `rql-mutate`: `editor \| call CreateMaterial("Assets/….mat")` |
| `assets-create-folder`, `assets-copy`, `assets-move`, `assets-delete`, `assets-refresh` | `rql-mutate`: `editor \| call CreateFolder(…)`, `CopyAsset(…)`, `MoveAsset(…)`, `DeleteAsset(…)`, `RefreshAssets()`؛ ذخیره با `SaveAssets()` |
| `assets-prefab-create` | `rql-mutate`: `… \| call SaveAsPrefab("Assets/….prefab")` |
| `assets-prefab-open`, `assets-prefab-save`, `assets-prefab-close`, `assets-prefab-instantiate` | `rql-mutate`: `editor \| call OpenPrefab(…)`, `SavePrefab()`, `ClosePrefab()`, `InstantiatePrefab(…)`؛ منبع `prefab` |
| `assets-shader-list-all` | `rql-query`: `shaders` |
| `console-get-logs`, `console-clear-logs` | `rql-query`: `console`; `rql-mutate`: `editor \| call ClearConsole()` |
| `editor-application-get-state`, `editor-application-set-state` | `editor \| select isPlaying isPaused isCompiling`; `editor \| set isPlaying = true` |
| `editor-selection-get`, `editor-selection-set` | `rql-query`: `selection`; `rql-mutate`: `editor \| set selection = [ref(…)]` |

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
