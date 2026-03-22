<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npm 包')](https://www.npmjs.com/package/unity-mcp-cli)
[![Node.js](https://img.shields.io/badge/Node.js-%5E20.19.0%20%7C%7C%20%3E%3D22.12.0-5FA04E?logo=nodedotjs&labelColor=333A41 'Node.js')](https://nodejs.org/)
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

<b>[中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

跨平台 CLI 工具，适用于 **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** — 创建项目、安装插件、配置 MCP 工具，以及启动带有活跃 MCP 连接的 Unity。所有操作只需一行命令。

## ![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.zh-CN.svg?raw=true)

- :white_check_mark: **创建项目** — 通过 Unity 编辑器搭建新的 Unity 项目
- :white_check_mark: **安装编辑器** — 从命令行安装任意版本的 Unity 编辑器
- :white_check_mark: **安装插件** — 将 Unity-MCP 插件连同所有必需的作用域注册表添加到 `manifest.json`
- :white_check_mark: **移除插件** — 从 `manifest.json` 中移除 Unity-MCP 插件
- :white_check_mark: **配置** — 启用/禁用 MCP 工具、提示和资源
- :white_check_mark: **运行工具** — 直接从命令行执行 MCP 工具
- :white_check_mark: **设置 MCP** — 为 14 个受支持的 AI 代理中的任何一个编写 MCP 配置文件
- :white_check_mark: **设置技能** — 通过 MCP 服务器为 AI 代理生成技能文件
- :white_check_mark: **打开并连接** — 启动 Unity，可选设置 MCP 环境变量以实现自动服务器连接
- :white_check_mark: **跨平台** — 支持 Windows、macOS 和 Linux
- :white_check_mark: **CI 友好** — 自动检测非交互式终端并禁用加载动画/颜色
- :white_check_mark: **详细模式** — 在任何命令中使用 `--verbose` 获取详细的诊断输出
- :white_check_mark: **版本感知** — 绝不降级插件版本，从 OpenUPM 解析最新版本

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 快速开始

全局安装并运行：

```bash
# 1.1 Install unity-mcp-cli                                #  ┌────────────────────┐
npm install -g unity-mcp-cli                               #  │ Available AI agent │
                                                           #  ├────────────────────┤
# 1.2 (Optional) Install Unity                             #  │ antigravity        │
unity-mcp-cli install-unity                                #  │ claude-code        │
                                                           #  │ claude-desktop     │
# 1.3 (Optional) Create Unity project                      #  │ cline              │
unity-mcp-cli create-project ./MyUnityProject              #  │ codex              │
                                                           #  │ cursor             │
# 2. Install "AI Game Developer" in Unity project          #  │ gemini             │
unity-mcp-cli install-plugin ./MyUnityProject              #  │ github-copilot-cli │
                                                           #  │ kilo-code          │
# 3. Setup Skills for Claude Code                          #  │ open-code          │
unity-mcp-cli setup-skills claude-code ./MyUnityProject    #  │ rider-junie        │
                                                           #  │ unity-ai           │
# (Optional) Setup MCP for Claude Code                     #  │ vs-copilot         │
unity-mcp-cli setup-mcp claude-code ./MyUnityProject       #  │ vscode-copilot     │
                                                           #  └────────────────────┘
# (It depends...) Open Unity project
unity-mcp-cli open ./MyUnityProject
```

或使用 `npx` 即时运行任何命令 — 无需全局安装：

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

> **系统要求：** [Node.js](https://nodejs.org/) ^20.19.0 || >=22.12.0。如果未找到 [Unity Hub](https://unity.com/download)，将自动安装。

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 目录

- [快速开始](#quick-start)
- [命令](#commands)
  - [`configure`](#configure)
  - [`create-project`](#create-project)
  - [`install-plugin`](#install-plugin)
  - [`install-unity`](#install-unity)
  - [`open`](#open)
  - [`run-tool`](#run-tool)
  - [`setup-mcp`](#setup-mcp)
  - [`setup-skills`](#setup-skills)
  - [`remove-plugin`](#remove-plugin)
  - [全局选项](#global-options)
- [完整自动化示例](#full-automation-example)
- [工作原理](#how-it-works)
    - [确定性端口](#deterministic-port)
    - [插件安装](#plugin-installation)
    - [配置文件](#configuration-file)
    - [Unity Hub 集成](#unity-hub-integration)

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 命令

## `configure`

在 `UserSettings/AI-Game-Developer-Config.json` 中配置 MCP 工具、提示和资源。

```bash
unity-mcp-cli configure ./MyGame --list
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | Unity 项目路径（位置参数或 `--path`） |
| `--list` | 否 | 列出当前配置并退出 |
| `--enable-tools <names>` | 否 | 启用指定工具（逗号分隔） |
| `--disable-tools <names>` | 否 | 禁用指定工具（逗号分隔） |
| `--enable-all-tools` | 否 | 启用所有工具 |
| `--disable-all-tools` | 否 | 禁用所有工具 |
| `--enable-prompts <names>` | 否 | 启用指定提示（逗号分隔） |
| `--disable-prompts <names>` | 否 | 禁用指定提示（逗号分隔） |
| `--enable-all-prompts` | 否 | 启用所有提示 |
| `--disable-all-prompts` | 否 | 禁用所有提示 |
| `--enable-resources <names>` | 否 | 启用指定资源（逗号分隔） |
| `--disable-resources <names>` | 否 | 禁用指定资源（逗号分隔） |
| `--enable-all-resources` | 否 | 启用所有资源 |
| `--disable-all-resources` | 否 | 禁用所有资源 |

**示例 — 启用指定工具并禁用所有提示：**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**示例 — 启用全部：**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

使用 Unity 编辑器创建新的 Unity 项目。

```bash
unity-mcp-cli create-project /path/to/new/project
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | 项目创建路径（位置参数或 `--path`） |
| `--unity <version>` | 否 | 使用的 Unity 编辑器版本（默认为已安装的最高版本） |

**示例 — 使用指定编辑器版本创建项目：**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

将 Unity-MCP 插件安装到 Unity 项目的 `Packages/manifest.json` 中。

```bash
unity-mcp-cli install-plugin ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | Unity 项目路径（位置参数或 `--path`） |
| `--plugin-version <version>` | 否 | 要安装的插件版本（默认为 [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) 上的最新版本） |

该命令将：
1. 添加 **OpenUPM 作用域注册表**及所有必需的作用域
2. 将 `com.ivanmurzak.unity.mcp` 添加到 `dependencies`
3. **绝不降级** — 如果已安装更高版本，将保留现有版本

**示例 — 安装指定的插件版本：**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

> 运行此命令后，请在 Unity 编辑器中打开项目以完成包安装。

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

通过 Unity Hub CLI 安装 Unity 编辑器版本。

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| 参数 / 选项 | 必需 | 描述 |
|---|---|---|
| `[version]` | 否 | 要安装的 Unity 编辑器版本（例如 `6000.3.1f1`） |
| `--path <path>` | 否 | 从现有项目中读取所需版本 |

如果既未提供参数也未提供选项，该命令将从 Unity Hub 的发行列表中安装最新的稳定版本。

**示例 — 安装项目所需的编辑器版本：**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

在 Unity 编辑器中打开 Unity 项目。默认情况下，如果提供了连接选项，将设置 MCP 连接环境变量。使用 `--no-connect` 可在不建立 MCP 连接的情况下打开。

```bash
unity-mcp-cli open ./MyGame
```

| 选项 | 环境变量 | 必需 | 描述 |
|---|---|---|---|
| `[path]` | — | 是 | Unity 项目路径（位置参数或 `--path`） |
| `--unity <version>` | — | 否 | 使用指定的 Unity 编辑器版本（默认为项目设置中的版本，回退到已安装的最高版本） |
| `--no-connect` | — | 否 | 不设置 MCP 连接环境变量直接打开 |
| `--url <url>` | `UNITY_MCP_HOST` | 否 | 要连接的 MCP 服务器 URL |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | 否 | 强制保持连接活跃 |
| `--token <token>` | `UNITY_MCP_TOKEN` | 否 | 认证令牌 |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | 否 | 认证模式：`none` 或 `required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | 否 | 要启用的工具列表（逗号分隔） |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | 否 | 传输方式：`streamableHttp` 或 `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | 否 | 设置为 `true` 或 `false` 以控制 MCP 服务器自动启动 |

编辑器进程以分离模式启动 — CLI 将立即返回。

**示例 — 使用 MCP 连接打开：**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**示例 — 不使用 MCP 连接打开（简单打开）：**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**示例 — 使用认证和指定工具打开：**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --tools gameobject-create,gameobject-find
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `run-tool`

通过 HTTP API 直接执行 MCP 工具。服务器 URL 和授权令牌会根据当前连接模式（自定义或云端）从项目的配置文件（`UserSettings/AI-Game-Developer-Config.json`）中**自动解析**。

```bash
unity-mcp-cli run-tool gameobject-create ./MyGame --input '{"name":"Cube"}'
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `<tool-name>` | 是 | 要执行的 MCP 工具名称 |
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`）— 用于读取配置和检测端口 |
| `--url <url>` | 否 | 直接覆盖服务器 URL（绕过配置） |
| `--token <token>` | 否 | 覆盖 Bearer 令牌（绕过配置） |
| `--input <json>` | 否 | 工具参数的 JSON 字符串（默认为 `{}`） |
| `--input-file <file>` | 否 | 从文件读取 JSON 参数 |
| `--raw` | 否 | 输出原始 JSON（无格式化、无加载动画） |
| `--timeout <ms>` | 否 | 请求超时时间（毫秒）（默认：60000） |

**URL 解析优先级：**
1. `--url` → 直接使用
2. 配置文件 → `host`（自定义模式）或硬编码的云端端点（云端模式）
3. 根据项目路径生成的确定性端口

**授权信息**会自动从项目配置中读取（自定义模式下为 `token`，云端模式下为 `cloudToken`）。使用 `--token` 可显式覆盖从配置中获取的令牌。

**示例 — 调用工具（URL 和认证来自配置）：**

```bash
unity-mcp-cli run-tool gameobject-find ./MyGame --input '{"query":"Player"}'
```

**示例 — 显式覆盖 URL：**

```bash
unity-mcp-cli run-tool scene-save --url http://localhost:8080
```

**示例 — 管道输出原始 JSON：**

```bash
unity-mcp-cli run-tool assets-list ./MyGame --raw | jq '.results'
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-mcp`

为 AI 代理编写 MCP 配置文件，支持无界面/CI 环境设置，无需 Unity 编辑器 UI。支持全部 14 个代理（Claude Code、Cursor、Gemini、Codex 等）。

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[agent-id]` | 是 | 要配置的代理（使用 `--list` 查看全部） |
| `[path]` | 否 | Unity 项目路径（默认为当前目录） |
| `--transport <transport>` | 否 | 传输方式：`stdio` 或 `http`（默认：`http`） |
| `--url <url>` | 否 | 覆盖服务器 URL（用于 http 传输） |
| `--token <token>` | 否 | 覆盖认证令牌 |
| `--list` | 否 | 列出所有可用的代理 ID |

**示例 — 列出所有支持的代理：**

```bash
unity-mcp-cli setup-mcp --list
```

**示例 — 使用 stdio 传输配置 Cursor：**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-skills`

通过调用 MCP 服务器的系统工具 API 为 AI 代理生成技能文件。需要 Unity 编辑器正在运行且已安装 MCP 插件。

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[agent-id]` | 是 | 要生成技能的代理（使用 `--list` 查看全部） |
| `[path]` | 否 | Unity 项目路径（默认为当前目录） |
| `--url <url>` | 否 | 覆盖服务器 URL |
| `--token <token>` | 否 | 覆盖认证令牌 |
| `--list` | 否 | 列出所有代理及其技能支持状态 |
| `--timeout <ms>` | 否 | 请求超时时间（毫秒）（默认：60000） |

**示例 — 列出支持技能的代理：**

```bash
unity-mcp-cli setup-skills --list
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

从 Unity 项目的 `Packages/manifest.json` 中移除 Unity-MCP 插件。

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | Unity 项目路径（位置参数或 `--path`） |

该命令将：
1. 从 `dependencies` 中移除 `com.ivanmurzak.unity.mcp`
2. **保留作用域注册表和作用域** — 其他包可能依赖它们
3. 如果插件未安装则**不执行任何操作**

> 运行此命令后，请在 Unity 编辑器中打开项目以应用更改。

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## 全局选项

以下选项适用于所有命令：

| 选项 | 描述 |
|---|---|
| `-v, --verbose` | 启用详细的诊断输出以进行故障排查 |
| `--version` | 显示 CLI 版本 |
| `--help` | 显示命令帮助信息 |

**示例 — 以详细模式运行任何命令：**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 完整自动化示例

使用一个脚本从零开始搭建完整的 Unity MCP 项目：

```bash
# 1. Create a new Unity project
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Install the Unity-MCP plugin
unity-mcp-cli install-plugin ./MyAIGame

# 3. Enable all MCP tools
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Setup Skills for Claude Code (requires Unity Editor running)
unity-mcp-cli setup-skills claude-code ./MyAIGame

# 5. Configure Claude Code MCP integration
unity-mcp-cli setup-mcp claude-code ./MyAIGame

# 6. Open the project with MCP connection
unity-mcp-cli open ./MyAIGame \
  --url http://localhost:8080 \
  --keep-connected
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 工作原理

### 确定性端口

CLI 根据 Unity 项目的目录路径生成**确定性端口**（SHA256 哈希映射到端口范围 20000-29999）。这与 Unity 插件中的端口生成方式一致，确保服务器和插件自动使用相同的端口，无需手动配置。

### 插件安装

`install-plugin` 命令直接修改 `Packages/manifest.json`：
- 添加 [OpenUPM](https://openupm.com/) 作用域注册表（`package.openupm.com`）
- 注册所有必需的作用域（`com.ivanmurzak`、`extensions.unity`、`org.nuget.*`）
- 添加 `com.ivanmurzak.unity.mcp` 依赖项，支持版本感知更新（绝不降级）

### 配置文件

`configure` 命令读写 `UserSettings/AI-Game-Developer-Config.json`，该文件控制：
- **工具** — 可供 AI 代理使用的 MCP 工具
- **提示** — 注入 LLM 对话中的预定义提示
- **资源** — 向 AI 代理公开的只读数据
- **连接设置** — 主机 URL、认证令牌、传输方式、超时时间

### Unity Hub 集成

管理编辑器或创建项目的命令使用 **Unity Hub CLI**（`--headless` 模式）。如果未安装 Unity Hub，CLI 将**自动下载并安装**：
- **Windows** — 通过 `UnityHubSetup.exe /S` 静默安装（可能需要管理员权限）
- **macOS** — 下载 DMG，挂载后将 `Unity Hub.app` 复制到 `/Applications`
- **Linux** — 将 `UnityHub.AppImage` 下载到 `~/Applications/`

> 如需完整的 Unity-MCP 项目文档，请参阅[主 README](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md)。

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
