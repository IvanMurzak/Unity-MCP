[English](mcp-server.md) | <b>中文</b> | [日本語](mcp-server.ja.md) | [Español](mcp-server.es.md) | [فارسی](mcp-server.fa.md) | [हिन्दी](mcp-server.hi.md) | [العربية](mcp-server.ar.md)

# MCP 服务器与连接

插件如何连接到 MCP 服务器、可覆盖其设置的环境变量，以及如何自行运行服务器。返回 [README](README.zh-CN.md)。

<div align="center">

`AI agent` ↔ **`MCP server`** ↔ `Unity plugin (Editor or game)`

</div>

服务器不在本仓库中。它是共享的、与引擎无关的 **[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)**（二进制文件 `gamedev-mcp-server`，Docker 镜像 `aigamedeveloper/mcp-server`），Godot-MCP 和 Unreal-MCP 也使用它。它的 README 是所有服务器选项的参考文档。智能体通过 `stdio` 或 `streamableHttp` 连接到服务器；插件则通过 SignalR 连接到服务器。

## 连接模式

连接模式在 AI Game Developer 窗口（**Window ▸ AI Game Dev**）中选择，并保存在 `UserSettings/AI-Game-Developer-Config.json` 中。

| 模式 | 服务器 | 说明 |
| --- | --- | --- |
| **Cloud**（默认） | `https://ai-game.dev/mcp` | 通过窗口或 `unity-mcp-cli login` 登录。从不启动本地服务器，即使 `keepServerRunning` 为 `true` 也是如此。 |
| **Custom** | 本地 `gamedev-mcp-server`，或你托管的任意 URL | 保存的 `host` 字段仅在此模式下使用。要使用本地服务器，请在窗口中选择 **Custom**，或在 Unity 关闭时设置 `"connectionMode": "Custom"`。 |

**为什么默认使用 Cloud：** 它不需要开放任何端口，避免与其他程序发生端口冲突，而且免费。中继只保留元数据（IP、时间、工具名称、数据大小）90 天，从不保存请求或响应内容。**Custom** 是同样可行的选择，适用于离线或物理隔离（air-gapped）环境。

在使用本地服务器的 Custom 模式下，插件会将 `McpServerManager.cs` 中 `ServerVersion` 固定的服务器版本下载到 `Library/mcp-server/<rid>/` 并启动它。**Tools ▸ AI Game Developer ▸ Server** 中提供 *Download Binaries* 和 *Open Server Logs*。

## 插件环境变量

在编辑器启动时读取。它们**仅在内存中**覆盖已保存的配置，从不写回，因此窗口仍显示已保存的选择。命令行参数优先于环境变量。每个变量也可以作为命令行参数 `-NAME=value` 传入（例如 `-UNITY_MCP_TRANSPORT=stdio`）；`-url`、`-auth` 和 `-token` 是简写别名。标记为 *仅环境变量* 的行没有对应的参数。

| 变量 | 取值 | 作用 |
| --- | --- | --- |
| `UNITY_MCP_CLOUD_URL` | URL | MCP 服务器 URL（`UNITY_MCP_HOST` 是旧版别名） |
| `UNITY_MCP_CONNECTION_MODE` | `Cloud` / `Custom` | 连接模式（回环地址 URL 意味着 `Custom`） |
| `UNITY_MCP_KEEP_CONNECTED` | `true` / `false` | 保持连接处于活动状态 |
| `UNITY_MCP_AUTH_OPTION` | `none` / `oauth` / `token` | 身份验证模式（旧版的 `required` 会变为 `token`） |
| `UNITY_MCP_TOKEN` | 字符串 | 身份验证令牌 |
| `UNITY_MCP_TRANSPORT` | `stdio` / `streamableHttp` | 插件所配置的客户端传输方式 |
| `UNITY_MCP_START_SERVER` | `true` / `false` | 保持本地服务器进程运行 |
| `UNITY_MCP_TOOLS` | 逗号分隔的工具 id | 只显示这些工具；其余工具被隐藏（仍可按名称调用）。未知的 id 会作为错误记录。 |
| `UNITY_MCP_CREDENTIALS_DIR` | 文件夹的绝对路径 | *仅环境变量。* 从此文件夹而不是 `~/.ai-game-dev` 读取本机凭据存储（用于开发和测试用的编辑器）。相对路径会被忽略。 |
| `UNITY_MCP_SERVER_PATH` | 指向 `gamedev-mcp-server` 的绝对路径 | *仅环境变量。* 启动此二进制文件而不是固定的发布版本（见下文） |
| `UNITY_MCP_DISABLE` | `1` / `true` | *仅环境变量。* 跳过插件初始化；参见[在 CI 中禁用](ci-disable.zh-CN.md) |

示例：以批处理模式连接带令牌的本地服务器：

```bash
Unity.exe -batchmode -nographics \
  -UNITY_MCP_CLOUD_URL=http://localhost:8080 \
  -UNITY_MCP_KEEP_CONNECTED=true \
  -UNITY_MCP_AUTH_OPTION=token \
  -UNITY_MCP_TOKEN=my-secret-token
```

### `UNITY_MCP_SERVER_PATH`（服务器开发与 CI）

编辑器会启动指定的文件而不是固定的发布版本，并跳过下载和版本检查。在开发服务器或 CI 从源码构建服务器时使用它。

- 先从进程环境中读取，再从 Unity 项目根目录下的 `.env` 文件中读取，因此从 GUI 或 IDE 启动的编辑器也能读取到它。
- 请使用**绝对**路径。如果文件不存在，插件会静默地表现为该变量未设置；控制台中的 `Starting MCP server: <path> …` 这一行会显示实际启动的二进制文件。
- 该覆盖也会成为生成的智能体配置中的 `command`，*Open Server Logs* 也会跟随它。*Download Binaries* 仍会下载到 `Library/mcp-server/<rid>/`。

## 自行运行服务器

适用于自托管或远程部署。从 [GameDev-MCP-Server releases](https://github.com/IvanMurzak/GameDev-MCP-Server/releases) 下载发布压缩包（`gamedev-mcp-server-<rid>.zip`），或使用 [Docker](DOCKER_DEPLOYMENT.zh-CN.md)。

```bash
# stdio: the agent starts the server itself
./gamedev-mcp-server --port 8080 --client-transport stdio

# HTTP: run it as a standalone process
./gamedev-mcp-server --port 8080 --client-transport streamableHttp
```

stdio 的智能体配置（替换路径；在 Windows 上文件以 `.exe` 结尾）：

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

插件必须使用相同的端口：在 AI Game Developer 窗口中设置 **Custom** 模式以及相同的主机和端口。

### 常用服务器选项

命令行参数优先于环境变量。参数也可以使用一个 `-` 或不加前缀（`port=8080`）。默认值和完整列表：[GameDev-MCP-Server 配置](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration)。

| 环境变量 | 参数 | 含义 |
| --- | --- | --- |
| `MCP_PLUGIN_PORT` | `--port` | 智能体（HTTP）和插件共用的端口（默认 `8080`） |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | 智能体传输方式：`stdio` 或 `streamableHttp` |
| `MCP_PLUGIN_CLIENT_TIMEOUT` | `--plugin-timeout` | 向插件发出的目录、提示词和资源请求的超时时间（毫秒） |
| `MCP_PLUGIN_TOOL_TIMEOUT` | `--tool-timeout` | 单次工具调用可运行的时长（毫秒）。每次调用只发送一次，从不重发。 |
| `MCP_AUTH`（或 `MCP_AUTHORIZATION`） | `--auth`（或 `--authorization`） | `none`、`token` 或 `oauth` |
| `MCP_PLUGIN_TOKEN` | `--token` | `token` 模式下的共享密钥；智能体和插件都必须发送它 |
| `MCP_BIND` | `--bind` | 绑定地址：`loopback`（默认）、`any` 或某个 IP |
| `MCP_PLUGIN_IDLE_TIMEOUT_SECONDS` | `--idle-timeout-seconds` | 在空闲达到指定秒数后关闭 streamableHttp 会话 |

托管部署还可以在工具、提示词、资源和连接事件发生时调用 webhook，并对传入连接进行授权：`MCP_PLUGIN_WEBHOOK_TOOL_URL`、`MCP_PLUGIN_WEBHOOK_PROMPT_URL`、`MCP_PLUGIN_WEBHOOK_RESOURCE_URL`、`MCP_PLUGIN_WEBHOOK_CONNECTION_URL`、`MCP_PLUGIN_WEBHOOK_AUTHORIZATION_URL`、`MCP_PLUGIN_WEBHOOK_AUTHORIZATION_FAIL_OPEN`、`MCP_PLUGIN_WEBHOOK_TOKEN`、`MCP_PLUGIN_WEBHOOK_HEADER` 和 `MCP_PLUGIN_WEBHOOK_TIMEOUT`（均为可选，默认未设置）。
