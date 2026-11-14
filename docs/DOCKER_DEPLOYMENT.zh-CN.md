[English](DOCKER_DEPLOYMENT.md) | <b>中文</b> | [日本語](DOCKER_DEPLOYMENT.ja.md) | [Español](DOCKER_DEPLOYMENT.es.md) | [فارسی](DOCKER_DEPLOYMENT.fa.md) | [हिन्दी](DOCKER_DEPLOYMENT.hi.md) | [العربية](DOCKER_DEPLOYMENT.ar.md)

# Docker 部署

在容器中运行 MCP 服务器，用于云端托管或隔离环境。返回 [README](README.zh-CN.md) · [MCP 服务器与连接](mcp-server.zh-CN.md)。

- **镜像：** [`aigamedeveloper/mcp-server`](https://hub.docker.com/r/aigamedeveloper/mcp-server)，由共享的 [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) 仓库构建
- **标签：** `latest` 以及发布版本号 `X.Y.Z`
- **端口：** `8080`（已暴露）

你的 Unity 项目中仍然需要安装 [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP#quick-start) 插件，并将其设置为 **Custom** 模式且填写服务器的 URL（参见[连接模式](mcp-server.zh-CN.md#连接模式)）。

![Docker 启动](img/editor/docker-launch.gif)

## HTTP（自托管，可从容器外部访问）

服务器默认绑定到回环地址，无法通过已发布的端口访问。请设置 `MCP_BIND=any`：

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp \
  -e MCP_PLUGIN_PORT=8080 \
  -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

智能体配置：

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

当服务器运行在其他位置时，请将 URL 替换为你实际的端点。

### 使用共享令牌

`token` 模式要求智能体和 Unity 插件（`UNITY_MCP_TOKEN`，或 AI Game Developer 窗口中的令牌字段）都提供同一个 bearer 密钥。除非在服务器前端终止 TLS，否则该密钥会以明文 HTTP 传输。

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

### 自定义端口

发布与 `MCP_PLUGIN_PORT` 中设置的相同端口：

```bash
docker run -p 9090:9090 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=9090 -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

## stdio（由智能体启动容器）

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

智能体通过容器的 stdin 和 stdout 与服务器通信；已发布的端口供 Unity 插件使用。

## 配置

所有服务器选项都是环境变量（`-e NAME=value`）。常用选项列在 [MCP 服务器与连接](mcp-server.zh-CN.md#常用服务器选项)中；完整列表（包括 OAuth 模式和基于 Redis 的会话）见 [GameDev-MCP-Server README](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration)。
