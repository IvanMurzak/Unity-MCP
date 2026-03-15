<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npm package')](https://www.npmjs.com/package/unity-mcp-cli)
[![Node.js](https://img.shields.io/badge/Node.js-%5E20.19.0%20%7C%7C%20%3E%3D22.12.0-5FA04E?logo=nodedotjs&labelColor=333A41 'Node.js')](https://nodejs.org/)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner-glitch.gif" alt="AI Game Developer" title="Unity MCP CLI" width="100%">

</div>

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 向けのクロスプラットフォーム CLI ツールです — プロジェクトの作成、プラグインのインストール、MCP ツールの設定、アクティブな MCP 接続で Unity を起動するまで、すべてを単一のコマンドラインから実行できます。

## ![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.svg?raw=true)

- :white_check_mark: **プロジェクトの作成** — Unity Editor を通じて新しい Unity プロジェクトをスキャフォールドする
- :white_check_mark: **エディターのインストール** — コマンドラインから任意の Unity Editor バージョンをインストールする
- :white_check_mark: **プラグインのインストール** — 必要なすべてのスコープ付きレジストリとともに Unity-MCP プラグインを `manifest.json` に追加する
- :white_check_mark: **プラグインの削除** — Unity-MCP プラグインを `manifest.json` から削除する
- :white_check_mark: **設定** — MCP ツール、プロンプト、リソースの有効化・無効化を行う
- :white_check_mark: **接続** — 自動サーバー接続のための MCP 環境変数を設定して Unity を起動する
- :white_check_mark: **クロスプラットフォーム** — Windows、macOS、Linux に対応
- :white_check_mark: **バージョン対応** — プラグインのバージョンをダウングレードせず、OpenUPM から最新バージョンを解決する

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# クイックスタート

インストール不要で `npx` を使って任意のコマンドをすぐに実行できます:

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

またはグローバルにインストール:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin /path/to/unity/project
```

> **動作要件:** [Node.js](https://nodejs.org/) ^20.19.0 または >=22.12.0。[Unity Hub](https://unity.com/download) は見つからない場合に自動的にインストールされます。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 目次

- [クイックスタート](#クイックスタート)
- [コマンド](#コマンド)
  - [`configure`](#configure) — MCP ツール、プロンプト、リソースを設定する
  - [`connect`](#connect) — MCP 接続を確立して Unity を起動する
  - [`create-project`](#create-project) — 新しい Unity プロジェクトを作成する
  - [`install-plugin`](#install-plugin) — Unity-MCP プラグインをプロジェクトにインストールする
  - [`install-unity`](#install-unity) — Unity Hub 経由で Unity Editor をインストールする
  - [`open`](#open) — Unity プロジェクトをエディターで開く
  - [`remove-plugin`](#remove-plugin) — Unity-MCP プラグインをプロジェクトから削除する
- [完全自動化の例](#完全自動化の例)
- [仕組み](#仕組み)

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# コマンド

## `configure`

`UserSettings/AI-Game-Developer-Config.json` 内の MCP ツール、プロンプト、リソースを設定します。

```bash
npx unity-mcp-cli configure ./MyGame --list
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
npx unity-mcp-cli configure ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**例 — すべてを有効化:**

```bash
npx unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `connect`

環境変数を通じて特定の MCP サーバーに接続した状態で Unity プロジェクトを開きます。各オプションは、Unity プラグインが起動時に読み取る `UNITY_MCP_*` 環境変数に対応しています。

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://localhost:8080
```

| オプション | 環境変数 | 必須 | 説明 |
|---|---|---|---|
| `--url <url>` | `UNITY_MCP_HOST` | はい | 接続先の MCP サーバー URL |
| `--path <path>` | — | はい | Unity プロジェクトへのパス |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | いいえ | 接続を強制的に維持する |
| `--token <token>` | `UNITY_MCP_TOKEN` | いいえ | 認証トークン |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | いいえ | 認証モード: `none` または `required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | いいえ | 有効にするツールのカンマ区切りリスト |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | いいえ | トランスポートメソッド: `streamableHttp` または `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | いいえ | `true` または `false` を指定して Unity Editor での MCP サーバー自動起動を制御する（`streamableHttp` トランスポートにのみ適用） |
| `--unity <version>` | — | いいえ | 使用する Unity Editor の特定バージョン（デフォルトはプロジェクト設定のバージョン、次に最も高いインストール済みバージョン） |

このコマンドは対応する `UNITY_MCP_*` 環境変数を設定した状態で Unity Editor を起動し、プラグインが起動時に自動的にそれらを読み取れるようにします。環境変数は実行時にプロジェクトの `UserSettings/AI-Game-Developer-Config.json` 設定ファイルの値を上書きします。

**例 — 認証と特定ツールを指定して接続:**

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --keep-connected \
  --tools gameobject-create,gameobject-find,script-execute
```

**例 — stdio トランスポートで接続（サーバーは AI エージェントが管理）:**

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://localhost:8080 \
  --transport stdio \
  --start-server false
```

**例 — streamableHttp とサーバー自動起動で接続:**

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://localhost:8080 \
  --transport streamableHttp \
  --start-server true \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

Unity Editor を使用して新しい Unity プロジェクトを作成します。

```bash
npx unity-mcp-cli create-project /path/to/new/project
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | プロジェクトの作成先パス（位置引数または `--path`） |
| `--unity <version>` | いいえ | 使用する Unity Editor バージョン（デフォルトは最も高いバージョン） |

**例 — 特定のエディターバージョンでプロジェクトを作成:**

```bash
npx unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

Unity-MCP プラグインを Unity プロジェクトの `Packages/manifest.json` にインストールします。

```bash
npx unity-mcp-cli install-plugin ./MyGame
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
npx unity-mcp-cli install-plugin ./MyGame --plugin-version 0.52.0
```

> このコマンドを実行した後、Unity Editor でプロジェクトを開いてパッケージのインストールを完了してください。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

Unity Hub CLI を通じて Unity Editor のバージョンをインストールします。

```bash
npx unity-mcp-cli install-unity 6000.3.1f1
```

| 引数 / オプション | 必須 | 説明 |
|---|---|---|
| `[version]` | いいえ | インストールする Unity Editor のバージョン（例: `6000.3.1f1`） |
| `--path <path>` | いいえ | 既存のプロジェクトから必要なバージョンを読み取る |

引数もオプションも指定しない場合、コマンドは Unity Hub のリリース一覧から最新の安定版をインストールします。

**例 — プロジェクトが必要とするエディターバージョンをインストール:**

```bash
npx unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

Unity プロジェクトを Unity Editor で開きます。

```bash
npx unity-mcp-cli open ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | Unity プロジェクトへのパス（位置引数または `--path`） |
| `--unity <version>` | いいえ | 使用する Unity Editor の特定バージョン（デフォルトはプロジェクト設定のバージョン、次に最も高いインストール済みバージョン） |

エディターのプロセスはデタッチモードで起動されるため、CLI はすぐに制御を返します。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

Unity-MCP プラグインを Unity プロジェクトの `Packages/manifest.json` から削除します。

```bash
npx unity-mcp-cli remove-plugin ./MyGame
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

# 完全自動化の例

1つのスクリプトで Unity MCP プロジェクトをゼロから完全セットアップします:

```bash
# 1. 新しい Unity プロジェクトを作成する
npx unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Unity-MCP プラグインをインストールする
npx unity-mcp-cli install-plugin ./MyAIGame

# 3. すべての MCP ツールを有効化する
npx unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. MCP 接続でプロジェクトを開く
npx unity-mcp-cli connect \
  --path ./MyAIGame \
  --url http://localhost:8080 \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 仕組み

### 決定論的ポート

CLI は各 Unity プロジェクトのディレクトリパスに基づいて**決定論的なポート**を生成します（SHA256 ハッシュをポート範囲 50000–59999 にマッピング）。これは Unity プラグイン内のポート生成と一致しており、手動設定なしでサーバーとプラグインが自動的に同じポートに合意できます。

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
