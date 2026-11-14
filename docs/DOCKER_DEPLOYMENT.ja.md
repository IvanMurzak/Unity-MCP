[English](DOCKER_DEPLOYMENT.md) | [中文](DOCKER_DEPLOYMENT.zh-CN.md) | <b>日本語</b> | [Español](DOCKER_DEPLOYMENT.es.md) | [فارسی](DOCKER_DEPLOYMENT.fa.md) | [हिन्दी](DOCKER_DEPLOYMENT.hi.md) | [العربية](DOCKER_DEPLOYMENT.ar.md)

# Docker デプロイ

クラウドホスティングや隔離された環境のために、MCP サーバーをコンテナーで実行します。[README](README.ja.md) に戻る · [MCP サーバーと接続](mcp-server.ja.md)。

- **イメージ:** [`aigamedeveloper/mcp-server`](https://hub.docker.com/r/aigamedeveloper/mcp-server)。共有の [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) リポジトリからビルドされます
- **タグ:** `latest` とリリースバージョン `X.Y.Z`
- **ポート:** `8080` (公開)

Unity プロジェクトには引き続き [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP#quick-start) プラグインが必要で、サーバーの URL を指定した **Custom** モードに設定しておきます ([接続モード](mcp-server.ja.md#接続モード)を参照)。

![Docker launch](img/editor/docker-launch.gif)

## HTTP (セルフホスト、コンテナーの外部から到達可能)

サーバーはデフォルトでループバックにバインドされるため、公開ポート経由では到達できません。`MCP_BIND=any` を設定してください:

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp \
  -e MCP_PLUGIN_PORT=8080 \
  -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

エージェント設定:

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

サーバーが別の場所で動作している場合は、URL を実際のエンドポイントに置き換えてください。

### 共有トークンを使う

`token` モードでは、エージェントと Unity プラグインの両方から 1 つの bearer シークレット (`UNITY_MCP_TOKEN`、または AI Game Developer ウィンドウのトークン欄) が必要です。サーバーの前段で TLS が終端されていない限り、シークレットは平文の HTTP で送られます。

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=8080 -e MCP_BIND=any \
  -e MCP_AUTH=token -e MCP_PLUGIN_TOKEN=<secret> \
  aigamedeveloper/mcp-server
```

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080",
      "headers": { "Authorization": "Bearer <secret>" }
    }
  }
}
```

### カスタムポート

`MCP_PLUGIN_PORT` で設定したものと同じポートを公開します:

```bash
docker run -p 9090:9090 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=9090 -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

## stdio (エージェントがコンテナーを起動する)

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-e", "MCP_PLUGIN_CLIENT_TRANSPORT=stdio",
        "-e", "MCP_BIND=any",
        "-p", "8080:8080",
        "aigamedeveloper/mcp-server"
      ]
    }
  }
}
```

エージェントはコンテナーの stdin と stdout を介してサーバーと通信します。公開ポートは Unity プラグイン用です。

## 設定

すべてのサーバーオプションは環境変数 (`-e NAME=value`) です。主なものは [MCP サーバーと接続](mcp-server.ja.md#主なサーバーオプション)に記載されています。OAuth モードや Redis ベースのセッションを含む完全な一覧は [GameDev-MCP-Server の README](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration) にあります。
