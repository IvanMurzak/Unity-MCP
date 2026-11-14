[English](default-mcp-tools.md) | [中文](default-mcp-tools.zh-CN.md) | <b>日本語</b> | [Español](default-mcp-tools.es.md) | [فارسی](default-mcp-tools.fa.md) | [हिन्दी](default-mcp-tools.hi.md) | [العربية](default-mcp-tools.ar.md)

# デフォルト MCP ツール

[README](README.ja.md) に戻る。同じレジストリは [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools) でもオンラインで閲覧できます。

Unity-MCP は、Unity Editor とランタイム向けの少数のツールを AI モデルに提供します。Editor 作業の大半は、Editor のデータに対するクエリ言語である **RQL** を通じて行います: `rql-query` は読み取り、`rql-mutate` は変更、`rql-schema` はソース、型、呼び出しを説明します。1.0.0 以降、RQL は以前のバージョンの「1 アクション 1 ツール」形式のツールを置き換えました ([#1017](https://github.com/IvanMurzak/Unity-MCP/pull/1017))。末尾の表は旧 ID と RQL の対応を示しています。

> **注**: ID はケバブケースです。*デフォルト*のツールはインストール後に AI エージェントに表示されます。*非表示*のツールはインストールされており名前を指定すれば実行されますが、表示するには `tool-set-visibility`、AI Game Developer ウィンドウ、または `unity-mcp-cli configure <path> --enable-tools <ids>` を使います。

## 🔎 RQL
| ツール | ID | デフォルト | 説明 |
| :--- | :--- | :--- | :--- |
| **RQL / Query** | `rql-query` | 表示 | Editor のデータを読み取ります。例: `roots \| recurse children \| select name active`。 |
| **RQL / Mutate** | `rql-mutate` | 表示 | Editor のデータを変更したり Editor の操作を呼び出したりします。例: `editor \| call CreateGameObject({name: "Ground", primitive: "Plane"})`。 |
| **RQL / Schema** | `rql-schema` | 表示 | ソース、型、メンバー、呼び出しを説明します。`rql-schema "guide:<topic>"` はガイドを表示します。 |

RQL のソース: `scenes`、`roots`、`objects` (読み込まれたシーン内のすべての GameObject)、`selection`、`prefab` (プレハブモードで開いているプレハブ)、`editor` (GameObject、マテリアル、フォルダー、シーンの作成、シーンとプレハブを開く/保存する、アセットのコピー/移動/削除、メニュー項目の実行)、`settings`、`console` (取得された Editor のログエントリ)、`shaders`、`componentTypes`、`tests`、`testResults`、`profiler`、`lighting`、`assetStore` (所有済みおよびダウンロード済みの Asset Store パッケージとそのインポート。Editor がサインアウトしているときは `SignIn()` でユーザー向けに Unity のサインインを開きます)。アセットは `unity.asset("Assets/…")` でパスによって指定します。

## 📸 スクリーンショット
| ツール | ID | デフォルト | 説明 |
| :--- | :--- | :--- | :--- |
| **Screenshot / Isolated** | `screenshot-isolated` | 表示 | GameObject を単独で、選んだ角度からレンダリングします (2x2 の合成ビューも可能)。 |
| **Screenshot / Scene View** | `screenshot-scene-view` | 非表示 | Unity Editor の Scene View をキャプチャします。 |
| **Screenshot / Game View** | `screenshot-game-view` | 非表示 | Unity Editor の Game View をキャプチャします。 |
| **Screenshot / Camera** | `screenshot-camera` | 非表示 | カメラのビューを画像としてキャプチャします。 |

4 つすべてが、ユーザーが画像をファイルとして欲しい場合のために、省略可能な `savePath` を受け取ります。PNG はプロジェクト内にも書き出され、結果には `Saved to <absolute path>` と表示されます。画像は引き続き返されます。`Screenshots/shot.png` は通常のファイルのままですが、`Assets/` 配下のパスは `.meta` 付きの Unity アセットとしてすぐにインポートされます。拒否されるもの: プロジェクト外のもの (`..`、その他の絶対パス、パス内のシンボリックリンクやジャンクション)、Unity 自身のフォルダー (`Library`、`Temp`、`Logs`、`obj`、`Packages`、`ProjectSettings`、`UserSettings`)、`.png` 以外の名前、そして既存のファイル (上書きは決して行いません)。

## 📜 スクリプトとテスト
| ツール | ID | デフォルト | 説明 |
| :--- | :--- | :--- | :--- |
| **Script / Execute** | `script-execute` | 表示 | Roslyn を使って C# コードを動的にコンパイルして実行します。 |
| **Script / Read** | `script-read` | 非表示 | スクリプトファイルの内容を読み取ります。 |
| **Script / Update or Create** | `script-update-or-create` | 非表示 | 指定された C# コードでスクリプトファイルを更新または作成します。 |
| **Script / Delete** | `script-delete` | 非表示 | スクリプトファイルを削除します。 |
| **Tests / Run** | `tests-run` | 表示 | Unity のテスト (EditMode/PlayMode) を実行します: `tests` ソースに対する任意の RQL 選択を 1 回の実行として行い、結果は RQL でフィルターします。 |

## 📦 パッケージ
| ツール | ID | デフォルト | 説明 |
| :--- | :--- | :--- | :--- |
| **Package / List** | `package-list` | 表示 | プロジェクトにインストールされているパッケージを一覧表示します。 |
| **Package / Search** | `package-search` | 表示 | Unity Package Manager のレジストリとインストール済みパッケージを検索します。 |
| **Package / Add** | `package-add` | 表示 | レジストリ、Git URL、またはローカルパスからパッケージをインストールします。 |
| **Package / Remove** | `package-remove` | 表示 | パッケージをアンインストールします。 |

## 🛒 Asset Store
| ツール | ID | デフォルト | 説明 |
| :--- | :--- | :--- | :--- |
| **Asset Store / Library** | `assetstore-library` | 表示 | 所有している Asset Store パッケージ (Editor の Unity サインインを通じた My Assets) とローカルのダウンロードキャッシュにあるパッケージを、単語でフィルターして一覧表示します。各行にはダウンロード済みかどうか、インポート済みかどうかが示されます。 |
| **Asset Store / Add** | `assetstore-add` | 表示 | ダウンロード済みのパッケージをインポートダイアログなしでインポートし、そのプレハブ、マテリアル、モデル、シーン、注記 (レンダーパイプライン、スクリプト、ネストされたパッケージ) を返します。Unity 6.6 では、ユーザーが Unity の "Missing signature" ウィンドウで確認します。 |

残りは RQL ソース `assetStore` が担います: `Contents(filter)` はパッケージをインポートせずにそのファイルを一覧表示し、`ImportFile(path)` はネストされた `.unitypackage` (例: レンダーパイプラインのバリアント) をインポートし、`SignIn()` は Editor がサインアウトしているときに Unity のサインインを開始します (`rql-schema "assetStore"`)。

## 🧰 ツール管理
| ツール | ID | デフォルト | 説明 |
| :--- | :--- | :--- | :--- |
| **Tool / List** | `unity-tool-list` | 表示 | すべてのツールと、エージェントから見えるかどうかを一覧表示します。 |
| **Tool / Set Visibility** | `tool-set-visibility` | 非表示 | エージェントのツール一覧でツールを表示または非表示にします。 |
| **Tool / Set Enabled State** | `tool-set-enabled-state` | 非表示 | `tool-set-visibility` の非推奨エイリアスです。 |

## 📊 プロファイリングと診断
| ツール | ID | 説明 |
| :--- | :--- | :--- |
| **Profiler (RQL)** | ソース `profiler` に対する `rql-query` / `rql-mutate` | メモリ、レンダリング、スクリプト、フレームのスナップショットとモジュールのフラグ。呼び出し: `Start`、`Stop`、`ClearFrames`、`EnableModule`、`Save`、`Load`。 |

## 💡 ライティング
| ツール | ID | 説明 |
| :--- | :--- | :--- |
| **Lighting (RQL)** | ソース `lighting` に対する `rql-query` / `rql-mutate` | アクティブなシーンの環境 (アンビエント、フォグ、スカイボックス、太陽、反射。元に戻せます)、Lighting Settings アセット、ベイク。呼び出し: `CreateSettings`、`UseSettings`、`Bake`、`CancelBake`、`ClearBakedData`、`BakeReflectionProbes`。 |
| **Light probes (RQL)** | `LightProbeGroup` に対する `call PlaceGrid(size, spacing)` | グループを囲むボックスを均等な間隔のプローブで埋めます。 |
| **Reflection probes (RQL)** | `ReflectionProbe` に対する `call Bake()` | Custom プローブをシーンの隣のテクスチャにベイクし、Realtime プローブを再レンダリングします (Baked プローブはライティングと一緒にベイクされます)。 |

## 🔁 1.0.0 で削除されたツールと RQL での代替
以前のバージョン向けに書かれたチュートリアルではこれらの ID が使われています。これらはもう存在しないため、RQL を使ってください (正確なシグネチャ: `rql-schema`)。

| 旧 ID | 現在 |
| :--- | :--- |
| `scene-list-opened`, `scene-get-data` | `rql-query`: `scenes`, `roots`, `objects` |
| `scene-create`, `scene-open`, `scene-save` | `rql-mutate`: `editor \| call NewScene(…)`, `OpenScene(…)`, `SaveScenes()`; 無題のシーン: `scenes \| where … \| call SaveAs("Assets/….unity")` |
| `scene-set-active`, `scene-unload` | `rql-mutate`: `scenes \| where … \| call SetActive()`, `Close()` |
| `gameobject-create` | `rql-mutate`: `editor \| call CreateGameObject({name, primitive, position, rotation, scale})` |
| `gameobject-duplicate` | `rql-mutate`: `objects \| where name == "…" \| call Duplicate()` |
| `gameobject-destroy` | `rql-mutate`: `objects \| where name == "…" \| call Destroy()` |
| `gameobject-find` | `rql-query`: `objects \| where name == "…" \| select path name active`、または `unity.find("Level/Player")` |
| `gameobject-set-parent` | `rql-mutate`: `… \| set transform.parent = ref(…)` |
| `gameobject-modify`, `object-modify` | `rql-mutate`: `… \| set <member> = <value>` |
| `gameobject-component-add` | `rql-mutate`: `… \| call AddComponent("Rigidbody")` |
| `gameobject-component-get`, `gameobject-component-modify`, `gameobject-component-destroy` | `… \| get components \| where $ is Rigidbody \| select …` / `set …` / `call Destroy()` |
| `gameobject-component-list-all` | `rql-query`: `componentTypes` |
| `assets-find`, `assets-get-data`, `assets-modify` | `unity.assets("t:Material")`; `unity.asset("Assets/…") \| select …` / `set …` |
| `assets-material-create` | `rql-mutate`: `editor \| call CreateMaterial("Assets/….mat")` |
| `assets-create-folder`, `assets-copy`, `assets-move`, `assets-delete`, `assets-refresh` | `rql-mutate`: `editor \| call CreateFolder(…)`, `CopyAsset(…)`, `MoveAsset(…)`, `DeleteAsset(…)`, `RefreshAssets()`; 保存は `SaveAssets()` |
| `assets-prefab-create` | `rql-mutate`: `… \| call SaveAsPrefab("Assets/….prefab")` |
| `assets-prefab-open`, `assets-prefab-save`, `assets-prefab-close`, `assets-prefab-instantiate` | `rql-mutate`: `editor \| call OpenPrefab(…)`, `SavePrefab()`, `ClosePrefab()`, `InstantiatePrefab(…)`; ソース `prefab` |
| `assets-shader-list-all` | `rql-query`: `shaders` |
| `console-get-logs`, `console-clear-logs` | `rql-query`: `console`; `rql-mutate`: `editor \| call ClearConsole()` |
| `editor-application-get-state`, `editor-application-set-state` | `editor \| select isPlaying isPaused isCompiling`; `editor \| set isPlaying = true` |
| `editor-selection-get`, `editor-selection-set` | `rql-query`: `selection`; `rql-mutate`: `editor \| set selection = [ref(…)]` |

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)