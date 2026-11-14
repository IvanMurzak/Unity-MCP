[English](installation.md) | <b>中文</b> | [日本語](installation.ja.md) | [Español](installation.es.md) | [فارسی](installation.fa.md) | [हिन्दी](installation.hi.md) | [العربية](installation.ar.md)

# 安装指南

将 **AI Game Developer** 插件安装到 Unity 项目中，连接 AI 智能体，并在需要时将其卸载。返回 [README](README.zh-CN.md)。

## 要求

- **Unity 2022.3** 或更高版本。
- 项目路径**不能包含空格**。路径中含有空格时，插件会在启动时记录一条错误。
  - ✅ `C:/MyProjects/MyProject`
  - ❌ `C:/My Projects/MyProject`
- 使用 CLI 时：[Node.js](https://nodejs.org/) 22.14 或更高版本。

## 1. 安装插件

### 方式 1：安装程序

1. 下载 **[AI-Game-Dev-Installer.unitypackage](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)**。
2. 在项目打开的状态下双击该文件，或在 Unity 中使用 **Assets ▸ Import Package ▸ Custom Package**。

### 方式 2：OpenUPM

```bash
openupm add com.ivanmurzak.unity.mcp
```

### 方式 3：CLI

[`unity-mcp-cli`](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) 会替你编辑 `Packages/manifest.json`，因此无需打开编辑器：

```bash
npm install -g unity-mcp-cli

unity-mcp-cli install-unity                      # optional: install a Unity Editor
unity-mcp-cli create-project ./MyUnityProject    # optional: create a project
unity-mcp-cli install-plugin ./MyUnityProject    # add the plugin
unity-mcp-cli login                              # sign in to ai-game.dev (browser device flow)
unity-mcp-cli open ./MyUnityProject              # open Unity; it connects and generates skills
unity-mcp-cli wait-for-ready ./MyUnityProject    # wait until tools can be called
```

`npx unity-mcp-cli <command>` 无需全局安装即可使用。

**登录与项目固定。** `login` 会将凭据保存到 `~/.ai-game-dev/credentials.json`；无需粘贴任何令牌。默认情况下，`setup-mcp` 写入的智能体配置指向按项目**固定**的端点（`https://ai-game.dev/mcp/p/<pin>`）；传入 `--no-pin` 则使用共享的 `https://ai-game.dev/mcp`。团队可以通过注册码共享访问权限：`unity-mcp-cli install-plugin --enroll <code>`。

## 2. 选择 AI 智能体

你只需要一个智能体，它就是你工作时使用的聊天窗口。它必须支持 Skills，或支持在运行期间更新的 MCP 工具。推荐使用 Claude Code。AI Game Developer 窗口和 `unity-mcp-cli setup-mcp` 会为 Claude Code、Claude Desktop、Codex、Cursor、Gemini、GitHub Copilot（CLI、VS Code、Visual Studio）、Antigravity、Cline、Open Code、Rider（Junie）、Unity AI 等写入配置；`unity-mcp-cli setup-mcp --list` 会打印完整列表。其他任何 MCP 客户端都可以使用该窗口中的 JSON 配置。

## 3. 连接智能体

### 自动

1. 在 Unity 中打开项目。
2. 打开 **Window ▸ AI Game Dev**。
3. 点击 **Auto-generate** 生成 Skills（推荐），或为你的智能体点击 **Configure** 配置 MCP。

![AI Game Developer 窗口](img/ai-connector-window.gif)

在终端中也可以完成相同操作：

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject             # http (default)
unity-mcp-cli setup-mcp cursor ./MyUnityProject --transport stdio
```

### 手动

如果你的智能体不在列表中，请将 AI Game Developer 窗口中显示的 JSON 复制到它的 MCP 配置中。

对于通过自身命令行添加服务器的智能体，请构造用于启动本地服务器的 `<command>`（Custom 模式）。将 `<unityProjectPath>` 替换为项目的完整路径，将 `<port>` 替换为 AI Game Developer 窗口中显示的端口：

| 平台 | `<command>` |
| --- | --- |
| Windows x64 | `"<unityProjectPath>/Library/mcp-server/win-x64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows x86 | `"<unityProjectPath>/Library/mcp-server/win-x86/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows arm64 | `"<unityProjectPath>/Library/mcp-server/win-arm64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| macOS Apple Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| macOS Intel | `"<unityProjectPath>/Library/mcp-server/osx-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux x64 | `"<unityProjectPath>/Library/mcp-server/linux-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux arm64 | `"<unityProjectPath>/Library/mcp-server/linux-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |

然后添加它：

```bash
claude mcp add ai-game-developer <command>    # Claude Code
gemini mcp add ai-game-developer <command>    # Gemini CLI
```

GitHub Copilot CLI：运行 `copilot`，然后执行 `/mcp add`，服务器名称填 `ai-game-developer`，类型选 `local`，命令填上面的 `<command>`。

> 在 VS Code 中使用 Copilot 时，请确保已启用 **Agent** 模式。

## 团队设置：更新通知

当 OpenUPM 上有更新的版本时，插件会在编辑器启动时显示更新弹窗。*"Do not show again"* 是按用户保存的设置。要为**整个团队**关闭该弹窗，请打开 **Edit ▸ Project Settings ▸ AI Game Developer** 并启用 *"Disable update notifications for the entire team"*。该设置保存在 `ProjectSettings/AI-Game-Developer-UpdateSettings.asset` 中；请提交该文件。同样的开关也位于 **Tools ▸ AI Game Developer ▸ Updates ▸ Disable Update Notifications (Team)**。

## 卸载

1. 打开 **Window ▸ Package Manager**，选择 **AI Game Developer — MCP**，然后点击 **Remove**（或运行 `unity-mcp-cli remove-plugin ./MyUnityProject`）。
2. 关闭 Unity，然后删除 `Assets/Plugins/NuGet` 文件夹及其 `Assets/Plugins/NuGet.meta` 文件。

## 下一步

- [默认 MCP 工具](default-mcp-tools.zh-CN.md)
- [自定义工具与运行时](custom-tools.zh-CN.md)
- [MCP 服务器与连接](mcp-server.zh-CN.md)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
