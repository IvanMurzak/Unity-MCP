[English](default-mcp-tools.md) | <b>中文</b> | [日本語](default-mcp-tools.ja.md) | [Español](default-mcp-tools.es.md) | [فارسی](default-mcp-tools.fa.md) | [हिन्दी](default-mcp-tools.hi.md) | [العربية](default-mcp-tools.ar.md)

# 默认 MCP 工具

返回 [README](README.zh-CN.md)。也可以在 [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools) 在线浏览同一份工具注册表。

Unity-MCP 为 AI 模型提供了一小组用于 Unity 编辑器和运行时的工具。大部分编辑器工作都通过 **RQL** 完成，这是一种针对编辑器数据的查询语言：`rql-query` 用于读取，`rql-mutate` 用于修改，`rql-schema` 用于描述数据源、类型和调用。自 1.0.0 起，RQL 取代了早期版本中“一个操作一个工具”的做法（[#1017](https://github.com/IvanMurzak/Unity-MCP/pull/1017)）；文末的表格列出了旧 ID 与 RQL 的对应关系。

> **注意**：ID 采用 kebab-case 命名。*默认*（shown）工具在安装后即对 AI 智能体可见；*隐藏*（hidden）工具已安装，按名称调用时会运行，可以通过 `tool-set-visibility`、AI Game Developer 窗口或 `unity-mcp-cli configure <path> --enable-tools <ids>` 将其显示出来。

## 🔎 RQL
| 工具 | ID | 默认 | 描述 |
| :--- | :--- | :--- | :--- |
| **RQL / Query** | `rql-query` | shown | 读取编辑器数据，例如 `roots \| recurse children \| select name active`。 |
| **RQL / Mutate** | `rql-mutate` | shown | 修改编辑器数据或调用编辑器操作，例如 `editor \| call CreateGameObject({name: "Ground", primitive: "Plane"})`。 |
| **RQL / Schema** | `rql-schema` | shown | 描述数据源、类型、成员和调用；`rql-schema "guide:<topic>"` 会打印一份指南。 |

RQL 数据源：`scenes`、`roots`、`objects`（已加载场景中的所有 GameObject）、`selection`、`prefab`（在预制体模式中打开的预制体）、`editor`（创建 GameObject、材质、文件夹和场景；打开和保存场景与预制体；复制、移动和删除资源；执行菜单项）、`settings`、`console`（捕获的编辑器日志条目）、`shaders`、`componentTypes`、`tests`、`testResults`、`profiler`、`lighting`、`assetStore`（已拥有和已下载的 Asset Store 包，支持导入；当编辑器未登录时，`SignIn()` 会为用户打开 Unity 的登录界面）。资源通过路径用 `unity.asset("Assets/…")` 定位。

## 📸 截图
| 工具 | ID | 默认 | 描述 |
| :--- | :--- | :--- | :--- |
| **Screenshot / Isolated** | `screenshot-isolated` | shown | 从指定角度单独渲染一个 GameObject（可选 2x2 组合视图）。 |
| **Screenshot / Scene View** | `screenshot-scene-view` | hidden | 截取 Unity 编辑器的 Scene 视图。 |
| **Screenshot / Game View** | `screenshot-game-view` | hidden | 截取 Unity 编辑器的 Game 视图。 |
| **Screenshot / Camera** | `screenshot-camera` | hidden | 将某个摄像机的视图截取为图像。 |

这四个工具都接受可选的 `savePath` 参数，用于用户希望将图片保存为文件的情况。PNG 也会被写入项目内部，结果中会显示 `Saved to <absolute path>`；图像仍会被返回。`Screenshots/shot.png` 保持为普通文件；位于 `Assets/` 下的路径会立即作为 Unity 资源导入并生成 `.meta`。以下情况会被拒绝：项目之外的任何位置（`..`、其他绝对路径、路径中的符号链接或联接点）、Unity 自身的文件夹（`Library`、`Temp`、`Logs`、`obj`、`Packages`、`ProjectSettings`、`UserSettings`）、扩展名不是 `.png` 的文件名，以及已存在的文件（从不覆盖）。

## 📜 脚本与测试
| 工具 | ID | 默认 | 描述 |
| :--- | :--- | :--- | :--- |
| **Script / Execute** | `script-execute` | shown | 使用 Roslyn 动态编译并执行 C# 代码。 |
| **Script / Read** | `script-read` | hidden | 读取脚本文件的内容。 |
| **Script / Update or Create** | `script-update-or-create` | hidden | 用提供的 C# 代码更新或创建脚本文件。 |
| **Script / Delete** | `script-delete` | hidden | 删除脚本文件。 |
| **Tests / Run** | `tests-run` | shown | 运行 Unity 测试（EditMode/PlayMode）：对 `tests` 数据源的任意 RQL 选择作为一次运行，结果用 RQL 过滤。 |

## 📦 包
| 工具 | ID | 默认 | 描述 |
| :--- | :--- | :--- | :--- |
| **Package / List** | `package-list` | shown | 列出项目中已安装的包。 |
| **Package / Search** | `package-search` | shown | 搜索 Unity Package Manager 注册表和已安装的包。 |
| **Package / Add** | `package-add` | shown | 从注册表、Git URL 或本地路径安装包。 |
| **Package / Remove** | `package-remove` | shown | 卸载一个包。 |

## 🛒 Asset Store
| 工具 | ID | 默认 | 描述 |
| :--- | :--- | :--- | :--- |
| **Asset Store / Library** | `assetstore-library` | shown | 按关键词过滤，列出你拥有的 Asset Store 包（My Assets，通过编辑器的 Unity 登录获取）以及本地下载缓存中的包；每一行都会说明它是否已下载、是否已导入。 |
| **Asset Store / Add** | `assetstore-add` | shown | 不弹出导入对话框直接导入已下载的包；返回其中的预制体、材质、模型、场景和说明（渲染管线、脚本、嵌套包）。在 Unity 6.6 上，需要用户在 Unity 的 "Missing signature" 窗口中确认。 |

其余功能由 RQL 数据源 `assetStore` 提供：`Contents(filter)` 列出包中的文件而不导入，`ImportFile(path)` 导入嵌套的 `.unitypackage`（例如某个渲染管线变体），`SignIn()` 在编辑器未登录时启动 Unity 登录（`rql-schema "assetStore"`）。

## 🧰 工具管理
| 工具 | ID | 默认 | 描述 |
| :--- | :--- | :--- | :--- |
| **Tool / List** | `unity-tool-list` | shown | 列出所有工具以及智能体是否能看到它们。 |
| **Tool / Set Visibility** | `tool-set-visibility` | hidden | 在智能体的工具列表中显示或隐藏工具。 |
| **Tool / Set Enabled State** | `tool-set-enabled-state` | hidden | `tool-set-visibility` 的已弃用别名。 |

## 📊 性能分析与诊断
| 工具 | ID | 描述 |
| :--- | :--- | :--- |
| **Profiler (RQL)** | 对数据源 `profiler` 使用 `rql-query` / `rql-mutate` | 内存、渲染、脚本和帧快照以及模块开关；调用 `Start`、`Stop`、`ClearFrames`、`EnableModule`、`Save`、`Load`。 |

## 💡 光照
| 工具 | ID | 描述 |
| :--- | :--- | :--- |
| **Lighting (RQL)** | 对数据源 `lighting` 使用 `rql-query` / `rql-mutate` | 活动场景的环境（环境光、雾、天空盒、太阳、反射；可撤销）、Lighting Settings 资源以及烘焙；调用 `CreateSettings`、`UseSettings`、`Bake`、`CancelBake`、`ClearBakedData`、`BakeReflectionProbes`。 |
| **Light probes (RQL)** | 对 `LightProbeGroup` 执行 `call PlaceGrid(size, spacing)` | 在该组周围的包围盒中均匀地填充探针。 |
| **Reflection probes (RQL)** | 对 `ReflectionProbe` 执行 `call Bake()` | 将 Custom 探针烘焙为位于其场景旁的纹理，或重新渲染 Realtime 探针（Baked 探针随光照一起烘焙）。 |

## 🔁 1.0.0 中移除的工具及其 RQL 等价写法
为早期版本编写的教程使用这些 ID。它们已不复存在；请使用 RQL（确切签名见 `rql-schema`）。

| 旧 ID | 现在 |
| :--- | :--- |
| `scene-list-opened`, `scene-get-data` | `rql-query`: `scenes`, `roots`, `objects` |
| `scene-create`, `scene-open`, `scene-save` | `rql-mutate`: `editor \| call NewScene(…)`, `OpenScene(…)`, `SaveScenes()`；未命名场景：`scenes \| where … \| call SaveAs("Assets/….unity")` |
| `scene-set-active`, `scene-unload` | `rql-mutate`: `scenes \| where … \| call SetActive()`, `Close()` |
| `gameobject-create` | `rql-mutate`: `editor \| call CreateGameObject({name, primitive, position, rotation, scale})` |
| `gameobject-duplicate` | `rql-mutate`: `objects \| where name == "…" \| call Duplicate()` |
| `gameobject-destroy` | `rql-mutate`: `objects \| where name == "…" \| call Destroy()` |
| `gameobject-find` | `rql-query`: `objects \| where name == "…" \| select path name active`，或 `unity.find("Level/Player")` |
| `gameobject-set-parent` | `rql-mutate`: `… \| set transform.parent = ref(…)` |
| `gameobject-modify`, `object-modify` | `rql-mutate`: `… \| set <member> = <value>` |
| `gameobject-component-add` | `rql-mutate`: `… \| call AddComponent("Rigidbody")` |
| `gameobject-component-get`, `gameobject-component-modify`, `gameobject-component-destroy` | `… \| get components \| where $ is Rigidbody \| select …` / `set …` / `call Destroy()` |
| `gameobject-component-list-all` | `rql-query`: `componentTypes` |
| `assets-find`, `assets-get-data`, `assets-modify` | `unity.assets("t:Material")`; `unity.asset("Assets/…") \| select …` / `set …` |
| `assets-material-create` | `rql-mutate`: `editor \| call CreateMaterial("Assets/….mat")` |
| `assets-create-folder`, `assets-copy`, `assets-move`, `assets-delete`, `assets-refresh` | `rql-mutate`: `editor \| call CreateFolder(…)`, `CopyAsset(…)`, `MoveAsset(…)`, `DeleteAsset(…)`, `RefreshAssets()`；用 `SaveAssets()` 保存 |
| `assets-prefab-create` | `rql-mutate`: `… \| call SaveAsPrefab("Assets/….prefab")` |
| `assets-prefab-open`, `assets-prefab-save`, `assets-prefab-close`, `assets-prefab-instantiate` | `rql-mutate`: `editor \| call OpenPrefab(…)`, `SavePrefab()`, `ClosePrefab()`, `InstantiatePrefab(…)`；数据源 `prefab` |
| `assets-shader-list-all` | `rql-query`: `shaders` |
| `console-get-logs`, `console-clear-logs` | `rql-query`: `console`; `rql-mutate`: `editor \| call ClearConsole()` |
| `editor-application-get-state`, `editor-application-set-state` | `editor \| select isPlaying isPaused isCompiling`; `editor \| set isPlaying = true` |
| `editor-selection-get`, `editor-selection-set` | `rql-query`: `selection`; `rql-mutate`: `editor \| set selection = [ref(…)]` |

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
