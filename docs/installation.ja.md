[English](installation.md) | [中文](installation.zh-CN.md) | <b>日本語</b> | [Español](installation.es.md) | [فارسی](installation.fa.md) | [हिन्दी](installation.hi.md) | [العربية](installation.ar.md)

# インストールガイド

**AI Game Developer** プラグインを Unity プロジェクトにインストールし、AI エージェントを接続し、必要に応じて再び削除する方法を説明します。[README](README.ja.md) に戻る。

## 要件

- **Unity 2022.3** 以降。
- **スペースを含まない**プロジェクトパス。パスにスペースが含まれていると、プラグインは起動時にエラーをログに出力します。
  - ✅ `C:/MyProjects/MyProject`
  - ❌ `C:/My Projects/MyProject`
- CLI を使う場合: [Node.js](https://nodejs.org/) 22.14 以降。

## 1. プラグインをインストールする

### 方法 1: インストーラー

1. **[AI-Game-Dev-Installer.unitypackage](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)** をダウンロードします。
2. プロジェクトを開いた状態でダブルクリックするか、Unity で **Assets ▸ Import Package ▸ Custom Package** を使用します。

### 方法 2: OpenUPM

```bash
openupm add com.ivanmurzak.unity.mcp
```

### 方法 3: CLI

[`unity-mcp-cli`](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) が `Packages/manifest.json` を代わりに編集するため、Editor を開いておく必要はありません:

```bash
npm install -g unity-mcp-cli

unity-mcp-cli install-unity                      # optional: install a Unity Editor
unity-mcp-cli create-project ./MyUnityProject    # optional: create a project
unity-mcp-cli install-plugin ./MyUnityProject    # add the plugin
unity-mcp-cli login                              # sign in to ai-game.dev (browser device flow)
unity-mcp-cli open ./MyUnityProject              # open Unity; it connects and generates skills
unity-mcp-cli wait-for-ready ./MyUnityProject    # wait until tools can be called
```

`npx unity-mcp-cli <command>` を使えばグローバルインストールなしで動作します。

**サインインとプロジェクトのピン留め。** `login` は認証情報を `~/.ai-game-dev/credentials.json` に保存します。貼り付けるトークンはありません。デフォルトでは、`setup-mcp` が書き出すエージェント設定はプロジェクトごとの**ピン留め**されたエンドポイント (`https://ai-game.dev/mcp/p/<pin>`) を指します。共有の `https://ai-game.dev/mcp` を使う場合は `--no-pin` を指定してください。チームは登録コードでアクセスを共有できます: `unity-mcp-cli install-plugin --enroll <code>`。

## 2. AI エージェントを選ぶ

必要なエージェントは 1 つだけで、それが作業に使うチャットウィンドウになります。エージェントは Skills、または実行中に更新される MCP ツールをサポートしている必要があります。Claude Code を推奨します。AI Game Developer ウィンドウと `unity-mcp-cli setup-mcp` は、Claude Code、Claude Desktop、Codex、Cursor、Gemini、GitHub Copilot (CLI、VS Code、Visual Studio)、Antigravity、Cline、Open Code、Rider (Junie)、Unity AI などの設定を自動で書き出します。完全な一覧は `unity-mcp-cli setup-mcp --list` で表示できます。その他の MCP クライアントも、ウィンドウに表示される JSON 設定で動作します。

## 3. エージェントを接続する

### 自動

1. Unity でプロジェクトを開きます。
2. **Window ▸ AI Game Dev** を開きます。
3. Skills の **Auto-generate** (推奨) をクリックするか、エージェント用に MCP の **Configure** をクリックします。

![AI Game Developer window](img/ai-connector-window.gif)

ターミナルから同じことを行う場合:

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject             # http (default)
unity-mcp-cli setup-mcp cursor ./MyUnityProject --transport stdio
```

### 手動

エージェントが一覧にない場合は、AI Game Developer ウィンドウに表示される JSON をそのエージェントの MCP 設定にコピーしてください。

自身のコマンドラインからサーバーを追加するエージェントの場合は、ローカルサーバーを起動する `<command>` を組み立てます (Custom モード)。`<unityProjectPath>` をプロジェクトのフルパスに、`<port>` を AI Game Developer ウィンドウに表示されるポートに置き換えてください:

| プラットフォーム | `<command>` |
| --- | --- |
| Windows x64 | `"<unityProjectPath>/Library/mcp-server/win-x64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows x86 | `"<unityProjectPath>/Library/mcp-server/win-x86/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows arm64 | `"<unityProjectPath>/Library/mcp-server/win-arm64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| macOS Apple Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| macOS Intel | `"<unityProjectPath>/Library/mcp-server/osx-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux x64 | `"<unityProjectPath>/Library/mcp-server/linux-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux arm64 | `"<unityProjectPath>/Library/mcp-server/linux-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |

次にそれを追加します:

```bash
claude mcp add ai-game-developer <command>    # Claude Code
gemini mcp add ai-game-developer <command>    # Gemini CLI
```

GitHub Copilot CLI: `copilot` を実行し、`/mcp add` でサーバー名 `ai-game-developer`、タイプ `local`、そして上記の `<command>` を指定します。

> VS Code で Copilot を使う場合は、**Agent** モードが有効になっていることを確認してください。

## チーム設定: 更新通知

OpenUPM に新しいバージョンがあると、プラグインは Editor 起動時に更新ポップアップを表示します。*"Do not show again"* はユーザーごとの設定です。**チーム全体**でポップアップをオフにするには、**Edit ▸ Project Settings ▸ AI Game Developer** を開き、*"Disable update notifications for the entire team"* を有効にします。この設定は `ProjectSettings/AI-Game-Developer-UpdateSettings.asset` に保存されるので、そのファイルをコミットしてください。同じトグルは **Tools ▸ AI Game Developer ▸ Updates ▸ Disable Update Notifications (Team)** にもあります。

## アンインストール

1. **Window ▸ Package Manager** を開き、**AI Game Developer — MCP** を選択して **Remove** をクリックします (または `unity-mcp-cli remove-plugin ./MyUnityProject` を実行します)。
2. Unity を閉じてから、`Assets/Plugins/NuGet` フォルダーとその `Assets/Plugins/NuGet.meta` ファイルを削除します。

## 次へ

- [デフォルト MCP ツール](default-mcp-tools.ja.md)
- [カスタムツールとランタイム](custom-tools.ja.md)
- [MCP サーバーと接続](mcp-server.ja.md)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
