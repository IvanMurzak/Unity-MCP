[English](commands.md) | [中文](commands.zh-CN.md) | <b>日本語</b> | [Español](commands.es.md) | [فارسی](commands.fa.md) | [हिन्दी](commands.hi.md) | [العربية](commands.ar.md)

# unity-mcp-cli リファレンス

[`unity-mcp-cli`](../README.md) のすべてのコマンドとオプション、終了コード、機械可読な出力、そしてライブラリ API。
ターミナルで同じ詳細を見るには `unity-mcp-cli <command> --help` を実行してください。

**コマンド:** [`configure`](#configure) · [`create-project`](#create-project) · [`update`](#update) · [`install-extension`](#install-extension) · [`install-plugin`](#install-plugin) · [`install-unity`](#install-unity) · [`login`](#login) · [`unity-account`](#unity-account) · [`open`](#open) · [`close`](#close) · [`tools`](#tools) · [`rql`](#rql) · [`run-tool`](#run-tool) · [`exec`](#exec) · [`test`](#test) · [`compile`](#compile) · [`dialog`](#dialog) · [`commands`](#commands) · [`wait-for-ready`](#wait-for-ready) · [`setup-mcp`](#setup-mcp) · [`setup-skills`](#setup-skills) · [`remove-plugin`](#remove-plugin) · [`status`](#status)

**その他:** [グローバルオプション](#グローバルオプション) · [終了コードと機械可読な出力](#終了コードと機械可読な出力) · [完全な自動化の例](#完全な自動化の例) · [仕組み](#仕組み) · [ライブラリ API](#ライブラリ-api-v0670)

# コマンド

## `configure`

`UserSettings/AI-Game-Developer-Config.json` 内の MCP ツール、プロンプト、リソースを設定します。

```bash
unity-mcp-cli configure ./MyGame --list
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | Unity プロジェクトへのパス (位置引数または `--path`) |
| `--list` | いいえ | 現在の設定を一覧表示して終了 |
| `--enable-tools <names>` | いいえ | 特定のツールを MCP クライアントに表示 (カンマ区切り) |
| `--disable-tools <names>` | いいえ | 特定のツールを MCP クライアントから非表示 (カンマ区切り。非表示のツールも名前を指定すれば実行されます) |
| `--enable-all-tools` | いいえ | すべてのツールを表示 |
| `--disable-all-tools` | いいえ | すべてのツールを非表示 |
| `--enable-prompts <names>` | いいえ | 特定のプロンプトを MCP クライアントに表示 (カンマ区切り) |
| `--disable-prompts <names>` | いいえ | 特定のプロンプトを MCP クライアントから非表示 (カンマ区切り。非表示のツールも名前を指定すれば実行されます) |
| `--enable-all-prompts` | いいえ | すべてのプロンプトを表示 |
| `--disable-all-prompts` | いいえ | すべてのプロンプトを非表示 |
| `--enable-resources <names>` | いいえ | 特定のリソースを MCP クライアントに表示 (カンマ区切り) |
| `--disable-resources <names>` | いいえ | 特定のリソースを MCP クライアントから非表示 (カンマ区切り。非表示のツールも名前を指定すれば実行されます) |
| `--enable-all-resources` | いいえ | すべてのリソースを表示 |
| `--disable-all-resources` | いいえ | すべてのリソースを非表示 |

**例 — 特定のツールを有効にし、すべてのプロンプトを無効にする:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools screenshot-scene-view,script-read \
  --disable-all-prompts
```

**例 — すべてを有効にする:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

## `create-project`

Unity Editor を使って新しい Unity プロジェクトを作成します。

```bash
unity-mcp-cli create-project /path/to/new/project
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | プロジェクトを作成するパス (位置引数または `--path`) |
| `--unity <version>` | いいえ | 使用する Unity Editor のバージョン (デフォルトはインストール済みの最新バージョン) |

**例 — 特定のエディターバージョンでプロジェクトを作成する:**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

## `update`

すべてを更新します: Unity プロジェクトの MCP プラグインとインストール済みの拡張機能を最新バージョンに更新し、その後 CLI 自体を更新します。

```bash
unity-mcp-cli update              # the project around the current folder, then the CLI
unity-mcp-cli update ./MyGame      # a project by path
unity-mcp-cli update --check       # only report; exit code 2 when something is outdated
unity-mcp-cli update --no-self     # leave the CLI as it is
unity-mcp-cli update --no-project  # update only the CLI
unity-mcp-cli update --no-extensions
```

プロジェクトの外では CLI のみを更新します。`file:` や git からのインストールはそのまま残され、ダウングレードは一切行われません。

## `install-extension`

Unity-MCP の拡張機能 (Animation、Cinemachine、Input System、Navigation、Particle System、ProBuilder、Splines、Terrain、Tilemap、Timeline 向けの MCP ツールを追加する任意のツールパック) の 1 つを、Unity プロジェクトの `Packages/manifest.json` にインストールします。これは *AI Game Developer* エディターウィンドウの **Extensions** セクションに相当するコマンドラインで、同じ共有カタログからインストールします。

```bash
# See what is installable (no project and no network required)
unity-mcp-cli install-extension --list

# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-extension Tilemap
```

| 引数 / オプション | 必須 | 説明 |
|---|---|---|
| `[id]` | はい (`--list` の場合を除く) | インストールする拡張機能 — OpenUPM のパッケージ id (`com.ivanmurzak.unity.mcp.tilemap`) またはカタログ名 (`Tilemap`)。大文字小文字を区別せずに照合されます。不明な id の場合はインストール可能なすべての拡張機能が一覧表示されるので、自分で修正できます。 |
| `[path]` | いいえ | Unity プロジェクトへのパス (位置引数または `--path`)。デフォルトはカレントディレクトリで、`install-plugin` と同じ方法で検証されます。 |
| `--extension-version <version>` | いいえ | インストールする拡張機能のバージョン (デフォルトは OpenUPM の最新)。**`--version` ではありません** — それは CLI 自身のグローバルなバージョンフラグです。 |
| `--list` | いいえ | インストール可能な拡張機能を一覧表示して終了します。 |

このコマンドは:
1. 必要なすべてのスコープを含む **OpenUPM スコープ付きレジストリ**を追加します (`com.ivanmurzak` スコープがあることで初めて拡張機能が解決可能になります)
2. 拡張機能のパッケージ id を `dependencies` に追加し、インストール時に OpenUPM の `latest` を解決します
3. 自動解決されたバージョンでは**決してダウングレードしません** — より新しいバージョンがすでにインストールされている場合はそれが維持され、警告が表示されます。ダウングレードを含め特定の値を強制するには `--extension-version` を指定します。
4. **冪等**です — 拡張機能がすでに最新であることがわかった再実行では、その旨を報告し何も書き込みません

**例 — 拡張機能の特定のバージョンをインストールする:**

```bash
unity-mcp-cli install-extension Cinemachine ./MyGame --extension-version 1.0.17
```

**例 — ライブラリからプロセス内で実行する:**

```ts
import { installExtension, EXTENSIONS_CATALOG } from 'unity-mcp-cli';

const result = await installExtension({
  unityProjectPath: './MyGame',
  extensionId: 'Tilemap',
});
if (result.kind === 'success') {
  console.log(result.outcome, result.fromVersion, '->', result.toVersion);
}
```

> このコマンドを実行した後、Package Manager が新しい依存関係を解決するように Unity Editor を開いて (またはフォーカスして) ください。

## `install-plugin`

Unity-MCP プラグインを Unity プロジェクトの `Packages/manifest.json` にインストールします。任意で、同じ実行の中で RID に合った MCP サーバーのバイナリをダウンロードし、チームの登録コードを引き換えることもできます。

```bash
# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-plugin
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unity プロジェクトへのパス (位置引数または `--path`)。デフォルトはカレントディレクトリです。解決されたディレクトリが実際の Unity プロジェクト (`Packages/manifest.json` マーカー) であることが検証され、該当しない場合はエラーに何をチェックしたかが正確に示されます。 |
| `--plugin-version <version>` | いいえ | インストールするプラグインのバージョン (デフォルトは [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) の最新) |
| `--with-server` | いいえ | RID に合った [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) のバイナリも CLI 管理のディレクトリにダウンロードします |
| `--server-version <version>` | いいえ | `--with-server` でダウンロードするサーバーのバージョン (デフォルト: CLI が固定しているサーバーバージョン) |
| `--server-source <path-or-url>` | いいえ | オフライン/CI 用の上書き — ローカルの zip パスまたは URL からサーバーをインストールします (チェックサム検証をスキップ) |
| `--enroll <code>` | いいえ | 登録コードを引き換えてプラグインの認証情報を取得します (共有のマシンストアに配置され、このプロジェクトに固定されます) |
| `--enroll-stdin` | いいえ | 登録コードを argv ではなく stdin から読み込みます (シェル履歴に残らないようにします) |

このコマンドは:
1. 必要なすべてのスコープを含む **OpenUPM スコープ付きレジストリ**を追加します
2. `com.ivanmurzak.unity.mcp` を `dependencies` に追加します
3. **決してダウングレードしません** — より新しいバージョンがすでにインストールされている場合はそれが維持されます

**例 — プラグインの特定のバージョンをインストールする:**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

**例 — チームの登録コードを引き換える (パスのデフォルトは cwd):**

```bash
cd ./MyGame && unity-mcp-cli install-plugin --enroll <code>
```

> このコマンドを実行した後、パッケージのインストールを完了するために Unity Editor でプロジェクトを開いてください。

## `install-unity`

Unity Hub CLI を使って Unity Editor のバージョンをインストールします。

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| 引数 / オプション | 必須 | 説明 |
|---|---|---|
| `[version]` | いいえ | インストールする Unity Editor のバージョン (例: `6000.3.1f1`) |
| `--path <path>` | いいえ | 既存のプロジェクトから必要なバージョンを読み取ります |

引数もオプションも指定されない場合、コマンドは Unity Hub のリリース一覧から最新の安定版をインストールします。

**例 — プロジェクトが必要とするエディターバージョンをインストールする:**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

## `login`

**ai-game.dev** にサインインし、認証情報を共有のマシン認証情報ストア (`~/.ai-game-dev/credentials.json`) に保存します。ブラウザーベースの **OAuth 2.1 デバイスフロー**を実行するため、作成したり貼り付けたりする個人アクセストークンはありません。サインインはマシン全体を認可します: **エージェント**の認証情報を発行し、そこからエンジンツール用の認証情報を派生させます。すべての利用者 (CLI のクラウドコマンド、Unity Editor プラグイン、デスクトップアプリ) が自動的に更新します — トークンはバックグラウンドでローテーションされるため、サインインはセッションごとではなくマシンごとに 1 回で済みます。

```bash
unity-mcp-cli login
```

| オプション | 必須 | 説明 |
|---|---|---|
| `--project <path>` | いいえ | 認証情報を共有のマシンストアではなく、プロジェクトローカルのストア (`<path>/.ai-game-dev/`) に保存します |
| `--force` | いいえ | 認証情報がすでに存在していても再認証します |
| `--tools-only` | いいえ | **エンジンツールのみ**を認可します (エージェントの認証情報は保存されないため、デスクトップアプリでの取得はできません)。CI / 自動化ランナー向けで、それらは独立した取り消し可能なデバイスグループとして表示されます |
| `--yes` | いいえ | プロンプトに対して "yes" とみなします — **アカウント切り替え** (マシンが現在保持しているのとは別のアカウントでのサインイン) を非対話的に確認するために必要です |

コマンドは短いユーザーコードと検証用 URL を表示し、ブラウザーを開き、サインインを承認するまでポーリングします。認証情報がすでに存在する場合は *"Already signed in"* で早期に終了します — 置き換えるには `--force` を指定してください。マシンが保持しているのとは**別のアカウント**でサインインする場合は、まず確認を求めます (拒否すると何も変更されません)。スクリプトで確認するには `--yes` を指定します。

**例 — サインインする (共有のマシンストア):**

```bash
unity-mcp-cli login
```

**例 — CI ランナー、エンジンツールのみ:**

```bash
unity-mcp-cli login --tools-only
```

**例 — 特定のプロジェクトの横に認証情報を保存する:**

```bash
unity-mcp-cli login --project ./MyGame
```

## `unity-account`

実行中の Unity Editor がサインインしている **Unity アカウント**を表示し、そのサインインを開始します。Asset Store の
"My Assets" (`assetStore` RQL ソース) にはこれが必要です。Personal ライセンスはこれがなくても動作するため、エディターは
サインアウトした状態でも実行できます。これは ai-game.dev のアカウントではありません: そちらは [`login`](#login) です。

`--sign-in` はそのエディターで Unity のサインインを開きます。**ユーザー**が Unity Hub を通じてそこでサインインを完了します。
直接開かれた (Hub を通さない、例えば古い `open` による) エディターはサインインできません: "Install Unity Hub" しか表示されないため、
回答には [`close`](#close) と [`open`](#open) でプロジェクトを開き直すよう示されます。これらは Hub を通じてエディターを起動します。
そのように開かれたエディターは、サインイン操作なしで Hub がサインインしているアカウントを使います。認証情報が CLI を通過することはありません。このコマンドには、ウィンドウ付きのエディターでプロジェクトが開かれている必要があり
([`open`](#open))、サインインを表示できないヘッドレスのエディターを起動することはありません。エージェントは `rql` で同じクエリを
実行できます: `assetStore | select signedIn userName` と `assetStore | call SignIn()`。

```bash
unity-mcp-cli unity-account ./MyGame            # signedIn|userName
unity-mcp-cli unity-account ./MyGame --sign-in  # opens Unity's sign-in for the user
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unity プロジェクトのパス (位置引数または `--path`。デフォルト: カレントディレクトリ) |
| `--sign-in` | いいえ | エディターがサインインしていない場合に Unity のサインインを開きます (サインイン済みの場合: アクセストークンを更新します) |
| `--format <format>` | いいえ | `text`、`json` または `ndjson` |

エディターの回答とともに `0` で、応答するエディターがない場合は `7` で終了します。このコマンドに対して古すぎるプラグインには、
[`update`](#update) を実行するようヒントが表示されます。

## `open`

Unity Editor で Unity プロジェクトを開きます。デフォルトでは、接続オプションが指定されていれば MCP 接続用の環境変数を設定します。MCP 接続なしで開くには `--no-connect` を使います。

Unity Hub がインストールされている場合、エディターは Hub 自身と同じように Hub を通じて開かれます (`-useHub -hubIPC`、認証情報なし:
エディターは Hub にセッションを要求し、Hub が実行されていなければバックグラウンドで起動します)。その後エディターは
Hub がサインインしている Unity アカウントを使います。これは Asset Store の "My Assets" に必要です
([`unity-account`](#unity-account) を参照)。ヘッドレス (`--headless`) のエディターは従来どおりに開かれます。

```bash
# Explicit path
unity-mcp-cli open ./MyGame

# From inside the Unity project folder — path defaults to the current directory
cd ./MyGame && unity-mcp-cli open
```

| オプション | 環境変数 | 必須 | 説明 |
|---|---|---|---|
| `[path]` | — | いいえ | Unity プロジェクトへのパス (位置引数または `--path`)。デフォルトはカレントワーキングディレクトリです。 |
| `--unity <version>` | — | いいえ | 使用する特定の Unity Editor のバージョン (デフォルトはプロジェクト設定のバージョンで、なければインストール済みの最新バージョン) |
| `--editor-path <path>` | — | いいえ | Unity Editor 実行ファイルへの明示的なパス。Unity Hub による検出をスキップし、カスタムのインストール場所に便利です。 |
| `--no-connect` | — | いいえ | MCP 接続用の環境変数なしで開きます |
| `--url <url>` | `UNITY_MCP_CLOUD_URL` | いいえ | 接続先の MCP サーバー URL (CLI はレガシーのエイリアス `UNITY_MCP_HOST` も引き続きエクスポートし、プラグインもそれを受け付けます) |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | いいえ | 接続を強制的に維持します |
| `--token <token>` | `UNITY_MCP_TOKEN` | いいえ | 認証トークン |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | いいえ | 認証モード: `none`、`oauth`、または `token` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | いいえ | 有効にするツールのカンマ区切りリスト |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | いいえ | トランスポート方式: `streamableHttp` または `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | いいえ | `true` または `false` を設定して MCP サーバーの自動起動を制御します |
| `--headless` | — | いいえ | ウィンドウなしでエディターを起動し (`-batchmode`。グラフィックスは有効なのでカメラは引き続きレンダリングされます)、その MCP サーバーが応答するまで待ちます。`--keep-connected` を含意します。プロジェクトの実行中のエディターは再利用されます。ここで起動したものは CLI 自身のものになり ([`close --owned-only`](#close) を参照)、並列の呼び出しでも起動されるのは 1 つだけです。 |
| `--idle-timeout <seconds>` | `AIGD_SESSION_IDLE_SECONDS` (headless) / `UNITY_MCP_IDLE_TIMEOUT` (window) | いいえ | ツール呼び出しがないままこの秒数が経過するとエディターを終了します (変更されたシーンとアセットを先に保存します) |
| `--ready-timeout <ms>` | — | いいえ | `--headless` 使用時: エディターの準備が整うまで待つ時間 (デフォルト: `300000`) |
| `--no-auto-dismiss-launch-errors` | — | いいえ | Unity Editor の「起動時のコンパイルエラー」ダイアログの自動クローズを無効にします (デフォルト: 有効) |
| `--launch-dismiss-timeout-ms <ms>` | — | いいえ | 起動エラー自動クローズのポーリングループ全体のタイムアウト (ミリ秒) (デフォルト: `30000`) |
| `--launch-dismiss-poll-interval-ms <ms>` | — | いいえ | 起動エラー自動クローズループのポーリング間隔 (ミリ秒) (デフォルト: `1500`) |

`--headless` なしの場合、CLI がそのプロジェクトのために起動したヘッドレスのエディター (`exec`、`open --headless`) には、ウィンドウを開けるようにまず保存して終了するよう要求されます。

エディターのプロセスはデタッチモードで起動されます。デフォルトでは、エディターの起動後に `open` は Unity の「起動時のコンパイルエラー」ダイアログ (Unity 2020.2+ では `"Enter Safe Mode?"`、それより古いリリースでは `"Hold On" / "Compiler Errors"`) をポーリングし、`Ignore` をクリックしてエディターの初期化を完了させます — これがないと、Unity 自体がコンパイルできない状態の後に実行する必要がある Editor 内の自動化 (例: NuGet 依存関係リゾルバー) が自己修復できません。ダイアログは Unity が起動し、Package Manager に接続し、コンパイルを開始した後に表示されます — 経験的に高速なマシンで約 6 秒、遅いマシンではそれ以上かかります — そのため、ポーリングループには猶予期間があり、その間にダイアログが見られなければ早期に終了します。猶予期間は Unity の起動フェーズ全体をカバーする必要があり、そうでないとダイアログが表示される前にループが打ち切られてしまいます (issue #737)。ダイアログがない場合に `--launch-dismiss-timeout-ms` の全時間を実行することはありません。ダイアログが観測された (そして正常に閉じられた) 場合は、再表示されたダイアログ (リゾルバーが 1 つのエラーを修正 → 次のエラーでダイアログが再表示される) も再び閉じられるよう、全体のタイムアウトまでポーリングを続けます。ライブラリモードの呼び出し元は `AbortSignal` (`OpenProjectOptions` の `launchDismissAbortSignal`) を渡して、自身の準備完了シグナルが発火した瞬間にループを中断できます。

### 自動クローズのプラットフォーム要件

| プラットフォーム | 要件 | 備考 |
|---|---|---|
| **Windows** | 組み込み (Win32 API) | PowerShell から `EnumWindows` / `EnumChildWindows` / `SendMessageW(BM_CLICK)` を使います。追加の設定は不要です。 |
| **macOS** | ターミナル (または `unity-mcp-cli` バイナリ) に**アクセシビリティの権限**を付与する必要があります。システム設定 → プライバシーとセキュリティ → アクセシビリティ。 | AppleScript / `osascript` で実装されています。この権限がないと、`osascript` はポーリングのたびにエラーを報告し、ダイアログを閉じることができません。 |
| **Linux/X11** | `PATH` 上の `xdotool` (例: `sudo apt-get install xdotool`)。 | 最初の実装では Wayland は**サポートされていません** — Wayland サポートについては上流の issue を追跡してください。 |

完全にオプトアウトするには `--no-auto-dismiss-launch-errors` を指定します。

**例 — MCP 接続付きで開く:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**例 — MCP 接続なしで開く (単純に開く):**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**例 — 認証と特定のツールを指定して開く:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth token \
  --tools rql-query,rql-mutate,rql-schema
```

## `close`

指定したプロジェクトパスで実行中の Unity Editor インスタンスを正常に終了させます。[`open`](#open) と対をなすコマンドで、OS レベルのプロセス強制終了に頼らずにクリーンな後片付けが必要なスクリプト化されたワークフロー (CI エージェント、パイプライン実行器、統合テストのフィクスチャ) 向けです。

```bash
unity-mcp-cli close ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unity プロジェクトへのパス (位置引数、デフォルトはカレントディレクトリ) |
| `--timeout <seconds>` | いいえ | 穏当な終了のタイムアウト秒数 (デフォルト: 30) |
| `--force` | いいえ | `--timeout` 以内に終了しない場合は Editor を強制終了します |
| `--owned-only` | いいえ | CLI が起動した Editor (`exec`、`open --headless`) のみを閉じます。ユーザーが開いた Editor には決して触れません |

**仕組み:**

0. CLI が起動した Editor はプロジェクトのセッションリース (`.ai-game-dev/session.json`: PID、プロセスの開始時刻、nonce) に記録されます。リースが稼働中の Editor を示している場合、`close` はそのプラグインに保存して終了するよう要求します (`editor-session-quit` システムツール。リースの nonce と照合され、Editor 起動時にリースが記録したサーバー URL に送られます) — これは `WM_CLOSE` では届かないヘッドレスの Editor にも届きます — そして以下の手順をスキップします。`--owned-only` の場合はここで止まります: リースがなければ何も閉じられません。`--timeout` 後もその Editor が実行中で (`--force` なし)、モーダルダイアログが開いている場合、`close` は手順 3 と同様に終了コード 18 で終了し、そのダイアログを示します。
1. `<project>/Temp/UnityLockfile` (4 バイトのリトルエンディアン uint32) を読み取って実行中の Editor の PID を解決し、古いロックファイルに対処するためにプロセス列挙と照合します。
2. 穏当な終了シグナルを送ります — Linux/macOS では `SIGTERM`、Windows では `taskkill` (`/F` なし) — Unity が自動保存 / アセットインポートを完了できるようにします。
3. プロセスが終了するか `--timeout` が経過するまで 250ms ごとにポーリングします。その間、Editor のモーダルダイアログ (Windows、macOS、Linux) を探します: 未保存の変更がある状態で穏当な終了を行うと "Scene(s) Have Been Modified" [Save | Don't Save | Cancel] が開き、Editor は人の操作を待ちます。`--force` なしの場合、`close` は 2 回連続のスキャンでダイアログが見つかった時点 (終了要求から約 3 秒後) で終了コード 18 で停止し、ダイアログとそのボタンを示します (Editor 内か [`dialog --press`](#dialog) で回答してください)。`--force` ありの場合は、強制終了の前に `--timeout` まで待ちます。
4. タイムアウトが経過し、かつ `--force` が設定されている場合は、`SIGKILL` / `taskkill /F` にフォールバックします。
5. 冪等 — すでに閉じられた Editor (または Editor が一度も実行されていなかったプロジェクト) を閉じると、`no running Editor for project at <path>` とともに 0 で終了します。
6. Unity プロジェクトのルートではないパス (`ProjectSettings/ProjectVersion.txt` が存在する必要があります) に対しては動作を拒否します — 誤ってホスト上のすべての Unity を終了させてしまう呼び出しを防ぎます。

> **Windows のヘッドレスに関する注意:** 穏当な終了の手順は `taskkill` (`/F` なし) を使い、これは `WM_CLOSE` を送ります。このメッセージは、CLI と**同じデスクトップ/セッション**上にトップレベルウィンドウを持つプロセスにしか届きません。Unity がセッション 0 の Windows サービス (またはその他の非対話型デスクトップ) によって起動された場合、穏当な終了は黙って破棄され、`--timeout` が経過し、Editor を終了させる唯一の手段は `--force` になります。ヘッドレスの CI ランナーではそれを考慮して計画してください。CLI が起動した Editor (手順 0) は影響を受けません: プラグインを通じて終了します。

**例 — 閉じ、60 秒後に強制終了にフォールバックする:**

```bash
unity-mcp-cli close ./MyGame --timeout 60 --force
```

**例 — 自動化スクリプトの最後でクリーンに後片付けする:**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
unity-mcp-cli close ./MyGame
```

## `tools`

プロジェクトの Unity Editor が提供する AI ツール — シーン、アセット、テスト、スクリーンショット、そしてインストール済みの拡張機能が
追加するすべて — を、それぞれの機能と入力とともに一覧表示するか、1 つのツールを詳細に表示します (完全な説明、入力、呼び出し例、
JSON 入力スキーマ)。エディターが実行されていなければ、[`exec`](#exec) と同様にヘッドレスのエディターを起動します。一覧は MCP
(`tools/list`) で読み取られてプロジェクト内 (git 無視) に保存されるため、エディターが閉じている間でも `--cached` は即座に応答します。
非表示のツール (MCP クライアントに提供されないもの) は名前で一覧表示されます。`run-tool` はそれらも呼び出せます。

```bash
unity-mcp-cli tools                 # every tool, one line each, with its inputs
unity-mcp-cli tools rql-mutate      # one tool: description, inputs, example call, schema
unity-mcp-cli tools screenshot      # a regex filter over names and descriptions
unity-mcp-cli tools --format json   # full schemas, machine-readable
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[name]` | いいえ | ツール名 (詳細) または正規表現 (フィルター) |
| `--path <path>` | いいえ | Unity プロジェクトのパス (デフォルト: カレントディレクトリ) |
| `--cached` | いいえ | エディターに接続せず、保存された一覧から応答します |
| `--no-start` | いいえ | エディターを起動しません。応答するエディターがなければ保存された一覧を使います |
| `--once` | いいえ | この呼び出しがエディターを起動した場合、その後エディターを閉じます |
| `--ready-timeout <ms>` | いいえ | 起動したエディターを待つ時間 (デフォルト: 300000) |
| `--url <url>` / `--token <token>` | いいえ | サーバーの上書き |
| `--format <format>` | いいえ | `text`、`json` または `ndjson` |

## `rql`

JSON のクォートなしで、コマンドラインから RQL を実行します。1 つのクエリ、または `;` で区切った複数のクエリです。最後のステージが `set` または `call` のクエリは
`rql-mutate` に送られます（複数の場合: いずれか 1 つがそうなら、すべてがそうでなければなりません。各クエリはそれぞれ独立した全か無かの変更で、最初に失敗したものが残りを止めます）。それ以外は `rql-query` に送られます。`--schema [type]` は `rql-schema` を実行します。`run-tool` と同様にヘッドレスのエディターを起動します。
Unity は変更をすぐにディスクに保存するため、`set` / `call` の後、CLI は書き込んだシーンとアセットのファイルを表示します
(`Saved to disk: Assets/Scenes/Forest.unity`)。他の言語のように綴られた演算子 (`startsWith`、`like`) には、
RQL での綴りのヒントが表示されます。`rql --help` はレシピを一覧表示します。

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli rql 'objects | where name starts-with "Rock" | set transform.localScale = transform.localScale * 1.5'
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[query]` | はい (`--schema` / `-f` の場合を除く) | RQL クエリ (シングルクォートで囲み、内部の文字列にはダブルクォートを使います) |
| `-f, --file <file>` | いいえ | ファイルからクエリを読み込みます (`-`: stdin)。内側のクォートを取り除いてしまうシェル向けです |
| `--schema [type]` | いいえ | RQL のソース、または 1 つの型のメンバーを一覧表示します |
| `--dry-run` | いいえ | `set` / `call` の場合: 何が変更されるかを表示し、何も変更しません |
| `--max-targets <n>` | いいえ | `set` / `call` の場合: 200 を超えるターゲット (最大 1000) |
| `--path <path>` | いいえ | Unity プロジェクトのパス (デフォルト: カレントディレクトリ) |
| `--no-start` / `--once` | いいえ | `run-tool` と同様 (`--url`、`--token`、`--raw`、`--timeout`、`--ready-timeout` も使えます) |
| `--format <format>` | いいえ | `text`、`json` または `ndjson` |

## `run-tool`

HTTP API を介して MCP ツールを直接実行します。サーバー URL と認可トークンは、現在の接続モード (Custom または Cloud) に基づいて、プロジェクトの設定ファイル (`UserSettings/AI-Game-Developer-Config.json`) から**自動的に解決**されます。

```bash
unity-mcp-cli run-tool rql-mutate ./MyGame --input '{"query":"editor | call CreateGameObject({name: \"Cube\", primitive: \"Cube\"})"}'
```

| オプション | 必須 | 説明 |
|---|---|---|
| `<tool-name>` | はい | 実行する MCP ツールの名前 |
| `[path]` | いいえ | Unity プロジェクトのパス (位置引数または `--path`) — 設定の読み取りとポートの検出に使われます |
| `--url <url>` | いいえ | サーバー URL の直接上書き (設定をバイパス) |
| `--token <token>` | いいえ | Bearer トークンの上書き (設定をバイパス) |
| `--input <json>` | いいえ | ツール引数の JSON 文字列 (デフォルトは `{}`) |
| `--input-file <file>` | いいえ | ファイルから JSON 引数を読み込みます |
| `--raw` | いいえ | 生の JSON を出力します (整形なし、スピナーなし) |
| `--format <format>` | いいえ | `text` (デフォルト)、`json` または `ndjson` — [機械可読な出力](#終了コードと機械可読な出力)を参照 |
| `--timeout <ms>` | いいえ | リクエストのタイムアウト (ミリ秒) (デフォルト: 60000) |
| `--no-start` | いいえ | エディターを起動しません。応答するエディターがなければ失敗します (終了コード 7) |
| `--once` | いいえ | この呼び出しがエディターを起動した場合、呼び出し後にエディターを閉じます (CI での単発利用) |
| `--ready-timeout <ms>` | いいえ | 起動したエディターの準備が整うまで待つ時間 (デフォルト: 300000) |

**エディターの起動。** プロジェクトのエディターが応答しない場合、`run-tool` は [`exec`](#exec) とまったく同じように
ヘッドレスのエディターを起動します (共有のセッションリース、自分が起動していないエディターは決して閉じません) — ただし、
`ProjectSettings/ProjectVersion.txt` に Unity のバージョンが記載されているプロジェクトに限るため、エディターを推測することはありません。`--no-start` はこれをオフにします。
`--once` は呼び出しを単発にします: この呼び出しが起動したエディターは呼び出し後に再び閉じられます (準備が整わなかった
場合も同様)。`run-system-tool` も同じ動作をします。

エラーで応答したツールは 6 で終了します。不明なツールは 2 (`NOT_FOUND`)、タイムアウトは 14、到達できないエディターは 7、ビジーな
エディターは 17、モーダルダイアログでブロックされた呼び出しは 18 です — HTTP ステータスを含む完全な表は
[終了コード](#終了コードと機械可読な出力)にあります。

**URL 解決の優先順位:**
1. `--url` → そのまま使用
2. 設定ファイル → `host` (Custom モード) またはハードコードされたクラウドエンドポイント (Cloud モード)
3. プロジェクトパスから決定的に導かれるポート

**認可**はプロジェクトの設定から自動的に読み取られます (Custom モードでは `token`、Cloud モードでは `cloudToken`)。設定から導かれたトークンを明示的に上書きするには `--token` を使います。

**例 — ツールを呼び出す (URL と認証は設定から):**

```bash
unity-mcp-cli run-tool rql-query ./MyGame --input '{"query":"objects | where name == \"Player\" | select path name active"}'
```

**例 — URL を明示的に上書きする:**

```bash
unity-mcp-cli run-tool rql-mutate --url http://localhost:8080 --input '{"query":"editor | call SaveScenes()"}'
```

**例 — 生の JSON 出力をパイプする:**

```bash
unity-mcp-cli run-tool unity-tool-list ./MyGame --raw | jq .
```

**例 — CI での単発呼び出し (ヘッドレスのエディターを起動し、後で閉じる):**

```bash
unity-mcp-cli run-tool package-list ./MyGame --url http://localhost:28343 --once --format json
```

## `exec`

Unity Editor で C# を実行し、結果を表示します。プロジェクトがすでに開かれている場合 (ウィンドウ付きまたはヘッドレス)、`exec` はそのエディターを使い、決して閉じません。それ以外の場合は、次の呼び出しのために起動したままになるヘッドレスのエディターを起動し、ツール呼び出しがないまま `--idle` 秒が経過すると自動的に終了します (変更されたシーンとアセットを保存します)。起動したエディターはプロジェクトのセッションリース (`.ai-game-dev/session.json`) に記録され、ファイルロックの下で起動されるため、並列の `exec` 呼び出しは 1 つのエディターを共有します。`close --owned-only` でそれを閉じることができ、`open` (ウィンドウ付き) はウィンドウを開く前にそれを閉じます。これらのセッションファイルはマシンローカルです: `.ai-game-dev/.gitignore` によって git の対象外になります。CLI が起動していないエディターでプロジェクトが開かれていて、そのプラグインが 60 秒以内に応答しない場合 (切断されている、または ai-game.dev クラウドに接続されている)、`exec` は待たずにヒントを出して停止します: プラグインを接続するか、接続を Custom (ローカルサーバー) に切り替えるか、`--url` を指定してください。

```bash
# The last expression is the result
unity-mcp-cli exec 'GameObject.FindObjectsOfType<Camera>().Length'

# From a file or stdin, with extra namespaces and helper files
unity-mcp-cli exec -f ./snippet.cs --using MyGame.Gameplay --include Helpers/
echo 'Application.unityVersion' | unity-mcp-cli exec
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[code]` | いいえ | C# スニペット: 単純な文で、最後の式が結果になります。代わりに `-f` または stdin も使えます。 |
| `--path <path>` | いいえ | Unity プロジェクトのパス (デフォルト: カレントディレクトリ) |
| `-f, --file <file>` | いいえ | ファイルからコードを読み込みます (stdin の場合は `-`) |
| `--using <namespace>` | いいえ | 追加の名前空間。繰り返し指定可能 |
| `--include <path>` | いいえ | ヘルパーの `.cs` ファイルまたはフォルダー (再帰的)。`<Project>/.ai-game-dev/snippets` からの相対パス。繰り返し指定可能 |
| `--window` | いいえ | ヘッドレスではなくウィンドウ付きでエディターを起動します (まだ実行されていない場合のみ) |
| `--idle <seconds>` | いいえ | `exec` が起動したエディターを、アイドル状態がこの秒数続いたら終了します (デフォルト: 300) |
| `--ready-timeout <ms>` | いいえ | エディターの準備が整うまで待つ時間 (デフォルト: 300000) |
| `--timeout <ms>` | いいえ | ツール呼び出しのタイムアウト (デフォルト: 310000) |
| `--raw` | いいえ | ツールの生の応答 JSON を出力します |
| `--format <format>` | いいえ | `text` (デフォルト)、`json` または `ndjson` |
| `--once` | いいえ | この呼び出しがエディターを起動した場合、呼び出し後にエディターを閉じます |
| `--url <url>` | いいえ | サーバー URL の上書き。ローカルの URL の場合は、新たに起動したエディターがそのサーバーも起動します |
| `--token <token>` | いいえ | Bearer トークンの上書き |

`exec` は `script-execute` ツールを呼び出すため、スニペット、`usings`、`include` には Code-Exec をサポートするプラグインのバージョンが必要です。コンパイルエラーまたは例外は終了コード 6 で終了し、あなた自身のコードの該当行を示します。

> **サインインしていない場合:** 新しいプロジェクトは **Cloud** 接続モードで始まります。ai-game.dev にサインインしていない場合、`exec`、
> `run-tool`、`compile`、`tools` は代わりにプロジェクトの**ローカル** MCP サーバーを使います (その旨を stderr に表示します): これらが起動する
> エディターには、メモリ上でのみそのサーバーを提供するよう指示され、プロジェクトの設定は変更されません。クラウド経由で作業するには `login` を実行してください。
>
> CLI が起動したヘッドレスのエディターは、プロジェクトのスクリプトがコンパイルできなくても実行を続けるため
> (`-ignorecompilererrors`)、`compile` はエラーを報告できます。そのログは `Logs/unity-mcp-cli-editor.log` です。

## `test`

CI と同じ方法でプロジェクトの Unity テストを実行します: **Unity 自身の `-runTests` を使うバッチモードのエディター** — AI Game
Developer プラグインも MCP サーバーも関与しないため、プラグインが壊れているか欠けているプロジェクトでも動作します。判定は
実行が書き出す NUnit レポートから得られ (同じパスにある以前のレポートは先に削除されます)、エディターの終了コードだけで
判定されることはありません。

```bash
unity-mcp-cli test ./MyGame                                  # EditMode, report in ./test-results.xml
unity-mcp-cli test ./MyGame --mode PlayMode --filter "MyGame.Tests.Combat" --junit-output junit.xml
unity-mcp-cli test ./MyGame --format github                  # ::error annotations at the failing test's source line
unity-mcp-cli test ./MyGame --format json --timeout 900 -- -nographics
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unity プロジェクトのパス (位置引数または `--path`、デフォルト: カレントディレクトリ) |
| `--mode <mode>` | いいえ | `EditMode` (デフォルト) または `PlayMode` |
| `--filter <pattern>` | いいえ | Unity の `-testFilter`: 完全なテスト名または正規表現。複数の場合は `;` で区切ります |
| `--category <names>` | いいえ | Unity の `-testCategory` |
| `--assembly <names>` | いいえ | Unity の `-assemblyNames` |
| `--output <file>` | いいえ | NUnit レポートのパス (デフォルト: `<project>/Logs/test-results.xml`) |
| `--junit-output <file>` | いいえ | JUnit レポートも書き出します |
| `--log-file <file>` | いいえ | エディターのログ (デフォルト: レポートのパスに `.log` を付けたもの) |
| `--timeout <seconds>` | いいえ | この時間が経過したらエディター (とその子プロセス) を強制終了します (デフォルト: 3600) |
| `--nographics` | いいえ | GPU なしで実行します |
| `--unity <version>` / `--editor-path <path>` | いいえ | 使用するエディター (デフォルト: プロジェクトのバージョン) |
| `--format <format>` | いいえ | `text`、`json`、`ndjson` または `github` |
| `-- <args>` | いいえ | 追加のエディター引数。`test` が管理する引数 (`-batchmode`、`-runTests`、`-quit`、...) は拒否されます |

**終了コード:** `0` すべて成功 (テストが 0 件の実行は警告付きで成功)、`8` テスト失敗、`6` 判定なし (コンパイル
エラー — `file(line,col): error CS...` として一覧表示 — ライセンスの問題、クラッシュ、またはプロジェクトが**すでにエディターで開かれている**:
2 つ目のエディターでは開けないため、代わりにそのエディターに対して `run-tool tests-run` を使ってください)、`14` タイムアウト、`2` 不正な
引数。Ctrl+C やキャンセルされた CI ジョブでもエディターは停止します。

## `compile`

プロジェクトのエディターに対して「C# はコンパイルできるか?」を確認します: 変更されたスクリプトがコンパイルされるようアセットを更新し、コンパイル
とそのドメインリロードを待ち、エディターの Console からコンパイラーのエラーと警告をファイル、行、列とともに報告します。
エディターが実行されていなければヘッドレスのエディターを起動します (`exec` と同様に、次の呼び出しのために起動したままにします)。起動済みの
エディターに対する呼び出しは数秒で済みます。

```bash
unity-mcp-cli compile ./MyGame
unity-mcp-cli compile ./MyGame --strict --format github   # warnings fail too
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unity プロジェクトのパス (位置引数または `--path`) |
| `--strict` | いいえ | コンパイラーの警告でも失敗にします |
| `--timeout <seconds>` | いいえ | コンパイルとドメインリロードを待つ時間 (デフォルト: 180) |
| `--ready-timeout <ms>` | いいえ | 起動したエディターを待つ時間 (デフォルト: 300000) |
| `--once` | いいえ | この呼び出しがエディターを起動した場合、その後エディターを閉じます |
| `--url <url>` / `--token <token>` | いいえ | サーバーの上書き。`exec` と同様 |
| `--format <format>` | いいえ | `text`、`json`、`ndjson` または `github` |

**終了コード:** `0` コンパイル成功、`6` コンパイルエラー (`--strict` の場合は警告も)、`7` どのエディターにも到達できない、`14`
タイムアウト、`18` モーダルダイアログがエディターをブロックしている。診断はコンパイラー自身の
`file(line,col): error CS0103: ...` 形式で stderr に出力されます。

## `dialog`

モーダルダイアログ (保存の確認、"Enter Safe Mode?"、`EditorUtility.DisplayDialog`) はエディターのメインスレッドをブロックするため、
誰かが回答するまですべてのツール呼び出しが待たされます。`dialog` はプロジェクトのエディターのダイアログを一覧表示し、
ボタンを押すことができます。`status`、`run-tool`、`exec`、`compile`、`wait-for-ready` は、そのようなダイアログを単なるタイムアウトではなく
終了コード `18` (`AWAITING_INPUT`) として報告します。Windows、macOS、Linux に対応しています。ヘッドレス (`-batchmode`) のエディターは
ダイアログを表示しません。

| プラットフォーム | 方法 | 要件 |
|---|---|---|
| **Windows** | PowerShell を介した Win32 (`EnumWindows`、`BM_CLICK`) | なし |
| **macOS** | `osascript -l JavaScript` を介した System Events の UI スクリプティング: エディターのダイアログウィンドウとシート、そのテキストとボタン | CLI を実行するアプリ (ターミナルまたは AI エージェントアプリ) に対する**アクセシビリティの権限**: システム設定 → プライバシーとセキュリティ → アクセシビリティ。これがないと `dialog` はそのヒントを出して失敗し、終了コード 18 のチェックではダイアログが見つかりません。 |
| **Linux** | `python3` ヘルパー (PyGObject) を介した AT-SPI アクセシビリティバス: エディターのダイアログとアラートのウィンドウ (Unity の `EditorUtility.DisplayDialog` が使う GTK)、そのラベルとプッシュボタンを、`click` アクションで押します。X11 と Wayland で動作します | PyGObject と AT-SPI typelib を備えた `python3` (Debian/Ubuntu: `python3-gi gir1.2-atspi-2.0`、Fedora: `python3-gobject at-spi2-core`、Arch: `python-gobject at-spi2-core`) と、実行中のアクセシビリティバス (`at-spi2-core`。GNOME、KDE、およびほとんどのデスクトップでは必要に応じて起動されます)。エディターを実行しているデスクトップセッションで CLI を実行してください。パッケージの欠如、到達できないバス、またはグラフィカルセッションがない場合、`dialog` は修正方法を示すヒントを出して失敗し、終了コード 18 のチェックではダイアログが見つかりません。 |

Windows では、Unity 自身のモーダルウィンドウも一覧表示されます。例えば Unity 6.6 が署名されていない `.unitypackage` (すべての Asset Store パッケージ) を
インポートする前に表示する "Missing signature" などです: Unity はそのテキストとボタンを自前で描画するため、一覧に表示されるのは
タイトルと 1 つのボタン **Cancel** だけで、`--press Cancel` はそれを Escape キーとして送ります。それ以外の回答 (Import Anyway)
はユーザーがエディター内で行います。

ダイアログの id は一覧から取得し、そのダイアログが開いている間は変わりません (Windows: ウィンドウハンドル、macOS と
Linux: pid、タイトル、テキスト、ボタンのハッシュ)。ボタンのラベルがスクリプトに埋め込まれることはなく、データとして渡されるだけです。

ヘッドレスの Linux マシンでは、`at-spi2-core` をインストールした上で、エディター (`-batchmode` なし) と CLI を 1 つの
`dbus-run-session` の中で `Xvfb` の下で実行します。同じユーザーがデスクトップセッションも持っている場合は、そのプライベート
セッションに独自の `XDG_RUNTIME_DIR` を与えてください: そうしないと、そのアクセシビリティバスがデスクトップの
`at-spi` ソケットを置き換えてしまいます (そして終了時に削除します)。

```bash
unity-mcp-cli dialog ./MyGame                          # list: title and buttons
unity-mcp-cli dialog ./MyGame --press "Don't Save"     # answer it
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unity プロジェクトのパス (位置引数または `--path`) |
| `--press <button>` | いいえ | このラベルのボタンを押します (大文字小文字を区別しない) |
| `--id <id>` | いいえ | `--press` と併用: 回答するダイアログ (一覧から取得) |
| `--format <format>` | いいえ | `text`、`json` または `ndjson` |

## `commands`

すべてのコマンドとその引数、オプション、デフォルト値、そして終了コードの表 — CLI 自体から読み取られるため、
ずれることはありません。エージェントとツール向けです。

```bash
unity-mcp-cli commands --format json
```

## `wait-for-ready`

Unity Editor と MCP サーバーが接続され、ツール呼び出しを受け付けられる状態になるまで待ちます。サーバーが正常に応答するかタイムアウトに達するまで、設定可能な間隔でサーバーをポーリングします。`open` が Unity を起動しても、エージェントがいつツールの呼び出しを開始できるかを知る必要がある、自動化スクリプトや AI エージェントのオーケストレーションで便利です。

```bash
unity-mcp-cli wait-for-ready ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unity プロジェクトのパス (位置引数または `--path`) — 設定の読み取りとポートの検出に使われます |
| `--url <url>` | いいえ | サーバー URL の直接上書き (設定をバイパス) |
| `--token <token>` | いいえ | Bearer トークンの上書き (設定をバイパス) |
| `--timeout <ms>` | いいえ | 最大待機時間 (ミリ秒) (デフォルト: 120000) |
| `--interval <ms>` | いいえ | ポーリング間隔 (ミリ秒) (デフォルト: 3000) |
| `--format <format>` | いいえ | `text` (デフォルト)、`json` または `ndjson` |

準備が整うと `0`、タイムアウトで `14`、モーダルダイアログがエディターをブロックしている場合は `18` で終了します。

**例 — デフォルトのタイムアウト (120 秒) で待つ:**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
```

**例 — CI 向けの短いタイムアウト:**

```bash
unity-mcp-cli wait-for-ready ./MyGame --timeout 60000 --interval 2000
```

**例 — サーバー URL を明示する:**

```bash
unity-mcp-cli wait-for-ready --url http://localhost:8080 --timeout 30000
```

## `setup-mcp`

AI エージェント用の MCP 設定ファイルを書き出し、Unity Editor の UI なしでのヘッドレス/CI セットアップを可能にします。14 のエージェントすべて (Claude Code、Cursor、Gemini、Codex など) をサポートしています。

設定はプロジェクトごとの URL (http トランスポートの場合は `https://ai-game.dev/mcp/p/<pin>`、stdio の場合は `project=<pin>` 引数) によってこのプロジェクトに**固定**されます。Cloud の http トランスポートの場合は、すべてのエージェントについてこのプロジェクトの**プロジェクトキー** (`Authorization: Bearer agd_pk_…`) も含まれます: このプロジェクトだけに紐づく、有効期限のない取り消し可能な認証情報で、`~/.ai-game-dev/project-keys.json` から再利用されるか、[`login`](#login) でサインインしたアカウントで発行されます。サインインしていない場合 (またはキーを取得できない場合)、設定は URL のみとなり、エージェントは自身の OAuth でサインインします。常に URL のみの設定を書き出すには `--oauth` を、キーを置き換えるには `--regenerate-key` を (以前のキーを含むこのプロジェクトの他のエージェント設定は新しいキーに移行されて表示され、その後以前のキーが取り消されます。いずれかを書き換えられない場合、以前のキーは有効なまま残され、警告でそれが示されます)、代わりに明示的な PAT を書き出すには `--token` を、共有の固定されていないエンドポイントには `--no-pin` を指定します。stdio とローカルサーバーの設定にプロジェクトキーが含まれることはありません。Antigravity はインストールによって `~/.gemini/config/mcp_config.json` または `~/.gemini/antigravity/mcp_config.json` のどちらかを読み込むため、両方のファイルが書き出され、書き出されたすべてのパスが表示されます。いずれかを書き出せない場合、コマンドは失敗します。この出力は Unity Editor の **Configure** ボタンと一致します。

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[agent-id]` | はい | 設定するエージェント (すべてを見るには `--list` を使用) |
| `[path]` | いいえ | Unity プロジェクトのパス (デフォルトは cwd) |
| `--transport <transport>` | いいえ | トランスポート方式: `stdio` または `http` (デフォルト: `http`) |
| `--url <url>` | いいえ | サーバー URL の上書き (http トランスポート用) |
| `--token <token>` | いいえ | 明示的な PAT — プロジェクトキーの代わりに `Authorization` ヘッダーとして書き出されます |
| `--oauth` | いいえ | プロジェクトキーの代わりに、URL のみの Cloud 設定 (エージェントは自身の OAuth でサインイン) を書き出します |
| `--regenerate-key` | いいえ | 新しいプロジェクトキーを発行し、設定を書き換え、以前のキーを取り消します (Cloud http のみ) |
| `--no-pin` | いいえ | 固定されていない URL を書き出す / `project=` 引数を省略します (デフォルト: `/mcp/p/<pin>` でこのプロジェクトに固定) |
| `--list` | いいえ | 利用可能なすべてのエージェント ID を一覧表示します |

**例 — サポートされているすべてのエージェントを一覧表示する:**

```bash
unity-mcp-cli setup-mcp --list
```

**例 — stdio トランスポートで Cursor を設定する:**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

**例 — このプロジェクトのキーを置き換える (古いキーは使えなくなります):**

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --regenerate-key
```

**例 — 固定されていない設定を書き出す (共有の `/mcp` エンドポイント):**

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --no-pin
```

## `setup-skills`

AI エージェント用のスキルファイルを書き出します。常にオフラインで (エディターもサーバーも不要): `unity-mcp-cli` スキル — エージェントが
コールドスタートからこの CLI を使う方法 (最初のエディターコマンドがヘッドレスのエディターを起動する、`rql` のレシピ、`compile` / `test`、
`--format json`、終了コードと `dialog`) — と、詳細のための `references/rql.md` と `references/install.md`、さらに
`ai-game-dev-app` を書き出します。レシピは `--help` と同じソースから生成されるため、両者がずれることはありません。その後、
プロジェクトのエディターが応答すれば、プラグインにツールごとのスキルも要求します。エディターが閉じていてもエラーにはなりません。

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[agent-id]` | はい | スキルを生成するエージェント (すべてを見るには `--list` を使用) |
| `[path]` | いいえ | Unity プロジェクトのパス (デフォルトは cwd) |
| `--url <url>` | いいえ | サーバー URL の上書き |
| `--token <token>` | いいえ | 認証トークンの上書き |
| `--list` | いいえ | すべてのエージェントとスキルのサポート状況を一覧表示します |
| `--offline` | いいえ | `unity-mcp-cli` スキルのみを書き出し、エディターには問い合わせません |
| `--timeout <ms>` | いいえ | リクエストのタイムアウト (ミリ秒) (デフォルト: 60000) |

**例 — スキルをサポートするエージェントを一覧表示する:**

```bash
unity-mcp-cli setup-skills --list
```

## `remove-plugin`

Unity プロジェクトの `Packages/manifest.json` から Unity-MCP プラグインを削除します。

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | はい | Unity プロジェクトへのパス (位置引数または `--path`) |

このコマンドは:
1. `dependencies` から `com.ivanmurzak.unity.mcp` を削除します
2. **スコープ付きレジストリとスコープは維持します** — 他のパッケージがそれらに依存している可能性があります
3. プラグインがインストールされていない場合は**何もしません**

> このコマンドを実行した後、変更を適用するために Unity Editor でプロジェクトを開いてください。

## `status`

Unity Editor と MCP サーバーの接続状態を確認します。Unity が実行中かどうか、ローカルの MCP サーバーに到達できるかどうか、そして設定から解決されたサーバー (例: クラウド) に到達できるかどうかを表示します。

```bash
unity-mcp-cli status ./MyGame
```

| オプション | 必須 | 説明 |
|---|---|---|
| `[path]` | いいえ | Unity プロジェクトのパス (位置引数または `--path`) |
| `--url <url>` | いいえ | サーバー URL の直接上書き (設定をバイパス) |
| `--token <token>` | いいえ | Bearer トークンの上書き (設定をバイパス) |
| `--timeout <ms>` | いいえ | プローブのタイムアウト (ミリ秒) (デフォルト: 5000) |
| `--format <format>` | いいえ | `text` (デフォルト)、`json` または `ndjson` |

MCP サーバーが応答すると `0`、応答しないと `7`、モーダルダイアログがエディターをブロックしている場合は `18` で終了します (出力に
一覧表示されます。[`dialog`](#dialog) を参照)。

**出力例:**

```
Unity-MCP Status
  Project: /path/to/MyGame
──────────────────────────────────────────────────
Unity Editor Process
✔  Unity is running (PID: 53740)
Local MCP Server
  URL: http://localhost:22958
✖  Not available (connection refused)
Config Server
  URL: https://ai-game.dev/mcp
✔  Connected
──────────────────────────────────────────────────
✔  MCP server is reachable — ready for tool calls
```

## グローバルオプション

これらのオプションはすべてのコマンドで使用できます:

| オプション | 説明 |
|---|---|
| `-v, --verbose` | トラブルシューティング用に詳細な診断出力を有効にします |
| `--version` | CLI のバージョンを表示します |
| `--help` | コマンドのヘルプを表示します |

**例 — 任意のコマンドを詳細出力付きで実行する:**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

## 終了コードと機械可読な出力

`unity-mcp-cli`、`unreal-mcp-cli`、`godot-cli` は 1 つの終了コード表を共有しているため、スクリプトやエージェントはメッセージを読まずに
「コマンドラインを修正する」「エディターが失敗した」「テストが失敗した」を区別できます:

| コード | 名前 | 意味 |
|---|---|---|
| `0` | `SUCCESS` | コマンドが成功しました |
| `1` | `ERROR` | 分類されないエラー |
| `2` | `USAGE` | 不正な引数、不明なツール。何も実行されていません。コマンドラインを修正してください — そのまま再試行しないでください |
| `6` | `COMMAND_FAILED` | コマンドは実行されたが失敗しました (ツールのエラー、コンパイルエラー、判定のないテスト実行、拒否された認証情報) |
| `7` | `UNREACHABLE` | エディターまたは MCP サーバーを起動できないか、到達できませんでした。環境を修正してから再試行してください |
| `8` | `TESTS_FAILED` | テストが実行され、一部が失敗しました |
| `14` | `TIMEOUT` | 結果が出る前にタイムアウトしました |
| `17` | `BUSY` | エディターがビジーで呼び出しを実行しませんでした。後で再試行してください |
| `18` | `AWAITING_INPUT` | モーダルダイアログがエディターをブロックしています。人が回答する必要があります ([`dialog`](#dialog) を参照) |

`--format json` (`run-tool`、`run-system-tool`、`exec`、`test`、`compile`、`dialog`、`status`、`wait-for-ready`、
`commands` で使用可能) は、stdout にちょうど 1 つの結果エンベロープを出力し、それ以外はすべて stderr に出力します。`--format ndjson` は
同じエンベロープを 1 行で出力し、進捗行 (`{"type":"progress",...}`) を stderr にストリーミングします。`--format json` 指定時の
コマンドラインエラーでもエンベロープが出力されます (コード `USAGE`)。

```json
{
  "success": false,
  "command": "test",
  "data": { "verdict": "failed", "summary": { "total": 3, "passed": 2, "failed": 1 } },
  "errors": [{ "code": "TESTS_FAILED", "message": "1 test(s) failed." }],
  "warnings": []
}
```

`errors[].code` は安定したトークンです (`USAGE`、`NOT_FOUND`、`NOT_AUTHENTICATED`、`TOOL_FAILED`、`UNREACHABLE`、`TIMEOUT`、
`BUSY`、`AWAITING_INPUT`、`TESTS_FAILED`、`TEST_RUN_ERROR`、`TEST_TIMED_OUT`、`COMPILATION_FAILED`、`PROJECT_OPEN`、
`ERROR`、...)。メッセージではなく、これと照合してください。

**成功しなかったツール呼び出し** (`run-tool`、`run-system-tool`、`exec`、`rql`、`tools`) は、3 つの CLI すべてで同じ終了コード
とエラーコードに対応付けられます (表は `@baizor/gamedev-cli-core` にあり、`commands --format json` はそれを
`errorCodes` として一覧表示します)。MCP サーバーが応答した場合はその HTTP ステータスで決まり、エンベロープのエラーにそれが含まれます:
`errors[{code, message, httpStatus}]`。

| 状況 | HTTP ステータス | 終了コード | `errors[].code` |
|---|---|---|---|
| 不正な引数: 不正なフラグ、CLI またはサーバーが拒否したツール引数 (ここに記載のないすべての 4xx) | 400 (422 `[invalid_params]` も含む) | `2` | `USAGE` |
| その名前のツールがない (`tools` で一覧表示)、またはパスにプロジェクトがない | 404 | `2` | `NOT_FOUND` |
| 使用可能な認証情報がない、またはサーバーがそれを拒否した: `login` でサインインしてください | 401, 403 | `6` | `NOT_AUTHENTICATED` |
| ツールが実行され、エラーを報告した (`status: "error"`、ここに記載のないすべての 5xx) | 409, 422, 500 | `6` | `TOOL_FAILED` |
| プロジェクトのコードがビルドできなかったため、エディターは起動されなかった | — | `6` | `COMPILATION_FAILED` |
| 接続が拒否またはリセットされた、サーバーにエンジンプラグインが接続されていない、呼び出し送信後にプラグインが失われた | 502 | `7` | `UNREACHABLE` |
| 時間内に応答がない: `--timeout`、またはサーバー自身のツールタイムアウト | 408, 504 | `14` | `TIMEOUT` |
| ビジーで呼び出しを実行しなかった (リロード中、レート制限、別の CLI 呼び出しがエディターを使用中)。後で再試行してください | 503, 429 | `17` | `BUSY` |
| モーダルダイアログがエディターをブロックしている | — | `18` | `AWAITING_INPUT` |
| その他すべて (呼び出しが中断された、予期しないエラー) | — | `1` | `ERROR` |

# 完全な自動化の例

1 つのスクリプトで、ゼロから完全な Unity MCP プロジェクトをセットアップします:

```bash
# 1. Create a new Unity project
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Install the Unity-MCP plugin
unity-mcp-cli install-plugin ./MyAIGame

# 3. Enable all MCP tools
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Sign in to ai-game.dev (browser OAuth; credential saved to ~/.ai-game-dev)
unity-mcp-cli login

# 5. Open the project (auto-connects and generates skills for claude-code)
unity-mcp-cli open ./MyAIGame

# 6. Wait for Unity Editor and MCP server to be ready
unity-mcp-cli wait-for-ready ./MyAIGame

# 7. Check that the scripts compile, then close the editor
unity-mcp-cli compile ./MyAIGame
unity-mcp-cli close ./MyAIGame

# 8. Run the tests in a batch-mode editor (no plugin needed); JUnit for the CI dashboard
unity-mcp-cli test ./MyAIGame --junit-output junit.xml
```

# 仕組み

### 決定的なポート

CLI は、各 Unity プロジェクトのディレクトリパスに基づいて**決定的なポート**を生成します (SHA256 ハッシュをポート範囲 20000–29999 に対応付け)。これは Unity プラグインのポート生成と一致するため、手動で設定しなくてもサーバーとプラグインが自動的に同じポートで合意します。

### プラグインのインストール

`install-plugin` コマンドは `Packages/manifest.json` を直接変更します:
- [OpenUPM](https://openupm.com/) のスコープ付きレジストリ (`package.openupm.com`) を追加します
- 必要なすべてのスコープ (`com.ivanmurzak`、`extensions.unity`) を登録します
- バージョンを考慮した更新 (決してダウングレードしない) で `com.ivanmurzak.unity.mcp` 依存関係を追加します

### 設定ファイル

`configure` コマンドは `UserSettings/AI-Game-Developer-Config.json` を読み書きします。このファイルは以下を制御します:
- **Tools** — AI エージェントが利用できる MCP ツール
- **Prompts** — LLM の会話に挿入される定義済みプロンプト
- **Resources** — AI エージェントに公開される読み取り専用データ
- **接続設定** — ホスト URL、認証トークン、トランスポート方式、タイムアウト

### Unity Hub との連携

エディターを管理したりプロジェクトを作成したりするコマンドは、**Unity Hub CLI** (`--headless` モード) を使います。Unity Hub がインストールされていない場合、CLI は**自動的にダウンロードしてインストール**します:
- **Windows** — `UnityHubSetup.exe /S` によるサイレントインストール (管理者権限が必要な場合があります)
- **macOS** — DMG をダウンロードしてマウントし、`Unity Hub.app` を `/Applications` にコピーします
- **Linux** — `UnityHub.AppImage` を `~/Applications/` にダウンロードします

# ライブラリ API (v0.67.0+)

CLI バイナリに加えて、`unity-mcp-cli` はそのコアコマンドを型付きで副作用のないライブラリとして公開しているため、他の Node.js / TypeScript ツールはシェルを呼び出さずに同じインストール / 設定フローを組み込めます。

```ts
import { installPlugin, removePlugin, configure, setupMcp } from 'unity-mcp-cli';

const result = await installPlugin({
  unityProjectPath: './MyUnityProject',
  // version: '0.67.0',        // optional — defaults to latest from OpenUPM
  onProgress: (event) => {
    // phase is one of: 'start' | 'dependencies-resolved' | 'manifest-patched' | 'done'
    console.log(event.phase, event.message);
  },
});

if (!result.success) {
  console.error('Install failed:', result.error?.message);
  return;
}

console.log(`Installed v${result.installedVersion}`);
for (const warning of result.warnings) console.warn(warning);
for (const step of result.nextSteps) console.log(step);
```

各関数は型付きの `{ success, ... }` 結果オブジェクトを返し、エラーが公開境界を越えてスローされることはありません。ライブラリのエントリにはトップレベルの副作用がありません — `import 'unity-mcp-cli'` が argv を解析したり、stdout や stderr に書き込んだりすることはありません。

エクスポートされている関数と型の完全な一覧は [`CHANGELOG.md`](../CHANGELOG.md#0670---2026-04-21) を参照してください。
