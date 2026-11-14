<div dir="rtl">

[English](default-mcp-tools.md) | [中文](default-mcp-tools.zh-CN.md) | [日本語](default-mcp-tools.ja.md) | [Español](default-mcp-tools.es.md) | [فارسی](default-mcp-tools.fa.md) | [हिन्दी](default-mcp-tools.hi.md) | <b>العربية</b>

# أدوات MCP الافتراضية

العودة إلى [README](README.ar.md). تصفّح السجل نفسه عبر الإنترنت على [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools).

يمنح Unity-MCP نماذج الذكاء الاصطناعي مجموعة صغيرة من الأدوات لمحرر Unity ووقت التشغيل. يمرّ معظم عمل المحرر عبر **RQL**، وهي لغة استعلام فوق بيانات المحرر: `rql-query` يقرأ، و`rql-mutate` يغيّر، و`rql-schema` يصف المصادر والأنواع والاستدعاءات. منذ الإصدار 1.0.0 تحل RQL محل أدوات «أداة لكل إجراء» في الإصدارات السابقة ([#1017](https://github.com/IvanMurzak/Unity-MCP/pull/1017))؛ ويربط الجدول في النهاية المعرّفات القديمة بما يقابلها في RQL.

> **ملاحظة**: المعرّفات بصيغة kebab-case. تُعرض الأدوات *الافتراضية* لوكيل الذكاء الاصطناعي بعد التثبيت؛ أما الأدوات *المخفية* فمثبّتة وتعمل عند استدعائها بالاسم، وتُظهَر بالأداة `tool-set-visibility`، أو في نافذة AI Game Developer، أو بالأمر `unity-mcp-cli configure <path> --enable-tools <ids>`.

## 🔎 RQL
| الأداة | المعرّف | افتراضيًا | الوصف |
| :--- | :--- | :--- | :--- |
| **RQL / Query** | `rql-query` | ظاهرة | قراءة بيانات المحرر، مثلًا `roots \| recurse children \| select name active`. |
| **RQL / Mutate** | `rql-mutate` | ظاهرة | تغيير بيانات المحرر أو استدعاء عمليات المحرر، مثلًا `editor \| call CreateGameObject({name: "Ground", primitive: "Plane"})`. |
| **RQL / Schema** | `rql-schema` | ظاهرة | وصف المصادر والأنواع والأعضاء والاستدعاءات؛ ويطبع `rql-schema "guide:<topic>"` دليلًا. |

مصادر RQL: `scenes`، و`roots`، و`objects` (كل GameObject في المشاهد المحمّلة)، و`selection`، و`prefab` (الـ prefab المفتوح في وضع prefab)، و`editor` (إنشاء GameObjects والمواد والمجلدات والمشاهد؛ وفتح المشاهد والـ prefabs وحفظها؛ ونسخ الأصول ونقلها وحذفها؛ وتنفيذ عناصر القوائم)، و`settings`، و`console` (إدخالات سجل المحرر الملتقطة)، و`shaders`، و`componentTypes`، و`tests`، و`testResults`، و`profiler`، و`lighting`، و`assetStore` (حزم Asset Store المملوكة والمنزّلة مع الاستيراد؛ ويفتح `SignIn()` نافذة تسجيل الدخول في Unity للمستخدم عندما يكون المحرر غير مسجّل الدخول). يُشار إلى الأصول بمسارها عبر `unity.asset("Assets/…")`.

## 📸 لقطات الشاشة
| الأداة | المعرّف | افتراضيًا | الوصف |
| :--- | :--- | :--- | :--- |
| **Screenshot / Isolated** | `screenshot-isolated` | ظاهرة | عرض GameObject بمعزل عن غيره من زاوية مختارة (مع عرض مركّب 2x2 اختياري). |
| **Screenshot / Scene View** | `screenshot-scene-view` | مخفية | التقاط Scene View في محرر Unity. |
| **Screenshot / Game View** | `screenshot-game-view` | مخفية | التقاط Game View في محرر Unity. |
| **Screenshot / Camera** | `screenshot-camera` | مخفية | التقاط ما تراه كاميرا كصورة. |

تقبل الأدوات الأربع جميعها `savePath` اختياريًا، عندما يريد المستخدم الصورة كملف. يُكتب ملف PNG أيضًا داخل المشروع وتقول النتيجة `Saved to <absolute path>`؛ ومع ذلك تُعاد الصورة. يبقى `Screenshots/shot.png` ملفًا عاديًا؛ أما المسار تحت `Assets/` فيُستورد فورًا كأصل Unity مع ملف `.meta`. يُرفض: أي شيء خارج المشروع (`..`، والمسارات المطلقة الأخرى، والروابط الرمزية أو نقاط الوصل (junction) في المسار)، ومجلدات Unity الخاصة (`Library`، `Temp`، `Logs`، `obj`، `Packages`، `ProjectSettings`، `UserSettings`)، وأي اسم لا ينتهي بـ `.png`، والملف الموجود مسبقًا (فهي لا تكتب فوقه أبدًا).

## 📜 البرمجة والاختبارات
| الأداة | المعرّف | افتراضيًا | الوصف |
| :--- | :--- | :--- | :--- |
| **Script / Execute** | `script-execute` | ظاهرة | ترجمة شيفرة C# وتنفيذها ديناميكيًا باستخدام Roslyn. |
| **Script / Read** | `script-read` | مخفية | قراءة محتوى ملف نصي برمجي. |
| **Script / Update or Create** | `script-update-or-create` | مخفية | تحديث ملف نصي برمجي أو إنشاؤه بشيفرة C# المقدَّمة. |
| **Script / Delete** | `script-delete` | مخفية | حذف ملف (ملفات) نصية برمجية. |
| **Tests / Run** | `tests-run` | ظاهرة | تشغيل اختبارات Unity (EditMode/PlayMode): أي تحديد RQL فوق المصدر `tests` كتشغيل واحد، مع تصفية النتائج بـ RQL. |

## 📦 الحزم
| الأداة | المعرّف | افتراضيًا | الوصف |
| :--- | :--- | :--- | :--- |
| **Package / List** | `package-list` | ظاهرة | سرد الحزم المثبّتة في المشروع. |
| **Package / Search** | `package-search` | ظاهرة | البحث في سجل Unity Package Manager وفي الحزم المثبّتة. |
| **Package / Add** | `package-add` | ظاهرة | تثبيت حزمة من السجل، أو من عنوان Git URL، أو من مسار محلي. |
| **Package / Remove** | `package-remove` | ظاهرة | إلغاء تثبيت حزمة. |

## 🛒 Asset Store
| الأداة | المعرّف | افتراضيًا | الوصف |
| :--- | :--- | :--- | :--- |
| **Asset Store / Library** | `assetstore-library` | ظاهرة | سرد حزم Asset Store التي تملكها (My Assets، عبر تسجيل دخول Unity في المحرر) والحزم الموجودة في ذاكرة التنزيل المحلية، مصفّاة بالكلمات؛ ويذكر كل صف ما إذا كانت الحزمة منزّلة ومستوردة بالفعل. |
| **Asset Store / Add** | `assetstore-add` | ظاهرة | استيراد حزمة منزّلة دون نافذة الاستيراد؛ وتُعيد الـ prefabs والمواد والنماذج والمشاهد والملاحظات الخاصة بها (خط أنابيب العرض، والسكربتات، والحزم المتداخلة). على Unity 6.6 يؤكّد المستخدم نافذة "Missing signature" في Unity. |

يتولى مصدر RQL `assetStore` الباقي: يسرد `Contents(filter)` ملفات حزمة دون استيرادها، ويستورد `ImportFile(path)` ملف `.unitypackage` متداخلًا (مثل نسخة خاصة بخط أنابيب عرض)، ويبدأ `SignIn()` تسجيل الدخول في Unity عندما يكون المحرر غير مسجّل الدخول (`rql-schema "assetStore"`).

## 🧰 إدارة الأدوات
| الأداة | المعرّف | افتراضيًا | الوصف |
| :--- | :--- | :--- | :--- |
| **Tool / List** | `unity-tool-list` | ظاهرة | سرد كل الأدوات وما إذا كان الوكيل يراها. |
| **Tool / Set Visibility** | `tool-set-visibility` | مخفية | إظهار الأدوات أو إخفاؤها في قائمة أدوات الوكيل. |
| **Tool / Set Enabled State** | `tool-set-enabled-state` | مخفية | اسم بديل مهمل لـ `tool-set-visibility`. |

## 📊 التحليل الأدائي والتشخيص
| الأداة | المعرّف | الوصف |
| :--- | :--- | :--- |
| **Profiler (RQL)** | `rql-query` / `rql-mutate` على المصدر `profiler` | لقطات الذاكرة والعرض والسكربتات والإطارات وأعلام الوحدات؛ والاستدعاءات `Start` و`Stop` و`ClearFrames` و`EnableModule` و`Save` و`Load`. |

## 💡 الإضاءة
| الأداة | المعرّف | الوصف |
| :--- | :--- | :--- |
| **Lighting (RQL)** | `rql-query` / `rql-mutate` على المصدر `lighting` | بيئة المشهد النشط (الإضاءة المحيطة، والضباب، وskybox، والشمس، والانعكاسات؛ مع إمكانية التراجع)، وأصل Lighting Settings، والخَبز (bake)؛ والاستدعاءات `CreateSettings` و`UseSettings` و`Bake` و`CancelBake` و`ClearBakedData` و`BakeReflectionProbes`. |
| **Light probes (RQL)** | `call PlaceGrid(size, spacing)` على `LightProbeGroup` | ملء صندوق حول المجموعة بمسابر متساوية التباعد. |
| **Reflection probes (RQL)** | `call Bake()` على `ReflectionProbe` | خَبز مسبار Custom في خامة بجوار مشهده، وإعادة عرض مسبار Realtime (أما مسابر Baked فتُخبز مع الإضاءة). |

## 🔁 الأدوات المحذوفة في 1.0.0 وما يقابلها في RQL
تستخدم الدروس المكتوبة للإصدارات السابقة هذه المعرّفات. لم تعد موجودة؛ استخدم RQL (التواقيع الدقيقة: `rql-schema`).

</div>

| المعرّف القديم | الآن |
| :--- | :--- |
| `scene-list-opened`, `scene-get-data` | `rql-query`: `scenes`, `roots`, `objects` |
| `scene-create`, `scene-open`, `scene-save` | `rql-mutate`: `editor \| call NewScene(…)`, `OpenScene(…)`, `SaveScenes()`؛ لمشهد بلا عنوان: `scenes \| where … \| call SaveAs("Assets/….unity")` |
| `scene-set-active`, `scene-unload` | `rql-mutate`: `scenes \| where … \| call SetActive()`, `Close()` |
| `gameobject-create` | `rql-mutate`: `editor \| call CreateGameObject({name, primitive, position, rotation, scale})` |
| `gameobject-duplicate` | `rql-mutate`: `objects \| where name == "…" \| call Duplicate()` |
| `gameobject-destroy` | `rql-mutate`: `objects \| where name == "…" \| call Destroy()` |
| `gameobject-find` | `rql-query`: `objects \| where name == "…" \| select path name active`، أو `unity.find("Level/Player")` |
| `gameobject-set-parent` | `rql-mutate`: `… \| set transform.parent = ref(…)` |
| `gameobject-modify`, `object-modify` | `rql-mutate`: `… \| set <member> = <value>` |
| `gameobject-component-add` | `rql-mutate`: `… \| call AddComponent("Rigidbody")` |
| `gameobject-component-get`, `gameobject-component-modify`, `gameobject-component-destroy` | `… \| get components \| where $ is Rigidbody \| select …` / `set …` / `call Destroy()` |
| `gameobject-component-list-all` | `rql-query`: `componentTypes` |
| `assets-find`, `assets-get-data`, `assets-modify` | `unity.assets("t:Material")`; `unity.asset("Assets/…") \| select …` / `set …` |
| `assets-material-create` | `rql-mutate`: `editor \| call CreateMaterial("Assets/….mat")` |
| `assets-create-folder`, `assets-copy`, `assets-move`, `assets-delete`, `assets-refresh` | `rql-mutate`: `editor \| call CreateFolder(…)`, `CopyAsset(…)`, `MoveAsset(…)`, `DeleteAsset(…)`, `RefreshAssets()`؛ والحفظ بـ `SaveAssets()` |
| `assets-prefab-create` | `rql-mutate`: `… \| call SaveAsPrefab("Assets/….prefab")` |
| `assets-prefab-open`, `assets-prefab-save`, `assets-prefab-close`, `assets-prefab-instantiate` | `rql-mutate`: `editor \| call OpenPrefab(…)`, `SavePrefab()`, `ClosePrefab()`, `InstantiatePrefab(…)`؛ المصدر `prefab` |
| `assets-shader-list-all` | `rql-query`: `shaders` |
| `console-get-logs`, `console-clear-logs` | `rql-query`: `console`; `rql-mutate`: `editor \| call ClearConsole()` |
| `editor-application-get-state`, `editor-application-set-state` | `editor \| select isPlaying isPaused isCompiling`; `editor \| set isPlaying = true` |
| `editor-selection-get`, `editor-selection-set` | `rql-query`: `selection`; `rql-mutate`: `editor \| set selection = [ref(…)]` |

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
