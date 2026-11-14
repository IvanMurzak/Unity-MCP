<b>English</b> | [中文](installation.zh-CN.md) | [日本語](installation.ja.md) | [Español](installation.es.md) | [فارسی](installation.fa.md) | [हिन्दी](installation.hi.md) | [العربية](installation.ar.md)

# Installation guide

Install the **AI Game Developer** plugin into a Unity project, connect an AI agent, and remove it again if needed. Back to the [README](https://github.com/IvanMurzak/Unity-MCP#readme).

## Requirements

- **Unity 2022.3** or newer.
- A project path **without spaces**. The plugin logs an error at startup when the path contains one.
  - ✅ `C:/MyProjects/MyProject`
  - ❌ `C:/My Projects/MyProject`
- For the CLI: [Node.js](https://nodejs.org/) 22.14 or newer.

## 1. Install the plugin

### Option 1: Installer

1. Download **[AI-Game-Dev-Installer.unitypackage](https://github.com/IvanMurzak/Unity-MCP/releases/latest/download/AI-Game-Dev-Installer.unitypackage)**.
2. Double-click it with the project open, or use **Assets ▸ Import Package ▸ Custom Package** in Unity.

### Option 2: OpenUPM

```bash
openupm add com.ivanmurzak.unity.mcp
```

### Option 3: CLI

[`unity-mcp-cli`](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/README.md) edits `Packages/manifest.json` for you, so the Editor does not need to be open:

```bash
npm install -g unity-mcp-cli

unity-mcp-cli install-unity                      # optional: install a Unity Editor
unity-mcp-cli create-project ./MyUnityProject    # optional: create a project
unity-mcp-cli install-plugin ./MyUnityProject    # add the plugin
unity-mcp-cli login                              # sign in to ai-game.dev (browser device flow)
unity-mcp-cli open ./MyUnityProject              # open Unity; it connects and generates skills
unity-mcp-cli wait-for-ready ./MyUnityProject    # wait until tools can be called
```

`npx unity-mcp-cli <command>` works without a global install.

**Sign-in and project pinning.** `login` stores the credential in `~/.ai-game-dev/credentials.json`; there is no token to paste. By default the agent config written by `setup-mcp` points at a per-project **pinned** endpoint (`https://ai-game.dev/mcp/p/<pin>`); pass `--no-pin` for the shared `https://ai-game.dev/mcp`. Teams can share access with an enrollment code: `unity-mcp-cli install-plugin --enroll <code>`.

## 2. Pick an AI agent

You need one agent; it is the chat window you work in. It must support Skills or MCP tools that update while it runs. Claude Code is recommended. The AI Game Developer window and `unity-mcp-cli setup-mcp` write the config for you for Claude Code, Claude Desktop, Codex, Cursor, Gemini, GitHub Copilot (CLI, VS Code, Visual Studio), Antigravity, Cline, Open Code, Rider (Junie), Unity AI and others; `unity-mcp-cli setup-mcp --list` prints the full list. Any other MCP client works with the JSON config from the window.

## 3. Connect the agent

### Automatic

1. Open the project in Unity.
2. Open **Window ▸ AI Game Dev**.
3. Click **Auto-generate** Skills (recommended), or **Configure** MCP for your agent.

![AI Game Developer window](img/ai-connector-window.gif)

The same from the terminal:

```bash
unity-mcp-cli setup-skills claude-code ./MyUnityProject
unity-mcp-cli setup-mcp claude-code ./MyUnityProject             # http (default)
unity-mcp-cli setup-mcp cursor ./MyUnityProject --transport stdio
```

### Manual

If your agent is not in the list, copy the JSON shown in the AI Game Developer window into its MCP configuration.

For an agent that adds servers from its own command line, build the `<command>` that starts the local server (Custom mode). Replace `<unityProjectPath>` with the full project path and `<port>` with the port from the AI Game Developer window:

| Platform | `<command>` |
| --- | --- |
| Windows x64 | `"<unityProjectPath>/Library/mcp-server/win-x64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows x86 | `"<unityProjectPath>/Library/mcp-server/win-x86/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| Windows arm64 | `"<unityProjectPath>/Library/mcp-server/win-arm64/gamedev-mcp-server.exe" port=<port> client-transport=stdio` |
| macOS Apple Silicon | `"<unityProjectPath>/Library/mcp-server/osx-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| macOS Intel | `"<unityProjectPath>/Library/mcp-server/osx-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux x64 | `"<unityProjectPath>/Library/mcp-server/linux-x64/gamedev-mcp-server" port=<port> client-transport=stdio` |
| Linux arm64 | `"<unityProjectPath>/Library/mcp-server/linux-arm64/gamedev-mcp-server" port=<port> client-transport=stdio` |

Then add it:

```bash
claude mcp add ai-game-developer <command>    # Claude Code
gemini mcp add ai-game-developer <command>    # Gemini CLI
```

GitHub Copilot CLI: run `copilot`, then `/mcp add` with server name `ai-game-developer`, type `local` and the `<command>` above.

> In VS Code with Copilot, make sure **Agent** mode is enabled.

## Team settings: update notifications

The plugin shows an update popup at Editor startup when OpenUPM has a newer version. *"Do not show again"* is a per-user setting. To turn the popup off for the **whole team**, open **Edit ▸ Project Settings ▸ AI Game Developer** and enable *"Disable update notifications for the entire team"*. The setting is saved to `ProjectSettings/AI-Game-Developer-UpdateSettings.asset`; commit that file. The same toggle is in **Tools ▸ AI Game Developer ▸ Updates ▸ Disable Update Notifications (Team)**.

## Uninstall

1. Open **Window ▸ Package Manager**, select **AI Game Developer — MCP**, and click **Remove** (or run `unity-mcp-cli remove-plugin ./MyUnityProject`).
2. Close Unity, then delete the `Assets/Plugins/NuGet` folder and its `Assets/Plugins/NuGet.meta` file.

## Next

- [Default MCP tools](default-mcp-tools.md)
- [Custom tools and runtime](custom-tools.md)
- [MCP server and connection](mcp-server.md)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
