<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npm package')](https://www.npmjs.com/package/unity-mcp-cli)
[![Node.js](https://img.shields.io/badge/Node.js-%5E20.19.0%20%7C%7C%20%3E%3D22.12.0-5FA04E?logo=nodedotjs&labelColor=333A41 'Node.js')](https://nodejs.org/)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner-glitch.gif" alt="AI Game Developer" title="Unity MCP CLI" width="100%">

</div>

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

适用于 **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 的跨平台 CLI 工具 — 创建项目、安装插件、配置 MCP 工具，并启动带有活跃 MCP 连接的 Unity。一切操作均可通过单一命令行完成。

## ![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.svg?raw=true)

- :white_check_mark: **创建项目** — 通过 Unity Editor 快速搭建新 Unity 项目
- :white_check_mark: **安装编辑器** — 从命令行安装任意 Unity Editor 版本
- :white_check_mark: **安装插件** — 将 Unity-MCP 插件连同所有必要的作用域注册表添加到 `manifest.json`
- :white_check_mark: **移除插件** — 从 `manifest.json` 中移除 Unity-MCP 插件
- :white_check_mark: **配置** — 启用/禁用 MCP 工具、提示词和资源
- :white_check_mark: **连接** — 携带 MCP 环境变量启动 Unity，实现自动化服务器连接
- :white_check_mark: **跨平台** — 支持 Windows、macOS 和 Linux
- :white_check_mark: **版本感知** — 从不降级插件版本，自动从 OpenUPM 解析最新版本

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 快速开始

使用 `npx` 即时运行任意命令，无需安装：

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

或全局安装：

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin /path/to/unity/project
```

> **环境要求：** [Node.js](https://nodejs.org/) ^20.19.0 或 >=22.12.0。若未检测到 [Unity Hub](https://unity.com/download)，将自动下载安装。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 目录

- [快速开始](#快速开始)
- [命令](#命令)
  - [`configure`](#configure) — 配置 MCP 工具、提示词和资源
  - [`connect`](#connect) — 启动 Unity 并建立 MCP 连接
  - [`create-project`](#create-project) — 创建新 Unity 项目
  - [`install-plugin`](#install-plugin) — 将 Unity-MCP 插件安装到项目中
  - [`install-unity`](#install-unity) — 通过 Unity Hub 安装 Unity Editor
  - [`open`](#open) — 在编辑器中打开 Unity 项目
  - [`remove-plugin`](#remove-plugin) — 从项目中移除 Unity-MCP 插件
- [完整自动化示例](#完整自动化示例)
- [工作原理](#工作原理)

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 命令

## `configure`

在 `UserSettings/AI-Game-Developer-Config.json` 中配置 MCP 工具、提示词和资源。

```bash
npx unity-mcp-cli configure ./MyGame --list
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
npx unity-mcp-cli configure ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**示例 — 启用所有功能：**

```bash
npx unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `connect`

打开 Unity 项目并通过环境变量将其连接到指定的 MCP 服务器。每个选项均对应一个 `UNITY_MCP_*` 环境变量，Unity 插件将在启动时读取这些变量。

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://localhost:8080
```

| 选项 | 环境变量 | 必需 | 描述 |
|---|---|---|---|
| `--url <url>` | `UNITY_MCP_HOST` | 是 | 要连接的 MCP 服务器 URL |
| `--path <path>` | — | 是 | Unity 项目的路径 |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | 否 | 强制保持连接 |
| `--token <token>` | `UNITY_MCP_TOKEN` | 否 | 身份验证令牌 |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | 否 | 认证模式：`none` 或 `required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | 否 | 要启用的工具列表（逗号分隔） |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | 否 | 传输方式：`streamableHttp` 或 `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | 否 | 设置为 `true` 或 `false` 以控制 Unity Editor 中 MCP 服务器的自动启动（仅适用于 `streamableHttp` 传输方式） |
| `--unity <version>` | — | 否 | 要使用的特定 Unity Editor 版本（默认为项目设置中的版本，回退为已安装的最高版本） |

此命令携带相应的 `UNITY_MCP_*` 环境变量启动 Unity Editor，以便插件在启动时自动获取这些配置。运行时，环境变量将覆盖项目 `UserSettings/AI-Game-Developer-Config.json` 配置文件中的对应值。

**示例 — 携带身份验证和指定工具连接：**

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --keep-connected \
  --tools gameobject-create,gameobject-find,script-execute
```

**示例 — 使用 stdio 传输方式连接（服务器由 AI 代理管理）：**

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://localhost:8080 \
  --transport stdio \
  --start-server false
```

**示例 — 使用 streamableHttp 连接并自动启动服务器：**

```bash
npx unity-mcp-cli connect \
  --path ./MyGame \
  --url http://localhost:8080 \
  --transport streamableHttp \
  --start-server true \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

使用 Unity Editor 创建新的 Unity 项目。

```bash
npx unity-mcp-cli create-project /path/to/new/project
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | 项目将被创建的路径（位置参数或 `--path`） |
| `--unity <version>` | 否 | 要使用的 Unity Editor 版本（默认为已安装的最高版本） |

**示例 — 使用指定编辑器版本创建项目：**

```bash
npx unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

将 Unity-MCP 插件安装到 Unity 项目的 `Packages/manifest.json` 中。

```bash
npx unity-mcp-cli install-plugin ./MyGame
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
npx unity-mcp-cli install-plugin ./MyGame --plugin-version 0.52.0
```

> 运行此命令后，请在 Unity Editor 中打开项目以完成包安装。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

通过 Unity Hub CLI 安装指定版本的 Unity Editor。

```bash
npx unity-mcp-cli install-unity 6000.3.1f1
```

| 参数 / 选项 | 必需 | 描述 |
|---|---|---|
| `[version]` | 否 | 要安装的 Unity Editor 版本（例如 `6000.3.1f1`） |
| `--path <path>` | 否 | 从现有项目中读取所需版本 |

若参数和选项均未提供，命令将从 Unity Hub 发布列表中安装最新稳定版本。

**示例 — 安装项目所需的编辑器版本：**

```bash
npx unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

在 Unity Editor 中打开 Unity 项目。

```bash
npx unity-mcp-cli open ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | Unity 项目的路径（位置参数或 `--path`） |
| `--unity <version>` | 否 | 要使用的特定 Unity Editor 版本（默认为项目设置中的版本，回退为已安装的最高版本） |

编辑器进程以分离模式启动 — CLI 会立即返回。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

从 Unity 项目的 `Packages/manifest.json` 中移除 Unity-MCP 插件。

```bash
npx unity-mcp-cli remove-plugin ./MyGame
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

# 完整自动化示例

通过一个脚本从零搭建完整的 Unity MCP 项目：

```bash
# 1. 创建新的 Unity 项目
npx unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. 安装 Unity-MCP 插件
npx unity-mcp-cli install-plugin ./MyAIGame

# 3. 启用所有 MCP 工具
npx unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. 打开项目并建立 MCP 连接
npx unity-mcp-cli connect \
  --path ./MyAIGame \
  --url http://localhost:8080 \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 工作原理

### 确定性端口

CLI 根据 Unity 项目的目录路径生成**确定性端口**（SHA256 哈希值映射到端口范围 50000–59999）。该端口生成机制与 Unity 插件中的实现完全一致，确保服务器与插件无需手动配置即可自动协商使用同一端口。

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

> 完整的 Unity-MCP 项目文档请参阅 [主 README](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md)。

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
