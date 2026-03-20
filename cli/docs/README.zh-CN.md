<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npm package')](https://www.npmjs.com/package/unity-mcp-cli)
[![Node.js](https://img.shields.io/badge/Node.js-%3E%3D18-5FA04E?logo=nodedotjs&labelColor=333A41 'Node.js')](https://nodejs.org/)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner-glitch.gif" alt="AI Game Developer" title="Unity MCP CLI" width="100%">

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
    <a href="https://github.com/anthropics/claude-code"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

</div>

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

适用于 **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 的跨平台 CLI 工具 — 创建项目、安装插件、配置 MCP 工具，并启动带有活跃 MCP 连接的 Unity。一切操作均可通过单一命令行完成。

## ![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.svg?raw=true)

- :white_check_mark: **创建项目** — 通过 Unity Editor 快速搭建新 Unity 项目
- :white_check_mark: **安装编辑器** — 从命令行安装任意 Unity Editor 版本
- :white_check_mark: **安装插件** — 将 Unity-MCP 插件连同所有必要的作用域注册表添加到 `manifest.json`
- :white_check_mark: **移除插件** — 从 `manifest.json` 中移除 Unity-MCP 插件
- :white_check_mark: **配置** — 启用/禁用 MCP 工具、提示词和资源
- :white_check_mark: **运行工具** — 直接从命令行执行 MCP 工具
- :white_check_mark: **设置 MCP** — 为 14 种支持的 AI 代理写入 MCP 配置文件
- :white_check_mark: **设置技能** — 通过 MCP 服务器为 AI 代理生成技能文件
- :white_check_mark: **打开并连接** — 启动 Unity，可选携带 MCP 环境变量实现自动化服务器连接
- :white_check_mark: **跨平台** — 支持 Windows、macOS 和 Linux
- :white_check_mark: **CI 友好** — 自动检测非交互式终端，禁用加载动画和颜色输出
- :white_check_mark: **详细模式** — 对任何命令使用 `--verbose` 获取详细诊断输出
- :white_check_mark: **版本感知** — 从不降级插件版本，自动从 OpenUPM 解析最新版本

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 快速开始

全局安装并运行：

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin /path/to/unity/project
```

或使用 `npx` 即时运行任意命令，无需全局安装：

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

> **环境要求：** [Node.js](https://nodejs.org/) ^20.19.0 || >=22.12.0。若未检测到 [Unity Hub](https://unity.com/download)，将自动下载安装。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 目录

- [快速开始](#快速开始)
- [目录](#目录)
- [命令](#命令)
  - [`configure`](#configure)
  - [`create-project`](#create-project)
  - [`install-plugin`](#install-plugin)
  - [`install-unity`](#install-unity)
  - [`open`](#open)
  - [`run-tool`](#run-tool)
  - [`setup-mcp`](#setup-mcp)
  - [`setup-skills`](#setup-skills)
  - [`remove-plugin`](#remove-plugin)
  - [全局选项](#全局选项)
- [完整自动化示例](#完整自动化示例)
- [工作原理](#工作原理)
    - [确定性端口](#确定性端口)
    - [插件安装](#插件安装)
    - [配置文件](#配置文件)
    - [Unity Hub 集成](#unity-hub-集成)

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 命令

## `configure`

在 `UserSettings/AI-Game-Developer-Config.json` 中配置 MCP 工具、提示词和资源。

```bash
unity-mcp-cli configure ./MyGame --list
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | Unity 项目的路径（位置参数或 `--path`） |
| `--list` | 否 | 列出当前配置并退出 |
| `--enable-tools <names>` | 否 | 启用指定工具（逗号分隔） |
| `--disable-tools <names>` | 否 | 禁用指定工具（逗号分隔） |
| `--enable-all-tools` | 否 | 启用所有工具 |
| `--disable-all-tools` | 否 | 禁用所有工具 |
| `--enable-prompts <names>` | 否 | 启用指定提示词（逗号分隔） |
| `--disable-prompts <names>` | 否 | 禁用指定提示词（逗号分隔） |
| `--enable-all-prompts` | 否 | 启用所有提示词 |
| `--disable-all-prompts` | 否 | 禁用所有提示词 |
| `--enable-resources <names>` | 否 | 启用指定资源（逗号分隔） |
| `--disable-resources <names>` | 否 | 禁用指定资源（逗号分隔） |
| `--enable-all-resources` | 否 | 启用所有资源 |
| `--disable-all-resources` | 否 | 禁用所有资源 |

**示例 — 启用指定工具并禁用所有提示词：**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**示例 — 启用所有功能：**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

使用 Unity Editor 创建新的 Unity 项目。

```bash
unity-mcp-cli create-project /path/to/new/project
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | 项目将被创建的路径（位置参数或 `--path`） |
| `--unity <version>` | 否 | 要使用的 Unity Editor 版本（默认为已安装的最高版本） |

**示例 — 使用指定编辑器版本创建项目：**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

将 Unity-MCP 插件安装到 Unity 项目的 `Packages/manifest.json` 中。

```bash
unity-mcp-cli install-plugin ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | Unity 项目的路径（位置参数或 `--path`） |
| `--plugin-version <version>` | 否 | 要安装的插件版本（默认为来自 [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) 的最新版本） |

此命令将：
1. 添加 **OpenUPM 作用域注册表**及所有必要的作用域
2. 将 `com.ivanmurzak.unity.mcp` 添加到 `dependencies`
3. **从不降级** — 若已安装更高版本，则保留现有版本

**示例 — 安装指定插件版本：**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

> 运行此命令后，请在 Unity Editor 中打开项目以完成包安装。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

通过 Unity Hub CLI 安装指定版本的 Unity Editor。

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| 参数 / 选项 | 必需 | 描述 |
|---|---|---|
| `[version]` | 否 | 要安装的 Unity Editor 版本（例如 `6000.3.1f1`） |
| `--path <path>` | 否 | 从现有项目中读取所需版本 |

若参数和选项均未提供，命令将从 Unity Hub 发布列表中安装最新稳定版本。

**示例 — 安装项目所需的编辑器版本：**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

在 Unity Editor 中打开 Unity 项目。默认情况下，若提供了连接选项，将设置 MCP 连接环境变量。使用 `--no-connect` 可在不建立 MCP 连接的情况下打开项目。

```bash
unity-mcp-cli open ./MyGame
```

| 选项 | 环境变量 | 必需 | 描述 |
|---|---|---|---|
| `[path]` | — | 是 | Unity 项目的路径（位置参数或 `--path`） |
| `--unity <version>` | — | 否 | 要使用的特定 Unity Editor 版本（默认为项目设置中的版本，回退为已安装的最高版本） |
| `--no-connect` | — | 否 | 不携带 MCP 连接环境变量打开项目 |
| `--url <url>` | `UNITY_MCP_HOST` | 否 | 要连接的 MCP 服务器 URL |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | 否 | 强制保持连接 |
| `--token <token>` | `UNITY_MCP_TOKEN` | 否 | 身份验证令牌 |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | 否 | 认证模式：`none` 或 `required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | 否 | 要启用的工具列表（逗号分隔） |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | 否 | 传输方式：`streamableHttp` 或 `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | 否 | 设置为 `true` 或 `false` 以控制 MCP 服务器的自动启动 |

编辑器进程以分离模式启动 — CLI 会立即返回。

**示例 — 携带 MCP 连接打开：**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**示例 — 不携带 MCP 连接打开（简单打开）：**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**示例 — 携带身份验证和指定工具打开：**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --tools gameobject-create,gameobject-find
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `run-tool`

通过 HTTP API 直接执行 MCP 工具。服务器 URL 和授权令牌会根据当前连接模式（自定义或云端），从项目配置文件（`UserSettings/AI-Game-Developer-Config.json`）中**自动解析**。

```bash
unity-mcp-cli run-tool gameobject-create ./MyGame --input '{"name":"Cube"}'
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `<tool-name>` | 是 | 要执行的 MCP 工具名称 |
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`）— 用于读取配置和检测端口 |
| `--url <url>` | 否 | 直接指定服务器 URL（跳过配置） |
| `--token <token>` | 否 | Bearer 令牌覆盖（跳过配置） |
| `--input <json>` | 否 | 工具参数的 JSON 字符串（默认为 `{}`） |
| `--input-file <file>` | 否 | 从文件中读取 JSON 参数 |
| `--raw` | 否 | 输出原始 JSON（无格式化、无加载动画） |
| `--timeout <ms>` | 否 | 请求超时时间（毫秒，默认：60000） |

**URL 解析优先级：**
1. `--url` → 直接使用
2. 配置文件 → `host`（自定义模式）或 `cloudServerUrl`（云端模式）
3. 根据项目路径生成的确定性端口

**授权**会从项目配置中自动读取（自定义模式使用 `token`，云端模式使用 `cloudToken`）。使用 `--token` 可显式覆盖从配置派生的令牌。

**示例 — 调用工具（URL 和授权来自配置）：**

```bash
unity-mcp-cli run-tool gameobject-find ./MyGame --input '{"query":"Player"}'
```

**示例 — 显式指定 URL：**

```bash
unity-mcp-cli run-tool scene-save --url http://localhost:8080
```

**示例 — 管道传输原始 JSON 输出：**

```bash
unity-mcp-cli run-tool assets-list ./MyGame --raw | jq '.results'
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-mcp`

为 AI 代理写入 MCP 配置文件，支持在无 Unity Editor UI 的情况下进行无头/CI 设置。支持全部 14 种代理（Claude Code、Cursor、Gemini、Codex 等）。

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[agent-id]` | 是 | 要配置的代理（使用 `--list` 查看全部） |
| `[path]` | 否 | Unity 项目路径（默认为当前目录） |
| `--transport <transport>` | 否 | 传输方式：`stdio` 或 `http`（默认：`http`） |
| `--url <url>` | 否 | 服务器 URL 覆盖（用于 http 传输） |
| `--token <token>` | 否 | 授权令牌覆盖 |
| `--list` | 否 | 列出所有可用的代理 ID |

**示例 — 列出所有支持的代理：**

```bash
unity-mcp-cli setup-mcp --list
```

**示例 — 使用 stdio 传输方式配置 Cursor：**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-skills`

通过调用 MCP 服务器的系统工具 API 为 AI 代理生成技能文件。需要 Unity Editor 正在运行且已安装 MCP 插件。

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[agent-id]` | 是 | 要生成技能的代理（使用 `--list` 查看全部） |
| `[path]` | 否 | Unity 项目路径（默认为当前目录） |
| `--url <url>` | 否 | 服务器 URL 覆盖 |
| `--token <token>` | 否 | 授权令牌覆盖 |
| `--list` | 否 | 列出所有代理及其技能支持状态 |
| `--timeout <ms>` | 否 | 请求超时时间（毫秒，默认：60000） |

**示例 — 列出支持技能的代理：**

```bash
unity-mcp-cli setup-skills --list
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

从 Unity 项目的 `Packages/manifest.json` 中移除 Unity-MCP 插件。

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | Unity 项目的路径（位置参数或 `--path`） |

此命令将：
1. 从 `dependencies` 中移除 `com.ivanmurzak.unity.mcp`
2. **保留作用域注册表和作用域** — 其他包可能依赖它们
3. 若插件未安装，则**不执行任何操作**

> 运行此命令后，请在 Unity Editor 中打开项目以应用更改。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## 全局选项

以下选项适用于所有命令：

| 选项 | 描述 |
|---|---|
| `-v, --verbose` | 启用详细诊断输出，用于故障排查 |
| `--version` | 显示 CLI 版本 |
| `--help` | 显示命令帮助信息 |

**示例 — 以详细输出运行任意命令：**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 完整自动化示例

通过一个脚本从零搭建完整的 Unity MCP 项目：

```bash
# 1. 创建新的 Unity 项目
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. 安装 Unity-MCP 插件
unity-mcp-cli install-plugin ./MyAIGame

# 3. 启用所有 MCP 工具
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. 配置 Claude Code MCP 集成
unity-mcp-cli setup-mcp claude-code ./MyAIGame

# 5. 打开项目并建立 MCP 连接
unity-mcp-cli open ./MyAIGame \
  --url http://localhost:8080 \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 工作原理

### 确定性端口

CLI 根据 Unity 项目的目录路径生成**确定性端口**（SHA256 哈希值映射到端口范围 20000-29999）。该端口生成机制与 Unity 插件中的实现完全一致，确保服务器与插件无需手动配置即可自动协商使用同一端口。

### 插件安装

`install-plugin` 命令直接修改 `Packages/manifest.json`：
- 添加 [OpenUPM](https://openupm.com/) 作用域注册表（`package.openupm.com`）
- 注册所有必要的作用域（`com.ivanmurzak`、`extensions.unity`、`org.nuget.*`）
- 以版本感知的方式添加 `com.ivanmurzak.unity.mcp` 依赖（从不降级）

### 配置文件

`configure` 命令读写 `UserSettings/AI-Game-Developer-Config.json`，该文件控制：
- **工具** — AI 代理可用的 MCP 工具
- **提示词** — 注入到 LLM 对话中的预定义提示词
- **资源** — 暴露给 AI 代理的只读数据
- **连接设置** — 主机 URL、认证令牌、传输方式、超时配置

### Unity Hub 集成

管理编辑器或创建项目的命令使用 **Unity Hub CLI**（`--headless` 模式）。若未安装 Unity Hub，CLI 将**自动下载并安装**：
- **Windows** — 通过 `UnityHubSetup.exe /S` 静默安装（可能需要管理员权限）
- **macOS** — 下载 DMG，挂载后将 `Unity Hub.app` 复制到 `/Applications`
- **Linux** — 将 `UnityHub.AppImage` 下载到 `~/Applications/`

> 完整的 Unity-MCP 项目文档请参阅[主 README](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md)。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
