<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npmパッケージ')](https://www.npmjs.com/package/unity-mcp-cli)
[![Node.js](https://img.shields.io/badge/Node.js-%5E20.19.0%20%7C%7C%20%3E%3D22.12.0-5FA04E?logo=nodedotjs&labelColor=333A41 'Node.js')](https://nodejs.org/)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner-glitch.gif" alt="AI Game Developer" title="Unity MCP CLI" width="100%">

  <p>
    <a href="https://claude.ai/download"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/claude-64.png" alt="Claude" title="Claude" height="36"></a>&nbsp;&nbsp;
    <a href="https://openai.com/index/introducing-codex/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/codex-64.png" alt="Codex" title="Codex" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.cursor.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/cursor-64.png" alt="Cursor" title="Cursor" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/docs/copilot/overview"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/github-copilot-64.png" alt="GitHub Copilot" title="GitHub Copilot" height="36"></a>&nbsp;&nbsp;
    <a href="https://gemini.google.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/gemini-64.png" alt="Gemini" title="Gemini" height="36"></a>&nbsp;&nbsp;
    <a href="https://antigravity.google/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/antigravity-64.png" alt="Antigravity" title="Antigravity" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/vs-code-64.png" alt="VS Code" title="VS Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.jetbrains.com/rider/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/rider-64.png" alt="Rider" title="Rider" height="36"></a>&nbsp;&nbsp;
    <a href="https://visualstudio.microsoft.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/visual-studio-64.png" alt="Visual Studio" title="Visual Studio" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/anthropics/claude-code"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

</div>

<b>[中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 用のクロスプラットフォームCLIツール — プロジェクトの作成、プラグインのインストール、MCPツールの設定、MCP接続を有効にしたUnityの起動まで、すべてを単一のコマンドラインから実行できます。

## ![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.ja.svg?raw=true)

- :white_check_mark: **プロジェクト作成** — Unity Editorを使用して新しいUnityプロジェクトをスキャフォールド
- :white_check_mark: **エディターのインストール** — コマンドラインから任意のバージョンのUnity Editorをインストール
- :white_check_mark: **プラグインのインストール** — 必要なスコープレジストリとともにUnity-MCPプラグインを`manifest.json`に追加
- :white_check_mark: **プラグインの削除** — Unity-MCPプラグインを`manifest.json`から削除
- :white_check_mark: **設定** — MCPツール、プロンプト、リソースの有効化/無効化
- :white_check_mark: **ステータス確認** — Unityプロセス、ローカルサーバー、クラウドサーバーの接続状態を一目で確認
- :white_check_mark: **ツールの実行** — コマンドラインからMCPツールを直接実行
- :white_check_mark: **MCPのセットアップ** — 14種類のサポート対象エージェントのAIエージェントMCP設定ファイルを書き出し
- :white_check_mark: **スキルのセットアップ** — MCPサーバー経由でAIエージェント用のスキルファイルを生成
- :white_check_mark: **準備完了待機** — Unity EditorとMCPサーバーが接続されてツール呼び出しを受け付けるまでポーリング
- :white_check_mark: **起動と接続** — MCP環境変数を設定してUnityを起動し、自動的にサーバーに接続
- :white_check_mark: **クロスプラットフォーム** — Windows、macOS、Linuxに対応
- :white_check_mark: **CI対応** — 非インタラクティブターミナルを自動検出し、スピナーやカラーを無効化
- :white_check_mark: **詳細モード** — 任意のコマンドで`--verbose`を使用して詳細な診断出力を取得
- :white_check_mark: **バージョン管理** — プラグインのバージョンをダウングレードせず、OpenUPMから最新版を解決

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# クイックスタート

グローバルにインストールして実行します：

```bash
# 1.1 Install unity-mcp-cli                                #  ┌────────────────────┐
npm install -g unity-mcp-cli                               #  │ Available AI agent │
                                                           #  ├────────────────────┤
# 1.2 (Optional) Install Unity                             #  │ antigravity        │
unity-mcp-cli install-unity                                #  │ claude-code        │
                                                           #  │ claude-desktop     │
# 1.3 (Optional) Create Unity project                      #  │ cline              │
unity-mcp-cli create-project ./MyUnityProject              #  │ codex              │
                                                           #  │ cursor             │
# 2. Install "AI Game Developer" in Unity project          #  │ gemini             │
unity-mcp-cli install-plugin ./MyUnityProject              #  │ github-copilot-cli │
                                                           #  │ kilo-code          │
# 3. Login to cloud server                                 #  │ open-code          │
unity-mcp-cli login ./MyUnityProject                       #  │ rider-junie        │
                                                           #  │ unity-ai           │
# 4. Open Unity project (auto-connects and generates skills)  │ vs-copilot         │
unity-mcp-cli open ./MyUnityProject                        #  │ vscode-copilot     │
                                                           #  └────────────────────┘
# 5. Wait for Unity Editor to be ready
unity-mcp-cli wait-for-ready ./MyUnityProject
```

または`npx`で任意のコマンドを即座に実行できます — グローバルインストールは不要です：

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

> **必要要件:** [Node.js](https://nodejs.org/) ^20.19.0 || >=22.12.0。[Unity Hub](https://unity.com/download)は見つからない場合、自動的にインストールされます。

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 目次

- [クイックスタート](#クイックスタート)
- [目次](#目次)
- [コマンド](#コマンド)
  - [`configure`](#configure)
  - [`create-project`](#create-project)
  - [`install-plugin`](#install-plugin)
  - [`install-unity`](#install-unity)
  - [`open`](#open)
  - [`run-tool`](#run-tool)
  - [`wait-for-ready`](#wait-for-ready)
  - [`setup-mcp`](#setup-mcp)
  - [`setup-skills`](#setup-skills)
  - [`remove-plugin`](#remove-plugin)
  - [`status`](#status)
  - [グローバルオプション](#グローバルオプション)
- [完全自動化の例](#完全自動化の例)
- [仕組み](#仕組み)
    - [決定論的ポート](#決定論的ポート)
    - [プラグインのインストール](#プラグインのインストール)
    - [設定ファイル](#設定ファイル)
    - [Unity Hub連携](#unity-hub連携)

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# コマンド

## `configure`

`UserSettings/AI-Game-Developer-Config.json`でMCPツール、プロンプト、リソースを設定します。

```bash
unity-mcp-cli configure ./MyGame --list
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | Unityプロジェクトへのパス（位置引数または`--path`） |
| `--list` | いいえ | 現在の設定を表示して終了 |
| `--enable-tools <names>` | いいえ | 特定のツールを有効化（カンマ区切り） |
| `--disable-tools <names>` | いいえ | 特定のツールを無効化（カンマ区切り） |
| `--enable-all-tools` | いいえ | すべてのツールを有効化 |
| `--disable-all-tools` | いいえ | すべてのツールを無効化 |
| `--enable-prompts <names>` | いいえ | 特定のプロンプトを有効化（カンマ区切り） |
| `--disable-prompts <names>` | いいえ | 特定のプロンプトを無効化（カンマ区切り） |
| `--enable-all-prompts` | いいえ | すべてのプロンプトを有効化 |
| `--disable-all-prompts` | いいえ | すべてのプロンプトを無効化 |
| `--enable-resources <names>` | いいえ | 特定のリソースを有効化（カンマ区切り） |
| `--disable-resources <names>` | いいえ | 特定のリソースを無効化（カンマ区切り） |
| `--enable-all-resources` | いいえ | すべてのリソースを有効化 |
| `--disable-all-resources` | いいえ | すべてのリソースを無効化 |

**例 — 特定のツールを有効化し、すべてのプロンプトを無効化：**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**例 — すべてを有効化：**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

Unity Editorを使用して新しいUnityプロジェクトを作成します。

```bash
unity-mcp-cli create-project /path/to/new/project
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | プロジェクトを作成するパス（位置引数または`--path`） |
| `--unity <version>` | いいえ | 使用するUnity Editorのバージョン（デフォルトはインストール済みの最新版） |

**例 — 特定のエディターバージョンでプロジェクトを作成：**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

Unityプロジェクトの`Packages/manifest.json`にUnity-MCPプラグインをインストールします。

```bash
unity-mcp-cli install-plugin ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | Unityプロジェクトへのパス（位置引数または`--path`） |
| `--plugin-version <version>` | いいえ | インストールするプラグインのバージョン（デフォルトは[OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)の最新版） |

このコマンドは以下を実行します：
1. 必要なすべてのスコープを含む**OpenUPMスコープレジストリ**を追加
2. `com.ivanmurzak.unity.mcp`を`dependencies`に追加
3. **ダウングレードしない** — より高いバージョンが既にインストールされている場合、それが維持されます

**例 — 特定のプラグインバージョンをインストール：**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

> このコマンドを実行した後、パッケージのインストールを完了するためにUnity Editorでプロジェクトを開いてください。

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

Unity Hub CLI経由でUnity Editorのバージョンをインストールします。

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| 引数/オプション | 必須 | 説明 |
|---|---|---|
| `[version]` | いいえ | インストールするUnity Editorのバージョン（例：`6000.3.1f1`） |
| `--path <path>` | いいえ | 既存のプロジェクトから必要なバージョンを読み取る |

引数もオプションも指定されない場合、Unity Hubのリリース一覧から最新の安定版をインストールします。

**例 — プロジェクトが必要とするエディターバージョンをインストール：**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

Unity EditorでUnityプロジェクトを開きます。デフォルトでは、接続オプションが指定されている場合にMCP接続環境変数を設定します。MCP接続なしで開くには`--no-connect`を使用してください。

```bash
unity-mcp-cli open ./MyGame
```

| オプション | 環境変数 | 必須 | 説明 |
|---|---|---|---|
| `[path]` | — | はい | Unityプロジェクトへのパス（位置引数または`--path`） |
| `--unity <version>` | — | いいえ | 使用する特定のUnity Editorバージョン（デフォルトはプロジェクト設定のバージョン、なければインストール済みの最新版） |
| `--no-connect` | — | いいえ | MCP接続環境変数なしで開く |
| `--url <url>` | `UNITY_MCP_HOST` | いいえ | 接続先のMCPサーバーURL |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | いいえ | 接続を維持し続ける |
| `--token <token>` | `UNITY_MCP_TOKEN` | いいえ | 認証トークン |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | いいえ | 認証モード：`none`または`required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | いいえ | 有効にするツールのカンマ区切りリスト |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | いいえ | トランスポート方式：`streamableHttp`または`stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | いいえ | MCPサーバーの自動起動を制御（`true`または`false`） |

エディタープロセスはデタッチモードで起動されます — CLIは即座に制御を返します。

**例 — MCP接続付きで開く：**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**例 — MCP接続なしで開く（シンプルな起動）：**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**例 — 認証と特定のツールを指定して開く：**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --tools gameobject-create,gameobject-find
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `run-tool`

HTTP API経由でMCPツールを直接実行します。サーバーURLと認証トークンは、現在の接続モード（カスタムまたはクラウド）に基づいて、プロジェクトの設定ファイル（`UserSettings/AI-Game-Developer-Config.json`）から**自動的に解決**されます。

```bash
unity-mcp-cli run-tool gameobject-create ./MyGame --input '{"name":"Cube"}'
```

| オプション | 必須 | 説明 |
|---|---|---|
| `<tool-name>` | はい | 実行するMCPツールの名前 |
| `[path]` | いいえ | Unityプロジェクトのパス（位置引数または`--path`）— 設定の読み取りとポートの検出に使用 |
| `--url <url>` | いいえ | サーバーURLの直接指定（設定をバイパス） |
| `--token <token>` | いいえ | Bearerトークンの直接指定（設定をバイパス） |
| `--input <json>` | いいえ | ツール引数のJSON文字列（デフォルトは`{}`） |
| `--input-file <file>` | いいえ | ファイルからJSON引数を読み取り |
| `--raw` | いいえ | 生のJSONを出力（フォーマットなし、スピナーなし） |
| `--timeout <ms>` | いいえ | リクエストタイムアウト（ミリ秒単位、デフォルト：60000） |

**URL解決の優先順位：**
1. `--url` → 直接使用
2. 設定ファイル → `host`（カスタムモード）またはハードコードされたクラウドエンドポイント（クラウドモード）
3. プロジェクトパスからの決定論的ポート

**認証**はプロジェクト設定から自動的に読み取られます（カスタムモードでは`token`、クラウドモードでは`cloudToken`）。`--token`を使用すると、設定から取得されたトークンを明示的にオーバーライドできます。

**例 — ツールを呼び出す（URLと認証は設定から取得）：**

```bash
unity-mcp-cli run-tool gameobject-find ./MyGame --input '{"query":"Player"}'
```

**例 — URLを明示的にオーバーライド：**

```bash
unity-mcp-cli run-tool scene-save --url http://localhost:8080
```

**例 — 生のJSON出力をパイプ：**

```bash
unity-mcp-cli run-tool assets-list ./MyGame --raw | jq '.results'
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `wait-for-ready`

Unity EditorとMCPサーバーが接続され、ツール呼び出しを受け付ける準備ができるまで待機します。設定可能な間隔でサーバーをポーリングし、正常に応答するかタイムアウトに達するまで待ちます。`open`でUnityを起動した後、エージェントがツールの呼び出しを開始できるタイミングを知る必要がある自動化スクリプトやAIエージェントのオーケストレーションに便利です。

```bash
unity-mcp-cli wait-for-ready ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unityプロジェクトのパス（位置引数または`--path`）— 設定の読み取りとポートの検出に使用 |
| `--url <url>` | いいえ | サーバーURLの直接指定（設定をバイパス） |
| `--token <token>` | いいえ | Bearerトークンの直接指定（設定をバイパス） |
| `--timeout <ms>` | いいえ | 最大待機時間（ミリ秒単位、デフォルト：120000） |
| `--interval <ms>` | いいえ | ポーリング間隔（ミリ秒単位、デフォルト：3000） |

**例 — デフォルトタイムアウト（120秒）で待機：**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
```

**例 — CI用の短いタイムアウト：**

```bash
unity-mcp-cli wait-for-ready ./MyGame --timeout 60000 --interval 2000
```

**例 — サーバーURLを明示的に指定：**

```bash
unity-mcp-cli wait-for-ready --url http://localhost:8080 --timeout 30000
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-mcp`

AIエージェント用のMCP設定ファイルを書き出し、Unity Editor UIなしでヘッドレス/CIセットアップを可能にします。14種類すべてのエージェント（Claude Code、Cursor、Geminiなど）をサポートしています。

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[agent-id]` | はい | 設定するエージェント（`--list`で一覧を表示） |
| `[path]` | いいえ | Unityプロジェクトのパス（デフォルトはカレントディレクトリ） |
| `--transport <transport>` | いいえ | トランスポート方式：`stdio`または`http`（デフォルト：`http`） |
| `--url <url>` | いいえ | サーバーURLのオーバーライド（httpトランスポート用） |
| `--token <token>` | いいえ | 認証トークンのオーバーライド |
| `--list` | いいえ | 利用可能なすべてのエージェントIDを一覧表示 |

**例 — サポート対象の全エージェントを一覧表示：**

```bash
unity-mcp-cli setup-mcp --list
```

**例 — stdioトランスポートでCursorを設定：**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-skills`

MCPサーバーのシステムツールAPIを呼び出して、AIエージェント用のスキルファイルを生成します。MCPプラグインがインストールされた状態でUnity Editorが実行中である必要があります。

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[agent-id]` | はい | スキルを生成するエージェント（`--list`で一覧を表示） |
| `[path]` | いいえ | Unityプロジェクトのパス（デフォルトはカレントディレクトリ） |
| `--url <url>` | いいえ | サーバーURLのオーバーライド |
| `--token <token>` | いいえ | 認証トークンのオーバーライド |
| `--list` | いいえ | スキルサポート状況を含むすべてのエージェントを一覧表示 |
| `--timeout <ms>` | いいえ | リクエストタイムアウト（ミリ秒単位、デフォルト：60000） |

**例 — スキルサポート対応のエージェントを一覧表示：**

```bash
unity-mcp-cli setup-skills --list
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

Unityプロジェクトの`Packages/manifest.json`からUnity-MCPプラグインを削除します。

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | Unityプロジェクトへのパス（位置引数または`--path`） |

このコマンドは以下を実行します：
1. `dependencies`から`com.ivanmurzak.unity.mcp`を削除
2. **スコープレジストリとスコープを維持** — 他のパッケージが依存している可能性があるため
3. プラグインがインストールされていない場合は**何もしない**

> このコマンドを実行した後、変更を適用するためにUnity Editorでプロジェクトを開いてください。

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `status`

Unity EditorとMCPサーバーの接続状態を確認します。Unityが実行中かどうか、ローカルMCPサーバーが到達可能かどうか、設定されたサーバー（例：クラウド）が到達可能かどうかを表示します。

```bash
unity-mcp-cli status ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unityプロジェクトのパス（位置引数または`--path`） |
| `--url <url>` | いいえ | サーバーURLの直接指定（設定をバイパス） |
| `--token <token>` | いいえ | Bearerトークンの直接指定（設定をバイパス） |
| `--timeout <ms>` | いいえ | プローブタイムアウト（ミリ秒単位、デフォルト：5000） |

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## グローバルオプション

以下のオプションはすべてのコマンドで使用できます：

| オプション | 説明 |
|---|---|
| `-v, --verbose` | トラブルシューティング用の詳細な診断出力を有効化 |
| `--version` | CLIバージョンを表示 |
| `--help` | コマンドのヘルプを表示 |

**例 — 詳細出力付きでコマンドを実行：**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 完全自動化の例

1つのスクリプトで完全なUnity MCPプロジェクトをゼロからセットアップします：

```bash
# 1. Create a new Unity project
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Install the Unity-MCP plugin
unity-mcp-cli install-plugin ./MyAIGame

# 3. Enable all MCP tools
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Login to cloud server (authenticates and saves token)
unity-mcp-cli login ./MyAIGame

# 5. Open the project (auto-connects and generates skills for claude-code)
unity-mcp-cli open ./MyAIGame

# 6. Wait for Unity Editor and MCP server to be ready
unity-mcp-cli wait-for-ready ./MyAIGame

# 7. Run tests to verify everything works
unity-mcp-cli run-tool tests-run ./MyAIGame --input '{"testMode":"EditMode"}'
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 仕組み

### 決定論的ポート

CLIは、ディレクトリパスに基づいて各Unityプロジェクトの**決定論的ポート**を生成します（SHA256ハッシュをポート範囲20000〜29999にマッピング）。これはUnityプラグインのポート生成と一致するため、手動設定なしでサーバーとプラグインが自動的に同じポートで通信できます。

### プラグインのインストール

`install-plugin`コマンドは`Packages/manifest.json`を直接変更します：
- [OpenUPM](https://openupm.com/)スコープレジストリ（`package.openupm.com`）を追加
- 必要なすべてのスコープを登録（`com.ivanmurzak`、`extensions.unity`、`org.nuget.*`）
- バージョン管理付きで`com.ivanmurzak.unity.mcp`依存関係を追加（ダウングレードしない）

### 設定ファイル

`configure`コマンドは`UserSettings/AI-Game-Developer-Config.json`を読み書きし、以下を制御します：
- **ツール** — AIエージェントが利用可能なMCPツール
- **プロンプト** — LLM会話に注入される事前定義プロンプト
- **リソース** — AIエージェントに公開される読み取り専用データ
- **接続設定** — ホストURL、認証トークン、トランスポート方式、タイムアウト

### Unity Hub連携

エディターの管理やプロジェクトの作成を行うコマンドは**Unity Hub CLI**（`--headless`モード）を使用します。Unity Hubがインストールされていない場合、CLIは**自動的にダウンロードしてインストール**します：
- **Windows** — `UnityHubSetup.exe /S`によるサイレントインストール（管理者権限が必要な場合があります）
- **macOS** — DMGをダウンロードしてマウントし、`Unity Hub.app`を`/Applications`にコピー
- **Linux** — `UnityHub.AppImage`を`~/Applications/`にダウンロード

> Unity-MCPプロジェクトの完全なドキュメントについては、[メインREADME](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md)をご覧ください。

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
