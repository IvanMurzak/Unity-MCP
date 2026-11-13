<div align="center" width="100%">

<p align="center"><img src="docs/img/readme/hero.svg" alt="AI Game Developer for Unity: Skills, MCP tools and a CLI that connect AI agents to the Unity Editor and to your running game" width="100%"></p>

<p align="center">
<a href="https://openupm.com/packages/com.ivanmurzak.unity.mcp/"><img src="https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&style=for-the-badge&labelColor=0F172A&logoColor=white&color=6D28D9" alt="OpenUPM version"></a> <a href="https://openupm.com/packages/com.ivanmurzak.unity.mcp/"><img src="https://img.shields.io/badge/dynamic/json?label=Downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.ivanmurzak.unity.mcp&style=for-the-badge&labelColor=0F172A&logoColor=white&color=6D28D9" alt="OpenUPM downloads per month"></a> <a href="https://www.npmjs.com/package/unity-mcp-cli"><img src="https://img.shields.io/npm/v/unity-mcp-cli?label=CLI&logo=npm&style=for-the-badge&labelColor=0F172A&logoColor=white&color=BE185D" alt="unity-mcp-cli on npm"></a> <a href="https://unity.com/releases/editor/archive"><img src="https://img.shields.io/badge/Unity-2022.3%2B-0E7490?logo=unity&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Unity 2022.3 or newer"></a>
<br>
<a href="https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml"><img src="https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/release.yml?label=release&logo=githubactions&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Release workflow status"></a> <a href="https://discord.gg/cfbdMZX99G"><img src="https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white" alt="Discord"></a> <a href="https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE"><img src="https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&style=for-the-badge&labelColor=0F172A&logoColor=white&color=047857" alt="License"></a>
</p>

  <p>
    <a href="https://claude.ai/download"><img src="docs/img/mcp-clients/claude-64.png" alt="Claude" title="Claude" height="36"></a>&nbsp;&nbsp;
    <a href="https://openai.com/index/introducing-codex/"><img src="docs/img/mcp-clients/codex-64.png" alt="Codex" title="Codex" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.cursor.com/"><img src="docs/img/mcp-clients/cursor-64.png" alt="Cursor" title="Cursor" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/docs/copilot/overview"><img src="docs/img/mcp-clients/github-copilot-64.png" alt="GitHub Copilot" title="GitHub Copilot" height="36"></a>&nbsp;&nbsp;
    <a href="https://gemini.google.com/"><img src="docs/img/mcp-clients/gemini-64.png" alt="Gemini" title="Gemini" height="36"></a>&nbsp;&nbsp;
    <a href="https://antigravity.google/"><img src="docs/img/mcp-clients/antigravity-64.png" alt="Antigravity" title="Antigravity" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/"><img src="docs/img/mcp-clients/vs-code-64.png" alt="VS Code" title="VS Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.jetbrains.com/rider/"><img src="docs/img/mcp-clients/rider-64.png" alt="Rider" title="Rider" height="36"></a>&nbsp;&nbsp;
    <a href="https://visualstudio.microsoft.com/"><img src="docs/img/mcp-clients/visual-studio-64.png" alt="Visual Studio" title="Visual Studio" height="36"></a>&nbsp;&nbsp;
    <a href="https://opencode.ai/"><img src="docs/img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="docs/img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="docs/img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

  <b>[中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.es.md) | [فارسی](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/README.fa.md)</b>

</div>

**AI Game Developer** (Unity-MCP) connects your AI agent to Unity. Claude Code, Codex, Cursor, GitHub Copilot, Gemini or any other [MCP](https://modelcontextprotocol.io/) client can read and change scenes, prefabs, assets and C# code, run tests and take screenshots in the **Unity Editor**, and call tools you write **inside your compiled game**. It ships as a free Unity package (`com.ivanmurzak.unity.mcp`), a command-line tool (`unity-mcp-cli`) and ten optional extensions.

## How it works

<p align="center"><img src="docs/img/readme/architecture.svg" alt="An AI agent or unity-mcp-cli talks MCP to a server, ai-game.dev Cloud by default or a local gamedev-mcp-server, which calls the tools of the Unity Editor plugin and of your running game" width="100%"></p>

The plugin registers its tools with an MCP server; your agent calls them through that server. By default the plugin uses the **Cloud** server at `https://ai-game.dev/mcp`. In **Custom** mode it starts the local [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) binary, or connects to one you host (binary or Docker). Instead of MCP tools, the agent can also use generated **Skills**.

## Features

✅ **RQL** — read and change scenes, objects, assets and settings with one query language\
✅ **C# and tests** — compile and run C# with Roslyn; run EditMode and PlayMode tests\
✅ **Skills or MCP** — generate Skills in one click, or connect as an MCP client\
✅ **Any agent** — Claude Code, Codex, Cursor, Copilot, Gemini and other MCP clients\
✅ **Runtime** — the same tools inside your built game: NPC logic, live debugging\
✅ **Your own tools** — any C# method becomes a tool with one `[AiTool]` attribute\
✅ **CLI** — install, open, query, compile and test from the terminal or CI\
✅ **Extensions** — ten optional tool packs, from Animation to Timeline

## Quick start

Requires **Unity 2022.3** or newer. Pick one way to install the plugin:

[![DOWNLOAD INSTALLER](docs/img/button/button_download.svg)](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)

- **Installer** — download the `.unitypackage` above and open it in your Unity project.
- **OpenUPM** — `openupm add com.ivanmurzak.unity.mcp`
- **CLI** (Node.js 22.14+) — no Editor needed:

```bash
npm install -g unity-mcp-cli
unity-mcp-cli install-plugin ./MyUnityProject   # add the plugin to Packages/manifest.json
unity-mcp-cli login                             # sign in to ai-game.dev in the browser
unity-mcp-cli open ./MyUnityProject             # open Unity; it connects and generates skills
```

<p align="center"><img src="docs/img/readme/terminal.svg" alt="Terminal session: install unity-mcp-cli, add the plugin, sign in, configure Claude Code and open the project" width="100%"></p>

Then connect your agent: in Unity open **Window ▸ AI Game Dev** and click **Auto-generate** Skills (recommended) or **Configure** MCP. Or from the terminal:

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject
```

![AI Game Developer window](docs/img/editor/ai-game-developer-windows.png)

Ask your agent *"Create 3 cubes in a circle with radius 2"* and watch it happen. Full steps, manual MCP configuration and uninstall: **[Installation guide](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md)**.

https://github.com/user-attachments/assets/228baf4d-4f00-4dce-939d-fb985ebdd8dd

## Usage

**Talk to your agent.** Describe the result you want; the more precise, the better.

```text
Explain my scene hierarchy
Create metallic golden material and attach it to a new sphere
Find every enemy prefab without a Rigidbody and add one
```

**Tools.** Shown by default: `rql-query`, `rql-mutate`, `rql-schema`, `script-execute`, `tests-run`, `screenshot-isolated`, the `package-*` and `assetstore-*` tools and `unity-tool-list`. More tools are installed but hidden until you show them. Full list: **[Default MCP tools](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md)** or [ai-game.dev/docs/tools](https://ai-game.dev/docs/tools).

**From the terminal** (starts a headless editor when none is running):

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli compile ./MyGame
unity-mcp-cli test ./MyGame --mode PlayMode
```

**Your own tool.** A class with `[AiToolType]` and a method with `[AiTool]` is all it takes:

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

Prompts, runtime (in-game) tools and a full example: **[Custom tools and runtime](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md)**.

## Extensions

Install from the **Extensions** section of the AI Game Developer window, or with `unity-mcp-cli install-extension <name>`.

[Animation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Animation/README.md) · [Cinemachine](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Cinemachine/README.md) · [InputSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/InputSystem/README.md) · [Navigation](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Navigation/README.md) · [ParticleSystem](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ParticleSystem/README.md) · [ProBuilder](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/ProBuilder/README.md) · [Splines](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Splines/README.md) · [Terrain](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Terrain/README.md) · [Tilemap](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Tilemap/README.md) · [Timeline](https://github.com/IvanMurzak/Unity-MCP/blob/main/extensions/Timeline/README.md)

## Documentation

| Page | What is in it |
| --- | --- |
| [Installation guide](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/installation.md) | Install options, connecting an agent, manual MCP config, team settings, uninstall |
| [Default MCP tools](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/default-mcp-tools.md) | Every built-in tool, RQL sources, tools removed in 1.0.0 and their RQL equivalents |
| [Custom tools and runtime](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/custom-tools.md) | Write tools and prompts; use MCP inside your game |
| [MCP server and connection](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/mcp-server.md) | Cloud and Custom modes, plugin environment variables, local server |
| [Docker deployment](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/DOCKER_DEPLOYMENT.md) | Run the MCP server in a container |
| [Disable in CI](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/ci-disable.md) | Keep the plugin out of ordinary CI test runs |
| [CLI reference](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) | Every `unity-mcp-cli` command and option |
| [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki) | Tutorials, guides and FAQ |
| [Development guide](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md) | Repository layout, code style, tests and CI for contributors |

Related: [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) (the shared MCP server) · [ai-game.dev](https://ai-game.dev) · [Discord](https://discord.gg/cfbdMZX99G)

## Contributing

Ideas, bug reports and pull requests are welcome. Read the [Development guide](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/Development.md), fork the repository, open `./Unity-MCP-Plugin` in Unity, and send a pull request to `main`. If the project helps you, please give it a star.

## License

[Apache-2.0](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)

<div align="center">
  <a href="https://stand-with-ukraine.pp.ua"><img src="https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-rounded.svg" width="28" height="28" alt="Stand with Ukraine" title="Stand with Ukraine"></a>
</div>
