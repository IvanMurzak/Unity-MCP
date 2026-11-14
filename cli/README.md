<div align="center">

<p align="center"><img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/cli-hero.svg" alt="unity-mcp-cli: install Unity and the AI Game Developer plugin, connect AI agents, run tools, compile and test from the terminal" width="100%"></p>

[![unity-mcp-cli on npm](https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D)](https://www.npmjs.com/package/unity-mcp-cli) [![Node.js 22.14 or newer](https://img.shields.io/badge/Node.js-22.14%2B-0E7490?logo=nodedotjs&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://nodejs.org/) [![Unity 2022.3 or newer](https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://unity.com/releases/editor/archive)\
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://discord.gg/cfbdMZX99G) [![Stand with Ukraine](https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-badge.svg)](https://stand-with-ukraine.pp.ua)

<b>[中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.fa.md) | [हिन्दी](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.hi.md) | [العربية](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ar.md)</b>

</div>

`unity-mcp-cli` is the command-line tool of **[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)** (Unity-MCP). It installs Unity Editors, creates projects, adds the plugin, connects AI agents, runs MCP tools, and compiles and tests Unity projects, from your terminal or CI.

## Features

✅ **Install** — Unity Editors, new projects and the plugin, with the OpenUPM registry set up\
✅ **Connect** — sign in once; write MCP configs or Skills for Claude Code, Codex, Cursor…\
✅ **Run tools** — RQL queries, any MCP tool and C# code in the Unity Editor\
✅ **Compile and test** — compiler errors with file and line; EditMode and PlayMode tests\
✅ **Scriptable** — `--format json`, stable exit codes, CI-friendly output\
✅ **Library** — the same commands as a typed Node.js API\
✅ **Cross-platform** — Windows, macOS and Linux

## Quick start

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-unity                  # optional: install a Unity Editor
unity-mcp-cli create-project ./MyGame        # optional: create a project
unity-mcp-cli install-plugin ./MyGame        # add the plugin
unity-mcp-cli login                          # sign in to ai-game.dev in the browser
unity-mcp-cli open ./MyGame                  # open Unity; it connects and generates skills
unity-mcp-cli wait-for-ready ./MyGame        # until tools can be called
```

No global install: `npx unity-mcp-cli <command>`. Requires Node.js 22.14 or newer; Unity Hub is installed automatically when missing.

<p align="center"><img src="https://raw.githubusercontent.com/IvanMurzak/Unity-MCP/docs/readme-refresh/docs/img/readme/terminal.svg" alt="A terminal session: install unity-mcp-cli, add the plugin, sign in, configure Claude Code and open the project" width="100%"></p>

## Usage

```bash
unity-mcp-cli setup-skills claude-code ./MyGame                  # Skills for your agent
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame --strict --format github
unity-mcp-cli test ./MyGame --mode PlayMode --junit-output junit.xml
unity-mcp-cli install-extension Tilemap ./MyGame
```

`rql` and `compile` start a headless editor when none is running; `test` runs a batch-mode editor, like CI.

## Commands

| Area | Commands |
| --- | --- |
| Setup | `install-unity` · `create-project` · `install-plugin` |
| Packages | `install-extension` · `update` · `remove-plugin` |
| Account | `login` · `unity-account` |
| AI agents | `setup-mcp` · `setup-skills` · `configure` |
| Editor | `open` · `close` · `wait-for-ready` · `status` · `dialog` |
| Tools | `tools` · `rql` · `run-tool` · `exec` |
| Build, test | `compile` · `test` |
| Discovery | `commands` |

Every option, exit codes and JSON output: **[command reference](https://github.com/IvanMurzak/Unity-MCP/blob/docs/readme-refresh/cli/docs/commands.md)**, or `unity-mcp-cli <command> --help`.

## Library

The commands are also a typed, side-effect-free library; results are `kind: 'success' | 'failure'`.

```ts
import { installPlugin } from 'unity-mcp-cli';

const result = await installPlugin({ unityProjectPath: './MyGame' });
if (result.kind === 'success') console.log(`Installed v${result.installedVersion}`);
```

## Links

[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) · [Installation guide](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md) · [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) · [Changelog](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/CHANGELOG.md) · [Discord](https://discord.gg/cfbdMZX99G)

## License

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
