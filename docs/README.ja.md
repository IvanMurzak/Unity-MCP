<div align="center" width="100%">
  <h1>✨ AI ゲーム開発者 — <i>Unity MCP</i></h1>

[![MCP](https://badge.mcpx.dev?type=server 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/level-building.gif" alt="AI work" title="Level building" width="100%">

</div>

📖 [English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md)

---

`Unity MCP` は `MCP クライアント` と `Unity` の橋渡しとして機能するAI駆動のゲーム開発アシスタントです。チャットにメッセージを入力するだけで、お好みの高度なLLMモデルを使用して作業を完了できます。修正が必要な問題がありますか？AIに修正してもらいましょう。**[デモ動画をご覧ください](https://www.youtube.com/watch?v=kQUOCQ-c0-M&list=PLyueiUu0xU70uzNoOaanGQD2hiyJmqHtK)**。

> **[💬 Discordサーバーに参加](https://discord.gg/cfbdMZX99G)** - 質問、作品の紹介、他の開発者との交流ができます！

## 機能

- ✔️ **自然な会話** - 人間と話すようにAIとチャット
- ✔️ **コードアシスタンス** - AIにコードの作成とテストの実行を依頼
- ✔️ **デバッグサポート** - AIにログの取得とエラーの修正を依頼
- ✔️ **複数のLLMプロバイダー** - Anthropic、OpenAI、Microsoft、または他のプロバイダーのエージェントを制限なく使用
- ✔️ **柔軟なデプロイメント** - 設定によりローカル（stdio）およびリモート（http）で動作
- ✔️ **豊富なツールセット** - 幅広いデフォルト[MCPツール](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)
- ✔️ **拡張可能** - プロジェクトコードで[カスタムMCPツール](#カスタムmcpツールの追加)を作成

### 安定性ステータス

| Unityバージョン | エディットモード | プレイモード | スタンドアロン |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.61f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-61f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-61f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-61f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 2023.2.20f1   | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-20f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-20f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-20f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 6000.2.3f1    | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-2-3f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-2-3f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-2-3f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  |

## 目次

- [インストール](#インストール)
  - [ステップ1：`Unity MCPプラグイン`のインストール](#ステップ1unity-mcpプラグインのインストール)
    - [オプション1 - インストーラー](#オプション1---インストーラー)
    - [オプション2 - OpenUPM-CLI](#オプション2---openupm-cli)
  - [ステップ2：`MCPクライアント`のインストール](#ステップ2mcpクライアントのインストール)
  - [ステップ3：`MCPクライアント`の設定](#ステップ3mcpクライアントの設定)
    - [自動設定](#自動設定)
    - [手動設定](#手動設定)
- [AIの使用](#aiの使用)
  - [LLMの高度な機能](#llmの高度な機能)
    - [コア機能](#コア機能)
    - [リフレクション機能](#リフレクション機能)
- [MCPのカスタマイズ](#mcpのカスタマイズ)
  - [カスタム`MCPツール`の追加](#カスタムmcpツールの追加)
  - [カスタムランタイム（ゲーム内）`MCPツール`の追加](#カスタムランタイムゲーム内mcpツールの追加)
  - [カスタム`MCPプロンプト`の追加](#カスタムmcpプロンプトの追加)
- [Unity `MCPサーバー` セットアップ](#unity-mcpサーバーセットアップ)
  - [変数](#変数)
  - [Docker 📦](#docker-)
    - [`HTTP` トランスポート](#http-トランスポート)
    - [`STDIO` トランスポート](#stdio-トランスポート)
    - [カスタム`ポート`](#カスタムポート)
  - [バイナリ実行ファイル](#バイナリ実行ファイル)
- [動作原理](#動作原理)
  - [`MCP`とは](#mcpとは)
  - [`MCPクライアント`とは](#mcpクライアントとは)
  - [`MCPサーバー`とは](#mcpサーバーとは)
  - [`MCPツール`とは](#mcpツールとは)
    - [`MCPツール`をいつ使用するか](#mcpツールをいつ使用するか)
  - [`MCPリソース`とは](#mcpリソースとは)
    - [`MCPリソース`をいつ使用するか](#mcpリソースをいつ使用するか)
  - [`MCPプロンプト`とは](#mcpプロンプトとは)
    - [`MCPプロンプト`をいつ使用するか](#mcpプロンプトをいつ使用するか)
- [貢献 💙💛](#貢献-)

# インストール

## ステップ1：`Unity MCPプラグイン`のインストール

<details>
  <summary><b>⚠️ 要件（クリック）</b></summary>

> [!重要]
> **プロジェクトパスにスペースを含めることはできません**
>
> - ✅ `C:/MyProjects/Project`
> - ❌ `C:/My Projects/Project`

</details>

### オプション1 - インストーラー

- **[⬇️ インストーラーをダウンロード](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.18.0/AI-Game-Dev-Installer.unitypackage)**
- **📂 インストーラーをUnityプロジェクトにインポート**
  > - ファイルをダブルクリック - Unityが自動的に開きます
  > - または：最初にUnityエディターを開き、`Assets/Import Package/Custom Package`をクリックして、ファイルを選択

### オプション2 - OpenUPM-CLI

- [⬇️ OpenUPM-CLIのインストール](https://github.com/openupm/openupm-cli#installation)
- 📟 Unityプロジェクトフォルダーでコマンドラインを開く

```bash
openupm add com.ivanmurzak.unity.mcp
```

## ステップ2：`MCPクライアント`のインストール

お好みの単一の`MCPクライアント`を選択してください - すべてインストールする必要はありません。これがLLMとの通信のメインチャットウィンドウになります。

- [Claude Code](https://github.com/anthropics/claude-code)（強く推奨）
- [Claude Desktop](https://claude.ai/download)
- [GitHub Copilot in VS Code](https://code.visualstudio.com/docs/copilot/overview)
- [Cursor](https://www.cursor.com/)
- [Windsurf](https://windsurf.com)
- その他サポートされているクライアント

> MCPプロトコルは非常に汎用的で、お好みの任意のMCPクライアントを使用できます - 他のクライアントと同様にスムーズに動作します。唯一の重要な要件は、MCPクライアントが動的MCPツール更新をサポートしている必要があることです。

## ステップ3：`MCPクライアント`の設定

### 自動設定

- Unityプロジェクトを開く
- `Window/AI Game Developer (Unity-MCP)`を開く
- MCPクライアントで`Configure`をクリック

![Unity_AI](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/ai-connector-window.gif)

> MCPクライアントがリストにない場合は、ウィンドウに表示されている生のJSONを使用してMCPクライアントに注入してください。特定のMCPクライアントでこれを行う方法については、説明書をお読みください。

### 手動設定

何らかの理由で自動設定が動作しない場合は、`AI Game Developer (Unity-MCP)`ウィンドウのJSONを使用して任意の`MCPクライアント`を手動設定してください。

<details>
  <summary><b>Windows</b>用<b><code>Claude Code</code></b>の設定</summary>

  `unityProjectPath`を実際のプロジェクトパスに置き換えてください

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" client-transport=stdio
  ```

</details>

<details>
  <summary><b>MacOS Apple-Silicon</b>用<b><code>Claude Code</code></b>の設定</summary>

  `unityProjectPath`を実際のプロジェクトパスに置き換えてください

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary><b>MacOS Apple-Intel</b>用<b><code>Claude Code</code></b>の設定</summary>

  `unityProjectPath`を実際のプロジェクトパスに置き換えてください

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary><b>Linux x64</b>用<b><code>Claude Code</code></b>の設定</summary>

  `unityProjectPath`を実際のプロジェクトパスに置き換えてください

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" client-transport=stdio
  ```

</details>

<details>
  <summary><b>Linux arm64</b>用<b><code>Claude Code</code></b>の設定</summary>

  `unityProjectPath`を実際のプロジェクトパスに置き換えてください

  ```bash
  claude mcp add Unity-MCP "<unityProjectPath>/Library/mcp-server/linux-arm64/unity-mcp-server" client-transport=stdio
  ```

</details>

---

# AIの使用

`MCPクライアント`でAI（LLM）と通信します。やりたいことは何でも依頼してください。タスクやアイデアをより良く説明すればするほど、より良いパフォーマンスを発揮します。

一部の`MCPクライアント`では異なるLLMモデルを選択できます。この機能に注意してください。一部のモデルは他のモデルよりもはるかに良く動作する場合があります。

**コマンド例：**

```text
シーン階層を説明してください
```

```text
半径2の円に3つの立方体を作成してください
```

```text
メタリックゴールドマテリアルを作成し、球体ゲームオブジェクトにアタッチしてください
```

> MCPクライアントで`Agent`モードがオンになっていることを確認してください

## LLMの高度な機能

Unity MCPは、LLMがより速く効果的に動作し、ミスを避け、エラーが発生した際に自己修正できるようにする高度なツールを提供します。すべてが目標を効率的に達成するように設計されています。

### コア機能

- ✔️ **エージェント対応ツール** - 1〜2ステップで必要なものを見つける
- ✔️ **インスタントコンパイル** - より高速な反復のための`Roslyn`を使用したC#コードコンパイルと実行
- ✔️ **完全なアセットアクセス** - アセットとC#スクリプトへの読み書きアクセス
- ✔️ **インテリジェントフィードバック** - 適切な問題理解のための詳細なポジティブおよびネガティブフィードバック

### リフレクション機能

- ✔️ **オブジェクト参照** - インスタントC#コード用の既存オブジェクトへの参照を提供
- ✔️ **プロジェクトデータアクセス** - 読み取り可能な形式でプロジェクト全体のデータへの完全アクセス
- ✔️ **細かい変更** - プロジェクト内の任意のデータ片を入力・変更
- ✔️ **メソッド発見** - コンパイルされたDLLファイルを含むコードベース全体で任意のメソッドを見つける
- ✔️ **メソッド実行** - コードベース全体で任意のメソッドを呼び出し
- ✔️ **高度なパラメータ** - メソッド呼び出しに任意のプロパティを提供、メモリ内の既存オブジェクトへの参照も含む
- ✔️ **ライブUnity API** - Unity APIがすぐに利用可能 - Unityが変更されても、最新のAPIを取得
- ✔️ **自己文書化** - `Description`属性を通じて任意の`class`、`method`、`field`、`property`の人間が読める説明にアクセス

---

# MCPのカスタマイズ

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)**は、プロジェクトオーナーによるカスタム`MCPツール`、`MCPリソース`、`MCPプロンプト`の開発をサポートします。MCPサーバーは`Unity MCPプラグイン`からデータを取得し、それをクライアントに公開します。MCP通信チェーンの誰もが新しいMCP機能についての情報を受信し、LLMは適切なタイミングでそれらを使用することを決定できます。

## カスタム`MCPツール`の追加

カスタム`MCPツール`を追加するには、以下が必要です：

1. `McpPluginToolType`属性を持つクラス
2. `McpPluginTool`属性を持つクラス内のメソッド
3. *オプション：* 各メソッド引数に`Description`属性を追加してLLMの理解を助ける
4. *オプション：* `string? optional = null`プロパティを`?`とデフォルト値で使用してLLMの`optional`としてマークする

> `MainThread.Instance.Run(() =>`の行に注意してください。これにより、Unity APIとの相互作用に必要なメインスレッドでコードを実行できます。これが不要で、バックグラウンドスレッドでツールを実行することが受け入れられる場合は、効率のためにメインスレッドの使用を避けてください。

```csharp
[McpPluginToolType]
public class Tool_GameObject
{
    [McpPluginTool
    (
        "MyCustomTask",
        Title = "Create a new GameObject"
    )]
    [Description("LLMにこれが何か、いつ呼び出されるべきかをここで説明してください。")]
    public string CustomTask
    (
        [Description("LLMにこれが何かを説明してください。")]
        string inputData
    )
    {
        // バックグラウンドスレッドで何でも実行

        return MainThread.Instance.Run(() =>
        {
            // 必要に応じてメインスレッドで何かを実行

            return $"[成功] 操作が完了しました。";
        });
    }
}
```

## カスタムランタイム（ゲーム内）`MCPツール`の追加

> ⚠️ まだサポートされていません。作業が進行中です

## カスタム`MCPプロンプト`の追加

`MCPプロンプト`により、LLMとの会話に事前定義されたプロンプトを注入できます。これらは、AIの動作を導くコンテキスト、指示、または知識を提供できるスマートテンプレートです。プロンプトは静的テキストか、プロジェクトの現在の状態に基づいて動的に生成できます。

```csharp
[McpPluginPromptType]
public static class Prompt_ScriptingCode
{
    [McpPluginPrompt(Name = "add-event-system", Role = Role.User)]
    [Description("GameObjects間でUnityEventベースの通信システムを実装します。")]
    public string AddEventSystem()
    {
        return "ゲームシステムとコンポーネント間の非結合通信のために、UnityEvents、UnityActions、またはカスタムイベントデリゲートを使用してイベントシステムを作成します。";
    }
}
```

---

# Unity `MCPサーバー` セットアップ

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)**サーバーは多くの異なる起動オプションとDockerデプロイメントをサポートします。両方のトランスポートプロトコルがサポートされています：`http`と`stdio`。Unity MCPサーバーをカスタマイズまたはクラウドにデプロイする必要がある場合、このセクションが適しています。[詳細を読む...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

## 変数

どの起動オプションを選択しても、すべて環境変数とコマンドライン引数の両方を使用したカスタム設定をサポートします。起動するだけであれば、デフォルト値で動作するので、変数に時間を費やす必要はありません。Unityプラグインもデフォルト値を持っていることを確認してください。特に`--port`は同じである必要があります。

| 環境変数 | コマンドライン引数 | 説明 |
|-----------------------------|-----------------------|-----------------------------------------------------------------------------|
| `UNITY_MCP_PORT`            | `--port`              | **クライアント** -> **サーバー** <- **プラグイン** 接続ポート（デフォルト：8080） |
| `UNITY_MCP_PLUGIN_TIMEOUT`  | `--plugin-timeout`    | **プラグイン** -> **サーバー** 接続タイムアウト（ms）（デフォルト：10000） |
| `UNITY_MCP_CLIENT_TRANSPORT`| `--client-transport`  | **クライアント** -> **サーバー** トランスポートタイプ：`stdio`または`http`（デフォルト：`http`） |

> コマンドライン引数は単一の`-`プレフィックス（`-port`）オプションと、プレフィックスなしのオプション（`port`）もサポートします。

## Docker 📦

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)

Dockerがインストールされていることを確認してください。Windowsオペレーティングシステムを使用している場合は、Docker Desktopが起動していることを確認してください。

### `HTTP` トランスポート

```bash
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCPクライアント</code>設定：</summary>

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "url": "http://localhost:8080"
    }
  }
}
```

> クラウドでホストされている場合は、`url`を実際のエンドポイントに置き換えてください

</details>

### `STDIO` トランスポート

この方法を使用するには、`MCPクライアント`がdocker内で`MCPサーバー`を起動する必要があります。これは修正された`MCPクライアント`設定を通じて実現できます。

```bash
docker run -t -e UNITY_MCP_CLIENT_TRANSPORT=stdio -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCPクライアント</code>設定：</summary>

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "command": "docker",
      "args": [
        "run",
        "-t",
        "-e",
        "UNITY_MCP_CLIENT_TRANSPORT=stdio",
        "-p",
        "8080:8080",
        "ivanmurzakdev/unity-mcp-server"
      ]
    }
  }
}
```

</details>

### カスタム`ポート`

```bash
docker run -e UNITY_MCP_PORT=123 -p 123:123 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCPクライアント</code>設定：</summary>

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "url": "http://localhost:123"
    }
  }
}
```

> クラウドでホストされている場合は、`url`を実際のエンドポイントに置き換えてください
</details>

## バイナリ実行ファイル

バイナリファイルから直接Unity `MCPサーバー`を起動できます。CPUアーキテクチャ専用にコンパイルされたバイナリが必要です。[GitHubリリースページ](https://github.com/IvanMurzak/Unity-MCP/releases)をチェックしてください。すべてのCPUアーキテクチャ用のプリコンパイルされたバイナリが含まれています。

```bash
./unity-mcp-server --port 8080 --plugin-timeout 10000 --client-transport stdio
```

<details>
  <summary><code>MCPクライアント</code>設定：</summary>

> `<project>`をUnityプロジェクトパスに置き換えてください。

```json
{
  "mcpServers": {
    "Unity-MCP": {
      "command": "<project>/Library/mcp-server/win-x64/unity-mcp-server.exe",
      "args": [
        "--port=8080",
        "--plugin-timeout=10000",
        "--client-transport=stdio"
      ]
    }
  }
}
```

</details>

---

# 動作原理

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)**はLLMとUnityの橋渡しとして機能します。UnityのツールをLLMに公開し説明し、LLMがインターフェースを理解してユーザーの要求に応じてツールを利用します。

統合された`AI Connector`ウィンドウを使用して、**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)**を[Claude](https://claude.ai/download)や[Cursor](https://www.cursor.com/)などのLLMクライアントに接続します。カスタムクライアントもサポートされています。

システムは高度に拡張可能です - Unityプロジェクトのコードベースで直接カスタム`MCPツール`、`MCPリソース`、`MCPプロンプト`を定義して、AIや自動化クライアントに新しい機能を公開できます。これにより、Unity MCPは高度なワークフロー構築、ラピッドプロトタイピング、開発プロセスへのAI駆動機能統合のための柔軟な基盤となります。

## `MCP`とは

MCP - Model Context Protocol。簡潔に言うと、これはAI、特にLLM（Large Language Model）用の`USB Type-C`です。LLMに外部機能の使用方法を教えます。この場合はUnity Engineや、コード内のカスタムC#メソッドも含みます。[公式ドキュメント](https://modelcontextprotocol.io/)。

## `MCPクライアント`とは

チャットウィンドウを持つアプリケーションです。より良い操作のためのスマートエージェントや、埋め込まれた高度なMCPツールを持つ場合があります。一般的に、よくできたMCPクライアントは、AIがタスクを実行する成功の50%を占めます。そのため、使用に最適なものを選択することが非常に重要です。

## `MCPサーバー`とは

`MCPクライアント`と「何か他のもの」の間の橋渡しです。この特定のケースではUnity Engineです。このプロジェクトには`MCPサーバー`が含まれています。

## `MCPツール`とは

`MCPツール`は、LLMがUnityと相互作用するために呼び出すことができる関数またはメソッドです。これらのツールは、自然言語の要求と実際のUnity操作の間の橋渡しとして機能します。AIに「立方体を作成」や「マテリアルの色を変更」を依頼すると、MCPツールを使用してこれらのアクションを実行します。

**主な特徴：**

- 特定の操作を実行する**実行可能な関数**
- LLMがどのようなデータを提供すべきかを理解するのに役立つ説明付きの**型付きパラメータ**
- 操作の成功または失敗についてのフィードバックを与える**戻り値**
- **スレッド対応** - Unity API呼び出しのためにメインスレッドで実行するか、重い処理のためにバックグラウンドスレッドで実行可能

### `MCPツール`をいつ使用するか

- **反復的なタスクの自動化** - 頻繁に行う一般的な操作のツールを作成
- **複雑な操作** - 複数のUnity API呼び出しを単一の使いやすいツールにバンドル
- **プロジェクト固有のワークフロー** - プロジェクトの特定の構造と規約を理解するツールを構築
- **エラーが起こりやすいタスク** - 検証とエラーハンドリングを含むツールを作成
- **カスタムゲームロジック** - 動的コンテンツ作成のためにゲームシステムをAIに公開

**例：**

- 特定のコンポーネントを持つGameObjectsの作成と設定
- アセット（テクスチャ、マテリアル、プレハブ）のバッチ処理
- ライティングとポストプロセシング効果の設定
- レベルジオメトリの生成やオブジェクトの手続き的配置
- 物理設定やコリジョンレイヤーの設定

## `MCPリソース`とは

`MCPリソース`は、Unityプロジェクト内のデータへの読み取り専用アクセスを提供します。アクションを実行するMCPツールとは異なり、リソースはLLMがプロジェクトの現在の状態、アセット、設定を検査し理解することを可能にします。これらをAIにプロジェクトのコンテキストを提供する「センサー」と考えてください。

**主な特徴：**

- プロジェクトデータとUnityオブジェクトへの**読み取り専用アクセス**
- LLMが理解できる形式で提示される**構造化された情報**
- プロジェクトの現在の状態を反映する**リアルタイムデータ**
- AIが情報に基づいた決定を行うのに役立つ**コンテキスト認識**

### `MCPリソース`をいつ使用するか

- **プロジェクト分析** - AIにプロジェクト構造、アセット、組織を理解させる
- **デバッグ支援** - トラブルシューティングのための現在の状態情報を提供
- **インテリジェントな提案** - AIにより良い推奨事項を作るためのコンテキストを提供
- **ドキュメント生成** - プロジェクト状態に基づいてドキュメントを自動作成
- **アセット管理** - AIが利用可能なアセットとそのプロパティを理解するのを助ける

**例：**

- シーン階層とGameObjectプロパティの公開
- 利用可能なマテリアル、テクスチャとその設定のリスト
- スクリプト依存関係とコンポーネント関係の表示
- 現在のライティング設定とレンダーパイプライン設定の表示
- オーディオソース、アニメーション、パーティクルシステムに関する情報の提供

## `MCPプロンプト`とは

`MCPプロンプト`により、LLMとの会話に事前定義されたプロンプトを注入できます。これらは、AIの動作を導くコンテキスト、指示、または知識を提供できるスマートテンプレートです。プロンプトは静的テキストまたはプロジェクトの現在の状態に基づいて動的に生成できます。

**主な特徴：**

- AIの応答方法に影響を与える**コンテキストガイダンス**
- **ロールベース** - 異なるペルソナ（ユーザー要求またはアシスタント知識）をシミュレート可能
- **動的コンテンツ** - リアルタイムプロジェクトデータを含むことが可能
- 一般的なシナリオとワークフローのための**再利用可能なテンプレート**

### `MCPプロンプト`をいつ使用するか

- **ドメイン知識の提供** - プロジェクト固有のベストプラクティスとコーディング標準を共有
- **コーディング規約の設定** - 命名規約、アーキテクチャパターン、コードスタイルを確立
- **プロジェクト構造についてのコンテキスト提供** - プロジェクトの組織方法とその理由を説明
- **ワークフロー指示の共有** - 一般的なタスクの段階的手順を提供
- **専門知識の注入** - 特定のUnity機能、サードパーティアセット、カスタムシステムに関する情報を追加

**例：**

- 「パブリックメソッドには常にPascalCaseを、プライベートフィールドにはcamelCaseを使用してください」
- 「このプロジェクトはScripts/Events/にあるカスタムイベントシステムを使用しています」
- 「UI要素を作成する際は、常にScene/UI/MainCanvasのCanvasに追加してください」
- 「パフォーマンスが重要です - 頻繁にインスタンス化されるオブジェクトにはオブジェクトプーリングを優先してください」
- 「このプロジェクトはSOLID原則に従います - アーキテクチャの決定を説明してください」

---

# 貢献 💙💛

貢献を高く評価しています。あなたのアイデアを持参して、ゲーム開発をこれまで以上にシンプルにしましょう！新しい`MCPツール`や機能のアイデアがあるか、バグを発見して修正方法を知っていますか？

1. 👉 [プロジェクトをフォーク](https://github.com/IvanMurzak/Unity-MCP/fork)
2. フォークをクローンし、Unityで`./Unity-MCP-Plugin`フォルダーを開く
3. プロジェクトで新しい機能を実装し、コミット、GitHubにプッシュ
4. 元の[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)リポジトリの`main`ブランチをターゲットとするプルリクエストを作成。