[English](default-mcp-tools.md) | [中文](default-mcp-tools.zh-CN.md) | [日本語](default-mcp-tools.ja.md) | <b>Español</b> | [فارسی](default-mcp-tools.fa.md) | [हिन्दी](default-mcp-tools.hi.md) | [العربية](default-mcp-tools.ar.md)

# Herramientas MCP predeterminadas

Volver al [README](README.es.md). Consulta el mismo registro en línea en [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools).

Unity-MCP ofrece a los modelos de IA un pequeño conjunto de herramientas para el Editor y el Runtime de Unity. La mayor parte del trabajo en el Editor pasa por **RQL**, un lenguaje de consulta sobre los datos del Editor: `rql-query` lee, `rql-mutate` modifica, `rql-schema` describe las fuentes, los tipos y las llamadas. Desde la 1.0.0, RQL sustituye a las herramientas de una acción por herramienta de versiones anteriores ([#1017](https://github.com/IvanMurzak/Unity-MCP/pull/1017)); la tabla del final relaciona los ID antiguos con RQL.

> **Nota**: los ID están en kebab-case. Las herramientas *predeterminadas* se muestran al agente de IA tras la instalación; las herramientas *ocultas* están instaladas y se ejecutan cuando se llaman por su nombre, y se muestran con `tool-set-visibility`, en la ventana de AI Game Developer o con `unity-mcp-cli configure <path> --enable-tools <ids>`.

## 🔎 RQL
| Herramienta | ID | Predeterminado | Descripción |
| :--- | :--- | :--- | :--- |
| **RQL / Query** | `rql-query` | visible | Leer datos del Editor, p. ej. `roots \| recurse children \| select name active`. |
| **RQL / Mutate** | `rql-mutate` | visible | Modificar datos del Editor o llamar a operaciones del Editor, p. ej. `editor \| call CreateGameObject({name: "Ground", primitive: "Plane"})`. |
| **RQL / Schema** | `rql-schema` | visible | Describir fuentes, tipos, miembros y llamadas; `rql-schema "guide:<topic>"` muestra una guía. |

Fuentes de RQL: `scenes`, `roots`, `objects` (todos los GameObject de las escenas cargadas), `selection`, `prefab` (el prefab abierto en modo prefab), `editor` (crear GameObjects, materiales, carpetas y escenas; abrir y guardar escenas y prefabs; copiar, mover y eliminar assets; ejecutar elementos de menú), `settings`, `console` (entradas de log del Editor capturadas), `shaders`, `componentTypes`, `tests`, `testResults`, `profiler`, `lighting`, `assetStore` (paquetes de Asset Store propios y descargados, con importación; `SignIn()` abre el inicio de sesión de Unity para el usuario cuando el Editor no tiene sesión iniciada). Los assets se direccionan por ruta con `unity.asset("Assets/…")`.

## 📸 Capturas de pantalla
| Herramienta | ID | Predeterminado | Descripción |
| :--- | :--- | :--- | :--- |
| **Screenshot / Isolated** | `screenshot-isolated` | visible | Renderizar un GameObject de forma aislada desde un ángulo elegido (vista compuesta 2x2 opcional). |
| **Screenshot / Scene View** | `screenshot-scene-view` | oculta | Capturar la Scene View del Editor de Unity. |
| **Screenshot / Game View** | `screenshot-game-view` | oculta | Capturar la Game View del Editor de Unity. |
| **Screenshot / Camera** | `screenshot-camera` | oculta | Capturar la vista de una cámara como imagen. |

Las cuatro aceptan un `savePath` opcional, para cuando el usuario quiere la imagen como archivo. El PNG también se escribe dentro del proyecto y el resultado indica `Saved to <absolute path>`; la imagen se sigue devolviendo. `Screenshots/shot.png` queda como archivo normal; una ruta bajo `Assets/` se importa de inmediato como asset de Unity con un `.meta`. Se rechaza: cualquier cosa fuera del proyecto (`..`, otras rutas absolutas, un symlink o junction en la ruta), las carpetas propias de Unity (`Library`, `Temp`, `Logs`, `obj`, `Packages`, `ProjectSettings`, `UserSettings`), un nombre que no sea `.png` y un archivo existente (nunca sobrescribe).

## 📜 Scripting y pruebas
| Herramienta | ID | Predeterminado | Descripción |
| :--- | :--- | :--- | :--- |
| **Script / Execute** | `script-execute` | visible | Compilar y ejecutar código C# dinámicamente con Roslyn. |
| **Script / Read** | `script-read` | oculta | Leer el contenido de un archivo de script. |
| **Script / Update or Create** | `script-update-or-create` | oculta | Actualizar o crear un archivo de script con el código C# proporcionado. |
| **Script / Delete** | `script-delete` | oculta | Eliminar archivo(s) de script. |
| **Tests / Run** | `tests-run` | visible | Ejecutar pruebas de Unity (EditMode/PlayMode): cualquier selección RQL sobre la fuente `tests` como una sola ejecución, con los resultados filtrados con RQL. |

## 📦 Paquetes
| Herramienta | ID | Predeterminado | Descripción |
| :--- | :--- | :--- | :--- |
| **Package / List** | `package-list` | visible | Listar los paquetes instalados en el proyecto. |
| **Package / Search** | `package-search` | visible | Buscar en el registro de Unity Package Manager y en los paquetes instalados. |
| **Package / Add** | `package-add` | visible | Instalar un paquete desde el registro, una URL de Git o una ruta local. |
| **Package / Remove** | `package-remove` | visible | Desinstalar un paquete. |

## 🛒 Asset Store
| Herramienta | ID | Predeterminado | Descripción |
| :--- | :--- | :--- | :--- |
| **Asset Store / Library** | `assetstore-library` | visible | Listar los paquetes de Asset Store que posees (My Assets, mediante el inicio de sesión de Unity del Editor) y los de la caché local de descargas, filtrados por palabras; cada fila indica si está descargado y si ya está importado. |
| **Asset Store / Add** | `assetstore-add` | visible | Importar un paquete descargado sin el diálogo de importación; devuelve sus prefabs, materiales, modelos, escenas y notas (render pipeline, scripts, paquetes anidados). En Unity 6.6 el usuario confirma la ventana "Missing signature" de Unity. |

La fuente RQL `assetStore` contiene el resto: `Contents(filter)` lista los archivos de un paquete sin importarlo, `ImportFile(path)` importa un `.unitypackage` anidado (p. ej. una variante de render pipeline), `SignIn()` inicia el inicio de sesión de Unity cuando el Editor no tiene sesión iniciada (`rql-schema "assetStore"`).

## 🧰 Gestión de herramientas
| Herramienta | ID | Predeterminado | Descripción |
| :--- | :--- | :--- | :--- |
| **Tool / List** | `unity-tool-list` | visible | Listar todas las herramientas e indicar si el agente las ve. |
| **Tool / Set Visibility** | `tool-set-visibility` | oculta | Mostrar u ocultar herramientas en la lista de herramientas del agente. |
| **Tool / Set Enabled State** | `tool-set-enabled-state` | oculta | Alias obsoleto de `tool-set-visibility`. |

## 📊 Perfilado y diagnóstico
| Herramienta | ID | Descripción |
| :--- | :--- | :--- |
| **Profiler (RQL)** | `rql-query` / `rql-mutate` sobre la fuente `profiler` | Instantáneas de memoria, renderizado, scripts y fotogramas, e indicadores de módulos; llamadas `Start`, `Stop`, `ClearFrames`, `EnableModule`, `Save`, `Load`. |

## 💡 Iluminación
| Herramienta | ID | Descripción |
| :--- | :--- | :--- |
| **Lighting (RQL)** | `rql-query` / `rql-mutate` sobre la fuente `lighting` | El entorno de la escena activa (ambiente, niebla, skybox, sol, reflejos; se puede deshacer), el asset Lighting Settings y el bake; llamadas `CreateSettings`, `UseSettings`, `Bake`, `CancelBake`, `ClearBakedData`, `BakeReflectionProbes`. |
| **Light probes (RQL)** | `call PlaceGrid(size, spacing)` sobre un `LightProbeGroup` | Rellenar una caja alrededor del grupo con probes espaciadas uniformemente. |
| **Reflection probes (RQL)** | `call Bake()` sobre un `ReflectionProbe` | Hacer bake de una probe Custom en una textura junto a su escena, volver a renderizar una Realtime (las probes Baked se hacen bake con la iluminación). |

## 🔁 Herramientas eliminadas en la 1.0.0 y sus equivalentes en RQL
Los tutoriales escritos para versiones anteriores usan estos ID. Ya no existen; usa RQL (firmas exactas: `rql-schema`).

| ID antiguo | Ahora |
| :--- | :--- |
| `scene-list-opened`, `scene-get-data` | `rql-query`: `scenes`, `roots`, `objects` |
| `scene-create`, `scene-open`, `scene-save` | `rql-mutate`: `editor \| call NewScene(…)`, `OpenScene(…)`, `SaveScenes()`; una escena sin título: `scenes \| where … \| call SaveAs("Assets/….unity")` |
| `scene-set-active`, `scene-unload` | `rql-mutate`: `scenes \| where … \| call SetActive()`, `Close()` |
| `gameobject-create` | `rql-mutate`: `editor \| call CreateGameObject({name, primitive, position, rotation, scale})` |
| `gameobject-duplicate` | `rql-mutate`: `objects \| where name == "…" \| call Duplicate()` |
| `gameobject-destroy` | `rql-mutate`: `objects \| where name == "…" \| call Destroy()` |
| `gameobject-find` | `rql-query`: `objects \| where name == "…" \| select path name active`, o `unity.find("Level/Player")` |
| `gameobject-set-parent` | `rql-mutate`: `… \| set transform.parent = ref(…)` |
| `gameobject-modify`, `object-modify` | `rql-mutate`: `… \| set <member> = <value>` |
| `gameobject-component-add` | `rql-mutate`: `… \| call AddComponent("Rigidbody")` |
| `gameobject-component-get`, `gameobject-component-modify`, `gameobject-component-destroy` | `… \| get components \| where $ is Rigidbody \| select …` / `set …` / `call Destroy()` |
| `gameobject-component-list-all` | `rql-query`: `componentTypes` |
| `assets-find`, `assets-get-data`, `assets-modify` | `unity.assets("t:Material")`; `unity.asset("Assets/…") \| select …` / `set …` |
| `assets-material-create` | `rql-mutate`: `editor \| call CreateMaterial("Assets/….mat")` |
| `assets-create-folder`, `assets-copy`, `assets-move`, `assets-delete`, `assets-refresh` | `rql-mutate`: `editor \| call CreateFolder(…)`, `CopyAsset(…)`, `MoveAsset(…)`, `DeleteAsset(…)`, `RefreshAssets()`; guardar con `SaveAssets()` |
| `assets-prefab-create` | `rql-mutate`: `… \| call SaveAsPrefab("Assets/….prefab")` |
| `assets-prefab-open`, `assets-prefab-save`, `assets-prefab-close`, `assets-prefab-instantiate` | `rql-mutate`: `editor \| call OpenPrefab(…)`, `SavePrefab()`, `ClosePrefab()`, `InstantiatePrefab(…)`; fuente `prefab` |
| `assets-shader-list-all` | `rql-query`: `shaders` |
| `console-get-logs`, `console-clear-logs` | `rql-query`: `console`; `rql-mutate`: `editor \| call ClearConsole()` |
| `editor-application-get-state`, `editor-application-set-state` | `editor \| select isPlaying isPaused isCompiling`; `editor \| set isPlaying = true` |
| `editor-selection-get`, `editor-selection-set` | `rql-query`: `selection`; `rql-mutate`: `editor \| set selection = [ref(…)]` |

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
