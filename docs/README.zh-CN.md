<div align="center" width="100%">
  <h1>✨ AI 游戏开发者 — <i>Unity MCP</i></h1>

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg '测试通过')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-加入-7289da?logo=discord&logoColor=white&labelColor=333A41 '加入')](https://discord.gg/cfbdMZX99G)
[![OpenUPM](https://img.shields.io/badge/dynamic/json?labelColor=333A41&label=下载量&query=%24.downloads&suffix=%2F月&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP '星标')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=许可证&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner.jpg" alt="AI 工作" title="关卡构建" width="100%">

  <b>[中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md)</b>

</div>

`Unity MCP` 是一款面向 **编辑器和运行时** 的 AI 驱动游戏开发助手。通过 MCP 协议将 **Claude**、**Cursor** 和 **Windsurf** 连接到 Unity。自动化工作流、生成代码，并**在游戏中启用 AI 能力**。

与其他工具不同，此插件可在**已编译的游戏内部**工作，支持实时 AI 调试和玩家与 AI 的交互。

> **[💬 加入我们的 Discord 服务器](https://discord.gg/cfbdMZX99G)** — 提问、展示作品，与其他开发者交流！

## ![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.zh-CN.svg?raw=true)

- ✔️ **AI 智能体** — 无限制使用来自 **Anthropic**、**OpenAI**、**Microsoft** 或任何其他服务商的最优秀智能体
- ✔️ **工具（TOOLS）** — 丰富的默认 [MCP 工具](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)，用于在 Unity 编辑器中操作
- ✔️ **技能（SKILLS）** — 自动为每个 MCP 工具生成技能描述
- ✔️ **代码与测试** — 让 AI 编写代码并运行测试
- ✔️ **运行时（游戏内）** — 直接在已编译的游戏中使用 LLM，实现动态 NPC 行为或调试
- ✔️ **调试支持** — 让 AI 获取日志并修复错误
- ✔️ **自然对话** — 像与人交流一样与 AI 聊天
- ✔️ **灵活部署** — 支持本地（stdio）和远程（http）两种配置方式
- ✔️ **可扩展** — 在项目代码中[创建自定义 MCP 工具](#添加自定义-mcp-tool)

[![下载安装器](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.51.2/AI-Game-Dev-Installer.unitypackage)

![AI 游戏开发者 Windows](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/editor/ai-game-developer-windows.png?raw=true)

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 快速开始

三步即可运行：

1. **[安装插件](#第一步安装-unity-mcp-插件)** — 下载 `.unitypackage` 安装器，或运行 `openupm add com.ivanmurzak.unity.mcp`
2. **[选择 MCP 客户端](#第二步安装-mcp-客户端)** — Claude Code、Claude Desktop、GitHub Copilot、Cursor 或其他
3. **[配置客户端](#第三步配置-mcp-客户端)** — 在 Unity 中打开 `Window/AI Game Developer — MCP`，点击 **Configure**

就是这样。询问 AI *"以半径 2 圆形排列创建 3 个立方体"*，然后观看它的执行。✨

---

# 工具参考

插件内置 50+ 个工具，分为三个类别。安装后所有工具立即可用，无需额外配置。完整参考及详细说明请见 [default-mcp-tools.md](default-mcp-tools.md)。

<details>
  <summary>项目与资产</summary>

- `assets-copy` — 复制指定路径的资产并保存到新路径
- `assets-create-folder` — 在指定父文件夹中创建新文件夹
- `assets-delete` — 从项目中删除指定路径的资产
- `assets-find` — 使用搜索过滤字符串搜索资产数据库
- `assets-find-built-in` — 搜索 Unity 编辑器的内置资产
- `assets-get-data` — 获取资产文件数据，包括所有可序列化字段和属性
- `assets-material-create` — 创建具有默认参数的新材质资产
- `assets-modify` — 修改项目中的资产文件
- `assets-move` — 移动项目中指定路径的资产（也可用于重命名）
- `assets-prefab-close` — 关闭当前打开的预制体
- `assets-prefab-create` — 从当前活动场景中的 GameObject 创建预制体
- `assets-prefab-instantiate` — 在当前活动场景中实例化预制体
- `assets-prefab-open` — 为特定 GameObject 打开预制体编辑模式
- `assets-prefab-save` — 保存预制体编辑模式中的预制体
- `assets-refresh` — 刷新 AssetDatabase
- `assets-shader-list-all` — 列出项目资产和包中所有可用的着色器
- `package-add` — 从 Unity Package Manager 注册表、Git URL 或本地路径安装包
- `package-list` — 列出 Unity 项目中安装的所有包（UPM 包）
- `package-remove` — 从 Unity 项目中移除（卸载）包
- `package-search` — 在 Unity Package Manager 注册表和已安装包中搜索

</details>

<details>
  <summary>场景与层级</summary>

- `gameobject-component-add` — 为 GameObject 添加组件
- `gameobject-component-destroy` — 从目标 GameObject 中销毁一个或多个组件
- `gameobject-component-get` — 获取 GameObject 上特定组件的详细信息
- `gameobject-component-list-all` — 列出继承自 UnityEngine.Component 的 C# 类名
- `gameobject-component-modify` — 修改 GameObject 上的特定组件
- `gameobject-create` — 在打开的预制体或场景中创建新 GameObject
- `gameobject-destroy` — 递归销毁 GameObject 及所有嵌套的 GameObjects
- `gameobject-duplicate` — 在打开的预制体或场景中复制 GameObjects
- `gameobject-find` — 根据提供的信息查找特定 GameObject
- `gameobject-modify` — 修改 GameObjects 和/或附加组件的字段和属性
- `gameobject-set-parent` — 为 GameObjects 列表设置父 GameObject
- `object-get-data` — 获取指定 Unity 对象的数据
- `object-modify` — 修改指定 Unity 对象
- `scene-create` — 在项目资产中创建新场景
- `scene-get-data` — 获取指定场景中根 GameObjects 的列表
- `scene-list-opened` — 返回 Unity 编辑器中当前打开的场景列表
- `scene-open` — 从项目资产文件中打开场景
- `scene-save` — 将打开的场景保存到资产文件
- `scene-set-active` — 将指定的已打开场景设为活动场景
- `scene-unload` — 从 Unity 编辑器的已打开场景中卸载场景
- `screenshot-camera` — 从摄像机捕获截图并以图像形式返回
- `screenshot-game-view` — 从 Unity 编辑器游戏视图捕获截图
- `screenshot-scene-view` — 从 Unity 编辑器场景视图捕获截图

</details>

<details>
  <summary>脚本与编辑器</summary>

- `console-get-logs` — 通过过滤选项获取 Unity 编辑器日志
- `editor-application-get-state` — 返回 Unity 编辑器应用程序状态（播放模式、暂停、编译）的信息
- `editor-application-set-state` — 控制 Unity 编辑器应用程序状态（启动/停止/暂停播放模式）
- `editor-selection-get` — 获取 Unity 编辑器中当前选择的信息
- `editor-selection-set` — 设置 Unity 编辑器中的当前选择
- `reflection-method-call` — 使用输入参数调用任意 C# 方法并返回结果
- `reflection-method-find` — 使用 C# 反射在项目中查找方法（包括私有方法）
- `script-delete` — 删除脚本文件
- `script-execute` — 使用 Roslyn 动态编译并执行 C# 代码
- `script-read` — 读取脚本文件的内容
- `script-update-or-create` — 根据提供的 C# 代码更新或创建脚本文件
- `tests-run` — 执行 Unity 测试（EditMode/PlayMode），支持过滤和详细结果

</details>

#### 附加工具

如需更多工具，可安装扩展或[创建自定义工具](#添加自定义-mcp-tool)。

- [Animation（动画）](https://github.com/IvanMurzak/Unity-AI-Animation/)
- [ParticleSystem（粒子系统）](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/)
- [ProBuilder](https://github.com/IvanMurzak/Unity-AI-ProBuilder/)

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 目录

- [快速开始](#快速开始)
- [工具参考](#工具参考)
      - [附加工具](#附加工具)
- [目录](#目录)
  - [更多文档](#更多文档)
- [安装](#安装)
  - [第一步：安装 `Unity MCP 插件`](#第一步安装-unity-mcp-插件)
    - [选项 1 — 安装器](#选项-1--安装器)
    - [选项 2 — OpenUPM-CLI](#选项-2--openupm-cli)
  - [第二步：安装 `MCP 客户端`](#第二步安装-mcp-客户端)
  - [第三步：配置 `MCP 客户端`](#第三步配置-mcp-客户端)
    - [自动配置](#自动配置)
    - [手动配置](#手动配置)
      - [命令行配置](#命令行配置)
- [AI 工作流示例：Claude 和 Gemini](#ai-工作流示例claude-和-gemini)
  - [LLM 高级功能](#llm-高级功能)
    - [核心能力](#核心能力)
    - [反射驱动功能](#反射驱动功能)
- [自定义 MCP](#自定义-mcp)
  - [添加自定义 `MCP Tool`](#添加自定义-mcp-tool)
  - [添加自定义 `MCP Prompt`](#添加自定义-mcp-prompt)
- [运行时使用（游戏内）](#运行时使用游戏内)
  - [示例：AI 驱动的国际象棋机器人](#示例ai-驱动的国际象棋机器人)
  - [为什么需要运行时使用？](#为什么需要运行时使用)
- [Unity `MCP Server` 设置](#unity-mcp-server-设置)
  - [变量](#变量)
  - [Docker 📦](#docker-)
    - [`streamableHttp` 传输](#streamablehttp-传输)
    - [`stdio` 传输](#stdio-传输)
    - [自定义 `端口`](#自定义-端口)
  - [二进制可执行文件](#二进制可执行文件)
- [Unity MCP 架构工作原理](#unity-mcp-架构工作原理)
  - [什么是 `MCP`](#什么是-mcp)
  - [什么是 `MCP 客户端`](#什么是-mcp-客户端)
  - [什么是 `MCP 服务器`](#什么是-mcp-服务器)
  - [什么是 `MCP Tool`](#什么是-mcp-tool)
    - [何时使用 `MCP Tool`](#何时使用-mcp-tool)
  - [什么是 `MCP Resource`](#什么是-mcp-resource)
    - [何时使用 `MCP Resource`](#何时使用-mcp-resource)
  - [什么是 `MCP Prompt`](#什么是-mcp-prompt)
    - [何时使用 `MCP Prompt`](#何时使用-mcp-prompt)
- [贡献 💙💛](#贡献-)

## 更多文档

| 文档 | 描述 |
| ---- | ---- |
| [默认 MCP 工具](default-mcp-tools.md) | 所有内置工具的完整参考及描述 |
| [MCP 服务器设置](mcp-server.md) | 服务器配置、环境变量、远程托管 |
| [Docker 部署](DOCKER_DEPLOYMENT.md) | 分步 Docker 部署指南 |
| [开发指南](dev/Development.md) | 架构、代码风格、CI/CD — 面向贡献者 |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | 入门教程、API 参考、常见问题 |

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 安装

## 第一步：安装 `Unity MCP 插件`

<details>
  <summary><b>⚠️ 系统要求（点击展开）</b></summary>

> [!IMPORTANT]
> **项目路径不能包含空格**
>
> - ✅ `C:/MyProjects/Project`
> - ❌ `C:/My Projects/Project`

</details>

### 选项 1 — 安装器

- **[⬇️ 下载安装器](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.51.2/AI-Game-Dev-Installer.unitypackage)**
- **📂 将安装器导入 Unity 项目**
  > - 双击文件 — Unity 将自动打开它
  > - 或者：先打开 Unity 编辑器，然后点击 `Assets/Import Package/Custom Package`，选择文件

### 选项 2 — OpenUPM-CLI

- [⬇️ 安装 OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 在 Unity 项目文件夹中打开命令行

```bash
openupm add com.ivanmurzak.unity.mcp
```

## 第二步：安装 `MCP 客户端`

选择一个你喜欢的 `MCP 客户端` — 无需全部安装。这将是你与 LLM 通信的主要聊天窗口。

- [Claude Code](https://github.com/anthropics/claude-code)（强烈推荐）
- [Claude Desktop](https://claude.ai/download)
- [GitHub Copilot in VS Code](https://code.visualstudio.com/docs/copilot/overview)
- [Antigravity](https://antigravity.google/)
- [Cursor](https://www.cursor.com/)
- [Windsurf](https://windsurf.com)
- 其他任何支持的客户端

> MCP 协议具有很强的通用性，因此你可以使用任何喜欢的 MCP 客户端 — 它们的效果一样流畅。唯一重要的要求是 MCP 客户端必须支持动态 MCP 工具更新。

## 第三步：配置 `MCP 客户端`

### 自动配置

- 打开 Unity 项目
- 打开 `Window/AI Game Developer (Unity-MCP)`
- 点击你的 MCP 客户端对应的 `Configure`

![Unity_AI](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/ai-connector-window.gif)

> 如果你的 MCP 客户端不在列表中，请使用窗口中显示的原始 JSON 将其注入到你的 MCP 客户端。请阅读你具体 MCP 客户端的相关说明了解操作方法。

### 手动配置

如果自动配置由于某些原因无法正常工作，请使用 `AI Game Developer (Unity-MCP)` 窗口中的 JSON 手动配置任意 `MCP 客户端`。

#### 命令行配置

<details>
  <summary><b>创建 <code>命令</code></b></summary>

**1. 根据你的环境选择 `<command>`**

| 平台                | `<command>`                                                                                                 |
| ------------------- | ----------------------------------------------------------------------------------------------------------- |
| Windows x64         | `"<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows x86         | `"<unityProjectPath>/Library/mcp-server/win-x86/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows arm64       | `"<unityProjectPath>/Library/mcp-server/win-arm64/unity-mcp-server.exe" port=<port> client-transport=stdio` |
| MacOS Apple-Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" port=<port> client-transport=stdio`     |
| MacOS Apple-Intel   | `"<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" port=<port> client-transport=stdio`       |
| Linux x64           | `"<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" port=<port> client-transport=stdio`     |
| Linux arm64         | `"<unityProjectPath>/Library/mcp-server/linux-arm64/unity-mcp-server" port=<port> client-transport=stdio`   |

**2. 将 `<unityProjectPath>` 替换为 Unity 项目的完整路径**

**3. 将 `<port>` 替换为 AI Game Developer 配置中的端口号**

**4. 使用命令行添加 MCP 服务器**

</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/gemini-64.png?raw=true" width="16" height="16" alt="Gemini CLI"> Gemini CLI</summary>

  ```bash
  gemini mcp add ai-game-developer <command>
  ```

  > 将 `<command>` 替换为上表中对应的命令
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/claude-64.png?raw=true" width="16" height="16" alt="Claude Code CLI"> Claude Code CLI</summary>

  ```bash
  claude mcp add ai-game-developer <command>
  ```

  > 将 `<command>` 替换为上表中对应的命令
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/github-copilot-64.png?raw=true" width="16" height="16" alt="GitHub Copilot CLI"> GitHub Copilot CLI</summary>

  ```bash
  copilot
  ```

  ```bash
  /mcp add
  ```

  服务器名称：`ai-game-developer`
  服务器类型：`local`
  命令：`<command>`
  > 将 `<command>` 替换为上表中对应的命令
</details>

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# AI 工作流示例：Claude 和 Gemini

在你的 `MCP 客户端` 中与 AI（LLM）通信。让它做你想要的任何事情。你对任务或想法描述得越清晰，它的执行效果就越好。

部分 `MCP 客户端` 允许选择不同的 LLM 模型。请注意此功能，因为某些模型的表现可能比其他模型好得多。

**示例命令：**

```text
解释我的场景层级结构
```

```text
以半径 2 圆形排列创建 3 个立方体
```

```text
创建金属金色材质并将其附加到球形 GameObject 上
```

> 确保在你的 MCP 客户端中已开启 `Agent`（智能体）模式

## LLM 高级功能

Unity MCP 提供高级工具，使 LLM 能够更快、更有效地工作，避免错误并在出现问题时自我纠正。一切都设计为高效地实现你的目标。

### 核心能力

- ✔️ **智能体就绪工具** — 1-2 步即可找到所需的任何内容
- ✔️ **即时编译** — 使用 `Roslyn` 进行 C# 代码编译与执行，加快迭代速度
- ✔️ **完整资产访问** — 对资产和 C# 脚本的读写访问
- ✔️ **智能反馈** — 详细描述的正负面反馈，便于正确理解问题

### 反射驱动功能

- ✔️ **对象引用** — 为现有对象提供引用，用于即时生成 C# 代码
- ✔️ **项目数据访问** — 以可读格式获取整个项目数据的完整访问权限
- ✔️ **精细修改** — 填充并修改项目中任意数据
- ✔️ **方法发现** — 在整个代码库（包括已编译的 DLL 文件）中查找任意方法
- ✔️ **方法执行** — 在整个代码库中调用任意方法
- ✔️ **高级参数** — 为方法调用提供任意属性，甚至是对内存中现有对象的引用
- ✔️ **实时 Unity API** — Unity API 即时可用 — 即使 Unity 更新，你也能获得最新的 API
- ✔️ **自文档化** — 通过 `Description` 属性访问任意 `类`、`方法` 或 `属性` 的人类可读描述

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 自定义 MCP

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 支持项目所有者开发自定义 `MCP Tool`、`MCP Resource` 和 `MCP Prompt`。MCP 服务器从 `Unity MCP Plugin` 获取数据并将其暴露给客户端。MCP 通信链中的任何人都将获得关于新 MCP 功能的信息，LLM 可能在某些时候决定使用这些功能。

## 添加自定义 `MCP Tool`

要添加自定义 `MCP Tool`，你需要：

1. 一个带有 `McpPluginToolType` 属性的类
2. 类中带有 `McpPluginTool` 属性的方法
3. *可选：* 为每个方法参数添加 `Description` 属性，帮助 LLM 理解参数含义
4. *可选：* 使用带 `?` 和默认值的 `string? optional = null` 属性，将其标记为 LLM 的 `可选` 参数

> 注意，`MainThread.Instance.Run(() =>` 这行代码允许你在主线程上运行代码，这是与 Unity's API 交互所必需的。如果不需要这个且在后台线程中运行工具是可接受的，为了效率请避免使用主线程。

```csharp
[McpPluginToolType]
public class Tool_GameObject
{
    [McpPluginTool
    (
        "MyCustomTask",
        Title = "Create a new GameObject"
    )]
    [Description("在此向 LLM 解释这是什么，以及何时应该调用它。")]
    public string CustomTask
    (
        [Description("向 LLM 解释这是什么。")]
        string inputData
    )
    {
        // 在后台线程中做任何事

        return MainThread.Instance.Run(() =>
        {
            // 如需在主线程中执行某些操作

            return $"[Success] Operation completed.";
        });
    }
}
```

## 添加自定义 `MCP Prompt`

`MCP Prompt` 允许你将自定义提示词注入到与 LLM 的对话中。它支持两种发送者角色：用户（User）和助手（Assistant）。这是指导 LLM 执行特定任务的快捷方式。你可以使用自定义数据生成提示词，提供列表或任何其他相关信息。

```csharp
[McpPluginPromptType]
public static class Prompt_ScriptingCode
{
    [McpPluginPrompt(Name = "add-event-system", Role = Role.User)]
    [Description("实现基于 UnityEvent 的 GameObjects 间通信系统。")]
    public string AddEventSystem()
    {
        return "使用 UnityEvents、UnityActions 或自定义事件委托创建事件系统，实现游戏系统和组件之间的解耦通信。";
    }
}
```

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 运行时使用（游戏内）

在你的游戏/应用中使用 **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)**。使用工具、资源或提示词。默认情况下没有内置工具，你需要实现自定义工具。

```csharp
// 构建 MCP 插件
var mcpPlugin = UnityMcpPluginRuntime.Initialize(builder =>
    {
        builder.WithConfig(config =>
        {
            config.Host = "http://localhost:8080";
            config.Token = "your-token";
        });
        // 自动注册当前程序集中的所有工具
        builder.WithToolsFromAssembly(Assembly.GetExecutingAssembly());
    })
    .Build();

await mcpPlugin.Connect(); // 开始与 Unity-MCP-Server 的活动连接（带重试）

await mcpPlugin.Disconnect(); // 停止活动连接并关闭现有连接
```

## 示例：AI 驱动的国际象棋机器人

这是一个经典国际象棋游戏。让我们将机器人逻辑外包给 LLM。机器人应根据游戏规则进行落子。

```csharp
[McpPluginToolType]
public static class ChessGameAI
{
    [McpPluginTool("chess-do-turn", Title = "Do the turn")]
    [Description("在国际象棋游戏中执行一步棋。如果该步被接受则返回 true，否则返回 false。")]
    public static Task<bool> DoTurn(int figureId, Vector2Int position)
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.DoTurn(figureId, position));
    }

    [McpPluginTool("chess-get-board", Title = "Get the board")]
    [Description("获取国际象棋棋盘的当前状态。")]
    public static Task<BoardData> GetBoard()
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.GetBoardData());
    }
}
```

## 为什么需要运行时使用？

使用场景有很多，想象一下你正在开发一款带有机器人的国际象棋游戏。你可以通过编写几行代码，将机器人的决策逻辑外包给 LLM。

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Unity `MCP Server` 设置

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 服务器支持多种不同的启动选项和 Docker 部署。两种传输协议均受支持：`streamableHttp` 和 `stdio`。如果你需要自定义或将 Unity MCP 服务器部署到云端，本节适合你。[了解更多...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

## 变量

无论你选择哪种启动选项，所有选项都支持通过环境变量和命令行参数进行自定义配置。如果只是需要启动它，使用默认值即可，不必浪费时间在变量上。只需确保 Unity Plugin 也有默认值，特别是 `--port`，两者应保持一致。

| 环境变量                      | 命令行参数           | 描述                                                                          |
| ----------------------------- | ------------------- | ----------------------------------------------------------------------------- |
| `MCP_PLUGIN_PORT`             | `--port`            | **客户端** -> **服务器** <- **插件** 连接端口（默认：8080）                    |
| `MCP_PLUGIN_CLIENT_TIMEOUT`   | `--plugin-timeout`  | **插件** -> **服务器** 连接超时（毫秒）（默认：10000）                         |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport`| **客户端** -> **服务器** 传输类型：`stdio` 或 `streamableHttp`（默认：`streamableHttp`）|

> 命令行参数也支持单个 `-` 前缀（`-port`）以及不带前缀的选项（`port`）。

> **选择传输方式：** 当 MCP 客户端直接启动服务器二进制文件时（本地使用 — 这是最常见的配置），使用 `stdio`。当以独立进程或在 Docker/云端运行服务器并通过 HTTP 连接时，使用 `streamableHttp`。

## Docker 📦

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)

确保已安装 Docker。如果你在 Windows 操作系统上，请确保 Docker Desktop 已启动。

[阅读高级 Docker 配置说明](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/DOCKER_DEPLOYMENT.md)。

### `streamableHttp` 传输

```bash
docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCP 客户端</code> 配置：</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

> 如果托管在云端，请将 `url` 替换为你的真实端点。

</details>

### `stdio` 传输

使用此方式，`MCP 客户端` 应在 Docker 中启动 `MCP 服务器`。这可以通过修改 `MCP 客户端` 配置来实现。

```bash
docker run -t -e MCP_PLUGIN_CLIENT_TRANSPORT=stdio -p 8080:8080 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCP 客户端</code> 配置：</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "docker",
      "args": [
        "run",
        "-t",
        "-e",
        "MCP_PLUGIN_CLIENT_TRANSPORT=stdio",
        "-p",
        "8080:8080",
        "ivanmurzakdev/unity-mcp-server"
      ]
    }
  }
}
```

</details>

### 自定义 `端口`

```bash
docker run -e MCP_PLUGIN_PORT=123 -p 123:123 ivanmurzakdev/unity-mcp-server
```

<details>
  <summary><code>MCP 客户端</code> 配置：</summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:123"
    }
  }
}
```

> 如果托管在云端，请将 `url` 替换为你的真实端点
</details>

## 二进制可执行文件

你可以直接从二进制文件启动 Unity `MCP Server`。你需要针对你的 CPU 架构专门编译的二进制文件。请查看 [GitHub 发布页面](https://github.com/IvanMurzak/Unity-MCP/releases)，其中包含为所有 CPU 架构预编译的二进制文件。

```bash
./unity-mcp-server --port 8080 --plugin-timeout 10000 --client-transport stdio
```

<details>
  <summary><code>MCP 客户端</code> 配置：</summary>

> 将 `<project>` 替换为你的 Unity 项目路径。

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/unity-mcp-server.exe",
      "args": [
        "--port=8080",
        "--plugin-timeout=10000",
        "--client-transport=stdio"
      ]
    }
  }
}
```

</details>

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Unity MCP 架构工作原理

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 充当 LLM 与 Unity 之间的桥梁。它向 LLM 暴露并说明 Unity 的工具，LLM 随后理解接口并根据用户请求利用这些工具。

通过集成的 `AI Connector` 窗口，将 **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 连接到 [Claude](https://claude.ai/download) 或 [Cursor](https://www.cursor.com/) 等 LLM 客户端。也支持自定义客户端。

该系统具有高度可扩展性 — 你可以直接在 Unity 项目代码库中定义自定义 `MCP Tools`、`MCP Resource` 或 `MCP Prompt`，向 AI 或自动化客户端暴露新能力。这使得 Unity MCP 成为构建高级工作流、快速原型开发以及将 AI 驱动功能集成到开发流程中的灵活基础。

## 什么是 `MCP`

MCP — 模型上下文协议（Model Context Protocol）。简而言之，这是 AI 的 `USB Type-C`，专门用于 LLM（大型语言模型）。它教会 LLM 如何使用外部功能。比如本例中的 Unity 引擎，甚至是你代码中的自定义 C# 方法。[官方文档](https://modelcontextprotocol.io/)。

## 什么是 `MCP 客户端`

它是一个带有聊天窗口的应用程序。它可能有智能代理以更好地运行，可能有嵌入的高级 MCP 工具。一般而言，一个优秀的 MCP 客户端占 AI 执行任务成功率的 50%。因此，选择最适合的客户端非常重要。

## 什么是 `MCP 服务器`

它是 `MCP 客户端` 与"其他内容"之间的桥梁，在本项目中即 Unity 引擎。本项目包含 `MCP 服务器`。

## 什么是 `MCP Tool`

`MCP Tool` 是 LLM 可以调用以与 Unity 交互的函数或方法。这些工具充当自然语言请求与实际 Unity 操作之间的桥梁。当你让 AI "创建一个立方体"或"改变材质颜色"时，它会使用 MCP 工具来执行这些操作。

**关键特性：**

- **可执行函数**，执行特定操作
- **带类型的参数**和描述，帮助 LLM 理解需要提供什么数据
- **返回值**，提供关于操作成功或失败的反馈
- **线程感知** — 可在主线程上运行以进行 Unity API 调用，或在后台线程上运行以进行大量处理

### 何时使用 `MCP Tool`

- **自动化重复性任务** — 为你经常执行的常见操作创建工具
- **复杂操作** — 将多个 Unity API 调用捆绑成一个易于使用的工具
- **项目特定工作流** — 构建了解你项目特定结构和约定的工具
- **容易出错的任务** — 创建包含验证和错误处理的工具
- **自定义游戏逻辑** — 将你的游戏系统暴露给 AI 以进行动态内容创建

**示例：**

- 创建和配置带有特定组件的 GameObjects
- 批量处理资产（纹理、材质、预制体）
- 设置光照和后处理效果
- 生成关卡几何体或程序化放置对象
- 配置物理设置或碰撞层

## 什么是 `MCP Resource`

`MCP Resource` 提供对 Unity 项目中数据的只读访问。与执行操作的 MCP 工具不同，Resource 允许 LLM 检查和了解你项目的当前状态、资产和配置。把它们看作给 AI 提供项目上下文的"传感器"。

**关键特性：**

- 对项目数据和 Unity 对象的**只读访问**
- 以 LLM 可理解的格式呈现的**结构化信息**
- 反映项目当前状态的**实时数据**
- 帮助 AI 做出明智决策的**上下文感知**

### 何时使用 `MCP Resource`

- **项目分析** — 让 AI 了解你的项目结构、资产和组织
- **调试辅助** — 提供当前状态信息用于故障排除
- **智能建议** — 为 AI 提供上下文以做出更好的建议
- **文档生成** — 基于项目状态自动创建文档
- **资产管理** — 帮助 AI 了解哪些资产可用及其属性

**示例：**

- 暴露场景层级和 GameObject 属性
- 列出可用的材质、纹理及其设置
- 显示脚本依赖关系和组件关系
- 展示当前光照设置和渲染管线配置
- 提供关于音频源、动画和粒子系统的信息

## 什么是 `MCP Prompt`

`MCP Prompt` 允许你将预定义的提示词注入到与 LLM 的对话中。这些是可以为 AI 的行为提供上下文、指令或知识的智能模板。提示词可以是静态文本，也可以根据你项目的当前状态动态生成。

**关键特性：**

- 影响 AI 响应方式的**上下文引导**
- **基于角色** — 可以模拟不同的角色（用户请求或助手知识）
- **动态内容** — 可以包含实时项目数据
- 用于常见场景和工作流的**可重用模板**

### 何时使用 `MCP Prompt`

- **提供领域知识** — 分享你项目特有的最佳实践和编码标准
- **设定编码约定** — 建立命名约定、架构模式和代码风格
- **提供项目结构上下文** — 解释你的项目如何组织以及原因
- **分享工作流指令** — 为常见任务提供分步骤程序
- **注入专业知识** — 添加关于特定 Unity 功能、第三方资产或自定义系统的信息

**示例：**

- "公共方法始终使用 PascalCase，私有字段使用 camelCase"
- "本项目使用位于 Scripts/Events/ 的自定义事件系统"
- "创建 UI 元素时，始终将其添加到 Scene/UI/MainCanvas 中的 Canvas"
- "性能至关重要 — 对于频繁实例化的对象优先使用对象池"
- "本项目遵循 SOLID 原则 — 解释任何架构决策"

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# 贡献 💙💛

非常欢迎贡献。带来你的想法，让我们让游戏开发变得前所未有的简单！你是否有新 `MCP Tool` 或功能的想法，或者发现了一个 bug 并知道如何修复？

**如果你觉得本项目有用，请给它一个星标 🌟！**

1. 👉 [阅读开发文档](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md)
2. 👉 [Fork 项目](https://github.com/IvanMurzak/Unity-MCP/fork)
3. 克隆 fork 并在 Unity 中打开 `./Unity-MCP-Plugin` 文件夹
4. 在项目中实现新功能，提交并推送到 GitHub
5. 创建针对原始 [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP/compare) 仓库 `main` 分支的 Pull Request

![AI 游戏开发者 — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
