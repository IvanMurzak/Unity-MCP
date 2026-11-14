<div align="center" width="100%">

<p align="center"><img src="img/readme/hero.svg" alt="Unity 版 AI Game Developer:通过 Skills、MCP 工具和 CLI 将 AI 智能体连接到 Unity 编辑器和正在运行的游戏" width="100%"></p>

[![OpenUPM version](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) [![OpenUPM downloads per month](https://img.shields.io/badge/dynamic/json?label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp&style=for-the-badge&labelColor=0F172A&logoColor=white&color=2F6F9F)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) [![unity-mcp-cli on npm](https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D)](https://www.npmjs.com/package/unity-mcp-cli) [![Unity 2022.3 or newer](https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://unity.com/releases/editor/archive)\
[![Release workflow status](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/release.yml?label=release&logo=github&color=15803D&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://discord.gg/cfbdMZX99G) [![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) [![Stand with Ukraine](https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-badge.svg)](https://stand-with-ukraine.pp.ua)

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

  <b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.fa.md) | [हिन्दी](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.hi.md) | [العربية](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ar.md)</b>

</div>

**AI Game Developer**(Unity-MCP)把你的 AI 智能体连接到 Unity。Claude Code、Codex、Cursor、GitHub Copilot、Gemini 或任何其他 [MCP](https://modelcontextprotocol.io/) 客户端都可以在 **Unity 编辑器**中读取和修改场景、预制体、资源和 C# 代码,运行测试并截图,还能在**编译后的游戏内**调用你编写的工具。它以免费的 Unity 包(`com.ivanmurzak.unity.mcp`)、命令行工具(`unity-mcp-cli`)和可选扩展的形式提供。

**状态:** [最新版本](https://github.com/IvanMurzak/Unity-MCP/releases/latest)和[更新日志](https://github.com/IvanMurzak/Unity-MCP/blob/main/Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/CHANGELOG.md)。插件和 `unity-mcp-cli` 共用同一个版本号。已在 **Windows、macOS 和 Linux** 上测试并支持。需要 Unity **2022.3** 或更高版本;CI 在 Unity 2022.3、6000.0、6000.3 和 6000.6 上运行 EditMode、PlayMode 和独立构建测试。

## 工作原理

<p align="center"><img src="img/readme/architecture.svg" alt="AI 智能体或 unity-mcp-cli 通过 MCP 与服务器通信(默认是 ai-game.dev Cloud,或本地 gamedev-mcp-server),服务器调用 Unity 编辑器插件和正在运行的游戏中的工具" width="100%"></p>

插件把它的工具注册到 MCP 服务器,智能体通过该服务器调用这些工具。除了 MCP 工具,智能体也可以使用生成的 **Skills**。你可以选择服务器运行的位置:

| | **本地**(Custom 模式,无需账号) | **Cloud**(默认设置) |
| --- | --- | --- |
| 服务器 | 由插件在 `127.0.0.1` 上启动的 [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server),或你自己托管的服务器 | `https://ai-game.dev/mcp` |
| 账号 | 不需要 | 登录 ai-game.dev |
| 你的项目数据 | 保留在你的电脑上 | 工具调用经由无状态中继转发,中继不检查也不持久保存其内容;它会记录 MCP 事件元数据,并在滚动的 90 天窗口内使用([隐私政策](https://ai-game.dev/legal/privacy)) |
| 费用 | 免费 | 免费 |

**为什么默认使用 Cloud:** 它不需要开放任何端口,避免与其他程序发生端口冲突,而且免费。本地模式是同样可行的选择,适用于离线或物理隔离(air-gapped)环境。详情:[安全与隐私](#安全与隐私)。

## 功能

✅ **覆盖编辑器** — 你在 Unity 编辑器中能做的事,智能体几乎都能做\
✅ **紧凑高效** — 专为智能体打造的工具:调用更少、token 更少,智能体工作更快、成本更低([在 Unity 编辑器中实测](benchmark.zh-CN.md))\
✅ **RQL** — 用一种查询语言读取和修改场景、对象、资源和设置\
✅ **C# 与测试** — 用 Roslyn 编译并运行 C#;运行 EditMode 和 PlayMode 测试\
✅ **Skills 或 MCP** — 一键生成 Skills,或作为 MCP 客户端连接\
✅ **任意智能体** — Claude Code、Codex、Cursor、Copilot、Gemini 及其他 MCP 客户端\
✅ **运行时** — 在构建好的游戏中使用同样的工具:NPC 逻辑、实时调试\
✅ **自定义工具** — 任何 C# 方法加上 `[AiTool]` 特性即成为工具\
✅ **CLI** — 在终端或 CI 中安装、打开、查询、编译和测试\
✅ **扩展** — 可选工具包,从 Animation 到 Timeline

## 快速开始

需要 **Unity 2022.3** 或更高版本。

**1. 安装插件**(推荐使用安装器,无需安装其他任何东西):

[![下载安装器](img/button/button_download_zh-CN.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

在项目已打开的状态下打开下载的 `.unitypackage`(双击它,或使用 **Assets ▸ Import Package ▸ Custom Package**)。

<details>
<summary>其他安装方式:OpenUPM / Package Manager,或用于脚本和 CI 的 CLI</summary>

- **OpenUPM** — `openupm add com.ivanmurzak.unity.mcp`;或者不使用任何 CLI:在 **Edit ▸ Project Settings ▸ Package Manager** 中添加作用域注册表(URL `https://package.openupm.com`,作用域 `com.ivanmurzak` 和 `extensions.unity`),然后通过 **Window ▸ Package Manager ▸ + ▸ Add package by name** → `com.ivanmurzak.unity.mcp`。
- **unity-mcp-cli**(Node.js 22.14+)— 用于脚本和 CI,无需打开编辑器:

  ```bash
  npm install -g unity-mcp-cli
  unity-mcp-cli install-plugin ./MyUnityProject   # 把插件加入 Packages/manifest.json
  unity-mcp-cli open ./MyUnityProject             # 在 Unity 中打开项目
  ```

  <p align="center"><img src="img/readme/terminal.svg" alt="终端示例:安装 unity-mcp-cli、添加插件、登录、配置 Claude Code 并打开项目" width="100%"></p>

</details>

**2. 连接智能体** — 任选其一,两种方式都完全支持:

| | **本地**(无需账号) | **Cloud**(默认设置) |
| --- | --- | --- |
| 在 **Window ▸ AI Game Dev** 中 | 连接方式选 **Custom** | 连接方式选 **Cloud**,然后点击 **Authorize**(在浏览器中登录) |
| 智能体配置 | 为你的智能体点击 **Configure** | 为你的智能体点击 **Configure** |
| 用 CLI 完成同样操作 | `unity-mcp-cli setup-mcp claude-code ./MyUnityProject --transport stdio` | `unity-mcp-cli login`,然后 `unity-mcp-cli setup-mcp claude-code ./MyUnityProject` |

在同一窗口中 **Auto-generate** Skills 是 MCP 之外的另一种选择。其他智能体:[客户端设置](#客户端设置)。

**3. 仅限本地:选择谁可以调用你的工具。** 本地服务器以 `none` 认证模式启动:它只监听 `127.0.0.1` 并拒绝外部浏览器来源,但你电脑上的任何程序都能访问它。如果有其他人或不受信任的程序使用这台电脑,一步切换到 `token`:**Window ▸ AI Game Dev** → **Authorization Token** → `token`。服务器会用生成的密钥重启;再次点击 **Configure**,让智能体发送该密钥。

![AI Game Developer 窗口](img/editor/ai-game-developer-windows.png)

让智能体 *“在半径为 2 的圆上创建 3 个立方体”*,看着它完成。完整步骤、手动 MCP 配置和卸载:**[安装指南](installation.zh-CN.md)**。

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

## 使用

**和智能体对话。** 描述你想要的结果,越具体越好。

```text
解释我的场景层级
创建金属质感的金色材质并赋给一个新球体
找到所有没有 Rigidbody 的敌人预制体并添加一个
```

**智能体能做什么**(默认显示的工具):

| 能力 | 工具 | 示例提示词 |
| --- | --- | --- |
| 场景和 GameObject | `rql-query`、`rql-mutate` | *“列出当前打开场景中所有被禁用的 GameObject”* |
| 资源和预制体 | `rql-query`、`rql-mutate` | *“创建一个红色材质并赋给所有立方体”* |
| 控制台和编辑器状态 | `rql-query` | *“控制台里有哪些错误?”* |
| 编辑器中的 C# | `script-execute` | *“运行 C#,在地形上的随机位置放置 100 棵树”* |
| 测试 | `tests-run` | *“运行 EditMode 测试并修复失败的部分”* |
| 截图 | `screenshot-isolated` | *“给我看 Player 模型的正面”* |
| 包和 Asset Store | `package-*`、`assetstore-*` | *“安装 Cinemachine”* |
| 工具列表 | `unity-tool-list` | *“你在这里可以使用哪些工具?”* |

你不需要学习 RQL:智能体会自己编写,并向 `rql-schema` 查询所需的每个数据源、类型和调用。在 Unity 编辑器中实测:完成相同任务时,所需的提示 token 比每个操作一个工具的方式少约 5 倍,工具调用也更少,成功率相同([基准测试](benchmark.zh-CN.md))。在一个沙盒游戏世界中,使用 RQL 时,智能体每个任务约需 2–3 次工具调用和 7–8k token,成功率 100%;而使用普通反射工具时需要 30–37 次调用和 125–170k token,成功率 67–91%([结果](https://github.com/IvanMurzak/RQL/blob/main/RESULTS.md#33-rql-vs-conventional-reflection-tools))。其他工具已安装,但在你显示之前处于隐藏状态。完整列表:**[默认 MCP 工具](default-mcp-tools.zh-CN.md)** 或 [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools)。

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

提示词、运行时(游戏内)工具和完整示例:**[自定义工具与运行时](custom-tools.zh-CN.md)**。

## 客户端设置

AI Game Developer 窗口中的 **Configure** 按钮,或 `unity-mcp-cli setup-mcp <agent> ./MyUnityProject`,会替你写好这些文件(默认使用 Cloud,加 `--transport stdio` 则使用本地)。若要手动配置本地服务器:把插件设为 **Custom**,从窗口中获取 `<port>`,并使用 `<project>/Library/mcp-server/<rid>/` 中对应你平台的二进制文件(`win-x64/gamedev-mcp-server.exe`、`osx-arm64/gamedev-mcp-server`、`linux-x64/gamedev-mcp-server` 等)。

<details>
<summary><b>Claude Code</b> — 项目根目录下的 <code>.mcp.json</code></summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["port=<port>", "client-transport=stdio"]
    }
  }
}
```

</details>

<details>
<summary><b>Cursor</b> — <code>.cursor/mcp.json</code></summary>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "type": "stdio",
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["port=<port>", "client-transport=stdio"]
    }
  }
}
```

</details>

<details>
<summary><b>Codex</b> — <code>.codex/config.toml</code></summary>

```toml
[mcp_servers.ai-game-developer]
enabled = true
command = "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe"
args = ["port=<port>", "client-transport=stdio"]
tool_timeout_sec = 300
```

</details>

其他智能体和手动设置:[安装指南](installation.zh-CN.md)。

## 安全与隐私

**最私密的设置:** 连接方式选 **Custom**(本地服务器运行在 `127.0.0.1`,无需账号,项目数据保留在你的电脑上),并在 **Window ▸ AI Game Dev** 中把 **Authorization Token** 设为 `token`,然后再次点击 **Configure**,让智能体发送该密钥。

✅ **插件不开放任何端口** — 它主动向外连接 MCP 服务器。\
✅ **本地服务器仅监听回环地址** — `gamedev-mcp-server` 绑定到 `127.0.0.1`,除非你为托管设置了 `MCP_BIND`。\
✅ **来源检查** — 在所有认证模式下,带有不被允许的 `Origin` 的浏览器请求都会被拒绝(403)。\
✅ **认证模式** — `none`(本地默认)、`token`(智能体和插件都必须发送的同一个共享密钥)、`oauth`(ai-game.dev 账号)。\
✅ **Cloud 中继** — 无状态中继:它在你的智能体和编辑器之间转发工具调用,不检查也不持久保存其内容,也不收集对话、源代码、项目文件、资源或场景数据。它会记录 MCP 事件元数据(事件类型、时间、状态、会话标识符、工具、提示或资源名称、错误详情),这些数据在滚动的 90 天窗口内使用([隐私政策](https://ai-game.dev/legal/privacy))。\
⚠️ **不受保护的情况** — 在 `none` 模式下,你电脑上任何能访问该端口的进程都可以调用工具,而工具以编辑器权限运行(`script-execute` 会运行 C#)。在共享电脑上请使用 `token` 模式。

服务器选项和 Docker:[MCP 服务器与连接](mcp-server.zh-CN.md)。

## 扩展

扩展是可选的 UPM 包,为特定的 Unity 功能添加工具。

```bash
unity-mcp-cli install-extension com.ivanmurzak.unity.mcp.cinemachine ./MyUnityProject
```

| 扩展 | 新增 | 依赖 |
| --- | --- | --- |
| [Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) | AnimationClip 和 AnimatorController |  |
| [Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) | 虚拟相机、目标和相机管线 | `com.unity.cinemachine` 3.1.6+ |
| [InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) | 输入动作资源、映射、动作和绑定 | `com.unity.inputsystem` 1.8.2+ |
| [Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) | NavMesh 表面、烘焙、代理和链接 | `com.unity.ai.navigation` 1.1.7+ |
| [ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) | ParticleSystem 的发射、形状、曲线和渐变 |  |
| [ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) | 可编辑网格:形状、挤出、倒角、材质 | `com.unity.probuilder` 5.2.4+ |
| [Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) | 样条容器、节点和切线 | `com.unity.splines` 2.8.4+ |
| [Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) | 地形:高度图、雕刻和采样 |  |
| [Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) | Grid 和 Tilemap:绘制、填充和读取瓦片 | `com.unity.2d.tilemap.extras` 3.1.3+ |
| [Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md) | Timeline 资源、轨道、片段和标记 | `com.unity.timeline` 1.8.12+ |

所需的 Unity 包会随扩展一起安装。也可以在 AI Game Developer 窗口的 **Extensions** 部分安装扩展。

## 文档

| 页面 | 内容 |
| --- | --- |
| [安装指南](installation.zh-CN.md) | 安装方式、连接智能体、手动 MCP 配置、团队设置、卸载 |
| [基准测试](benchmark.zh-CN.md) | Unity 编辑器和沙盒中的测量:使用 RQL 工具时的 token、工具调用和成功率 |
| [默认 MCP 工具](default-mcp-tools.zh-CN.md) | 所有内置工具、RQL 数据源、1.0.0 中移除的工具及其 RQL 等价写法 |
| [自定义工具与运行时](custom-tools.zh-CN.md) | 编写工具和提示词;在游戏中使用 MCP |
| [MCP 服务器与连接](mcp-server.zh-CN.md) | Cloud 和 Custom 模式、插件环境变量、本地服务器 |
| [Docker 部署](DOCKER_DEPLOYMENT.zh-CN.md) | 在容器中运行 MCP 服务器 |
| [在 CI 中禁用](ci-disable.zh-CN.md) | 让插件不参与普通的 CI 测试运行 |
| [CLI 参考](../cli/docs/README.zh-CN.md) | `unity-mcp-cli` 的所有命令和选项 |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | 教程、指南和常见问题 |
| [开发指南](dev/Development.zh-CN.md) | 面向贡献者:仓库结构、代码风格、测试和 CI |

相关:[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)(共享的 MCP 服务器)· [ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

## 参与贡献

欢迎提出想法、报告问题和提交 Pull Request。请阅读[开发指南](dev/Development.zh-CN.md),fork 本仓库,在 Unity 中打开 `./Unity-MCP-Plugin`,然后向 `main` 提交 Pull Request。如果项目对你有帮助,请点个 Star。

## 许可证

**可免费用于游戏开发,包括商业游戏。** 插件采用 [Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) 许可证。它使用了 [RQL](https://github.com/IvanMurzak/RQL) 和 [Code-Exec](https://github.com/IvanMurzak/Code-Exec),二者采用 PolyForm Shield 1.0.0 许可证:该许可证仅限制开发与之竞争的游戏开发 AI 工具的公司。游戏开发者可以自由使用全部内容。
