[English](default-mcp-tools.md) | [中文](default-mcp-tools.zh-CN.md) | [日本語](default-mcp-tools.ja.md) | [Español](default-mcp-tools.es.md) | [فارسی](default-mcp-tools.fa.md) | <b>हिन्दी</b> | [العربية](default-mcp-tools.ar.md)

# डिफ़ॉल्ट MCP टूल्स

[README](README.hi.md) पर वापस जाएँ। यही रजिस्ट्री ऑनलाइन [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools) पर देखें।

Unity-MCP AI मॉडलों को Unity Editor और Runtime के लिए टूल्स का एक छोटा सेट देता है। Editor का अधिकांश कार्य **RQL** के ज़रिए होता है, जो Editor के डेटा पर एक क्वेरी भाषा है: `rql-query` पढ़ता है, `rql-mutate` बदलता है, `rql-schema` सोर्स, टाइप्स और कॉल्स का वर्णन करता है। 1.0.0 से RQL पिछले वर्ज़नों के प्रति-एक्शन-एक-टूल वाले टूल्स की जगह लेता है ([#1017](https://github.com/IvanMurzak/Unity-MCP/pull/1017)); अंत में दी गई तालिका पुराने IDs को RQL से मैप करती है।

> **नोट**: IDs kebab-case में हैं। *डिफ़ॉल्ट* टूल्स इंस्टॉलेशन के बाद AI एजेंट को दिखाए जाते हैं; *hidden* टूल्स इंस्टॉल होते हैं और नाम से कॉल किए जाने पर चलते हैं, और इन्हें `tool-set-visibility` से, AI Game Developer विंडो में, या `unity-mcp-cli configure <path> --enable-tools <ids>` से दिखाया जाता है।

## 🔎 RQL
| टूल | ID | डिफ़ॉल्ट | विवरण |
| :--- | :--- | :--- | :--- |
| **RQL / Query** | `rql-query` | shown | Editor डेटा पढ़ें, जैसे `roots \| recurse children \| select name active`। |
| **RQL / Mutate** | `rql-mutate` | shown | Editor डेटा बदलें या Editor ऑपरेशन कॉल करें, जैसे `editor \| call CreateGameObject({name: "Ground", primitive: "Plane"})`। |
| **RQL / Schema** | `rql-schema` | shown | सोर्स, टाइप्स, मेंबर्स और कॉल्स का वर्णन करें; `rql-schema "guide:<topic>"` एक गाइड प्रिंट करता है। |

RQL सोर्स: `scenes`, `roots`, `objects` (लोड किए गए सीन में हर GameObject), `selection`, `prefab` (prefab मोड में खुला prefab), `editor` (GameObjects, materials, फ़ोल्डर और सीन बनाना; सीन और prefabs खोलना और सहेजना; assets कॉपी करना, ले जाना और हटाना; मेनू आइटम चलाना), `settings`, `console` (कैप्चर की गई Editor लॉग प्रविष्टियाँ), `shaders`, `componentTypes`, `tests`, `testResults`, `profiler`, `lighting`, `assetStore` (इम्पोर्ट सहित स्वामित्व वाले और डाउनलोड किए गए Asset Store पैकेज; जब Editor साइन आउट हो, तो `SignIn()` उपयोगकर्ता के लिए Unity का साइन-इन खोलता है)। Assets को पाथ से `unity.asset("Assets/…")` के ज़रिए संबोधित किया जाता है।

## 📸 स्क्रीनशॉट
| टूल | ID | डिफ़ॉल्ट | विवरण |
| :--- | :--- | :--- | :--- |
| **Screenshot / Isolated** | `screenshot-isolated` | shown | किसी GameObject को चुने गए कोण से अलग करके रेंडर करें (वैकल्पिक संयुक्त 2x2 व्यू)। |
| **Screenshot / Scene View** | `screenshot-scene-view` | hidden | Unity Editor का Scene View कैप्चर करें। |
| **Screenshot / Game View** | `screenshot-game-view` | hidden | Unity Editor का Game View कैप्चर करें। |
| **Screenshot / Camera** | `screenshot-camera` | hidden | किसी कैमरे का व्यू इमेज के रूप में कैप्चर करें। |

चारों एक वैकल्पिक `savePath` लेते हैं, उस स्थिति के लिए जब उपयोगकर्ता चित्र को फ़ाइल के रूप में चाहता है। PNG प्रोजेक्ट के अंदर भी लिखा जाता है और परिणाम में `Saved to <absolute path>` लिखा होता है; इमेज फिर भी लौटाई जाती है। `Screenshots/shot.png` एक सादी फ़ाइल रहती है; `Assets/` के अंतर्गत कोई पाथ तुरंत `.meta` के साथ Unity asset के रूप में इम्पोर्ट हो जाता है। अस्वीकृत: प्रोजेक्ट के बाहर कुछ भी (`..`, अन्य पूर्ण पाथ, पाथ में कोई symlink या junction), Unity के अपने फ़ोल्डर (`Library`, `Temp`, `Logs`, `obj`, `Packages`, `ProjectSettings`, `UserSettings`), `.png` के अलावा कोई नाम, और मौजूदा फ़ाइल (यह कभी ओवरराइट नहीं करता)।

## 📜 स्क्रिप्टिंग और टेस्ट
| टूल | ID | डिफ़ॉल्ट | विवरण |
| :--- | :--- | :--- | :--- |
| **Script / Execute** | `script-execute` | shown | Roslyn का उपयोग करके C# कोड को डायनामिक रूप से कंपाइल और एक्ज़िक्यूट करें। |
| **Script / Read** | `script-read` | hidden | किसी स्क्रिप्ट फ़ाइल की सामग्री पढ़ें। |
| **Script / Update or Create** | `script-update-or-create` | hidden | दिए गए C# कोड से स्क्रिप्ट फ़ाइल अपडेट करें या बनाएँ। |
| **Script / Delete** | `script-delete` | hidden | स्क्रिप्ट फ़ाइल(ें) हटाएँ। |
| **Tests / Run** | `tests-run` | shown | Unity टेस्ट (EditMode/PlayMode) चलाएँ: `tests` सोर्स पर कोई भी RQL चयन एक रन के रूप में, परिणाम RQL से फ़िल्टर किए गए। |

## 📦 पैकेज
| टूल | ID | डिफ़ॉल्ट | विवरण |
| :--- | :--- | :--- | :--- |
| **Package / List** | `package-list` | shown | प्रोजेक्ट में इंस्टॉल किए गए पैकेजों की सूची बनाएँ। |
| **Package / Search** | `package-search` | shown | Unity Package Manager रजिस्ट्री और इंस्टॉल किए गए पैकेजों में खोजें। |
| **Package / Add** | `package-add` | shown | रजिस्ट्री, Git URL या लोकल पाथ से कोई पैकेज इंस्टॉल करें। |
| **Package / Remove** | `package-remove` | shown | कोई पैकेज अनइंस्टॉल करें। |

## 🛒 Asset Store
| टूल | ID | डिफ़ॉल्ट | विवरण |
| :--- | :--- | :--- | :--- |
| **Asset Store / Library** | `assetstore-library` | shown | आपके स्वामित्व वाले Asset Store पैकेज (My Assets, Editor के Unity साइन-इन के ज़रिए) और लोकल डाउनलोड कैश वाले पैकेज, शब्दों से फ़िल्टर करके सूचीबद्ध करें; हर पंक्ति बताती है कि वह डाउनलोड हुआ है या नहीं और पहले से इम्पोर्ट है या नहीं। |
| **Asset Store / Add** | `assetstore-add` | shown | डाउनलोड किए गए पैकेज को इम्पोर्ट डायलॉग के बिना इम्पोर्ट करें; उसके prefabs, materials, models, scenes और नोट्स (render pipeline, scripts, nested packages) लौटाता है। Unity 6.6 पर उपयोगकर्ता Unity की "Missing signature" विंडो की पुष्टि करता है। |

बाकी RQL सोर्स `assetStore` में है: `Contents(filter)` किसी पैकेज की फ़ाइलों को इम्पोर्ट किए बिना सूचीबद्ध करता है, `ImportFile(path)` एक नेस्टेड `.unitypackage` (जैसे कोई render-pipeline वैरिएंट) इम्पोर्ट करता है, `SignIn()` Editor के साइन आउट होने पर Unity साइन-इन शुरू करता है (`rql-schema "assetStore"`)।

## 🧰 टूल प्रबंधन
| टूल | ID | डिफ़ॉल्ट | विवरण |
| :--- | :--- | :--- | :--- |
| **Tool / List** | `unity-tool-list` | shown | हर टूल की सूची और यह कि एजेंट उसे देखता है या नहीं। |
| **Tool / Set Visibility** | `tool-set-visibility` | hidden | एजेंट की टूल सूची में टूल्स दिखाएँ या छिपाएँ। |
| **Tool / Set Enabled State** | `tool-set-enabled-state` | hidden | `tool-set-visibility` का अप्रचलित उपनाम। |

## 📊 प्रोफ़ाइलिंग और डायग्नोस्टिक्स
| टूल | ID | विवरण |
| :--- | :--- | :--- |
| **Profiler (RQL)** | सोर्स `profiler` पर `rql-query` / `rql-mutate` | मेमोरी, रेंडरिंग, स्क्रिप्ट और फ़्रेम स्नैपशॉट और मॉड्यूल फ़्लैग; कॉल्स `Start`, `Stop`, `ClearFrames`, `EnableModule`, `Save`, `Load`। |

## 💡 लाइटिंग
| टूल | ID | विवरण |
| :--- | :--- | :--- |
| **Lighting (RQL)** | सोर्स `lighting` पर `rql-query` / `rql-mutate` | सक्रिय सीन का एनवायरनमेंट (ambient, fog, skybox, sun, reflections; undo योग्य), Lighting Settings asset और बेक; कॉल्स `CreateSettings`, `UseSettings`, `Bake`, `CancelBake`, `ClearBakedData`, `BakeReflectionProbes`। |
| **Light probes (RQL)** | किसी `LightProbeGroup` पर `call PlaceGrid(size, spacing)` | समूह के चारों ओर एक बॉक्स को समान दूरी वाले probes से भरें। |
| **Reflection probes (RQL)** | किसी `ReflectionProbe` पर `call Bake()` | Custom probe को उसके सीन के बगल में एक टेक्सचर में बेक करें, Realtime probe को फिर से रेंडर करें (Baked probes लाइटिंग के साथ बेक होते हैं)। |

## 🔁 1.0.0 में हटाए गए टूल्स और उनके RQL समकक्ष
पिछले वर्ज़नों के लिए लिखे गए ट्यूटोरियल इन IDs का उपयोग करते हैं। ये अब मौजूद नहीं हैं; RQL का उपयोग करें (सटीक सिग्नेचर: `rql-schema`)।

| पुराना ID | अब |
| :--- | :--- |
| `scene-list-opened`, `scene-get-data` | `rql-query`: `scenes`, `roots`, `objects` |
| `scene-create`, `scene-open`, `scene-save` | `rql-mutate`: `editor \| call NewScene(…)`, `OpenScene(…)`, `SaveScenes()`; बिना शीर्षक वाला सीन: `scenes \| where … \| call SaveAs("Assets/….unity")` |
| `scene-set-active`, `scene-unload` | `rql-mutate`: `scenes \| where … \| call SetActive()`, `Close()` |
| `gameobject-create` | `rql-mutate`: `editor \| call CreateGameObject({name, primitive, position, rotation, scale})` |
| `gameobject-duplicate` | `rql-mutate`: `objects \| where name == "…" \| call Duplicate()` |
| `gameobject-destroy` | `rql-mutate`: `objects \| where name == "…" \| call Destroy()` |
| `gameobject-find` | `rql-query`: `objects \| where name == "…" \| select path name active`, या `unity.find("Level/Player")` |
| `gameobject-set-parent` | `rql-mutate`: `… \| set transform.parent = ref(…)` |
| `gameobject-modify`, `object-modify` | `rql-mutate`: `… \| set <member> = <value>` |
| `gameobject-component-add` | `rql-mutate`: `… \| call AddComponent("Rigidbody")` |
| `gameobject-component-get`, `gameobject-component-modify`, `gameobject-component-destroy` | `… \| get components \| where $ is Rigidbody \| select …` / `set …` / `call Destroy()` |
| `gameobject-component-list-all` | `rql-query`: `componentTypes` |
| `assets-find`, `assets-get-data`, `assets-modify` | `unity.assets("t:Material")`; `unity.asset("Assets/…") \| select …` / `set …` |
| `assets-material-create` | `rql-mutate`: `editor \| call CreateMaterial("Assets/….mat")` |
| `assets-create-folder`, `assets-copy`, `assets-move`, `assets-delete`, `assets-refresh` | `rql-mutate`: `editor \| call CreateFolder(…)`, `CopyAsset(…)`, `MoveAsset(…)`, `DeleteAsset(…)`, `RefreshAssets()`; `SaveAssets()` से सहेजें |
| `assets-prefab-create` | `rql-mutate`: `… \| call SaveAsPrefab("Assets/….prefab")` |
| `assets-prefab-open`, `assets-prefab-save`, `assets-prefab-close`, `assets-prefab-instantiate` | `rql-mutate`: `editor \| call OpenPrefab(…)`, `SavePrefab()`, `ClosePrefab()`, `InstantiatePrefab(…)`; सोर्स `prefab` |
| `assets-shader-list-all` | `rql-query`: `shaders` |
| `console-get-logs`, `console-clear-logs` | `rql-query`: `console`; `rql-mutate`: `editor \| call ClearConsole()` |
| `editor-application-get-state`, `editor-application-set-state` | `editor \| select isPlaying isPaused isCompiling`; `editor \| set isPlaying = true` |
| `editor-selection-get`, `editor-selection-set` | `rql-query`: `selection`; `rql-mutate`: `editor \| set selection = [ref(…)]` |

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
