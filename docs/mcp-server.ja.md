[English](mcp-server.md) | [中文](mcp-server.zh-CN.md) | <b>日本語</b> | [Español](mcp-server.es.md) | [فارسی](mcp-server.fa.md) | [हिन्दी](mcp-server.hi.md) | [العربية](mcp-server.ar.md)

# MCP サーバーと接続

プラグインが MCP サーバーに接続する仕組み、設定を上書きする環境変数、そしてサーバーを自分で実行する方法を説明します。[README](README.ja.md) に戻る。

<div align="center">

`AI agent` ↔ **`MCP server`** ↔ `Unity plugin (Editor or game)`

</div>

サーバーはこのリポジトリには含まれていません。それは共有のエンジン非依存の **[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)** (バイナリ `gamedev-mcp-server`、Docker イメージ `aigamedeveloper/mcp-server`) で、Godot-MCP と Unreal-MCP でも使われています。すべてのサーバーオプションのリファレンスはその README です。エージェントは `stdio` または `streamableHttp` でサーバーに接続し、プラグインは SignalR でサーバーに接続します。

## 接続モード

モードは AI Game Developer ウィンドウ (**Window ▸ AI Game Dev**) で選択し、`UserSettings/AI-Game-Developer-Config.json` に保存されます。

| モード | サーバー | 備考 |
| --- | --- | --- |
| **Cloud** (デフォルト) | `https://ai-game.dev/mcp` | ウィンドウまたは `unity-mcp-cli login` でサインインします。`keepServerRunning` が `true` でも、ローカルサーバーを起動することはありません。 |
| **Custom** | ローカルの `gamedev-mcp-server`、または自分でホストする任意の URL | 保存された `host` フィールドはこのモードでのみ使われます。ローカルサーバーを使うには、ウィンドウで **Custom** を選択するか、Unity を閉じた状態で `"connectionMode": "Custom"` を設定します。 |

**Cloud が既定である理由:** ポートを開く必要がなく、ほかのプログラムとのポート競合を避けられ、無料です。リレーはステートレスです。ツール呼び出しの内容を検査も永続化もせず、MCP イベントのメタデータ(イベントの種類、タイミング、ステータス、セッション識別子、ツール・プロンプト・リソースの名前、エラーの詳細)を記録し、ローリング方式の 90 日間の期間内で使用します([プライバシーポリシー](https://ai-game.dev/legal/privacy))。**Custom** はオフラインやエアギャップ環境での作業向けの対等な選択肢です。

ローカルサーバーを使う Custom モードでは、プラグインは `McpServerManager.cs` の `ServerVersion` で固定されたサーバーリリースを `Library/mcp-server/<rid>/` にダウンロードして起動します。**Tools ▸ AI Game Developer ▸ Server** には *Download Binaries* と *Open Server Logs* があります。

## プラグインの環境変数

Editor の起動時に読み込まれます。これらは保存された設定を**メモリ上でのみ**上書きし、書き戻されることはないため、ウィンドウには保存された選択がそのまま表示されます。コマンドライン引数は環境変数より優先されます。各変数はコマンドライン引数 `-NAME=value` としても受け付けられます (例: `-UNITY_MCP_TRANSPORT=stdio`)。`-url`、`-auth`、`-token` は短いエイリアスです。*env only* と記された行には対応する引数がありません。

| 変数 | 値 | 効果 |
| --- | --- | --- |
| `UNITY_MCP_CLOUD_URL` | URL | MCP サーバーの URL (`UNITY_MCP_HOST` はレガシーのエイリアス) |
| `UNITY_MCP_CONNECTION_MODE` | `Cloud` / `Custom` | 接続モード (ループバック URL の場合は `Custom` とみなされます) |
| `UNITY_MCP_KEEP_CONNECTED` | `true` / `false` | 接続をアクティブに保つ |
| `UNITY_MCP_AUTH_OPTION` | `none` / `oauth` / `token` | 認証モード (レガシーの `required` は `token` になります) |
| `UNITY_MCP_TOKEN` | 文字列 | 認証トークン |
| `UNITY_MCP_TRANSPORT` | `stdio` / `streamableHttp` | プラグインが設定するクライアントトランスポート |
| `UNITY_MCP_START_SERVER` | `true` / `false` | ローカルサーバープロセスを起動したままにする |
| `UNITY_MCP_TOOLS` | カンマ区切りのツール id | これらのツールのみを表示し、残りは非表示にします (名前を指定すれば呼び出し可能)。不明な id はエラーとしてログに記録されます。 |
| `UNITY_MCP_CREDENTIALS_DIR` | 絶対フォルダーパス | *env only.* マシンの認証情報ストアを `~/.ai-game-dev` の代わりにこのフォルダーから読み込みます (開発用およびテスト用の Editor)。相対パスの値は無視されます。 |
| `UNITY_MCP_SERVER_PATH` | `gamedev-mcp-server` への絶対パス | *env only.* 固定されたリリースの代わりにこのバイナリを起動します (下記参照) |
| `UNITY_MCP_DISABLE` | `1` / `true` | *env only.* プラグインの初期化をスキップします。[CI での無効化](ci-disable.ja.md)を参照 |

例: トークン付きでローカルサーバーに接続するバッチモード:

```bash
Unity.exe -batchmode -nographics \
  -UNITY_MCP_CLOUD_URL=http://localhost:8080 \
  -UNITY_MCP_KEEP_CONNECTED=true \
  -UNITY_MCP_AUTH_OPTION=token \
  -UNITY_MCP_TOKEN=my-secret-token
```

### `UNITY_MCP_SERVER_PATH` (サーバー開発と CI)

Editor は固定されたリリースの代わりに指定されたファイルを起動し、ダウンロードとバージョンチェックをスキップします。サーバーを開発するときや、CI がソースからサーバーをビルドするときに使います。

- まずプロセス環境から、次に Unity プロジェクトのルートにある `.env` ファイルから読み込まれるため、GUI や IDE から起動した Editor でも取得できます。
- **絶対**パスを使ってください。ファイルが存在しない場合、プラグインは変数が未設定であるかのように何も言わずに動作します。コンソールの `Starting MCP server: <path> …` の行に、実際に起動されたバイナリが表示されます。
- この上書きは生成されるエージェント設定の `command` にもなり、*Open Server Logs* もそれに従います。*Download Binaries* は引き続き `Library/mcp-server/<rid>/` にダウンロードします。

## サーバーを自分で実行する

セルフホストまたはリモート構成の場合に使います。[GameDev-MCP-Server のリリース](https://github.com/IvanMurzak/GameDev-MCP-Server/releases)からリリースの zip (`gamedev-mcp-server-<rid>.zip`) をダウンロードするか、[Docker](DOCKER_DEPLOYMENT.ja.md) を使ってください。

```bash
# stdio: the agent starts the server itself
./gamedev-mcp-server --port 8080 --client-transport stdio

# HTTP: run it as a standalone process
./gamedev-mcp-server --port 8080 --client-transport streamableHttp
```

stdio 用のエージェント設定 (パスを置き換えてください。Windows ではファイル名が `.exe` で終わります):

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["--port=8080", "--client-transport=stdio"]
    }
  }
}
```

プラグインも同じポートを使う必要があります。AI Game Developer ウィンドウで **Custom** モードを設定し、同じホストとポートを指定してください。

### 主なサーバーオプション

コマンドライン引数は環境変数より優先されます。引数は `-` 1 つでも、なし (`port=8080`) でも動作します。デフォルト値と完全な一覧: [GameDev-MCP-Server の設定](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration)。

| 環境変数 | 引数 | 意味 |
| --- | --- | --- |
| `MCP_PLUGIN_PORT` | `--port` | エージェント (HTTP) とプラグインの両方が使うポート (デフォルト `8080`) |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | エージェントのトランスポート: `stdio` または `streamableHttp` |
| `MCP_PLUGIN_CLIENT_TIMEOUT` | `--plugin-timeout` | プラグインへのカタログ、プロンプト、リソース要求のタイムアウト (ms) |
| `MCP_PLUGIN_TOOL_TIMEOUT` | `--tool-timeout` | 1 回のツール呼び出しが実行できる時間 (ms)。呼び出しは 1 回だけ送信され、再送されることはありません。 |
| `MCP_AUTH` (または `MCP_AUTHORIZATION`) | `--auth` (または `--authorization`) | `none`、`token`、または `oauth` |
| `MCP_PLUGIN_TOKEN` | `--token` | `token` モード用の共有シークレット。エージェントとプラグインの両方が送信する必要があります |
| `MCP_BIND` | `--bind` | バインドアドレス: `loopback` (デフォルト)、`any`、または IP |
| `MCP_PLUGIN_IDLE_TIMEOUT_SECONDS` | `--idle-timeout-seconds` | アイドル状態の streamableHttp セッションをこの秒数後に閉じる |

ホスト型のデプロイでは、ツール、プロンプト、リソース、接続の各イベントで webhook を呼び出したり、受信接続を認可したりすることもできます: `MCP_PLUGIN_WEBHOOK_TOOL_URL`、`MCP_PLUGIN_WEBHOOK_PROMPT_URL`、`MCP_PLUGIN_WEBHOOK_RESOURCE_URL`、`MCP_PLUGIN_WEBHOOK_CONNECTION_URL`、`MCP_PLUGIN_WEBHOOK_AUTHORIZATION_URL`、`MCP_PLUGIN_WEBHOOK_AUTHORIZATION_FAIL_OPEN`、`MCP_PLUGIN_WEBHOOK_TOKEN`、`MCP_PLUGIN_WEBHOOK_HEADER`、`MCP_PLUGIN_WEBHOOK_TIMEOUT` (すべて任意で、デフォルトでは未設定)。
