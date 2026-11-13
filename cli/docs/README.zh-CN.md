<div align="center">

<p align="center"><img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/cli-hero.svg" alt="unity-mcp-cli:在终端中安装 Unity 和 AI Game Developer 插件、连接 AI 智能体、运行工具、编译和测试" width="100%"></p>

<p align="center">
<a href="https://www.npmjs.com/package/unity-mcp-cli"><img src="https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D" alt="unity-mcp-cli on npm"></a> <a href="https://nodejs.org/"><img src="https://img.shields.io/badge/Node.js-22.14%2B-0E7490?logo=nodedotjs&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Node.js 22.14 or newer"></a> <a href="https://unity.com/releases/editor/archive"><img src="https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Unity 2022.3 or newer"></a> <a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE"><img src="https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857" alt="License"></a> <a href="https://discord.gg/cfbdMZX99G"><img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Discord"></a>
</p>

<b>[English](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

</div>

`unity-mcp-cli` 是 **[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)**(Unity-MCP)的命令行工具。它可以在终端或 CI 中安装 Unity 编辑器、创建项目、添加插件、连接 AI 智能体、运行 MCP 工具,并编译和测试 Unity 项目。

## 功能

✅ **安装** — Unity 编辑器、新项目和插件,并配置好 OpenUPM 注册表\
✅ **连接** — 登录一次;为 Claude Code、Codex、Cursor 等写入 MCP 配置或 Skills\
✅ **运行工具** — RQL 查询、任意 MCP 工具以及在编辑器中运行 C# 代码\
✅ **编译与测试** — 带文件和行号的编译错误;EditMode 和 PlayMode 测试\
✅ **可脚本化** — `--format json`、稳定的退出码、适合 CI 的输出\
✅ **库** — 同样的命令也是类型化的 Node.js API\
✅ **跨平台** — Windows、macOS 和 Linux

## 快速开始

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-unity                  # 可选:安装 Unity 编辑器
unity-mcp-cli create-project ./MyGame        # 可选:创建项目
unity-mcp-cli install-plugin ./MyGame        # 添加插件
unity-mcp-cli login                          # 在浏览器中登录 ai-game.dev
unity-mcp-cli open ./MyGame                  # 打开 Unity;自动连接并生成 skills
unity-mcp-cli wait-for-ready ./MyGame        # 等到可以调用工具
```

无需全局安装:`npx unity-mcp-cli <command>`。需要 Node.js 22.14 或更高版本;缺少 Unity Hub 时会自动安装。

<p align="center"><img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/terminal.svg" alt="终端示例:安装 unity-mcp-cli、添加插件、登录、配置 Claude Code 并打开项目" width="100%"></p>

## 使用

```bash
unity-mcp-cli setup-skills claude-code ./MyGame                  # 为智能体生成 Skills
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame --strict --format github
unity-mcp-cli test ./MyGame --mode PlayMode --junit-output junit.xml
unity-mcp-cli install-extension Tilemap ./MyGame
```

没有编辑器运行时,`rql` 和 `compile` 会启动无界面编辑器;`test` 像 CI 一样以批处理模式运行编辑器。

## 命令

| 类别 | 命令 |
| --- | --- |
| 安装 | `install-unity` · `create-project` · `install-plugin` |
| 包 | `install-extension` · `update` · `remove-plugin` |
| 账户 | `login` · `unity-account` |
| AI 智能体 | `setup-mcp` · `setup-skills` · `configure` |
| 编辑器 | `open` · `close` · `wait-for-ready` · `status` · `dialog` |
| 工具 | `tools` · `rql` · `run-tool` · `exec` |
| 编译测试 | `compile` · `test` |
| 查询 | `commands` |

所有选项、退出码和 JSON 输出:**[命令参考](https://github.com/IvanMurzak/Unity-MCP/blob/docs/readme-refresh/cli/docs/commands.md)**(英文),或 `unity-mcp-cli <command> --help`。

## 库

这些命令也是无副作用的类型化库;结果为 `kind: 'success' | 'failure'`。

```ts
import { installPlugin } from 'unity-mcp-cli';

const result = await installPlugin({ unityProjectPath: './MyGame' });
if (result.kind === 'success') console.log(`Installed v${result.installedVersion}`);
```

## 链接

[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) · [安装指南](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md) · [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) · [Changelog](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/CHANGELOG.md) · [Discord](https://discord.gg/cfbdMZX99G)

## 许可证

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)

<div align="center">
  <a href="https://stand-with-ukraine.pp.ua"><img src="https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-rounded.svg" width="28" height="28" alt="Stand with Ukraine" title="Stand with Ukraine"></a>
</div>
