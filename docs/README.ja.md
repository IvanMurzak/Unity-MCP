<div align="center" width="100%">
  <h1>✨ AI ゲーム開発者 — <i>Unity MCP</i></h1>

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![OpenUPM](https://img.shields.io/badge/dynamic/json?labelColor=333A41&label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner.jpg" alt="AI work" title="Level building" width="100%">

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md)</b>

</div>

`Unity MCP` は**エディターとランタイム両方に対応した**AI駆動のゲーム開発アシスタントです。MCP経由で **Claude**、**Cursor**、**Windsurf** をUnityに接続します。ワークフローを自動化し、コードを生成し、**ゲーム内でAIを有効化**できます。

他のツールとは異なり、このプラグインは**コンパイル済みゲーム内で動作**し、リアルタイムAIデバッグやプレイヤー-AI間のインタラクションを可能にします。

> **[💬 Discordサーバーに参加](https://discord.gg/cfbdMZX99G)** - 質問、作品の紹介、他の開発者との交流ができます！

## 機能

- ✔️ **AIエージェント** - **Anthropic**、**OpenAI**、**Microsoft**、または他のプロバイダーの最高のエージェントを制限なく使用
- ✔️ **ツール** - Unity Editorで操作するための幅広いデフォルト[MCPツール](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)
- ✔️ **スキル** - 各MCPツールのスキルを自動生成し、MCPツール登録にトークンを浪費せずにツールを使用可能
- ✔️ **コードとテスト** - AIにコードの作成とテストの実行を依頼
- ✔️ **ランタイム（ゲーム内）** - コンパイル済みゲーム内でLLMを直接使用し、動的NPCの動作やデバッグを実現
- ✔️ **デバッグサポート** - AIにログの取得とエラーの修正を依頼
- ✔️ **自然な会話** - 人間と話すようにAIとチャット
- ✔️ **柔軟なデプロイメント** - 設定によりローカル（stdio）およびリモート（http）で動作
- ✔️ **拡張可能** - プロジェクトコードで[カスタムMCPツール](#カスタムmcpツールの追加)を作成

[![インストーラーをダウンロード](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download_ja.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.50.1/AI-Game-Dev-Installer.unitypackage)

![AIゲーム開発者 窓](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/editor/ai-game-developer-windows.png?raw=true)

### ツール

<details>
  <summary>アセット (Assets)</summary>

- `copy` - 指定したパスのアセットをコピーし、新しいパスに保存します
- `create-folder` - 指定した親フォルダー内に新しいフォルダーを作成します
- `delete` - プロジェクトから指定したパスのアセットを削除します
- `find` - 検索フィルター文字列を使用してアセットデータベースを検索します
- `get-data` - シリアル化可能なすべてのフィールドとプロパティを含む、アセットファイルからアセットデータを取得します
- `modify` - プロジェクト内のアセットファイルを修正します
- `move` - プロジェクト内のアセットのパスを移動します（名前の変更にも使用されます）
- `refresh` - アセットデータベース (AssetDatabase) をリフレッシュします

</details>

<details>
  <summary>アセット / マテリアル (Assets / Material)</summary>

- `create` - デフォルトのパラメータで新しいマテリアルアセットを作成します

</details>

<details>
  <summary>アセット / シェーダー (Assets / Shader)</summary>

- `list-all` - プロジェクトのアセットとパッケージで利用可能なすべてのシェーダーをリストします

</details>

<details>
  <summary>アセット / プレハブ (Assets / Prefab)</summary>

- `create` - 現在のアクティブなシーン内の GameObject からプレハブを作成します
- `instantiate` - 現在のアクティブなシーン内にプレハブをインスタンス化します
- `open` - 特定の GameObject のプレハブ編集モードを開きます
- `close` - 現在開いているプレハブを閉じます
- `save` - プレハブ編集モードでプレハブを保存します

</details>

<details>
  <summary>ゲームオブジェクト (GameObject)</summary>

- `create` - 開いているプレハブまたはシーン内に新しい GameObject を作成します
- `destroy` - GameObject とすべてのネストされた GameObject を再帰的に破棄します
- `duplicate` - 開いているプレハブまたはシーン内の GameObject を複製します
- `find` - 提供された情報によって特定の GameObject を検索します
- `modify` - GameObject および/或いはアタッチされたコンポーネントのフィールドとプロパティを修正します
- `set-parent` - GameObject のリストに親 GameObject を設定します

</details>

<details>
  <summary>ゲームオブジェクト / コンポーネント (GameObject / Component)</summary>

- `add` - GameObject にコンポーネントを追加します
- `destroy` - ターゲットの GameObject から 1 つまたは複数のコンポーネントを破棄します
- `get` - GameObject 上の特定のコンポーネントに関する詳細情報を取得します
- `modify` - GameObject 上の特定のコンポーネントを修正します

</details>

<details>
  <summary>コンポーネント (Component)</summary>

- `list` - UnityEngine.Component から拡張された C# クラス名をリストします

</details>

<details>
  <summary>シーン (Scene)</summary>

- `create` - プロジェクトアセット内に新しいシーンを作成します
- `get-data` - 指定したシーン内のルート GameObject のリストを取得します
- `list-opened` - Unity エディターで現在開いているシーンのリストを返します
- `open` - プロジェクトアセットファイルからシーンを開きます
- `save` - 開いているシーンをアセットファイルに保存します
- `set-active` - 指定した開いているシーンをアクティブなシーンとして設定します
- `unload` - Unity エディターで開いているシーンをアンロードします

</details>

<details>
  <summary>スクリプト (Script)</summary>

- `delete` - スクリプトファイルを削除します
- `execute` - Roslyn を使用して C# コードを動的にコンパイルおよび実行します
- `read` - スクリプトファイルの内容を読み取ります
- `update-or-create` - 提供された C# コードでスクリプトファイルを更新または作成します

</details>

<details>
  <summary>コンソール (Console)</summary>

- `get-logs` - フィルタリングオプションを使用して Unity エディターのログを取得します

</details>

<details>
  <summary>エディター / アプリケーション (Editor / Application)</summary>

- `get-state` - Unity エディターのアプリケーション状態（プレイモード、一時停止、コンパイル）に関する情報を返します
- `set-state` - Unity エディターのアプリケーション状態（プレイモードの開始/停止/一時停止）を制御します

</details>

<details>
  <summary>エディター / 選択 (Editor / Selection)</summary>

- `get` - Unity エディターでの現在の選択に関する情報を取得します
- `set` - Unity エディターでの現在の選択を設定します

</details>

<details>
  <summary>リフレクション (Reflection)</summary>

- `method-find` - C# リフレクションを使用してプロジェクト内のメソッドを検索します（プライベートメソッドも含む）
- `method-call` - 入力パラメータを使用して任意の C# メソッドを呼び出し、結果を返します

</details>

<details>
  <summary>テストランナー (Tests)</summary>

- `run` - フィルタリングと詳細な結果を使用して Unity テスト（エディットモード/プレイモード）を実行します

</details>

<details>
  <summary>パッケージマネージャー (Package Manager)</summary>

- `add` - Unity パッケージマネージャーレジストリ、Git URL、またはローカルパスからパッケージをインストールします
- `remove` - Unity プロジェクトからパッケージを削除（アンインストール）します
- `list` - Unity プロジェクトにインストールされているすべてのパッケージ（UPM パッケージ）をリストします
- `search` - Unity パッケージマネージャーレジストリとインストール済みパッケージの両方でパッケージを検索します

</details>

#### 追加ツール

- [アニメーション (Animation)](https://github.com/IvanMurzak/Unity-AI-Animation/)
- [ParticleSystem](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/)
- [ProBuilder](https://github.com/IvanMurzak/Unity-AI-ProBuilder/)

### 安定性ステータス

| Unityバージョン | エディットモード                                                                                                                                                                       | プレイモード                                                                                                                                                                           | スタンドアロン                                                                                                                                                                           |
| --------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3     | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 2023.2.22f1     | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 6000.3.1f1      | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  |

## 目次

- [インストール](#インストール)
  - [ステップ1：`Unity MCPプラグイン`のインストール](#ステップ1unity-mcpプラグインのインストール)
    - [オプション1 - インストーラー](#オプション1---インストーラー)
    - [オプション2 - OpenUPM-CLI](#オプション2---openupm-cli)
  - [ステップ2：`MCPクライアント`のインストール](#ステップ2mcpクライアントのインストール)
  - [ステップ3：`MCPクライアント`の設定](#ステップ3mcpクライアントの設定)
    - [自動設定](#自動設定)
    - [手動設定](#手動設定)
    - [コマンドライン設定](#コマンドライン設定)
- [AIワークフロー例：Claude と Gemini](#aiワークフロー例claude-と-gemini)
  - [LLMの高度な機能](#llmの高度な機能)
    - [コア機能](#コア機能)
    - [リフレクション機能](#リフレクション機能)
- [MCPのカスタマイズ](#mcpのカスタマイズ)
  - [カスタム`MCPツール`の追加](#カスタムmcpツールの追加)
  - [カスタム`MCPプロンプト`の追加](#カスタムmcpプロンプトの追加)
- [ランタイムでの使用（ゲーム内）](#ランタイムでの使用ゲーム内)
  - [サンプル：AI駆動のチェスゲームボット](#サンプルai駆動のチェスゲームボット)
  - [ランタイムでの使用が必要な理由](#ランタイムでの使用が必要な理由)
- [Unity `MCPサーバー` セットアップ](#unity-mcpサーバー-セットアップ)
  - [変数](#変数)
  - [Docker 📦](#docker-)
    - [`streamableHttp` トランスポート](#streamablehttp-トランスポート)
    - [`stdio` トランスポート](#stdio-トランスポート)
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

- **[⬇️ インストーラーをダウンロード](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.50.1/AI-Game-Dev-Installer.unitypackage)**
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
- [Antigravity](https://antigravity.google/)
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

### コマンドライン設定

<details>
  <summary><b>作成 <code>command</code></b></summary>

**1. 環境に応じた`<command>`を選択**

| プラットフォーム    | `<command>`                                                                                                 |
| ------------------- | ----------------------------------------------------------------------------------------------------------- |
| Windows x64         | `"<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows x86         | `"<unityProjectPath>/Library/mcp-server/win-x86/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows arm64       | `"<unityProjectPath>/Library/mcp-server/win-arm64/unity-mcp-server.exe" port=<port> client-transport=stdio` |
| MacOS Apple-Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" port=<port> client-transport=stdio`     |
| MacOS Apple-Intel   | `"<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" port=<port> client-transport=stdio`       |
| Linux x64           | `"<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" port=<port> client-transport=stdio`     |
| Linux arm64         | `"<unityProjectPath>/Library/mcp-server/linux-arm64/unity-mcp-server" port=<port> client-transport=stdio`   |

**2. `<unityProjectPath>`をUnityプロジェクトへのフルパスに置き換える**
**3. `<port>`をAI Game Developer設定のポートに置き換える**
**4. コマンドラインを使用してMCPサーバーを追加**

</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/gemini-64.png?raw=true" width="16" height="16" alt="Gemini"> Gemini</summary>

  ```bash
  gemini mcp add ai-game-developer <command>
  ```

  > 上の表から`<command>`を置き換えてください
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/claude-64.png?raw=true" width="16" height="16" alt="Gemini"> Claude Code</summary>

  ```bash
  claude mcp add ai-game-developer <command>
  ```

  > 上の表から`<command>`を置き換えてください
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/github-copilot-64.png?raw=true" width="16" height="16" alt="Gemini"> GitHub Copilot CLI</summary>

  ```bash
  copilot
  ```

  ```bash
  /mcp add
  ```

  サーバー名: `ai-game-developer`
  サーバータイプ: `local`
  コマンド: `<command>`
  > 上の表から`<command>`を置き換えてください
</details>

---

# AIワークフロー例：Claude と Gemini

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

# ランタイムでの使用（ゲーム内）

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)**をゲーム/アプリで使用します。ツール、リソース、プロンプトを使用できます。デフォルトではツールがないため、カスタムツールを実装する必要があります。

```csharp
// MCPプラグインをビルド
var mcpPlugin = UnityMcpPluginRuntime.Initialize(builder =>
    {
        builder.WithConfig(config =>
        {
            config.Host = "http://localhost:8080";
            config.Token = "your-token";
        });
        // 現在のアセンブリからすべてのツールを自動登録
        builder.WithToolsFromAssembly(Assembly.GetExecutingAssembly());
    })
    .Build();

await mcpPlugin.Connect(); // Unity-MCP-Serverへの再試行付きアクティブ接続を開始

await mcpPlugin.Disconnect(); // アクティブ接続を停止し、既存の接続を閉じる
```

## サンプル：AI駆動のチェスゲームボット

クラシックなチェスゲームがあります。ボットのロジックをLLMに任せましょう。ボットはゲームルールを使用してターンを実行する必要があります。

```csharp
[McpPluginToolType]
public static class ChessGameAI
{
    [McpPluginTool("chess-do-turn", Title = "Do the turn")]
    [Description("Do the turn in the chess game. Returns true if the turn was accepted, false otherwise.")]
    public static Task<bool> DoTurn(int figureId, Vector2Int position)
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.DoTurn(figureId, position));
    }

    [McpPluginTool("chess-get-board", Title = "Get the board")]
    [Description("Get the current state of the chess board.")]
    public static Task<BoardData> GetBoard()
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.GetBoardData());
    }
}
```

## ランタイムでの使用が必要な理由

ユースケースは多数あります。例えば、ボット付きのチェスゲームに取り組んでいるとします。数行のコードを書くだけで、ボットの意思決定をLLMに委託できます。

---

# Unity `MCPサーバー` セットアップ

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)**サーバーは多くの異なる起動オプションとDockerデプロイメントをサポートします。両方のトランスポートプロトコルがサポートされています：`streamableHttp`と`stdio`。Unity MCPサーバーをカスタマイズまたはクラウドにデプロイする必要がある場合、このセクションが適しています。[詳細を読む...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

## 変数

どの起動オプションを選択しても、すべて環境変数とコマンドライン引数の両方を使用したカスタム設定をサポートします。起動するだけであれば、デフォルト値で動作するので、変数に時間を費やす必要はありません。Unityプラグインもデフォルト値を持っていることを確認してください。特に`--port`は同じである必要があります。

| 環境変数                     | コマンドライン引数   | 説明                                                                                             |
| ---------------------------- | -------------------- | ------------------------------------------------------------------------------------------------ |
| `MCP_PLUGIN_PORT`            | `--port`             | **クライアント** -> **サーバー** <- **プラグイン** 接続ポート（デフォルト：8080）                |
| `MCP_PLUGIN_CLIENT_TIMEOUT`   | `--plugin-timeout`   | **プラグイン** -> **サーバー** 接続タイムアウト（ms）（デフォルト：10000）                       |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | **クライアント** -> **サーバー** トランスポートタイプ：`stdio`または`streamableHttp`（デフォルト：`streamableHttp`） |

> コマンドライン引数は単一の`-`プレフィックス（`-port`）オプションと、プレフィックスなしのオプション（`port`）もサポートします。

## Docker 📦

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)

Dockerがインストールされていることを確認してください。Windowsオペレーティングシステムを使用している場合は、Docker Desktopが起動していることを確認してください。

### `streamableHttp` トランスポート

```bash
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCPクライアント</code>設定：</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

> クラウドでホストされている場合は、`url`を実際のエンドポイントに置き換えてください

</details>

### `stdio` トランスポート

この方法を使用するには、`MCPクライアント`がdocker内で`MCPサーバー`を起動する必要があります。これは修正された`MCPクライアント`設定を通じて実現できます。

```bash
docker run -t -e MCP_PLUGIN_CLIENT_TRANSPORT=stdio -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCPクライアント</code>設定：</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "docker",
      "args": [
        "run",
        "-t",
        "-e",
        "MCP_PLUGIN_CLIENT_TRANSPORT=stdio",
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
docker run -e MCP_PLUGIN_PORT=123 -p 123:123 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCPクライアント</code>設定：</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
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
    "ai-game-developer": {
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

**このプロジェクトが役に立ったら、ぜひスター 🌟 をお願いします！**

1. 👉 [開発ドキュメントを読む](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.ja.md)
2. 👉 [プロジェクトをフォーク](https://github.com/IvanMurzak/Unity-MCP/fork)
3. フォークをクローンし、Unityで`./Unity-MCP-Plugin`フォルダーを開く
4. プロジェクトで新しい機能を実装し、コミット、GitHubにプッシュ
5. 元の[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP/compare)リポジトリの`main`ブランチをターゲットとするプルリクエストを作成。
