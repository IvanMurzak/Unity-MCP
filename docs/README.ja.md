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

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md)</b>

</div>

`Unity MCP` は **エディター & ランタイム** 向けの AI 駆動ゲーム開発アシスタントです。**Claude**、**Cursor**、**Windsurf** を MCP を通じて Unity に接続できます。ワークフローの自動化、コード生成、そして**ゲーム内での AI 活用**を実現します。

他のツールとは異なり、このプラグインは**コンパイル済みのゲーム内部**でも動作するため、リアルタイムの AI デバッグやプレイヤーと AI のインタラクションが可能です。

> **[💬 Discord サーバーに参加](https://discord.gg/cfbdMZX99G)** - 質問したり、作品を披露したり、他の開発者とつながりましょう！

## ![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.ja.svg?raw=true)

- ✔️ **AI エージェント** - **Anthropic**、**OpenAI**、**Microsoft**、その他あらゆるプロバイダーの最良のエージェントを制限なく使用
- ✔️ **ツール** - Unity Editor で操作するための豊富なデフォルト [MCP ツール](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)
- ✔️ **スキル** - 各 MCP ツールのスキルを自動生成
- ✔️ **コードとテスト** - AI にコードを書かせ、テストを実行
- ✔️ **ランタイム（ゲーム内）** - コンパイル済みゲーム内で直接 LLM を使用し、動的な NPC 挙動やデバッグに活用
- ✔️ **デバッグサポート** - AI にログを取得させ、エラーを修正
- ✔️ **自然な会話** - 人と話すように AI とチャット
- ✔️ **柔軟なデプロイ** - 設定によりローカル（stdio）およびリモート（http）で動作
- ✔️ **拡張可能** - [プロジェクトコードにカスタム MCP ツールを作成](#カスタム-mcp-ツールの追加)可能

[![インストーラーをダウンロード](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.51.2/AI-Game-Dev-Installer.unitypackage)

![AI Game Developer Windows](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/editor/ai-game-developer-windows.png?raw=true)

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# クイックスタート

3 ステップで始められます：

1. **[プラグインをインストール](#ステップ-1-unity-mcp-プラグインのインストール)** — `.unitypackage` インストーラーをダウンロードするか `openupm add com.ivanmurzak.unity.mcp` を実行
2. **[MCP クライアントを選ぶ](#ステップ-2-mcp-クライアントのインストール)** — Claude Code、Claude Desktop、GitHub Copilot、Cursor など
3. **[クライアントを設定](#ステップ-3-mcp-クライアントの設定)** — Unity で `Window/AI Game Developer - MCP` を開き **Configure** をクリック

以上です。AI に *「半径 2 の円上に 3 つのキューブを作成して」* と尋ねると、それが実現されます。✨

---

# ツールリファレンス

プラグインには 3 つのカテゴリにわたる 50 以上の組み込みツールが含まれています。インストール直後にすべてのツールが利用可能で、追加設定は不要です。詳細な説明を含む完全なリファレンスは [docs/default-mcp-tools.md](docs/default-mcp-tools.md) をご覧ください。

<details>
  <summary>プロジェクト & アセット</summary>

- `assets-copy` - 指定パスのアセットをコピーして newPath に保存
- `assets-create-folder` - 指定した親フォルダに新しいフォルダを作成
- `assets-delete` - プロジェクトから指定パスのアセットを削除
- `assets-find` - 検索フィルター文字列でアセットデータベースを検索
- `assets-find-built-in` - Unity Editor の組み込みアセットを検索
- `assets-get-data` - アセットファイルからすべてのシリアライズ可能なフィールドとプロパティを含むアセットデータを取得
- `assets-material-create` - デフォルトパラメーターで新しいマテリアルアセットを作成
- `assets-modify` - プロジェクト内のアセットファイルを変更
- `assets-move` - プロジェクト内のアセットを移動（名前変更にも使用）
- `assets-prefab-close` - 現在開いているプレハブを閉じる
- `assets-prefab-create` - 現在アクティブなシーンの GameObject からプレハブを作成
- `assets-prefab-instantiate` - 現在アクティブなシーンにプレハブをインスタンス化
- `assets-prefab-open` - 特定の GameObject のプレハブ編集モードを開く
- `assets-prefab-save` - プレハブ編集モードでプレハブを保存
- `assets-refresh` - AssetDatabase を更新
- `assets-shader-list-all` - プロジェクトアセットとパッケージ内の利用可能なシェーダーをすべて一覧表示
- `package-add` - Unity Package Manager レジストリ、Git URL、またはローカルパスからパッケージをインストール
- `package-list` - Unity プロジェクトにインストールされたすべてのパッケージを一覧表示（UPM パッケージ）
- `package-remove` - Unity プロジェクトからパッケージをアンインストール
- `package-search` - Unity Package Manager レジストリとインストール済みパッケージの両方でパッケージを検索

</details>

<details>
  <summary>シーン & ヒエラルキー</summary>

- `gameobject-component-add` - GameObject にコンポーネントを追加
- `gameobject-component-destroy` - 対象 GameObject から 1 つまたは複数のコンポーネントを削除
- `gameobject-component-get` - GameObject 上の特定のコンポーネントの詳細情報を取得
- `gameobject-component-list-all` - UnityEngine.Component を継承する C# クラス名を一覧表示
- `gameobject-component-modify` - GameObject 上の特定のコンポーネントを変更
- `gameobject-create` - 開いているプレハブまたはシーン内に新しい GameObject を作成
- `gameobject-destroy` - GameObject とすべてのネストされた GameObject を再帰的に削除
- `gameobject-duplicate` - 開いているプレハブまたはシーン内で GameObject を複製
- `gameobject-find` - 提供された情報で特定の GameObject を検索
- `gameobject-modify` - GameObject およびアタッチされたコンポーネントのフィールドとプロパティを変更
- `gameobject-set-parent` - GameObject のリストに親 GameObject を設定
- `object-get-data` - 指定した Unity オブジェクトのデータを取得
- `object-modify` - 指定した Unity オブジェクトを変更
- `scene-create` - プロジェクトアセットに新しいシーンを作成
- `scene-get-data` - 指定したシーンのルート GameObject のリストを取得
- `scene-list-opened` - Unity Editor で現在開いているシーンのリストを返す
- `scene-open` - プロジェクトアセットファイルからシーンを開く
- `scene-save` - 開いているシーンをアセットファイルに保存
- `scene-set-active` - 指定した開いているシーンをアクティブシーンに設定
- `scene-unload` - Unity Editor の開いているシーンからシーンをアンロード
- `screenshot-camera` - カメラからスクリーンショットをキャプチャして画像として返す
- `screenshot-game-view` - Unity Editor ゲームビューからスクリーンショットをキャプチャ
- `screenshot-scene-view` - Unity Editor シーンビューからスクリーンショットをキャプチャ

</details>

<details>
  <summary>スクリプティング & エディター</summary>

- `console-get-logs` - フィルタリングオプション付きで Unity Editor ログを取得
- `editor-application-get-state` - Unity Editor アプリケーションの状態（プレイモード、一時停止、コンパイル）に関する情報を返す
- `editor-application-set-state` - Unity Editor アプリケーションの状態を制御（プレイモードの開始/停止/一時停止）
- `editor-selection-get` - Unity Editor の現在の選択情報を取得
- `editor-selection-set` - Unity Editor の現在の選択を設定
- `reflection-method-call` - 入力パラメーターで任意の C# メソッドを呼び出し、結果を返す
- `reflection-method-find` - C# リフレクションを使用してプロジェクト内のメソッドを検索（プライベートメソッドも含む）
- `script-delete` - スクリプトファイルを削除
- `script-execute` - Roslyn を使用して C# コードを動的にコンパイル・実行
- `script-read` - スクリプトファイルの内容を読み込む
- `script-update-or-create` - 提供された C# コードでスクリプトファイルを更新または作成
- `tests-run` - フィルタリングと詳細な結果付きで Unity テスト（EditMode/PlayMode）を実行

</details>

#### 追加ツール

より多くのツールが必要な場合は拡張機能をインストールするか、[独自のツールを作成](#カスタム-mcp-ツールの追加)してください。

- [アニメーション](https://github.com/IvanMurzak/Unity-AI-Animation/)
- [パーティクルシステム](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/)
- [ProBuilder](https://github.com/IvanMurzak/Unity-AI-ProBuilder/)

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 目次

- [クイックスタート](#クイックスタート)
- [ツールリファレンス](#ツールリファレンス)
      - [追加ツール](#追加ツール)
- [目次](#目次)
  - [その他のドキュメント](#その他のドキュメント)
- [インストール](#インストール)
  - [ステップ 1: `Unity MCP Plugin` のインストール](#ステップ-1-unity-mcp-plugin-のインストール)
    - [オプション 1 - インストーラー](#オプション-1---インストーラー)
    - [オプション 2 - OpenUPM-CLI](#オプション-2---openupm-cli)
  - [ステップ 2: `MCP Client` のインストール](#ステップ-2-mcp-client-のインストール)
  - [ステップ 3: `MCP Client` の設定](#ステップ-3-mcp-client-の設定)
    - [自動設定](#自動設定)
    - [手動設定](#手動設定)
      - [コマンドライン設定](#コマンドライン設定)
- [AI ワークフロー例: Claude \& Gemini](#ai-ワークフロー例-claude--gemini)
  - [LLM 向け高度な機能](#llm-向け高度な機能)
    - [コア機能](#コア機能)
    - [リフレクション活用機能](#リフレクション活用機能)
- [MCP のカスタマイズ](#mcp-のカスタマイズ)
  - [カスタム MCP ツールの追加](#カスタム-mcp-ツールの追加)
  - [カスタム MCP プロンプトの追加](#カスタム-mcp-プロンプトの追加)
- [ランタイム使用（ゲーム内）](#ランタイム使用ゲーム内)
  - [サンプル: AI 搭載チェスゲームボット](#サンプル-ai-搭載チェスゲームボット)
  - [なぜランタイム使用が必要か？](#なぜランタイム使用が必要か)
- [Unity `MCP Server` のセットアップ](#unity-mcp-server-のセットアップ)
  - [変数](#変数)
  - [Docker 📦](#docker-)
    - [`streamableHttp` トランスポート](#streamablehttp-トランスポート)
    - [`stdio` トランスポート](#stdio-トランスポート)
    - [カスタム `port`](#カスタム-port)
  - [バイナリ実行ファイル](#バイナリ実行ファイル)
- [Unity MCP アーキテクチャの仕組み](#unity-mcp-アーキテクチャの仕組み)
  - [`MCP` とは](#mcp-とは)
  - [`MCP Client` とは](#mcp-client-とは)
  - [`MCP Server` とは](#mcp-server-とは)
  - [`MCP Tool` とは](#mcp-tool-とは)
    - [`MCP Tool` を使うべき場面](#mcp-tool-を使うべき場面)
  - [`MCP Resource` とは](#mcp-resource-とは)
    - [`MCP Resource` を使うべき場面](#mcp-resource-を使うべき場面)
  - [`MCP Prompt` とは](#mcp-prompt-とは)
    - [`MCP Prompt` を使うべき場面](#mcp-prompt-を使うべき場面)
- [コントリビューション 💙💛](#コントリビューション-)

## その他のドキュメント

| ドキュメント | 説明 |
| -------- | ----------- |
| [デフォルト MCP ツール](docs/default-mcp-tools.md) | 説明付きすべての組み込みツールの完全リファレンス |
| [MCP Server セットアップ](docs/mcp-server.md) | サーバー設定、環境変数、リモートホスティング |
| [Docker デプロイ](docs/DOCKER_DEPLOYMENT.md) | Docker デプロイのステップバイステップガイド |
| [開発ガイド](docs/dev/Development.md) | アーキテクチャ、コードスタイル、CI/CD（コントリビューター向け） |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | はじめに、チュートリアル、API リファレンス、FAQ |

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# インストール

## ステップ 1: `Unity MCP Plugin` のインストール

<details>
  <summary><b>⚠️ 要件（クリック）</b></summary>

> [!IMPORTANT]
> **プロジェクトパスにスペースを含めないでください**
>
> - ✅ `C:/MyProjects/Project`
> - ❌ `C:/My Projects/Project`

</details>

### オプション 1 - インストーラー

- **[⬇️ インストーラーをダウンロード](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.51.2/AI-Game-Dev-Installer.unitypackage)**
- **📂 Unity プロジェクトにインストーラーをインポート**
  > - ファイルをダブルクリックすると Unity が自動的に開きます
  > - または: Unity Editor を先に開き、`Assets/Import Package/Custom Package` をクリックしてファイルを選択

### オプション 2 - OpenUPM-CLI

- [⬇️ OpenUPM-CLI をインストール](https://github.com/openupm/openupm-cli#installation)
- 📟 Unity プロジェクトフォルダでコマンドラインを開く

```bash
openupm add com.ivanmurzak.unity.mcp
```

## ステップ 2: `MCP Client` のインストール

お好みの `MCP クライアント` を 1 つ選んでください — すべてをインストールする必要はありません。これが LLM とコミュニケーションするメインのチャットウィンドウになります。

- [Claude Code](https://github.com/anthropics/claude-code)（強く推奨）
- [Claude Desktop](https://claude.ai/download)
- [GitHub Copilot in VS Code](https://code.visualstudio.com/docs/copilot/overview)
- [Antigravity](https://antigravity.google/)
- [Cursor](https://www.cursor.com/)
- [Windsurf](https://windsurf.com)
- その他サポートされているクライアント

> MCP プロトコルは非常に汎用的なので、お好みの MCP クライアントを使用できます — どれも同様にスムーズに動作します。唯一の重要な要件は、MCP クライアントが MCP ツールの動的更新をサポートしている必要があることです。

## ステップ 3: `MCP Client` の設定

### 自動設定

- Unity プロジェクトを開く
- `Window/AI Game Developer (Unity-MCP)` を開く
- MCP クライアントの `Configure` をクリック

![Unity_AI](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/ai-connector-window.gif)

> MCP クライアントがリストにない場合は、ウィンドウに表示されている生の JSON を MCP クライアントに挿入してください。具体的な MCP クライアントの手順については、そのクライアントのドキュメントを参照してください。

### 手動設定

何らかの理由で自動設定がうまくいかない場合は、`AI Game Developer (Unity-MCP)` ウィンドウの JSON を使用して任意の `MCP クライアント` を手動で設定できます。

#### コマンドライン設定

<details>
  <summary><b><code>command</code> の作成</b></summary>

**1. 環境に合った `<command>` を選択してください**

| プラットフォーム     | `<command>`                                                                                                 |
| ------------------- | ----------------------------------------------------------------------------------------------------------- |
| Windows x64         | `"<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows x86         | `"<unityProjectPath>/Library/mcp-server/win-x86/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows arm64       | `"<unityProjectPath>/Library/mcp-server/win-arm64/unity-mcp-server.exe" port=<port> client-transport=stdio` |
| MacOS Apple-Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" port=<port> client-transport=stdio`     |
| MacOS Apple-Intel   | `"<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" port=<port> client-transport=stdio`       |
| Linux x64           | `"<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" port=<port> client-transport=stdio`     |
| Linux arm64         | `"<unityProjectPath>/Library/mcp-server/linux-arm64/unity-mcp-server" port=<port> client-transport=stdio`   |

**2. `<unityProjectPath>` を Unity プロジェクトへのフルパスに置き換える**

**3. `<port>` を AI Game Developer 設定のポート番号に置き換える**

**4. コマンドラインを使用して MCP サーバーを追加**

</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/gemini-64.png?raw=true" width="16" height="16" alt="Gemini CLI"> Gemini CLI</summary>

  ```bash
  gemini mcp add ai-game-developer <command>
  ```

  > 上の表の `<command>` に置き換えてください
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/claude-64.png?raw=true" width="16" height="16" alt="Claude Code CLI"> Claude Code CLI</summary>

  ```bash
  claude mcp add ai-game-developer <command>
  ```

  > 上の表の `<command>` に置き換えてください
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/github-copilot-64.png?raw=true" width="16" height="16" alt="GitHub Copilot CLI"> GitHub Copilot CLI</summary>

  ```bash
  copilot
  ```

  ```bash
  /mcp add
  ```

  サーバー名: `ai-game-developer`
  サーバータイプ: `local`
  コマンド: `<command>`
  > 上の表の `<command>` に置き換えてください
</details>

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# AI ワークフロー例: Claude & Gemini

`MCP クライアント` で AI（LLM）とコミュニケーションしてください。やりたいことを何でも尋ねましょう。タスクやアイデアをより詳しく説明するほど、AI はより良いパフォーマンスを発揮します。

一部の `MCP クライアント` では異なる LLM モデルを選択できます。この機能に注目してください。モデルによって大きく性能差が出る場合があります。

**コマンド例:**

```text
シーン階層を説明して
```

```text
半径 2 の円上に 3 つのキューブを作成して
```

```text
メタリックゴールドのマテリアルを作成して球体の GameObject に適用して
```

> MCP クライアントで `エージェント` モードがオンになっていることを確認してください

## LLM 向け高度な機能

Unity MCP は LLM がより速く効果的に作業できる高度なツールを提供し、ミスを回避してエラーが発生した際に自己修正できるようにします。すべてはあなたのゴールを効率的に達成するために設計されています。

### コア機能

- ✔️ **エージェント対応ツール** - 1〜2 ステップで必要なものをすべて検索
- ✔️ **インスタントコンパイル** - より速い反復のための `Roslyn` を使った C# コードコンパイル & 実行
- ✔️ **フルアセットアクセス** - アセットと C# スクリプトへの読み書きアクセス
- ✔️ **インテリジェントフィードバック** - 適切な問題理解のための詳細なポジティブ/ネガティブフィードバック

### リフレクション活用機能

- ✔️ **オブジェクト参照** - インスタントな C# コードのために既存オブジェクトへの参照を提供
- ✔️ **プロジェクトデータアクセス** - 読みやすい形式でプロジェクト全体のデータにフルアクセス
- ✔️ **細かい変更** - プロジェクト内の任意のデータを設定・変更
- ✔️ **メソッド探索** - コンパイル済み DLL ファイルを含むコードベース全体のあらゆるメソッドを検索
- ✔️ **メソッド実行** - コードベース全体の任意のメソッドを呼び出し
- ✔️ **高度なパラメーター** - メモリ内の既存オブジェクトへの参照を含む、メソッド呼び出しのあらゆるプロパティを提供
- ✔️ **ライブ Unity API** - Unity の変更に応じて新鮮な API が即座に利用可能
- ✔️ **自己ドキュメント化** - `Description` 属性を通じて任意の `クラス`、`メソッド`、`プロパティ` の人間が読める説明にアクセス

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# MCP のカスタマイズ

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** はプロジェクトオーナーによるカスタム `MCP Tool`、`MCP Resource`、`MCP Prompt` の開発をサポートします。MCP サーバーは `Unity MCP Plugin` からデータを取得してクライアントに公開します。MCP 通信チェーン上のすべての人が新しい MCP 機能の情報を受け取り、LLM はある時点でそれを使用することを決定する場合があります。

## カスタム MCP ツールの追加

カスタム `MCP Tool` を追加するには：

1. `McpPluginToolType` 属性を持つクラス
2. `McpPluginTool` 属性を持つクラス内のメソッド
3. *オプション:* LLM が理解しやすいように各メソッド引数に `Description` 属性を追加
4. *オプション:* LLM に対して `optional`（任意）であることを示すために、`?` とデフォルト値を持つ `string? optional = null` プロパティを使用

> `MainThread.Instance.Run(() =>` という行は、Unity の API とやり取りするために必要なメインスレッドでコードを実行することを可能にします。これが不要でバックグラウンドスレッドでツールを実行することが許容できる場合は、効率性のためにメインスレッドの使用を避けてください。

```csharp
[McpPluginToolType]
public class Tool_GameObject
{
    [McpPluginTool
    (
        "MyCustomTask",
        Title = "Create a new GameObject"
    )]
    [Description("これが何であるか、いつ呼び出すべきかを LLM に説明します。")]
    public string CustomTask
    (
        [Description("これが何であるかを LLM に説明します。")]
        string inputData
    )
    {
        // バックグラウンドスレッドで何でも実行

        return MainThread.Instance.Run(() =>
        {
            // 必要に応じてメインスレッドで何かを実行

            return $"[Success] Operation completed.";
        });
    }
}
```

## カスタム MCP プロンプトの追加

`MCP プロンプト` を使用すると、LLM との会話にカスタムプロンプトを挿入できます。ユーザーとアシスタントの 2 つの送信者ロールをサポートします。これは LLM に特定のタスクを実行するよう指示する素早い方法です。カスタムデータを使ってプロンプトを生成し、リストやその他の関連情報を提供できます。

```csharp
[McpPluginPromptType]
public static class Prompt_ScriptingCode
{
    [McpPluginPrompt(Name = "add-event-system", Role = Role.User)]
    [Description("GameObject 間の UnityEvent ベースの通信システムを実装します。")]
    public string AddEventSystem()
    {
        return "UnityEvents、UnityActions、またはカスタムイベントデリゲートを使用して、ゲームシステムとコンポーネント間の疎結合通信のためのイベントシステムを作成してください。";
    }
}
```

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# ランタイム使用（ゲーム内）

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** をゲーム/アプリ内で使用します。ツール、リソース、プロンプトを活用できます。デフォルトではツールがないため、カスタムで実装する必要があります。

```csharp
// MCP プラグインのビルド
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

await mcpPlugin.Connect(); // Unity-MCP-Server との再試行付きアクティブ接続を開始

await mcpPlugin.Disconnect(); // アクティブ接続を停止し、既存の接続を閉じる
```

## サンプル: AI 搭載チェスゲームボット

クラシックなチェスゲームを考えてみましょう。ゲームルールに従ってターンを実行するボットロジックを LLM に委ねます。

```csharp
[McpPluginToolType]
public static class ChessGameAI
{
    [McpPluginTool("chess-do-turn", Title = "Do the turn")]
    [Description("チェスゲームでターンを実行します。ターンが受け入れられた場合は true、そうでない場合は false を返します。")]
    public static Task<bool> DoTurn(int figureId, Vector2Int position)
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.DoTurn(figureId, position));
    }

    [McpPluginTool("chess-get-board", Title = "Get the board")]
    [Description("チェスボードの現在の状態を取得します。")]
    public static Task<BoardData> GetBoard()
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.GetBoardData());
    }
}
```

## なぜランタイム使用が必要か？

多くのユースケースがあります。ボット付きのチェスゲームを作っていると想像してください。数行のコードを書くだけで、ボットの意思決定を LLM に委ねることができます。

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Unity `MCP Server` のセットアップ

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** サーバーはさまざまな起動オプションと Docker デプロイをサポートしています。`streamableHttp` と `stdio` の両方のトランスポートプロトコルをサポートしています。Unity MCP サーバーをカスタマイズするかクラウドにデプロイする必要がある場合は、このセクションをご覧ください。[詳細はこちら...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

## 変数

どの起動オプションを選択しても、環境変数とコマンドライン引数の両方を使ったカスタム設定をサポートします。単に起動するだけであれば、デフォルト値で動作するので変数を設定する手間は不要です。Unity プラグインも、特に `--port` についてデフォルト値を持っていること（両方が同じ値）を確認してください。

| 環境変数                      | コマンドライン引数    | 説明                                                                          |
| ----------------------------- | -------------------- | ----------------------------------------------------------------------------- |
| `MCP_PLUGIN_PORT`             | `--port`             | **クライアント** -> **サーバー** <- **プラグイン** 接続ポート（デフォルト: 8080） |
| `MCP_PLUGIN_CLIENT_TIMEOUT`   | `--plugin-timeout`   | **プラグイン** -> **サーバー** 接続タイムアウト（ms）（デフォルト: 10000）       |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | **クライアント** -> **サーバー** トランスポートタイプ: `stdio` または `streamableHttp`（デフォルト: `streamableHttp`） |

> コマンドライン引数は単一の `-` プレフィックス（`-port`）とプレフィックスなし（`port`）のオプションもサポートしています。

> **トランスポートの選択:** MCP クライアントがサーバーバイナリを直接起動する場合（ローカル使用 — 最も一般的な設定）は `stdio` を使用します。サーバーをスタンドアロンプロセスとして実行するか Docker/クラウドで実行して HTTP 経由で接続する場合は `streamableHttp` を使用します。

## Docker 📦

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)

Docker がインストールされていることを確認してください。Windows をお使いの場合は Docker Desktop が起動していることもご確認ください。

[高度な Docker 設定手順を読む](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/DOCKER_DEPLOYMENT.md)。

### `streamableHttp` トランスポート

```bash
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCP クライアント</code> 設定:</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

> クラウドでホストされている場合は `url` を実際のエンドポイントに置き換えてください。

</details>

### `stdio` トランスポート

このオプションを使用するには、`MCP クライアント` が Docker で `MCP サーバー` を起動する必要があります。修正された `MCP クライアント` 設定で実現できます。

```bash
docker run -t -e MCP_PLUGIN_CLIENT_TRANSPORT=stdio -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCP クライアント</code> 設定:</summary>

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

### カスタム `port`

```bash
docker run -e MCP_PLUGIN_PORT=123 -p 123:123 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCP クライアント</code> 設定:</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:123"
    }
  }
}
```

> クラウドでホストされている場合は `url` を実際のエンドポイントに置き換えてください。
</details>

## バイナリ実行ファイル

バイナリファイルから Unity `MCP Server` を直接起動できます。CPU アーキテクチャに合わせてコンパイルされたバイナリが必要です。すべての CPU アーキテクチャ向けのコンパイル済みバイナリを含む [GitHub リリースページ](https://github.com/IvanMurzak/Unity-MCP/releases) をご確認ください。

```bash
./unity-mcp-server --port 8080 --plugin-timeout 10000 --client-transport stdio
```

<details>
  <summary><code>MCP クライアント</code> 設定:</summary>

> `<project>` を Unity プロジェクトパスに置き換えてください。

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

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Unity MCP アーキテクチャの仕組み

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** は LLM と Unity の橋渡し役として機能します。Unity のツールを LLM に公開して説明し、LLM はインターフェースを理解してユーザーのリクエストに応じてツールを活用します。

統合された `AI コネクター` ウィンドウを使用して、**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** を [Claude](https://claude.ai/download) や [Cursor](https://www.cursor.com/) などの LLM クライアントに接続します。カスタムクライアントもサポートされています。

システムは高度に拡張可能で、Unity プロジェクトのコードベースに直接カスタム `MCP ツール`、`MCP リソース`、`MCP プロンプト` を定義し、AI や自動化クライアントに新しい機能を公開できます。これにより Unity MCP は高度なワークフローの構築、迅速なプロトタイピング、開発プロセスへの AI 駆動機能の統合のための柔軟な基盤となります。

## `MCP` とは

MCP（Model Context Protocol）は、一言で言えば AI 向けの `USB Type-C` であり、特に LLM（大規模言語モデル）向けです。LLM が Unity エンジンや、コード内のカスタム C# メソッドなどの外部機能を使用する方法を教えます。[公式ドキュメント](https://modelcontextprotocol.io/)

## `MCP Client` とは

チャットウィンドウを持つアプリケーションです。より効果的に動作するスマートエージェントを持つ場合もあり、高度な組み込み MCP ツールを持つ場合もあります。一般的に優れた MCP クライアントは AI がタスクを実行する成功の 50% を占めます。だから最良のものを選ぶことが非常に重要です。

## `MCP Server` とは

`MCP クライアント` と「他のもの」（この場合は Unity エンジン）の橋渡し役です。このプロジェクトには `MCP サーバー` が含まれています。

## `MCP Tool` とは

`MCP ツール` は LLM が Unity とやり取りするために呼び出せる関数またはメソッドです。これらのツールは自然言語リクエストと実際の Unity 操作の橋渡し役として機能します。AI に「キューブを作成して」や「マテリアルの色を変えて」と頼むと、AI は MCP ツールを使ってこれらのアクションを実行します。

**主な特徴:**

- 特定の操作を実行する**実行可能な関数**
- LLM がどのデータを提供するか理解するための説明付き**型付きパラメーター**
- 操作の成功・失敗に関するフィードバックを提供する**戻り値**
- Unity API 呼び出しのためのメインスレッド実行や重い処理のバックグラウンドスレッド実行が可能な**スレッド対応**

### `MCP Tool` を使うべき場面

- **定型タスクの自動化** - 頻繁に行う一般的な操作のツールを作成
- **複雑な操作** - 複数の Unity API 呼び出しを 1 つの使いやすいツールにまとめる
- **プロジェクト固有のワークフロー** - プロジェクトの特定の構造と規約を理解するツールを構築
- **エラーが起きやすいタスク** - バリデーションとエラー処理を含むツールを作成
- **カスタムゲームロジック** - 動的なコンテンツ作成のためにゲームのシステムを AI に公開

**例:**

- 特定のコンポーネントを持つ GameObject の作成と設定
- アセット（テクスチャ、マテリアル、プレハブ）のバッチ処理
- ライティングとポストプロセッシング効果の設定
- レベルジオメトリの生成やオブジェクトの手続き的配置
- 物理設定やコリジョンレイヤーの設定

## `MCP Resource` とは

`MCP リソース` は Unity プロジェクト内のデータへの読み取り専用アクセスを提供します。アクションを実行する MCP ツールとは異なり、リソースは LLM がプロジェクトの現在の状態、アセット、設定を検査して理解することを可能にします。プロジェクトについて AI にコンテキストを与える「センサー」と考えてください。

**主な特徴:**

- プロジェクトデータと Unity オブジェクトへの**読み取り専用アクセス**
- LLM が理解できる形式で提示される**構造化された情報**
- プロジェクトの現在の状態を反映する**リアルタイムデータ**
- AI がより良い判断を下すための**コンテキスト認識**

### `MCP Resource` を使うべき場面

- **プロジェクト分析** - AI がプロジェクトの構造、アセット、組織を理解できるようにする
- **デバッグ支援** - トラブルシューティングのための現在の状態情報を提供
- **インテリジェントな提案** - AI がより良い推奨事項を出せるようにコンテキストを提供
- **ドキュメント生成** - プロジェクトの状態に基づいてドキュメントを自動作成
- **アセット管理** - AI が利用可能なアセットとそのプロパティを理解するのを支援

**例:**

- シーン階層と GameObject プロパティの公開
- 利用可能なマテリアル、テクスチャ、その設定の一覧表示
- スクリプトの依存関係とコンポーネントの関係の表示
- 現在のライティング設定とレンダーパイプライン設定の提供
- オーディオソース、アニメーション、パーティクルシステムに関する情報の提供

## `MCP Prompt` とは

`MCP プロンプト` を使用すると、LLM との会話に事前定義されたプロンプトを挿入できます。これらは AI の動作を導くためのコンテキスト、指示、または知識を提供するスマートテンプレートです。プロンプトは静的テキストまたはプロジェクトの現在の状態に基づいて動的に生成することができます。

**主な特徴:**

- AI の応答に影響を与える**コンテキストガイダンス**
- 異なるペルソナをシミュレートできる**ロールベース**（ユーザーリクエストまたはアシスタントの知識）
- リアルタイムのプロジェクトデータを含むことができる**動的コンテンツ**
- 一般的なシナリオとワークフロー向けの**再利用可能なテンプレート**

### `MCP Prompt` を使うべき場面

- **ドメイン知識の提供** - プロジェクト固有のベストプラクティスとコーディング基準を共有
- **コーディング規約の設定** - 命名規則、アーキテクチャパターン、コードスタイルを確立
- **プロジェクト構造に関するコンテキストを提供** - プロジェクトがどのように組織されているか、そしてその理由を説明
- **ワークフロー手順の共有** - 一般的なタスクのステップバイステップの手順を提供
- **専門知識の注入** - 特定の Unity 機能、サードパーティアセット、カスタムシステムに関する情報を追加

**例:**

- 「パブリックメソッドは PascalCase、プライベートフィールドは camelCase を常に使用する」
- 「このプロジェクトは Scripts/Events/ にあるカスタムイベントシステムを使用しています」
- 「UI 要素を作成する際は、常に Scene/UI/MainCanvas の Canvas に追加してください」
- 「パフォーマンスが重要 - 頻繁にインスタンス化するオブジェクトにはオブジェクトプールを優先使用」
- 「このプロジェクトは SOLID 原則に従っています - アーキテクチャの決定を説明してください」

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# コントリビューション 💙💛

コントリビューションは大歓迎です。あなたのアイデアを持ち寄って、ゲーム開発をこれまでよりもシンプルにしましょう！新しい `MCP ツール` や機能のアイデアはありますか？あるいはバグを発見して修正方法を知っていますか？

**役立つと思ったらこのプロジェクトにスター 🌟 をつけてください！**

1. 👉 [開発ドキュメントを読む](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md)
2. 👉 [プロジェクトをフォーク](https://github.com/IvanMurzak/Unity-MCP/fork)
3. フォークをクローンして `./Unity-MCP-Plugin` フォルダーを Unity で開く
4. プロジェクトに新しいものを実装し、コミットして GitHub にプッシュ
5. 元の [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP/compare) リポジトリの `main` ブランチを対象にプルリクエストを作成

![AI ゲーム開発者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
