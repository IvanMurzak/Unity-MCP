<div align="center" width="100%">
  <h1>✨ AI 游戏开发助手 — <i>Unity MCP</i></h1>

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![OpenUPM](https://img.shields.io/badge/dynamic/json?labelColor=333A41&label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner.jpg" alt="AI work" title="Level building" width="100%">

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md)</b>

</div>

`Unity MCP` 是一个由AI驱动的游戏开发助手，**适用于编辑器和运行时**。通过MCP将 **Claude**、**Cursor** 和 **Windsurf** 连接到Unity。自动化工作流程、生成代码，并**在您的游戏中启用AI**。

与其他工具不同，该插件可在**编译后的游戏内部**运行，支持实时AI调试和玩家-AI交互。

> **[💬 加入我们的Discord服务器](https://discord.gg/cfbdMZX99G)** - 提问、展示你的作品，与其他开发者交流！

## 功能特性

- ✔️ **AI代理** - 使用来自 **Anthropic**、**OpenAI**、**Microsoft** 或任何其他提供商的最佳代理，无限制
- ✔️ **工具** - 广泛的默认[MCP工具](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)，用于在Unity编辑器中操作
- ✔️ **技能** - 自动为每个MCP工具生成技能，让您无需在MCP工具注册上浪费令牌即可使用工具
- ✔️ **代码和测试** - 请AI编写代码和运行测试
- ✔️ **运行时（游戏内）** - 在编译后的游戏中直接使用LLM实现动态NPC行为或调试
- ✔️ **调试支持** - 请AI获取日志并修复错误
- ✔️ **自然对话** - 像与人类交谈一样与AI聊天
- ✔️ **灵活部署** - 通过配置支持本地（stdio）和远程（http）工作
- ✔️ **可扩展** - 在您的项目代码中创建[自定义MCP工具](#添加自定义mcp工具)

[![下载安装程序](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download_zh-CN.svg?raw=true)](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.50.1/AI-Game-Dev-Installer.unitypackage)

![AI 游戏开发者 窗户](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/editor/ai-game-developer-windows.png?raw=true)

### 工具

<details>
  <summary>资源 (Assets)</summary>

- `copy` - 复制指定路径的资源并存储到新路径
- `create-folder` - 在指定的父文件夹中创建新文件夹
- `delete` - 从项目中删除指定路径的资源
- `find` - 使用搜索过滤字符串搜索资源数据库
- `get-data` - 从资源文件中获取资源数据，包括所有可序列化的字段和属性
- `modify` - 修改项目中的资源文件
- `move` - 移动项目中的资源路径（也用于重命名）
- `refresh` - 刷新资源数据库 (AssetDatabase)

</details>

<details>
  <summary>资源 / 材质 (Assets / Material)</summary>

- `create` - 使用默认参数创建新的材质资源

</details>

<details>
  <summary>资源 / 着色器 (Assets / Shader)</summary>

- `list-all` - 列出项目资源和包中所有可用的着色器

</details>

<details>
  <summary>资源 / 预制体 (Assets / Prefab)</summary>

- `create` - 从当前活动场景中的游戏对象创建预制体
- `instantiate` - 在当前活动场景中实例化预制体
- `open` - 为特定游戏对象打开预制体编辑模式
- `close` - 关闭当前打开的预制体
- `save` - 在预制体编辑模式下保存预制体

</details>

<details>
  <summary>游戏对象 (GameObject)</summary>

- `create` - 在打开的预制体或场景中创建新的游戏对象
- `destroy` - 递归销毁游戏对象及其所有嵌套的游戏对象
- `duplicate` - 在打开的预制体或场景中复制游戏对象
- `find` - 通过提供的信息查找特定的游戏对象
- `modify` - 修改游戏对象和/或附加组件的字段和属性
- `set-parent` - 为游戏对象列表设置父级游戏对象

</details>

<details>
  <summary>游戏对象 / 组件 (GameObject / Component)</summary>

- `add` - 向游戏对象添加组件
- `destroy` - 从目标游戏对象中销毁一个或多个组件
- `get` - 获取有关游戏对象上特定组件的详细信息
- `modify` - 修改游戏对象上的特定组件

</details>

<details>
  <summary>组件 (Component)</summary>

- `list` - 列出继承自 UnityEngine.Component 的 C# 类名

</details>

<details>
  <summary>场景 (Scene)</summary>

- `create` - 在项目资源中创建新场景
- `get-data` - 检索指定场景中的根游戏对象列表
- `list-opened` - 返回 Unity 编辑器中当前打开的场景列表
- `open` - 从项目资源文件打开场景
- `save` - 将打开的场景保存到资源文件
- `set-active` - 将指定的已打开场景设置为活动场景
- `unload` - 从 Unity 编辑器中卸载已打开的场景

</details>

<details>
  <summary>脚本 (Script)</summary>

- `delete` - 删除脚本文件
- `execute` - 使用 Roslyn 动态编译并执行 C# 代码
- `read` - 读取脚本文件的内容
- `update-or-create` - 使用提供的 C# 代码更新或创建脚本文件

</details>

<details>
  <summary>控制台 (Console)</summary>

- `get-logs` - 检索带有过滤选项的 Unity 编辑器日志

</details>

<details>
  <summary>编辑器 / 应用程序 (Editor / Application)</summary>

- `get-state` - 返回有关 Unity 编辑器应用程序状态的信息（播放模式、暂停、编译）
- `set-state` - 控制 Unity 编辑器应用程序状态（开始/停止/暂停播放模式）

</details>

<details>
  <summary>编辑器 / 选择 (Editor / Selection)</summary>

- `get` - 获取有关 Unity 编辑器中当前选择的信息
- `set` - 设置 Unity 编辑器中的当前选择

</details>

<details>
  <summary>反射 (Reflection)</summary>

- `method-find` - 使用 C# 反射在项目中查找方法（甚至是私有方法）
- `method-call` - 调用任何带有输入参数的 C# 方法并返回结果

</details>

<details>
  <summary>测试运行器 (Tests)</summary>

- `run` - 执行带有过滤和详细结果的 Unity 测试（编辑模式/播放模式）

</details>

<details>
  <summary>包管理器 (Package Manager)</summary>

- `add` - 从 Unity 包管理器注册表、Git URL 或本地路径安装包
- `remove` - 从 Unity 项目中移除（卸载）包
- `list` - 列出 Unity 项目中安装的所有包（UPM 包）
- `search` - 在 Unity 包管理器注册表和已安装的包中搜索包

</details>

#### 附加工具

- [动画 (Animation)](https://github.com/IvanMurzak/Unity-AI-Animation/)
- [ParticleSystem](https://github.com/IvanMurzak/Unity-AI-ParticleSystem/)
- [ProBuilder](https://github.com/IvanMurzak/Unity-AI-ProBuilder/)

### 稳定性状态

| Unity版本   | 编辑模式                                                                                                                                                                               | 播放模式                                                                                                                                                                               | 独立模式                                                                                                                                                                                 |
| ----------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3 | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 2023.2.22f1 | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) |
| 6000.3.1f1  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)  |

## 目录

- [安装](#安装)
  - [步骤1：安装 `Unity MCP 插件`](#步骤1安装-unity-mcp-插件)
    - [选项1 - 安装程序](#选项1---安装程序)
    - [选项2 - OpenUPM-CLI](#选项2---openupm-cli)
  - [步骤2：安装 `MCP 客户端`](#步骤2安装-mcp-客户端)
  - [步骤3：配置 `MCP 客户端`](#步骤3配置-mcp-客户端)
    - [自动配置](#自动配置)
    - [手动配置](#手动配置)
    - [命令行配置](#命令行配置)
- [AI工作流示例：Claude 和 Gemini](#ai工作流示例claude-和-gemini)
  - [LLM高级功能](#llm高级功能)
    - [核心功能](#核心功能)
    - [反射功能](#反射功能)
- [自定义MCP](#自定义mcp)
  - [添加自定义 `MCP 工具`](#添加自定义-mcp-工具)
  - [添加自定义 `MCP 提示`](#添加自定义-mcp-提示)
- [运行时使用（游戏内）](#运行时使用游戏内)
  - [示例：AI驱动的国际象棋游戏机器人](#示例ai驱动的国际象棋游戏机器人)
  - [为什么需要运行时使用](#为什么需要运行时使用)
- [Unity `MCP 服务器` 设置](#unity-mcp-服务器-设置)
  - [变量](#变量)
  - [Docker 📦](#docker-)
    - [`streamableHttp` 传输](#streamablehttp-传输)
    - [`stdio` 传输](#stdio-传输)
    - [自定义 `端口`](#自定义-端口)
  - [二进制可执行文件](#二进制可执行文件)
- [工作原理](#工作原理)
  - [什么是 `MCP`](#什么是-mcp)
  - [什么是 `MCP 客户端`](#什么是-mcp-客户端)
  - [什么是 `MCP 服务器`](#什么是-mcp-服务器)
  - [什么是 `MCP 工具`](#什么是-mcp-工具)
    - [何时使用 `MCP 工具`](#何时使用-mcp-工具)
  - [什么是 `MCP 资源`](#什么是-mcp-资源)
    - [何时使用 `MCP 资源`](#何时使用-mcp-资源)
  - [什么是 `MCP 提示`](#什么是-mcp-提示)
    - [何时使用 `MCP 提示`](#何时使用-mcp-提示)
- [贡献 💙💛](#贡献-)

# 安装

## 步骤1：安装 `Unity MCP 插件`

<details>
  <summary><b>⚠️ 要求（点击）</b></summary>

> [!重要]
> **项目路径不能包含空格**
>
> - ✅ `C:/MyProjects/Project`
> - ❌ `C:/My Projects/Project`

</details>

### 选项1 - 安装程序

- **[⬇️ 下载安装程序](https://github.com/IvanMurzak/Unity-MCP/releases/download/0.50.1/AI-Game-Dev-Installer.unitypackage)**
- **📂 将安装程序导入Unity项目**
  > - 您可以双击文件 - Unity会自动打开它
  > - 或者：先打开Unity编辑器，然后点击 `Assets/Import Package/Custom Package`，选择文件

### 选项2 - OpenUPM-CLI

- [⬇️ 安装OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 在Unity项目文件夹中打开命令行

```bash
openupm add com.ivanmurzak.unity.mcp
```

## 步骤2：安装 `MCP 客户端`

选择一个您喜欢的 `MCP 客户端` - 您不需要安装所有客户端。这将是您与LLM通信的主聊天窗口。

- [Claude Code](https://github.com/anthropics/claude-code)（强烈推荐）
- [Claude Desktop](https://claude.ai/download)
- [GitHub Copilot in VS Code](https://code.visualstudio.com/docs/copilot/overview)
- [Antigravity](https://antigravity.google/)
- [Cursor](https://www.cursor.com/)
- [Windsurf](https://windsurf.com)
- 任何其他支持的客户端

> MCP协议非常通用，这就是为什么您可以使用任何您喜欢的MCP客户端 - 它会像其他客户端一样流畅工作。唯一重要的要求是MCP客户端必须支持动态MCP工具更新。

## 步骤3：配置 `MCP 客户端`

### 自动配置

- 打开Unity项目
- 打开 `Window/AI Game Developer (Unity-MCP)`
- 在您的MCP客户端处点击 `Configure`

![Unity_AI](https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/ai-connector-window.gif)

> 如果您的MCP客户端不在列表中，请使用窗口中显示的原始JSON将其注入到您的MCP客户端中。阅读您特定MCP客户端的说明了解如何执行此操作。

### 手动配置

如果自动配置因任何原因对您不起作用，请使用 `AI Game Developer (Unity-MCP)` 窗口中的JSON手动配置任何 `MCP 客户端`。

### 命令行配置

<details>
  <summary><b>创建 <code>command</code></b></summary>

**1. 为您的环境选择`<command>`**

| 平台                | `<command>`                                                                                                 |
| ------------------- | ----------------------------------------------------------------------------------------------------------- |
| Windows x64         | `"<unityProjectPath>/Library/mcp-server/win-x64/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows x86         | `"<unityProjectPath>/Library/mcp-server/win-x86/unity-mcp-server.exe" port=<port> client-transport=stdio`   |
| Windows arm64       | `"<unityProjectPath>/Library/mcp-server/win-arm64/unity-mcp-server.exe" port=<port> client-transport=stdio` |
| MacOS Apple-Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/unity-mcp-server" port=<port> client-transport=stdio`     |
| MacOS Apple-Intel   | `"<unityProjectPath>/Library/mcp-server/osx-x64/unity-mcp-server" port=<port> client-transport=stdio`       |
| Linux x64           | `"<unityProjectPath>/Library/mcp-server/linux-x64/unity-mcp-server" port=<port> client-transport=stdio`     |
| Linux arm64         | `"<unityProjectPath>/Library/mcp-server/linux-arm64/unity-mcp-server" port=<port> client-transport=stdio`   |

**2. 将`<unityProjectPath>`替换为Unity项目的完整路径**
**3. 将`<port>`替换为AI Game Developer配置中的端口**
**4. 使用命令行添加MCP服务器**

</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/gemini-64.png?raw=true" width="16" height="16" alt="Gemini"> Gemini</summary>

  ```bash
  gemini mcp add ai-game-developer <command>
  ```

  > 从上表中替换`<command>`
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/claude-64.png?raw=true" width="16" height="16" alt="Gemini"> Claude Code</summary>

  ```bash
  claude mcp add ai-game-developer <command>
  ```

  > 从上表中替换`<command>`
</details>

<details>
  <summary><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/mcp-clients/github-copilot-64.png?raw=true" width="16" height="16" alt="Gemini"> GitHub Copilot CLI</summary>

  ```bash
  copilot
  ```

  ```bash
  /mcp add
  ```

  服务器名称: `ai-game-developer`
  服务器类型: `local`
  命令: `<command>`
  > 从上表中替换`<command>`
</details>

---

# AI工作流示例：Claude 和 Gemini

在您的 `MCP 客户端` 中与AI（LLM）交流。要求它做任何您想要的事情。您对任务或想法描述得越好，它的表现就越好。

一些 `MCP 客户端` 允许您选择不同的LLM模型。注意这个功能，因为一些模型可能比其他模型工作得更好。

**示例命令：**

```text
解释我的场景层次结构
```

```text
创建3个立方体排成半径为2的圆形
```

```text
创建金属金色材质并将其附加到球体游戏对象
```

> 确保在您的MCP客户端中打开 `Agent` 模式

## LLM高级功能

Unity MCP提供高级工具，使LLM能够更快、更有效地工作，避免错误并在出现错误时自我纠正。一切都旨在高效地实现您的目标。

### 核心功能

- ✔️ **代理就绪工具** - 在1-2步内找到您需要的任何东西
- ✔️ **即时编译** - 使用 `Roslyn` 进行C#代码编译和执行，实现更快迭代
- ✔️ **完全资产访问** - 对资产和C#脚本的读/写访问
- ✔️ **智能反馈** - 为正确理解问题提供详细的正面和负面反馈

### 反射功能

- ✔️ **对象引用** - 提供对现有对象的引用以生成即时C#代码
- ✔️ **项目数据访问** - 以可读格式完全访问整个项目数据
- ✔️ **细粒度修改** - 填充和修改项目中的任何数据片段
- ✔️ **方法发现** - 在整个代码库中找到任何方法，包括编译的DLL文件
- ✔️ **方法执行** - 调用整个代码库中的任何方法
- ✔️ **高级参数** - 为方法调用提供任何属性，甚至是内存中现有对象的引用
- ✔️ **实时Unity API** - Unity API即时可用 - 即使Unity发生变化，您也能获得最新的API
- ✔️ **自文档化** - 通过 `Description` 属性访问任何 `class`、`method`、`field` 或 `property` 的人类可读描述

---

# 自定义MCP

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 支持项目所有者开发自定义 `MCP 工具`、`MCP 资源` 和 `MCP 提示`。MCP服务器从 `Unity MCP 插件` 获取数据并将其暴露给客户端。MCP通信链中的任何人都将收到关于新MCP功能的信息，LLM可能会决定在某个时候使用这些功能。

## 添加自定义 `MCP 工具`

要添加自定义 `MCP 工具`，您需要：

1. 带有 `McpPluginToolType` 属性的类
2. 类中带有 `McpPluginTool` 属性的方法
3. *可选：* 为每个方法参数添加 `Description` 属性以帮助LLM理解
4. *可选：* 使用 `string? optional = null` 属性与 `?` 和默认值将它们标记为LLM的 `optional`

> 注意 `MainThread.Instance.Run(() =>` 这一行允许您在主线程上运行代码，这是与Unity API交互所必需的。如果您不需要这个且在后台线程运行工具是可接受的，请避免使用主线程以提高效率。

```csharp
[McpPluginToolType]
public class Tool_GameObject
{
    [McpPluginTool
    (
        "MyCustomTask",
        Title = "Create a new GameObject"
    )]
    [Description("在此向LLM解释这是什么，何时应该调用它。")]
    public string CustomTask
    (
        [Description("向LLM解释这是什么。")]
        string inputData
    )
    {
        // 在后台线程中做任何事情

        return MainThread.Instance.Run(() =>
        {
            // 如果需要，在主线程中做一些事情

            return $"[成功] 操作完成。";
        });
    }
}
```

## 添加自定义 `MCP 提示`

`MCP 提示` 允许您将预定义的提示注入到与LLM的对话中。这些是智能模板，可以提供上下文、指令或知识来指导AI的行为。提示可以是静态文本或基于项目当前状态动态生成。

```csharp
[McpPluginPromptType]
public static class Prompt_ScriptingCode
{
    [McpPluginPrompt(Name = "add-event-system", Role = Role.User)]
    [Description("在GameObjects之间实现基于UnityEvent的通信系统。")]
    public string AddEventSystem()
    {
        return "使用UnityEvents、UnityActions或自定义事件委托创建事件系统，用于游戏系统和组件之间的解耦通信。";
    }
}
```

---

# 运行时使用（游戏内）

在您的游戏/应用中使用 **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)**。使用工具、资源或提示。默认情况下没有工具，您需要实现自定义工具。

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

await mcpPlugin.Connect(); // 启动与 Unity-MCP-Server 的主动连接（含重试）

await mcpPlugin.Disconnect(); // 停止主动连接并关闭现有连接
```

## 示例：AI驱动的国际象棋游戏机器人

有一个经典的国际象棋游戏。让我们将机器人逻辑外包给LLM。机器人应该使用游戏规则执行回合。

```csharp
[McpPluginToolType]
public static class ChessGameAI
{
    [McpPluginTool("chess-do-turn", Title = "Do the turn")]
    [Description("Do the turn in the chess game. Returns true if the turn was accepted, false otherwise.")]
    public static Task<bool> DoTurn(int figureId, Vector2Int position)
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.DoTurn(figureId, position));
    }

    [McpPluginTool("chess-get-board", Title = "Get the board")]
    [Description("Get the current state of the chess board.")]
    public static Task<BoardData> GetBoard()
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.GetBoardData());
    }
}
```

## 为什么需要运行时使用

有很多用例，假设您正在开发一个带有机器人的国际象棋游戏。您可以通过编写几行代码将机器人的决策外包给LLM。

---

# Unity `MCP 服务器` 设置

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 服务器支持许多不同的启动选项和Docker部署。支持两种传输协议：`streamableHttp` 和 `stdio`。如果您需要自定义或将Unity MCP服务器部署到云端，这一节适合您。[阅读更多...](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md)

## 变量

无论您选择什么启动选项，它们都支持使用环境变量和命令行参数进行自定义配置。如果您只需要启动它，它会使用默认值工作，不要浪费时间在变量上。只需确保Unity插件也有默认值，特别是 `--port`，它们应该相等。

| 环境变量                     | 命令行参数           | 描述                                                                 |
| ---------------------------- | -------------------- | -------------------------------------------------------------------- |
| `MCP_PLUGIN_PORT`            | `--port`             | **客户端** -> **服务器** <- **插件** 连接端口（默认：8080）          |
| `MCP_PLUGIN_CLIENT_TIMEOUT`   | `--plugin-timeout`   | **插件** -> **服务器** 连接超时（毫秒）（默认：10000）               |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | **客户端** -> **服务器** 传输类型：`stdio` 或 `streamableHttp`（默认：`streamableHttp`） |

> 命令行参数还支持单个 `-` 前缀的选项（`-port`）和完全没有前缀的选项（`port`）。

## Docker 📦

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)

确保已安装Docker。如果您使用Windows操作系统，请确保已启动Docker Desktop。

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

> 如果托管在云端，请将 `url` 替换为您的真实端点

</details>

### `stdio` 传输

要使用此变体，`MCP 客户端` 应该在docker中启动 `MCP 服务器`。这可以通过修改的 `MCP 客户端` 配置来实现。

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

> 如果托管在云端，请将 `url` 替换为您的真实端点
</details>

## 二进制可执行文件

您可以直接从二进制文件启动Unity `MCP 服务器`。您需要一个专门为您的CPU架构编译的二进制文件。查看[GitHub发布页面](https://github.com/IvanMurzak/Unity-MCP/releases)，它包含所有CPU架构的预编译二进制文件。

```bash
./unity-mcp-server --port 8080 --plugin-timeout 10000 --client-transport stdio
```

<details>
  <summary><code>MCP 客户端</code> 配置：</summary>

> 将 `<project>` 替换为您的Unity项目路径。

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

---

# 工作原理

**[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 充当LLM和Unity之间的桥梁。它向LLM暴露并解释Unity的工具，然后LLM理解接口并根据用户请求使用这些工具。

使用集成的 `AI Connector` 窗口将 **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** 连接到LLM客户端，如[Claude](https://claude.ai/download)或[Cursor](https://www.cursor.com/)。也支持自定义客户端。

系统高度可扩展 - 您可以直接在Unity项目代码库中定义自定义 `MCP 工具`、`MCP 资源` 或 `MCP 提示`，向AI或自动化客户端暴露新功能。这使Unity MCP成为构建高级工作流程、快速原型制作和将AI驱动功能集成到开发过程中的灵活基础。

## 什么是 `MCP`

MCP - 模型上下文协议。简而言之，这是AI的 `USB Type-C`，专门用于LLM（大型语言模型）。它教LLM如何使用外部功能。比如在这种情况下的Unity引擎，甚至是您代码中的自定义C#方法。[官方文档](https://modelcontextprotocol.io/)。

## 什么是 `MCP 客户端`

它是一个带有聊天窗口的应用程序。它可能有智能代理以更好地操作，它可能有嵌入的高级MCP工具。一般来说，做得好的MCP客户端是AI执行任务成功的50%。这就是为什么选择最好的客户端进行使用非常重要。

## 什么是 `MCP 服务器`

它是 `MCP 客户端` 和"其他东西"之间的桥梁，在这种特殊情况下是Unity引擎。这个项目包含 `MCP 服务器`。

## 什么是 `MCP 工具`

`MCP 工具` 是LLM可以调用与Unity交互的函数或方法。这些工具充当自然语言请求和实际Unity操作之间的桥梁。当您要求AI"创建立方体"或"更改材质颜色"时，它使用MCP工具来执行这些操作。

**关键特征：**

- **可执行函数** 执行特定操作
- **类型化参数** 带有描述，帮助LLM理解要提供什么数据
- **返回值** 提供关于操作成功或失败的反馈
- **线程感知** - 可以在主线程上运行Unity API调用或在后台线程上进行重处理

### 何时使用 `MCP 工具`

- **自动化重复任务** - 为您经常执行的常见操作创建工具
- **复杂操作** - 将多个Unity API调用捆绑到一个易于使用的工具中
- **项目特定工作流程** - 构建理解您项目特定结构和约定的工具
- **容易出错的任务** - 创建包含验证和错误处理的工具
- **自定义游戏逻辑** - 向AI暴露您的游戏系统以进行动态内容创建

**示例：**

- 创建和配置带有特定组件的GameObjects
- 批处理资产（纹理、材质、预制件）
- 设置照明和后处理效果
- 生成关卡几何或程序性放置对象
- 配置物理设置或碰撞层

## 什么是 `MCP 资源`

`MCP 资源` 提供对Unity项目内数据的只读访问。与执行操作的MCP工具不同，资源允许LLM检查和理解项目的当前状态、资产和配置。将它们视为为AI提供项目上下文的"传感器"。

**关键特征：**

- **只读访问** 项目数据和Unity对象
- **结构化信息** 以LLM可以理解的格式呈现
- **实时数据** 反映项目的当前状态
- **上下文感知** 帮助AI做出明智决策

### 何时使用 `MCP 资源`

- **项目分析** - 让AI理解您的项目结构、资产和组织
- **调试协助** - 提供当前状态信息用于故障排除
- **智能建议** - 为AI提供上下文以做出更好的推荐
- **文档生成** - 基于项目状态自动创建文档
- **资产管理** - 帮助AI理解可用的资产及其属性

**示例：**

- 暴露场景层次结构和GameObject属性
- 列出可用材质、纹理及其设置
- 显示脚本依赖关系和组件关系
- 显示当前照明设置和渲染管线配置
- 提供关于音频源、动画和粒子系统的信息

## 什么是 `MCP 提示`

`MCP 提示` 允许您将预定义的提示注入到与LLM的对话中。这些是智能模板，可以提供上下文、指令或知识来指导AI的行为。提示可以是静态文本或基于项目当前状态动态生成。

**关键特征：**

- **上下文指导** 影响AI如何响应
- **基于角色** - 可以模拟不同角色（用户请求或助手知识）
- **动态内容** - 可以包含实时项目数据
- **可重用模板** 用于常见场景和工作流程

### 何时使用 `MCP 提示`

- **提供领域知识** - 分享特定于您项目的最佳实践和编码标准
- **设置编码约定** - 建立命名约定、架构模式和代码风格
- **给出项目结构上下文** - 解释您的项目是如何组织的以及为什么
- **分享工作流程指令** - 为常见任务提供分步程序
- **注入专业知识** - 添加关于特定Unity功能、第三方资产或自定义系统的信息

**示例：**

- "总是为公共方法使用PascalCase，为私有字段使用camelCase"
- "这个项目使用位于Scripts/Events/的自定义事件系统"
- "创建UI元素时，总是将它们添加到Scene/UI/MainCanvas中的Canvas"
- "性能至关重要 - 对于频繁实例化的对象优先使用对象池"
- "这个项目遵循SOLID原则 - 解释任何架构决策"

---

# 贡献 💙💛

非常欢迎贡献。带来您的想法，让我们让游戏开发比以往任何时候都更简单！您有新的 `MCP 工具` 或功能的想法，或者发现了错误并知道如何修复它吗？

**如果您觉得这个项目有用，请给它一个星标 🌟！**

1. 👉 [阅读开发文档](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.zh-CN.md)
2. 👉 [Fork项目](https://github.com/IvanMurzak/Unity-MCP/fork)
3. 克隆fork并在Unity中打开 `./Unity-MCP-Plugin` 文件夹
4. 在项目中实现新功能，提交，推送到GitHub
5. 创建针对原始[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP/compare)仓库 `main` 分支的Pull Request。
