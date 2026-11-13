<div align="center" width="100%">

<p align="center"><img src="img/readme/hero.svg" alt="Unity 版 AI Game Developer:通过 Skills、MCP 工具和 CLI 将 AI 智能体连接到 Unity 编辑器和正在运行的游戏" width="100%"></p>

<p align="center">
<a href="https://openupm.com/packages/com.ivanmurzak.unity.mcp/"><img src="https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F" alt="OpenUPM version"></a> <a href="https://openupm.com/packages/com.ivanmurzak.unity.mcp/"><img src="https://img.shields.io/badge/dynamic/json?label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F" alt="OpenUPM downloads per month"></a> <a href="https://www.npmjs.com/package/unity-mcp-cli"><img src="https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D" alt="unity-mcp-cli on npm"></a> <a href="https://unity.com/releases/editor/archive"><img src="https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Unity 2022.3 or newer"></a>
<br>
<a href="https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml"><img src="https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/release.yml?label=release&logo=githubactions&color=15803D&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Release workflow status"></a> <a href="https://discord.gg/cfbdMZX99G"><img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Discord"></a> <a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE"><img src="https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857" alt="License"></a> <a href="https://stand-with-ukraine.pp.ua"><img src="https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-square.svg" height="28" alt="Stand with Ukraine" title="Stand with Ukraine"></a>
</p>

  <p>
    <a href="https://claude.ai/download"><img src="img/mcp-clients/claude-64.png" alt="Claude" title="Claude" height="36"></a>&nbsp;&nbsp;
    <a href="https://openai.com/index/introducing-codex/"><img src="img/mcp-clients/codex-64.png" alt="Codex" title="Codex" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.cursor.com/"><img src="img/mcp-clients/cursor-64.png" alt="Cursor" title="Cursor" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/docs/copilot/overview"><img src="img/mcp-clients/github-copilot-64.png" alt="GitHub Copilot" title="GitHub Copilot" height="36"></a>&nbsp;&nbsp;
    <a href="https://gemini.google.com/"><img src="img/mcp-clients/gemini-64.png" alt="Gemini" title="Gemini" height="36"></a>&nbsp;&nbsp;
    <a href="https://antigravity.google/"><img src="img/mcp-clients/antigravity-64.png" alt="Antigravity" title="Antigravity" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/"><img src="img/mcp-clients/vs-code-64.png" alt="VS Code" title="VS Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.jetbrains.com/rider/"><img src="img/mcp-clients/rider-64.png" alt="Rider" title="Rider" height="36"></a>&nbsp;&nbsp;
    <a href="https://visualstudio.microsoft.com/"><img src="img/mcp-clients/visual-studio-64.png" alt="Visual Studio" title="Visual Studio" height="36"></a>&nbsp;&nbsp;
    <a href="https://opencode.ai/"><img src="img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.fa.md)</b>

</div>

**AI Game Developer**(Unity-MCP)把你的 AI 智能体连接到 Unity。Claude Code、Codex、Cursor、GitHub Copilot、 Gemini 或任何其他 [MCP](https://modelcontextprotocol.io/) 客户端都可以在 **Unity 编辑器**中读取和修改场景、预制体、资源和 C# 代码,运行测试并截图,还能在**编译后的游戏内**调用你编写的工具。 它以免费的 Unity 包(`com.ivanmurzak.unity.mcp`)、命令行工具(`unity-mcp-cli`)和十个可选扩展的形式提供。

## 工作原理

<p align="center"><img src="img/readme/architecture.svg" alt="AI 智能体或 unity-mcp-cli 通过 MCP 与服务器通信(默认是 ai-game.dev Cloud,或本地 gamedev-mcp-server),服务器调用 Unity 编辑器插件和正在运行的游戏中的工具" width="100%"></p>

插件把它的工具注册到 MCP 服务器,智能体通过该服务器调用这些工具。默认情况下插件使用 `https://ai-game.dev/mcp` 上的 **Cloud** 服务器。在 **Custom** 模式下,它会启动本地的 [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) 程序,或连接到你自己托管的服务器 (二进制或 Docker)。除了 MCP 工具,智能体也可以使用生成的 **Skills**。

## 功能

✅ **RQL** — 用一种查询语言读取和修改场景、对象、资源和设置\
✅ **C# 与测试** — 用 Roslyn 编译并运行 C#;运行 EditMode 和 PlayMode 测试\
✅ **Skills 或 MCP** — 一键生成 Skills,或作为 MCP 客户端连接\
✅ **任意智能体** — Claude Code、Codex、Cursor、Copilot、Gemini 及其他 MCP 客户端\
✅ **运行时** — 在构建好的游戏中使用同样的工具:NPC 逻辑、实时调试\
✅ **自定义工具** — 任何 C# 方法加上 `[AiTool]` 特性即成为工具\
✅ **CLI** — 在终端或 CI 中安装、打开、查询、编译和测试\
✅ **扩展** — 十个可选工具包,从 Animation 到 Timeline

## 快速开始

需要 **Unity 2022.3** 或更高版本。选择一种方式安装插件:

[![下载安装器](img/button/button_download_zh-CN.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

- **安装器**:下载上面的 `.unitypackage` 并在 Unity 项目中打开。
- **OpenUPM**: `openupm add com.ivanmurzak.unity.mcp`
- **CLI**(Node.js 22.14+),无需打开编辑器:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin ./MyUnityProject   # 把插件加入 Packages/manifest.json
unity-mcp-cli login                             # 在浏览器中登录 ai-game.dev
unity-mcp-cli open ./MyUnityProject             # 打开 Unity;自动连接并生成 skills
```

<p align="center"><img src="img/readme/terminal.svg" alt="终端示例:安装 unity-mcp-cli、添加插件、登录、配置 Claude Code 并打开项目" width="100%"></p>

然后连接智能体:在 Unity 中打开 **Window ▸ AI Game Dev**,点击 **Auto-generate** Skills(推荐)或 **Configure** MCP。或在终端中:

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject
```

![AI Game Developer 窗口](img/editor/ai-game-developer-windows.png)

让智能体 *“在半径为 2 的圆上创建 3 个立方体”*,看着它完成。完整步骤、手动 MCP 配置和卸载: **[安装指南](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md)**(英文)。

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

## 使用

**和智能体对话。** 描述你想要的结果,越具体越好。

```text
解释我的场景层级
创建金属质感的金色材质并赋给一个新球体
找到所有没有 Rigidbody 的敌人预制体并添加一个
```

**工具。** 默认显示:`rql-query`、`rql-mutate`、`rql-schema`、`script-execute`、`tests-run`、 `screenshot-isolated`、`package-*` 和 `assetstore-*` 工具以及 `unity-tool-list`。其他工具已安装, 但在你显示之前处于隐藏状态。完整列表:**[默认 MCP 工具](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)** 或 [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools)。

**在终端中**(没有编辑器运行时会启动一个无界面编辑器):

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame
unity-mcp-cli test ./MyGame --mode PlayMode
```

**自定义工具。** 只需一个带 `[AiToolType]` 的类和一个带 `[AiTool]` 的方法:

```csharp
[AiToolType]
public class Tool_Score
{
    [AiTool("score-reset", Title = "Reset the score")]
    [Description("Sets the player's score to zero.")]
    public string Reset()
    {
        return MainThread.Instance.Run(() =>
        {
            ScoreManager.Instance.Score = 0;
            return "[Success] Score reset.";
        });
    }
}
```

提示词、运行时(游戏内)工具和完整示例:**[自定义工具与运行时](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md)**。

## 扩展

在 AI Game Developer 窗口的 **Extensions** 部分安装,或使用 `unity-mcp-cli install-extension <name>`。

[Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) · [Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) · [InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) · [Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) · [ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) · [ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) · [Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) · [Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) · [Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) · [Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md)

## 文档

| 页面 | 内容 |
| --- | --- |
| [安装指南](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md) | 安装方式、连接智能体、手动 MCP 配置、团队设置、卸载 |
| [默认 MCP 工具](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md) | 所有内置工具、RQL 数据源、1.0.0 中移除的工具及其 RQL 等价写法 |
| [自定义工具与运行时](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md) | 编写工具和提示词;在游戏中使用 MCP |
| [MCP 服务器与连接](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md) | Cloud 和 Custom 模式、插件环境变量、本地服务器 |
| [Docker 部署](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/DOCKER_DEPLOYMENT.md) | 在容器中运行 MCP 服务器 |
| [在 CI 中禁用](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/ci-disable.md) | 让插件不参与普通的 CI 测试运行 |
| [CLI 参考](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | `unity-mcp-cli` 的所有命令和选项 |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | 教程、指南和常见问题 |
| [开发指南](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md) | 面向贡献者:仓库结构、代码风格、测试和 CI |

相关:[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)(共享的 MCP 服务器)· [ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

文档页面为英文。

## 参与贡献

欢迎提出想法、报告问题和提交 Pull Request。请阅读 [开发指南](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md),fork 本仓库, 在 Unity 中打开 `./Unity-MCP-Plugin`,然后向 `main` 提交 Pull Request。如果项目对你有帮助,请点个 Star。

## 许可证

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
