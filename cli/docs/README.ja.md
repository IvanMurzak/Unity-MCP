<div align="center">

<p align="center"><img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/cli-hero.svg" alt="unity-mcp-cli: Unity と AI Game Developer プラグインのインストール、AI エージェントの接続、ツール実行、コンパイルとテストをターミナルから" width="100%"></p>

<p align="center">
<a href="https://www.npmjs.com/package/unity-mcp-cli"><img src="https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D" alt="unity-mcp-cli on npm"></a> <a href="https://nodejs.org/"><img src="https://img.shields.io/badge/Node.js-22.14%2B-0E7490?logo=nodedotjs&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Node.js 22.14 or newer"></a> <a href="https://unity.com/releases/editor/archive"><img src="https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Unity 2022.3 or newer"></a> <a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE"><img src="https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857" alt="License"></a> <a href="https://discord.gg/cfbdMZX99G"><img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Discord"></a>
</p>

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

</div>

`unity-mcp-cli` は **[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)**(Unity-MCP)のコマンドラインツールです。Unity エディターのインストール、プロジェクト作成、プラグインの追加、AI エージェントの接続、MCP ツールの実行、Unity プロジェクトのコンパイルとテストを、ターミナルや CI から行えます。

## 特長

✅ **インストール** — Unity エディター、新規プロジェクト、プラグイン。OpenUPM レジストリも設定\
✅ **接続** — 一度サインインすれば Claude Code、Codex、Cursor などの MCP 設定や Skills を作成\
✅ **ツール実行** — RQL クエリ、任意の MCP ツール、エディター内での C# コード\
✅ **コンパイルとテスト** — ファイルと行付きのコンパイルエラー、EditMode / PlayMode テスト\
✅ **スクリプト対応** — `--format json`、安定した終了コード、CI 向けの出力\
✅ **ライブラリ** — 同じコマンドを型付きの Node.js API として\
✅ **クロスプラットフォーム** — Windows、macOS、Linux

## クイックスタート

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-unity                  # 任意: Unity エディターをインストール
unity-mcp-cli create-project ./MyGame        # 任意: プロジェクトを作成
unity-mcp-cli install-plugin ./MyGame        # プラグインを追加
unity-mcp-cli login                          # ブラウザで ai-game.dev にサインイン
unity-mcp-cli open ./MyGame                  # Unity を開く。接続して skills を生成
unity-mcp-cli wait-for-ready ./MyGame        # ツールを呼べるまで待つ
```

グローバルインストールなしで: `npx unity-mcp-cli <command>`。Node.js 22.14 以降が必要です。Unity Hub がなければ自動でインストールされます。

<p align="center"><img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/terminal.svg" alt="ターミナルの例: unity-mcp-cli のインストール、プラグインの追加、サインイン、Claude Code の設定、プロジェクトを開く" width="100%"></p>

## 使い方

```bash
unity-mcp-cli setup-skills claude-code ./MyGame                  # エージェント用の Skills
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame --strict --format github
unity-mcp-cli test ./MyGame --mode PlayMode --junit-output junit.xml
unity-mcp-cli install-extension Tilemap ./MyGame
```

`rql` と `compile` はエディターが起動していなければヘッドレスで起動します。`test` は CI と同じくバッチモードのエディターで実行します。

## コマンド

| 分類 | コマンド |
| --- | --- |
| セットアップ | `install-unity` · `create-project` · `install-plugin` |
| パッケージ | `install-extension` · `update` · `remove-plugin` |
| アカウント | `login` · `unity-account` |
| AI エージェント | `setup-mcp` · `setup-skills` · `configure` |
| エディター | `open` · `close` · `wait-for-ready` · `status` · `dialog` |
| ツール | `tools` · `rql` · `run-tool` · `exec` |
| ビルド | `compile` · `test` |
| 一覧 | `commands` |

すべてのオプション、終了コード、JSON 出力: **[コマンドリファレンス](https://github.com/IvanMurzak/Unity-MCP/blob/docs/readme-refresh/cli/docs/commands.md)**(英語)、または `unity-mcp-cli <command> --help`。

## ライブラリ

コマンドは副作用のない型付きライブラリとしても使えます。結果は `kind: 'success' | 'failure'` です。

```ts
import { installPlugin } from 'unity-mcp-cli';

const result = await installPlugin({ unityProjectPath: './MyGame' });
if (result.kind === 'success') console.log(`Installed v${result.installedVersion}`);
```

## リンク

[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) · [インストールガイド](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md) · [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) · [Changelog](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/CHANGELOG.md) · [Discord](https://discord.gg/cfbdMZX99G)

## ライセンス

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)

<div align="center">
  <a href="https://stand-with-ukraine.pp.ua"><img src="https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-rounded.svg" width="28" height="28" alt="Stand with Ukraine" title="Stand with Ukraine"></a>
</div>
