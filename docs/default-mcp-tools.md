# Default MCP Tools

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/aigamedeveloper/mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/aigamedeveloper/mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

Unity-MCP gives AI models a small set of tools for the Unity Editor and Runtime. Most Editor work goes through **RQL**, a query language over the Editor's data: `rql-query` reads, `rql-mutate` changes, `rql-schema` describes the sources, types and calls. Since 1.0.0 RQL replaces the one-tool-per-action tools of earlier versions ([#1017](https://github.com/IvanMurzak/Unity-MCP/pull/1017)); the table at the end maps the old IDs to RQL.

> **Note**: IDs are kebab-case. *Default* tools are shown to the AI agent after installation; *hidden* tools are installed and run when called by name, and are shown with `tool-set-visibility`, in the AI Game Developer window, or with `unity-mcp-cli configure <path> --enable-tools <ids>`.

## 🔎 RQL
| Tool | ID | Default | Description |
| :--- | :--- | :--- | :--- |
| **RQL / Query** | `rql-query` | shown | Read Editor data, e.g. `roots \| recurse children \| select name active`. |
| **RQL / Mutate** | `rql-mutate` | shown | Change Editor data or call Editor operations, e.g. `editor \| call CreateGameObject({name: "Ground", primitive: "Plane"})`. |
| **RQL / Schema** | `rql-schema` | shown | Describe sources, types, members and calls; `rql-schema "guide:<topic>"` prints a guide. |

RQL sources: `scenes`, `roots`, `objects` (every GameObject in the loaded scenes), `selection`, `prefab` (the prefab open in prefab mode), `editor` (create GameObjects, materials, folders and scenes; open and save scenes and prefabs; copy, move and delete assets; execute menu items), `settings`, `console` (captured Editor log entries), `shaders`, `componentTypes`, `tests`, `testResults`, `profiler`, `lighting`. Assets are addressed by path with `unity.asset("Assets/…")`.

## 📸 Screenshots
| Tool | ID | Default | Description |
| :--- | :--- | :--- | :--- |
| **Screenshot / Isolated** | `screenshot-isolated` | shown | Render a GameObject in isolation from a chosen angle (optional composite 2x2 view). |
| **Screenshot / Scene View** | `screenshot-scene-view` | hidden | Capture the Unity Editor Scene View. |
| **Screenshot / Game View** | `screenshot-game-view` | hidden | Capture the Unity Editor Game View. |
| **Screenshot / Camera** | `screenshot-camera` | hidden | Capture a camera's view as an image. |

## 📜 Scripting & Tests
| Tool | ID | Default | Description |
| :--- | :--- | :--- | :--- |
| **Script / Execute** | `script-execute` | shown | Compile and execute C# code dynamically using Roslyn. |
| **Script / Read** | `script-read` | hidden | Read the content of a script file. |
| **Script / Update or Create** | `script-update-or-create` | hidden | Update or create a script file with the provided C# code. |
| **Script / Delete** | `script-delete` | hidden | Delete script file(s). |
| **Tests / Run** | `tests-run` | shown | Run Unity tests (EditMode/PlayMode): any RQL selection over the `tests` source as one run, results filtered with RQL. |

## 📦 Packages
| Tool | ID | Default | Description |
| :--- | :--- | :--- | :--- |
| **Package / List** | `package-list` | hidden | List the packages installed in the project. |
| **Package / Search** | `package-search` | hidden | Search the Unity Package Manager registry and installed packages. |
| **Package / Add** | `package-add` | hidden | Install a package from the registry, a Git URL or a local path. |
| **Package / Remove** | `package-remove` | hidden | Uninstall a package. |

## 🧰 Tool management
| Tool | ID | Default | Description |
| :--- | :--- | :--- | :--- |
| **Tool / List** | `unity-tool-list` | shown | List every tool and whether the agent sees it. |
| **Tool / Set Visibility** | `tool-set-visibility` | hidden | Show or hide tools in the agent's tool list. |
| **Tool / Set Enabled State** | `tool-set-enabled-state` | hidden | Deprecated alias of `tool-set-visibility`. |

## 📊 Profiling & Diagnostics
| Tool | ID | Description |
| :--- | :--- | :--- |
| **Profiler (RQL)** | `rql-query` / `rql-mutate` on source `profiler` | Memory, rendering, script and frame snapshots and module flags; calls `Start`, `Stop`, `ClearFrames`, `EnableModule`, `Save`, `Load`. |

## 💡 Lighting
| Tool | ID | Description |
| :--- | :--- | :--- |
| **Lighting (RQL)** | `rql-query` / `rql-mutate` on source `lighting` | The active scene's environment (ambient, fog, skybox, sun, reflections; undoable), Lighting Settings asset and the bake; calls `CreateSettings`, `UseSettings`, `Bake`, `CancelBake`, `ClearBakedData`, `BakeReflectionProbes`. |
| **Light probes (RQL)** | `call PlaceGrid(size, spacing)` on a `LightProbeGroup` | Fill a box around the group with evenly spaced probes. |
| **Reflection probes (RQL)** | `call Bake()` on a `ReflectionProbe` | Bake a Custom probe into a texture next to its scene, re-render a Realtime one (Baked probes bake with the lighting). |

## 🔁 Tools removed in 1.0.0 and their RQL equivalents
Tutorials written for earlier versions use these IDs. They no longer exist; use RQL (exact signatures: `rql-schema`).

| Old ID | Now |
| :--- | :--- |
| `scene-list-opened`, `scene-get-data` | `rql-query`: `scenes`, `roots`, `objects` |
| `scene-create`, `scene-open`, `scene-save` | `rql-mutate`: `editor \| call NewScene(…)`, `OpenScene(…)`, `SaveScenes()`; an untitled scene: `scenes \| where … \| call SaveAs("Assets/….unity")` |
| `scene-set-active`, `scene-unload` | `rql-mutate`: `scenes \| where … \| call SetActive()`, `Close()` |
| `gameobject-create` | `rql-mutate`: `editor \| call CreateGameObject({name, primitive, position, rotation, scale})` |
| `gameobject-duplicate` | `rql-mutate`: `objects \| where name == "…" \| call Duplicate()` |
| `gameobject-destroy` | `rql-mutate`: `objects \| where name == "…" \| call Destroy()` |
| `gameobject-find` | `rql-query`: `objects \| where name == "…" \| select path name active`, or `unity.find("Level/Player")` |
| `gameobject-set-parent` | `rql-mutate`: `… \| set transform.parent = ref(…)` |
| `gameobject-modify`, `object-modify` | `rql-mutate`: `… \| set <member> = <value>` |
| `gameobject-component-add` | `rql-mutate`: `… \| call AddComponent("Rigidbody")` |
| `gameobject-component-get`, `gameobject-component-modify`, `gameobject-component-destroy` | `… \| get components \| where $ is Rigidbody \| select …` / `set …` / `call Destroy()` |
| `gameobject-component-list-all` | `rql-query`: `componentTypes` |
| `assets-find`, `assets-get-data`, `assets-modify` | `unity.assets("t:Material")`; `unity.asset("Assets/…") \| select …` / `set …` |
| `assets-material-create` | `rql-mutate`: `editor \| call CreateMaterial("Assets/….mat")` |
| `assets-create-folder`, `assets-copy`, `assets-move`, `assets-delete`, `assets-refresh` | `rql-mutate`: `editor \| call CreateFolder(…)`, `CopyAsset(…)`, `MoveAsset(…)`, `DeleteAsset(…)`, `RefreshAssets()`; save with `SaveAssets()` |
| `assets-prefab-create` | `rql-mutate`: `… \| call SaveAsPrefab("Assets/….prefab")` |
| `assets-prefab-open`, `assets-prefab-save`, `assets-prefab-close`, `assets-prefab-instantiate` | `rql-mutate`: `editor \| call OpenPrefab(…)`, `SavePrefab()`, `ClosePrefab()`, `InstantiatePrefab(…)`; source `prefab` |
| `assets-shader-list-all` | `rql-query`: `shaders` |
| `console-get-logs`, `console-clear-logs` | `rql-query`: `console`; `rql-mutate`: `editor \| call ClearConsole()` |
| `editor-application-get-state`, `editor-application-set-state` | `editor \| select isPlaying isPaused isCompiling`; `editor \| set isPlaying = true` |
| `editor-selection-get`, `editor-selection-set` | `rql-query`: `selection`; `rql-mutate`: `editor \| set selection = [ref(…)]` |

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)