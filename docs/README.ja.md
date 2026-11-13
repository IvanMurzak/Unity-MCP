<div align="center" width="100%">

<img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/readme/hero.svg" alt="Unity 向け AI Game Developer: AI エージェントを Unity エディターと実行中のゲームにつなぐ Skills、MCP ツール、CLI" width="100%">

[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Downloads](https://img.shields.io/badge/dynamic/json?labelColor=333A41&label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp 'OpenUPM downloads')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=unity-mcp-cli&labelColor=333A41 'npm package')](https://www.npmjs.com/package/unity-mcp-cli)
[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-2A2A2A?logo=unity&labelColor=333A41 'Unity 2022.3 or newer')](https://unity.com/releases/editor/archive)
[![release](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml/badge.svg 'Release')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

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
    <a href="https://opencode.ai/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.fa.md)</b>

</div>

**AI Game Developer**(Unity-MCP)は、AI エージェントを Unity につなぎます。Claude Code、Codex、Cursor、GitHub Copilot、
Gemini など任意の [MCP](https://modelcontextprotocol.io/) クライアントが、**Unity エディター**でシーン、プレハブ、アセット、
C# コードを読み書きし、テストを実行し、スクリーンショットを撮り、**ビルドしたゲームの中**ではあなたが書いたツールを呼び出せます。
無料の Unity パッケージ(`com.ivanmurzak.unity.mcp`)、コマンドラインツール(`unity-mcp-cli`)、10 個のオプション拡張として提供されます。

## 仕組み

<img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/readme/architecture.svg" alt="AI エージェントまたは unity-mcp-cli が MCP でサーバー(既定は ai-game.dev Cloud、またはローカルの gamedev-mcp-server)と通信し、サーバーが Unity エディタープラグインと実行中のゲームのツールを呼び出す" width="100%">

プラグインはツールを MCP サーバーに登録し、エージェントはそのサーバー経由でツールを呼び出します。既定では
`https://ai-game.dev/mcp` の **Cloud** サーバーを使います。**Custom** モードでは
[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) のローカルバイナリを起動するか、
自分でホストしたサーバー(バイナリまたは Docker)に接続します。MCP ツールの代わりに、生成した **Skills** を使うこともできます。

## 特長

| | |
| --- | --- |
| **少数で強力なツール** | エディター作業の大半は **RQL** で行います: `rql-query` が読み、`rql-mutate` が変更し、`rql-schema` が説明します。ほかに Roslyn による C# 実行、テスト、スクリーンショット、パッケージ。 |
| **Skills または MCP** | ワンクリックでエージェント用の Skills を生成するか、MCP クライアントとして設定します。 |
| **どのエージェントでも** | Claude Code、Codex、Cursor、Copilot、Gemini などは自動で設定されます。その他の MCP クライアントも JSON 設定で動作します。 |
| **ランタイム(ゲーム内)** | ビルドしたゲームの中でも同じツールを使えます: NPC のロジック、ライブデバッグ、プレイヤーと AI のやり取り。 |
| **独自ツール** | どの C# メソッドも属性ひとつ `[AiTool]` でツールになります。 |
| **スクリプトと CI 向け CLI** | Unity とプラグインのインストール、プロジェクトを開く、クエリ、コンパイル、テストをターミナルから。 |
| **拡張** | Animation、Cinemachine、Input System、Navigation、ParticleSystem、ProBuilder、Splines、Terrain、Tilemap、Timeline 用のオプションパック。 |

## クイックスタート

**Unity 2022.3** 以降が必要です。プラグインのインストール方法をひとつ選んでください:

[![インストーラーをダウンロード](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/button/button_download_ja.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

- **インストーラー**: 上の `.unitypackage` をダウンロードし、Unity プロジェクトで開きます。
- **OpenUPM**: `openupm add com.ivanmurzak.unity.mcp`
- **CLI**(Node.js 22.14+)、エディター不要:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin ./MyUnityProject   # Packages/manifest.json にプラグインを追加
unity-mcp-cli login                             # ブラウザで ai-game.dev にサインイン
unity-mcp-cli open ./MyUnityProject             # Unity を開く。接続して skills を生成
```

<img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/readme/terminal.svg" alt="ターミナルの例: unity-mcp-cli のインストール、プラグインの追加、サインイン、Claude Code の設定、プロジェクトを開く" width="100%">

次にエージェントを接続します: Unity で **Window ▸ AI Game Dev** を開き、**Auto-generate** Skills(推奨)または
**Configure** MCP をクリックします。ターミナルでは `unity-mcp-cli setup-skills claude-code ./MyUnityProject` または
`unity-mcp-cli setup-mcp claude-code ./MyUnityProject` です。

![AI Game Developer ウィンドウ](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/editor/ai-game-developer-windows.png)

エージェントに *「半径 2 の円上に立方体を 3 つ作って」* と頼んでみてください。詳しい手順、手動の MCP 設定、アンインストール:
**[インストールガイド](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md)**(英語)。

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

## 使い方

**エージェントと話す。** 欲しい結果を説明してください。具体的なほど良い結果になります。

```text
シーンの階層を説明して
金属的な金色のマテリアルを作り、新しい球に割り当てて
Rigidbody のない敵プレハブをすべて探して Rigidbody を追加して
```

**ツール。** 既定で表示: `rql-query`、`rql-mutate`、`rql-schema`、`script-execute`、`tests-run`、
`screenshot-isolated`、`package-*` と `assetstore-*` のツール、`unity-tool-list`。ほかのツールもインストール済みですが、
表示するまで隠れています。全一覧: **[既定の MCP ツール](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)**
または [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools)。

**ターミナルから**(エディターが起動していなければヘッドレスで起動します):

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame
unity-mcp-cli test ./MyGame --mode PlayMode
```

**独自ツール。** `[AiToolType]` を付けたクラスと `[AiTool]` を付けたメソッドだけで作れます:

```csharp
[AiToolType]
public class Tool_Score
{
    [AiTool("score-reset", Title = "Reset the score")]
    [Description("Sets the player's score to zero.")]
    public string Reset()
    {
        return MainThread.Instance.Run(() =>
        {
            ScoreManager.Instance.Score = 0;
            return "[Success] Score reset.";
        });
    }
}
```

プロンプト、ランタイム(ゲーム内)ツール、完全な例: **[独自ツールとランタイム](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md)**。

## 拡張

AI Game Developer ウィンドウの **Extensions** セクション、または
`unity-mcp-cli install-extension <name>` でインストールします:
[Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) ·
[Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) ·
[InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) ·
[Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) ·
[ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) ·
[ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) ·
[Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) ·
[Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) ·
[Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) ·
[Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md)

## ドキュメント

| ページ | 内容 |
| --- | --- |
| [インストールガイド](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md) | インストール方法、エージェントの接続、手動 MCP 設定、チーム設定、アンインストール |
| [既定の MCP ツール](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md) | すべての組み込みツール、RQL ソース、1.0.0 で削除されたツールと RQL での置き換え |
| [独自ツールとランタイム](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md) | ツールとプロンプトを書く。ゲーム内で MCP を使う |
| [MCP サーバーと接続](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md) | Cloud / Custom モード、プラグインの環境変数、ローカルサーバー |
| [Docker デプロイ](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/DOCKER_DEPLOYMENT.md) | MCP サーバーをコンテナで実行 |
| [CI で無効化](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/ci-disable.md) | 通常の CI テスト実行からプラグインを外す |
| [CLI リファレンス](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | `unity-mcp-cli` のすべてのコマンドとオプション |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | チュートリアル、ガイド、FAQ |
| [開発ガイド](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md) | コントリビューター向け: リポジトリ構成、コードスタイル、テスト、CI |

関連: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)(共有 MCP サーバー)·
[ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

ドキュメントページは英語です。

## コントリビュート

アイデア、バグ報告、プルリクエストを歓迎します。
[開発ガイド](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md) を読み、リポジトリをフォークし、
Unity で `./Unity-MCP-Plugin` を開いて、`main` へプルリクエストを送ってください。役に立ったらスターをお願いします。

## ライセンス

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
