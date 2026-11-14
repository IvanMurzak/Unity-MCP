<div align="center" width="100%">

<p align="center"><img src="img/readme/hero.svg" alt="Unity 向け AI Game Developer: AI エージェントを Unity エディターと実行中のゲームにつなぐ Skills、MCP ツール、CLI" width="100%"></p>

[![OpenUPM version](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) [![OpenUPM downloads per month](https://img.shields.io/badge/dynamic/json?label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) [![unity-mcp-cli on npm](https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D)](https://www.npmjs.com/package/unity-mcp-cli) [![Unity 2022.3 or newer](https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://unity.com/releases/editor/archive)\
[![Release workflow status](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/release.yml?label=release&logo=github&color=15803D&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://discord.gg/cfbdMZX99G) [![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) [![Stand with Ukraine](https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-badge.svg)](https://stand-with-ukraine.pp.ua)

  <p>
    <a href="https://claude.ai/download"><img src="img/mcp-clients/claude-64.png" alt="Claude" title="Claude" height="36"></a>&nbsp;&nbsp;
    <a href="https://openai.com/index/introducing-codex/"><img src="img/mcp-clients/codex-64.png" alt="Codex" title="Codex" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.cursor.com/"><img src="img/mcp-clients/cursor-64.png" alt="Cursor" title="Cursor" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/docs/copilot/overview"><img src="img/mcp-clients/github-copilot-64.png" alt="GitHub Copilot" title="GitHub Copilot" height="36"></a>&nbsp;&nbsp;
    <a href="https://gemini.google.com/"><img src="img/mcp-clients/gemini-64.png" alt="Gemini" title="Gemini" height="36"></a>&nbsp;&nbsp;
    <a href="https://antigravity.google/"><img src="img/mcp-clients/antigravity-64.png" alt="Antigravity" title="Antigravity" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/"><img src="img/mcp-clients/vs-code-64.png" alt="VS Code" title="VS Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.jetbrains.com/rider/"><img src="img/mcp-clients/rider-64.png" alt="Rider" title="Rider" height="36"></a>&nbsp;&nbsp;
    <a href="https://visualstudio.microsoft.com/"><img src="img/mcp-clients/visual-studio-64.png" alt="Visual Studio" title="Visual Studio" height="36"></a>&nbsp;&nbsp;
    <a href="https://opencode.ai/"><img src="img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.fa.md) | [हिन्दी](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.hi.md) | [العربية](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ar.md)</b>

</div>

**AI Game Developer**(Unity-MCP)は、AI エージェントを Unity につなぎます。Claude Code、Codex、Cursor、GitHub Copilot、Gemini など任意の [MCP](https://modelcontextprotocol.io/) クライアントが、**Unity エディター**でシーン、プレハブ、アセット、C# コードを読み書きし、テストを実行し、スクリーンショットを撮り、**ビルドしたゲームの中**ではあなたが書いたツールを呼び出せます。無料の Unity パッケージ(`com.ivanmurzak.unity.mcp`)、コマンドラインツール(`unity-mcp-cli`)、10 個のオプション拡張として提供されます。

**ステータス:** [最新リリース](https://github.com/IvanMurzak/Unity-MCP/releases/latest)と[変更履歴](https://github.com/IvanMurzak/Unity-MCP/blob/main/Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/CHANGELOG.md)。プラグインと `unity-mcp-cli` は同じバージョンを共有します。**Windows、macOS、Linux** でテスト済み・サポート対象です。Unity **2022.3** 以降が必要です。CI は Unity 2022.3、6000.0、6000.3、6000.6 で EditMode、PlayMode、スタンドアロンビルドのテストを実行します。

## 仕組み

<p align="center"><img src="img/readme/architecture.svg" alt="AI エージェントまたは unity-mcp-cli が MCP でサーバー(既定は ai-game.dev Cloud、またはローカルの gamedev-mcp-server)と通信し、サーバーが Unity エディタープラグインと実行中のゲームのツールを呼び出す" width="100%"></p>

プラグインはツールを MCP サーバーに登録し、エージェントはそのサーバー経由でツールを呼び出します。MCP ツールの代わりに、生成した **Skills** を使うこともできます。サーバーをどこで動かすかを選べます:

| | **Local**(Custom モード、アカウント不要) | **Cloud**(既定の設定) |
| --- | --- | --- |
| サーバー | プラグインが `127.0.0.1` で起動する [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)、または自分でホストするサーバー | `https://ai-game.dev/mcp` |
| アカウント | 不要 | ai-game.dev にサインイン |
| プロジェクトのデータ | あなたのマシンから出ません | ツール呼び出しはリレーを経由します。リレーはメタデータのみを 90 日間保持し、リクエストやレスポンスの内容は保持しません |
| 料金 | 無料 | 無料 |

**Cloud が既定である理由:** ポートを開く必要がなく、ほかのプログラムとのポート競合を避けられ、無料です。Local はオフラインやエアギャップ環境での作業向けの対等な選択肢です。詳細: [セキュリティとプライバシー](#セキュリティとプライバシー)。

## 特長

✅ **RQL** — ひとつのクエリ言語でシーン、オブジェクト、アセット、設定を読み書き\
✅ **C# とテスト** — Roslyn で C# をコンパイル・実行。EditMode / PlayMode テストを実行\
✅ **Skills または MCP** — ワンクリックで Skills を生成、または MCP クライアントとして接続\
✅ **どのエージェントでも** — Claude Code、Codex、Cursor、Copilot、Gemini などの MCP クライアント\
✅ **ランタイム** — ビルドしたゲーム内でも同じツール: NPC ロジック、ライブデバッグ\
✅ **独自ツール** — どの C# メソッドも `[AiTool]` 属性ひとつでツールに\
✅ **CLI** — ターミナルや CI からインストール、起動、クエリ、コンパイル、テスト\
✅ **拡張** — Animation から Timeline まで 10 個のオプションツールパック

## クイックスタート

**Unity 2022.3** 以降が必要です。

**1. プラグインをインストールする**(推奨: インストーラー。ほかに何もインストールする必要はありません):

[![インストーラーをダウンロード](img/button/button_download_ja.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

プロジェクトを開いた状態で、ダウンロードした `.unitypackage` を開きます(ダブルクリック、または **Assets ▸ Import Package ▸ Custom Package**)。

<details>
<summary>その他のインストール方法: OpenUPM / Package Manager、またはスクリプトや CI 向けの CLI</summary>

- **OpenUPM** — `openupm add com.ivanmurzak.unity.mcp`。CLI を使わない場合は、**Edit ▸ Project Settings ▸ Package Manager** でスコープ付きレジストリを追加し(URL `https://package.openupm.com`、スコープ `com.ivanmurzak` と `extensions.unity`)、**Window ▸ Package Manager ▸ + ▸ Add package by name** → `com.ivanmurzak.unity.mcp` を指定します。
- **unity-mcp-cli**(Node.js 22.14+)— スクリプトや CI 向け、エディター不要:

  ```bash
  npm install -g unity-mcp-cli
  unity-mcp-cli install-plugin ./MyUnityProject   # Packages/manifest.json にプラグインを追加
  unity-mcp-cli open ./MyUnityProject             # プロジェクトを Unity で開く
  ```

  <p align="center"><img src="img/readme/terminal.svg" alt="ターミナルの例: unity-mcp-cli のインストール、プラグインの追加、サインイン、Claude Code の設定、プロジェクトを開く" width="100%"></p>

</details>

**2. エージェントを接続する** — どちらかを選んでください。どちらも完全にサポートされています:

| | **Local**(アカウント不要) | **Cloud**(既定の設定) |
| --- | --- | --- |
| **Window ▸ AI Game Dev** で | 接続を **Custom** に | 接続を **Cloud** にし、**Authorize**(ブラウザでサインイン) |
| エージェントの設定 | 使うエージェントで **Configure** | 使うエージェントで **Configure** |
| CLI で同じことをする | `unity-mcp-cli setup-mcp claude-code ./MyUnityProject --transport stdio` | `unity-mcp-cli login` の後、`unity-mcp-cli setup-mcp claude-code ./MyUnityProject` |

MCP の代わりに、同じウィンドウで Skills を **Auto-generate** することもできます。その他のエージェント: [クライアント設定](#クライアント設定)。

**3. Local のみ: ツールを呼び出せる相手を選ぶ。** ローカルサーバーは認証 `none` で起動します。`127.0.0.1` でのみ待ち受け、外部のブラウザオリジンは拒否しますが、あなたのマシン上のどのプログラムからも到達できます。ほかの人や信頼できないプログラムがそのマシンを使う場合は、ワンステップで `token` に切り替えてください: **Window ▸ AI Game Dev** → **Authorization Token** → `token`。サーバーは生成されたシークレットで再起動します。エージェントがそれを送信するよう、もう一度 **Configure** を押してください。

![AI Game Developer ウィンドウ](img/editor/ai-game-developer-windows.png)

エージェントに *「半径 2 の円上に立方体を 3 つ作って」* と頼んでみてください。詳しい手順、手動の MCP 設定、アンインストール: **[インストールガイド](installation.ja.md)**。

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

## 使い方

**エージェントと話す。** 欲しい結果を説明してください。具体的なほど良い結果になります。

```text
シーンの階層を説明して
金属的な金色のマテリアルを作り、新しい球に割り当てて
Rigidbody のない敵プレハブをすべて探して Rigidbody を追加して
```

**エージェントにできること**(既定で表示されるツール):

| 機能 | ツール | プロンプトの例 |
| --- | --- | --- |
| シーンと GameObject | `rql-query`、`rql-mutate` | *「開いているシーンで無効になっている GameObject をすべて一覧にして」* |
| アセットとプレハブ | `rql-query`、`rql-mutate` | *「赤いマテリアルを作って、すべての立方体に割り当てて」* |
| コンソールとエディターの状態 | `rql-query` | *「コンソールにどんなエラーが出ている?」* |
| エディターでの C# | `script-execute` | *「テレイン上のランダムな位置に木を 100 本配置する C# を実行して」* |
| テスト | `tests-run` | *「EditMode テストを実行して、失敗したものを修正して」* |
| スクリーンショット | `screenshot-isolated` | *「Player モデルを正面から見せて」* |
| パッケージと Asset Store | `package-*`、`assetstore-*` | *「Cinemachine をインストールして」* |
| ツール一覧 | `unity-tool-list` | *「ここで使えるツールは?」* |

RQL を覚える必要はありません。エージェントが RQL を書き、必要なソース、型、呼び出しごとに `rql-schema` に問い合わせます。ほかのツールもインストール済みですが、表示するまで隠れています。全一覧: **[既定の MCP ツール](default-mcp-tools.ja.md)** または [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools)。

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

プロンプト、ランタイム(ゲーム内)ツール、完全な例: **[独自ツールとランタイム](custom-tools.ja.md)**。

## クライアント設定

AI Game Developer ウィンドウの **Configure** ボタン、または `unity-mcp-cli setup-mcp <agent> ./MyUnityProject` が、これらのファイルを書き込みます(既定は Cloud、`--transport stdio` でローカル)。ローカルサーバー用に手動で設定する場合: プラグインを **Custom** にし、ウィンドウで `<port>` を確認し、`<project>/Library/mcp-server/<rid>/` にあるお使いのプラットフォーム用のバイナリを使います(`win-x64/gamedev-mcp-server.exe`、`osx-arm64/gamedev-mcp-server`、`linux-x64/gamedev-mcp-server` など)。

<details>
<summary><b>Claude Code</b> — プロジェクトルートの <code>.mcp.json</code></summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["port=<port>", "client-transport=stdio"]
    }
  }
}
```

</details>

<details>
<summary><b>Cursor</b> — <code>.cursor/mcp.json</code></summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "type": "stdio",
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["port=<port>", "client-transport=stdio"]
    }
  }
}
```

</details>

<details>
<summary><b>Codex</b> — <code>.codex/config.toml</code></summary>

```toml
[mcp_servers.ai-game-developer]
enabled = true
command = "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe"
args = ["port=<port>", "client-transport=stdio"]
tool_timeout_sec = 300
```

</details>

その他のエージェントと手動設定: [インストールガイド](installation.ja.md)。

## セキュリティとプライバシー

**最もプライベートな構成:** 接続を **Custom** にし(`127.0.0.1` 上のローカルサーバー、アカウント不要、プロジェクトのデータはあなたのマシンから出ません)、**Window ▸ AI Game Dev** で **Authorization Token** を `token` に設定してから、エージェントがシークレットを送信するようもう一度 **Configure** を押します。

✅ **プラグインはポートを開きません** — プラグインから MCP サーバーへ外向きに接続します。\
✅ **ローカルサーバーはループバック上** — ホスティング用に `MCP_BIND` を設定しない限り、`gamedev-mcp-server` は `127.0.0.1` にバインドします。\
✅ **オリジンチェック** — 許可されていない `Origin` を持つブラウザからのリクエストは、どの認証モードでも拒否されます(403)。\
✅ **認証モード** — `none`(ローカルの既定)、`token`(エージェントとプラグインの両方が送信する必要がある共有シークレット 1 つ)、`oauth`(ai-game.dev アカウント)。\
✅ **Cloud リレー** — ツール呼び出しを中継し、送信した内容は保存しません。リクエストやレスポンスの内容は一切保存されません。保持するのはメタデータ(IP、時刻、ツール名、サイズ)のみで、期間は 90 日間です。\
⚠️ **保護されないもの** — `none` モードでは、ポートに到達できるマシン上のどのプロセスもツールを呼び出せ、ツールはエディターの権限で実行されます(`script-execute` は C# を実行します)。共有マシンでは `token` モードを使ってください。

サーバーのオプションと Docker: [MCP サーバーと接続](mcp-server.ja.md)。

## 拡張

拡張は、特定の Unity 機能向けのツールを追加するオプションの UPM パッケージです。

```bash
unity-mcp-cli install-extension com.ivanmurzak.unity.mcp.cinemachine ./MyUnityProject
```

| 拡張 | 追加されるもの | 必要なもの |
| --- | --- | --- |
| [Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) | AnimationClip と AnimatorController |  |
| [Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) | バーチャルカメラ、ターゲット、カメラパイプライン | `com.unity.cinemachine` 3.1.6+ |
| [InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) | 入力アクションアセット、マップ、アクション、バインディング | `com.unity.inputsystem` 1.8.2+ |
| [Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) | NavMesh サーフェス、ベイク、エージェント、リンク | `com.unity.ai.navigation` 1.1.7+ |
| [ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) | ParticleSystem の放出、形状、カーブ、グラデーション |  |
| [ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) | 編集可能メッシュ: 形状、押し出し、ベベル、マテリアル | `com.unity.probuilder` 5.2.4+ |
| [Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) | スプラインコンテナ、ノット、タンジェント | `com.unity.splines` 2.8.4+ |
| [Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) | テレイン: ハイトマップ、スカルプト、サンプリング |  |
| [Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) | Grid と Tilemap: タイルの描画、塗りつぶし、読み取り | `com.unity.2d.tilemap.extras` 3.1.3+ |
| [Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md) | Timeline アセット、トラック、クリップ、マーカー | `com.unity.timeline` 1.8.12+ |

必要な Unity パッケージは拡張と一緒にインストールされます。AI Game Developer ウィンドウの **Extensions** セクションからもインストールできます。

## ドキュメント

| ページ | 内容 |
| --- | --- |
| [インストールガイド](installation.ja.md) | インストール方法、エージェントの接続、手動 MCP 設定、チーム設定、アンインストール |
| [既定の MCP ツール](default-mcp-tools.ja.md) | すべての組み込みツール、RQL ソース、1.0.0 で削除されたツールと RQL での置き換え |
| [独自ツールとランタイム](custom-tools.ja.md) | ツールとプロンプトを書く。ゲーム内で MCP を使う |
| [MCP サーバーと接続](mcp-server.ja.md) | Cloud / Custom モード、プラグインの環境変数、ローカルサーバー |
| [Docker デプロイ](DOCKER_DEPLOYMENT.ja.md) | MCP サーバーをコンテナで実行 |
| [CI で無効化](ci-disable.ja.md) | 通常の CI テスト実行からプラグインを外す |
| [CLI リファレンス](../cli/docs/README.ja.md) | `unity-mcp-cli` のすべてのコマンドとオプション |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | チュートリアル、ガイド、FAQ |
| [開発ガイド](dev/Development.ja.md) | コントリビューター向け: リポジトリ構成、コードスタイル、テスト、CI |

関連: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)(共有 MCP サーバー)· [ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

## コントリビュート

アイデア、バグ報告、プルリクエストを歓迎します。[開発ガイド](dev/Development.ja.md)を読み、リポジトリをフォークし、Unity で `./Unity-MCP-Plugin` を開いて、`main` へプルリクエストを送ってください。役に立ったらスターをお願いします。

## ライセンス

**ゲーム開発には無料で使えます。商用ゲームも含みます。** プラグインは [Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) です。[RQL](https://github.com/IvanMurzak/RQL) と [Code-Exec](https://github.com/IvanMurzak/Code-Exec) を使用しており、これらは PolyForm Shield 1.0.0 でライセンスされています。このライセンスが制限するのは、ゲーム開発向けの競合 AI ツールを作る企業だけです。ゲーム開発者はすべてを自由に使えます。
