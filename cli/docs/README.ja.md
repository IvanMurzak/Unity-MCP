<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npm package')](https://www.npmjs.com/package/unity-mcp-cli)
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

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 向けのクロスプラットフォーム CLI ツールです — プロジェクトの作成、プラグインのインストール、MCP ツールの設定、アクティブな MCP 接続での Unity の起動まで、すべてを単一のコマンドラインから実行できます。

## ![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.svg?raw=true)

- :white_check_mark: **プロジェクトの作成** — Unity Editor を通じて新しい Unity プロジェクトをスキャフォールドする
- :white_check_mark: **エディターのインストール** — コマンドラインから任意の Unity Editor バージョンをインストールする
- :white_check_mark: **プラグインのインストール** — 必要なすべてのスコープ付きレジストリとともに Unity-MCP プラグインを `manifest.json` に追加する
- :white_check_mark: **プラグインの削除** — Unity-MCP プラグインを `manifest.json` から削除する
- :white_check_mark: **設定** — MCP ツール、プロンプト、リソースの有効化・無効化を行う
- :white_check_mark: **ツールの実行** — MCP ツールをコマンドラインから直接実行する
- :white_check_mark: **MCP セットアップ** — サポートされている 14 のエージェントに対して AI エージェント MCP 設定ファイルを書き出す
- :white_check_mark: **スキルのセットアップ** — MCP サーバーを通じて AI エージェント向けのスキルファイルを生成する
- :white_check_mark: **オープン＆接続** — オプションの MCP 環境変数を設定して Unity を起動し、自動サーバー接続を実現する
- :white_check_mark: **クロスプラットフォーム** — Windows、macOS、Linux に対応
- :white_check_mark: **CI 対応** — 非対話型ターミナルを自動検出し、スピナーやカラーを無効化する
- :white_check_mark: **詳細モード** — 任意のコマンドに `--verbose` を付けて詳細な診断出力を取得する
- :white_check_mark: **バージョン対応** — プラグインのバージョンをダウングレードせず、OpenUPM から最新バージョンを解決する

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# クイックスタート

グローバルにインストールして実行:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin /path/to/unity/project
```

または `npx` を使えばグローバルインストール不要で任意のコマンドをすぐに実行できます:

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

> **動作要件:** [Node.js](https://nodejs.org/) ^20.19.0 || >=22.12.0。[Unity Hub](https://unity.com/download) は見つからない場合に自動的にインストールされます。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

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
  - [`setup-mcp`](#setup-mcp)
  - [`setup-skills`](#setup-skills)
  - [`remove-plugin`](#remove-plugin)
  - [グローバルオプション](#グローバルオプション)
- [完全自動化の例](#完全自動化の例)
- [仕組み](#仕組み)
    - [決定論的ポート](#決定論的ポート)
    - [プラグインインストール](#プラグインインストール)
    - [設定ファイル](#設定ファイル)
    - [Unity Hub 統合](#unity-hub-統合)

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# コマンド

## `configure`

`UserSettings/AI-Game-Developer-Config.json` 内の MCP ツール、プロンプト、リソースを設定します。

```bash
unity-mcp-cli configure ./MyGame --list
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | Unity プロジェクトへのパス（位置引数または `--path`） |
| `--list` | いいえ | 現在の設定を一覧表示して終了する |
| `--enable-tools <names>` | いいえ | 特定のツールを有効化する（カンマ区切り） |
| `--disable-tools <names>` | いいえ | 特定のツールを無効化する（カンマ区切り） |
| `--enable-all-tools` | いいえ | すべてのツールを有効化する |
| `--disable-all-tools` | いいえ | すべてのツールを無効化する |
| `--enable-prompts <names>` | いいえ | 特定のプロンプトを有効化する（カンマ区切り） |
| `--disable-prompts <names>` | いいえ | 特定のプロンプトを無効化する（カンマ区切り） |
| `--enable-all-prompts` | いいえ | すべてのプロンプトを有効化する |
| `--disable-all-prompts` | いいえ | すべてのプロンプトを無効化する |
| `--enable-resources <names>` | いいえ | 特定のリソースを有効化する（カンマ区切り） |
| `--disable-resources <names>` | いいえ | 特定のリソースを無効化する（カンマ区切り） |
| `--enable-all-resources` | いいえ | すべてのリソースを有効化する |
| `--disable-all-resources` | いいえ | すべてのリソースを無効化する |

**例 — 特定のツールを有効化してすべてのプロンプトを無効化:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**例 — すべてを有効化:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

Unity Editor を使用して新しい Unity プロジェクトを作成します。

```bash
unity-mcp-cli create-project /path/to/new/project
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | プロジェクトの作成先パス（位置引数または `--path`） |
| `--unity <version>` | いいえ | 使用する Unity Editor バージョン（デフォルトは最も高いインストール済みバージョン） |

**例 — 特定のエディターバージョンでプロジェクトを作成:**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

Unity-MCP プラグインを Unity プロジェクトの `Packages/manifest.json` にインストールします。

```bash
unity-mcp-cli install-plugin ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | Unity プロジェクトへのパス（位置引数または `--path`） |
| `--plugin-version <version>` | いいえ | インストールするプラグインバージョン（デフォルトは [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) の最新バージョン） |

このコマンドは以下を実行します:
1. 必要なすべてのスコープを含む **OpenUPM スコープ付きレジストリ** を追加する
2. `com.ivanmurzak.unity.mcp` を `dependencies` に追加する
3. **ダウングレードしない** — より高いバージョンがすでにインストールされている場合はそれを保持する

**例 — 特定のプラグインバージョンをインストール:**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

> このコマンドを実行した後、Unity Editor でプロジェクトを開いてパッケージのインストールを完了してください。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

Unity Hub CLI を通じて Unity Editor のバージョンをインストールします。

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| 引数 / オプション | 必須 | 説明 |
|---|---|---|
| `[version]` | いいえ | インストールする Unity Editor のバージョン（例: `6000.3.1f1`） |
| `--path <path>` | いいえ | 既存のプロジェクトから必要なバージョンを読み取る |

引数もオプションも指定しない場合、コマンドは Unity Hub のリリース一覧から最新の安定版をインストールします。

**例 — プロジェクトが必要とするエディターバージョンをインストール:**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

Unity プロジェクトを Unity Editor で開きます。デフォルトでは、接続オプションが指定された場合に MCP 接続環境変数を設定します。MCP 接続なしで開くには `--no-connect` を使用します。

```bash
unity-mcp-cli open ./MyGame
```

| オプション | 環境変数 | 必須 | 説明 |
|---|---|---|---|
| `[path]` | — | はい | Unity プロジェクトへのパス（位置引数または `--path`） |
| `--unity <version>` | — | いいえ | 使用する Unity Editor の特定バージョン（デフォルトはプロジェクト設定のバージョン、次に最も高いインストール済みバージョン） |
| `--no-connect` | — | いいえ | MCP 接続環境変数なしで開く |
| `--url <url>` | `UNITY_MCP_HOST` | いいえ | 接続先の MCP サーバー URL |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | いいえ | 接続を強制的に維持する |
| `--token <token>` | `UNITY_MCP_TOKEN` | いいえ | 認証トークン |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | いいえ | 認証モード: `none` または `required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | いいえ | 有効にするツールのカンマ区切りリスト |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | いいえ | トランスポートメソッド: `streamableHttp` または `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | いいえ | `true` または `false` を指定して MCP サーバー自動起動を制御する |

エディターのプロセスはデタッチモードで起動されるため、CLI はすぐに制御を返します。

**例 — MCP 接続でオープン:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**例 — MCP 接続なしでオープン（シンプルオープン）:**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**例 — 認証と特定ツールを指定してオープン:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --tools gameobject-create,gameobject-find
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `run-tool`

HTTP API を通じて MCP ツールを直接実行します。サーバー URL と認証トークンは、現在の接続モード（Custom または Cloud）に基づいてプロジェクトの設定ファイル（`UserSettings/AI-Game-Developer-Config.json`）から**自動的に解決**されます。

```bash
unity-mcp-cli run-tool gameobject-create ./MyGame --input '{"name":"Cube"}'
```

| オプション | 必須 | 説明 |
|---|---|---|
| `<tool-name>` | はい | 実行する MCP ツールの名前 |
| `[path]` | いいえ | Unity プロジェクトパス（位置引数または `--path`） — 設定の読み取りとポート検出に使用 |
| `--url <url>` | いいえ | サーバー URL の直接指定（設定をバイパス） |
| `--token <token>` | いいえ | Bearer トークンの直接指定（設定をバイパス） |
| `--input <json>` | いいえ | ツール引数の JSON 文字列（デフォルトは `{}`） |
| `--input-file <file>` | いいえ | ファイルから JSON 引数を読み込む |
| `--raw` | いいえ | 生の JSON を出力する（フォーマットやスピナーなし） |
| `--timeout <ms>` | いいえ | リクエストタイムアウト（ミリ秒単位、デフォルト: 60000） |

**URL 解決の優先順位:**
1. `--url` → 直接使用
2. 設定ファイル → `host`（Custom モード）または `cloudServerUrl`（Cloud モード）
3. プロジェクトパスからの決定論的ポート

**認証**はプロジェクト設定から自動的に読み取られます（Custom モードでは `token`、Cloud モードでは `cloudToken`）。設定から導出されたトークンを明示的に上書きするには `--token` を使用します。

**例 — ツールを呼び出す（URL と認証は設定から）:**

```bash
unity-mcp-cli run-tool gameobject-find ./MyGame --input '{"query":"Player"}'
```

**例 — URL を明示的に指定:**

```bash
unity-mcp-cli run-tool scene-save --url http://localhost:8080
```

**例 — 生の JSON 出力をパイプ:**

```bash
unity-mcp-cli run-tool assets-list ./MyGame --raw | jq '.results'
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-mcp`

AI エージェント向けの MCP 設定ファイルを書き出します。Unity Editor の UI を使わずにヘッドレス/CI セットアップが可能です。14 のエージェント（Claude Code、Cursor、Gemini、Codex など）すべてに対応しています。

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[agent-id]` | はい | 設定するエージェント（`--list` で一覧表示） |
| `[path]` | いいえ | Unity プロジェクトパス（デフォルトは cwd） |
| `--transport <transport>` | いいえ | トランスポートメソッド: `stdio` または `http`（デフォルト: `http`） |
| `--url <url>` | いいえ | サーバー URL の上書き（http トランスポート用） |
| `--token <token>` | いいえ | 認証トークンの上書き |
| `--list` | いいえ | 利用可能なすべてのエージェント ID を一覧表示する |

**例 — サポートされているすべてのエージェントを一覧表示:**

```bash
unity-mcp-cli setup-mcp --list
```

**例 — Cursor を stdio トランスポートで設定:**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-skills`

MCP サーバーのシステムツール API を呼び出して、AI エージェント向けのスキルファイルを生成します。MCP プラグインがインストールされた Unity Editor が実行中である必要があります。

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[agent-id]` | はい | スキルを生成するエージェント（`--list` で一覧表示） |
| `[path]` | いいえ | Unity プロジェクトパス（デフォルトは cwd） |
| `--url <url>` | いいえ | サーバー URL の上書き |
| `--token <token>` | いいえ | 認証トークンの上書き |
| `--list` | いいえ | スキルサポート状況とともにすべてのエージェントを一覧表示する |
| `--timeout <ms>` | いいえ | リクエストタイムアウト（ミリ秒単位、デフォルト: 60000） |

**例 — スキルサポートのあるエージェントを一覧表示:**

```bash
unity-mcp-cli setup-skills --list
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

Unity-MCP プラグインを Unity プロジェクトの `Packages/manifest.json` から削除します。

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | Unity プロジェクトへのパス（位置引数または `--path`） |

このコマンドは以下を実行します:
1. `com.ivanmurzak.unity.mcp` を `dependencies` から削除する
2. **スコープ付きレジストリとスコープを保持する** — 他のパッケージがそれらに依存している可能性があるため
3. プラグインがインストールされていない場合は **何もしない**

> このコマンドを実行した後、Unity Editor でプロジェクトを開いて変更を適用してください。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## グローバルオプション

以下のオプションはすべてのコマンドで利用可能です:

| オプション | 説明 |
|---|---|
| `-v, --verbose` | トラブルシューティング用の詳細な診断出力を有効化する |
| `--version` | CLI バージョンを表示する |
| `--help` | コマンドのヘルプを表示する |

**例 — 任意のコマンドを詳細出力で実行:**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 完全自動化の例

1つのスクリプトで Unity MCP プロジェクトをゼロから完全セットアップします:

```bash
# 1. 新しい Unity プロジェクトを作成する
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Unity-MCP プラグインをインストールする
unity-mcp-cli install-plugin ./MyAIGame

# 3. すべての MCP ツールを有効化する
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Claude Code の MCP 統合を設定する
unity-mcp-cli setup-mcp claude-code ./MyAIGame

# 5. MCP 接続でプロジェクトを開く
unity-mcp-cli open ./MyAIGame \
  --url http://localhost:8080 \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 仕組み

### 決定論的ポート

CLI は各 Unity プロジェクトのディレクトリパスに基づいて**決定論的なポート**を生成します（SHA256 ハッシュをポート範囲 20000-29999 にマッピング）。これは Unity プラグイン内のポート生成と一致しており、手動設定なしでサーバーとプラグインが自動的に同じポートに合意できます。

### プラグインインストール

`install-plugin` コマンドは `Packages/manifest.json` を直接変更します:
- [OpenUPM](https://openupm.com/) スコープ付きレジストリ（`package.openupm.com`）を追加する
- 必要なすべてのスコープ（`com.ivanmurzak`、`extensions.unity`、`org.nuget.*`）を登録する
- バージョン対応の更新（ダウングレードなし）で `com.ivanmurzak.unity.mcp` 依存関係を追加する

### 設定ファイル

`configure` コマンドは `UserSettings/AI-Game-Developer-Config.json` を読み書きし、以下を制御します:
- **Tools** — AI エージェントが利用できる MCP ツール
- **Prompts** — LLM の会話に注入される事前定義プロンプト
- **Resources** — AI エージェントに公開される読み取り専用データ
- **接続設定** — ホスト URL、認証トークン、トランスポートメソッド、タイムアウト

### Unity Hub 統合

エディターを管理したりプロジェクトを作成するコマンドは **Unity Hub CLI**（`--headless` モード）を使用します。Unity Hub がインストールされていない場合、CLI は**自動的にダウンロードしてインストール**します:
- **Windows** — `UnityHubSetup.exe /S` によるサイレントインストール（管理者権限が必要な場合があります）
- **macOS** — DMG をダウンロードしてマウントし、`Unity Hub.app` を `/Applications` にコピーする
- **Linux** — `UnityHub.AppImage` を `~/Applications/` にダウンロードする

> Unity-MCP プロジェクトの完全なドキュメントについては、[メイン README](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) を参照してください。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
