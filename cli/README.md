<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npm package')](https://www.npmjs.com/package/unity-mcp-cli)
[![Node.js](https://img.shields.io/badge/Node.js-%5E20.19.0%20%7C%7C%20%3E%3D22.12.0-5FA04E?logo=nodedotjs&labelColor=333A41 'Node.js')](https://nodejs.org/)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner-glitch.gif" alt="AI Game Developer" title="Unity MCP CLI" width="100%">

  <p>
    <a href="https://claude.ai/download"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/claude-64.png" alt="Claude" title="Claude" height="36"></a>&nbsp;&nbsp;
    <a href="https://openai.com/index/introducing-codex/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/codex-64.png" alt="Codex" title="Codex" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.cursor.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/cursor-64.png" alt="Cursor" title="Cursor" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/docs/copilot/overview"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/github-copilot-64.png" alt="GitHub Copilot" title="GitHub Copilot" height="36"></a>&nbsp;&nbsp;
    <a href="https://gemini.google.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/gemini-64.png" alt="Gemini" title="Gemini" height="36"></a>&nbsp;&nbsp;
    <a href="https://antigravity.google/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/antigravity-64.png" alt="Antigravity" title="Antigravity" height="36"></a>&nbsp;&nbsp;
    <a href="https://code.visualstudio.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/vs-code-64.png" alt="VS Code" title="VS Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://www.jetbrains.com/rider/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/rider-64.png" alt="Rider" title="Rider" height="36"></a>&nbsp;&nbsp;
    <a href="https://visualstudio.microsoft.com/"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/visual-studio-64.png" alt="Visual Studio" title="Visual Studio" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/anthropics/claude-code"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/open-code-64.png" alt="Open Code" title="Open Code" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/cline/cline"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/cline-64.png" alt="Cline" title="Cline" height="36"></a>&nbsp;&nbsp;
    <a href="https://github.com/Kilo-Org/kilocode"><img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/mcp-clients/kilo-code-64.png" alt="Kilo Code" title="Kilo Code" height="36"></a>
  </p>

</div>

<b>[中文](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.zh-CN.md) | [日本語](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.ja.md) | [Español](https://github.com/IvanMurzak/Unity-MCP/blob/main/cli/docs/README.es.md)</b>

Cross-platform CLI tool for **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** — create projects, install plugins, configure MCP tools, and launch Unity with active MCP connections. All from a single command line.

## ![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.svg?raw=true)

- :white_check_mark: **Create projects** — scaffold new Unity projects via Unity Editor
- :white_check_mark: **Install editors** — install any Unity Editor version from the command line
- :white_check_mark: **Install plugin** — add Unity-MCP plugin to `manifest.json` with all required scoped registries
- :white_check_mark: **Remove plugin** — remove Unity-MCP plugin from `manifest.json`
- :white_check_mark: **Configure** — enable/disable MCP tools, prompts, and resources
- :white_check_mark: **Status check** — see Unity process, local server, and cloud server connection status at a glance
- :white_check_mark: **Run tools** — execute MCP tools directly from the command line
- :white_check_mark: **Setup MCP** — write AI agent MCP config files for any of 14 supported agents
- :white_check_mark: **Setup skills** — generate skill files for AI agents via the MCP server
- :white_check_mark: **Wait for ready** — poll until Unity Editor and MCP server are connected and accepting tool calls
- :white_check_mark: **Open & Connect** — launch Unity with optional MCP environment variables for automated server connection
- :white_check_mark: **Cross-platform** — Windows, macOS, and Linux
- :white_check_mark: **CI-friendly** — auto-detects non-interactive terminals and disables spinners/colors
- :white_check_mark: **Verbose mode** — use `--verbose` on any command for detailed diagnostic output
- :white_check_mark: **Version-aware** — never downgrades plugin versions, resolves latest from OpenUPM

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Quick Start

Install globally and run:

```bash
# 1.1 Install unity-mcp-cli                                #  ┌────────────────────┐
npm install -g unity-mcp-cli                               #  │ Available AI agent │
                                                           #  ├────────────────────┤
# 1.2 (Optional) Install Unity                             #  │ antigravity        │
unity-mcp-cli install-unity                                #  │ claude-code        │
                                                           #  │ claude-desktop     │
# 1.3 (Optional) Create Unity project                      #  │ cline              │
unity-mcp-cli create-project ./MyUnityProject              #  │ codex              │
                                                           #  │ cursor             │
# 2. Install "AI Game Developer" in Unity project          #  │ gemini             │
unity-mcp-cli install-plugin ./MyUnityProject              #  │ github-copilot-cli │
                                                           #  │ kilo-code          │
# 3. Login to cloud server                                 #  │ open-code          │
unity-mcp-cli login ./MyUnityProject                       #  │ rider-junie        │
                                                           #  │ unity-ai           │
# 4. Open Unity project (auto-connects and generates skills)  │ vs-copilot         │
unity-mcp-cli open ./MyUnityProject                        #  │ vscode-copilot     │
                                                           #  └────────────────────┘
# 5. Wait for Unity Editor to be ready
unity-mcp-cli wait-for-ready ./MyUnityProject
```

Or run any command instantly with `npx` — no global installation required:

```bash
npx unity-mcp-cli install-plugin /path/to/unity/project
```

> **Requirements:** [Node.js](https://nodejs.org/) ^20.19.0 || >=22.12.0. [Unity Hub](https://unity.com/download) is installed automatically if not found.

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Contents

- [Quick Start](#quick-start)
- [Contents](#contents)
- [Commands](#commands)
  - [`configure`](#configure)
  - [`create-project`](#create-project)
  - [`install-plugin`](#install-plugin)
  - [`install-unity`](#install-unity)
  - [`open`](#open)
  - [`run-tool`](#run-tool)
  - [`wait-for-ready`](#wait-for-ready)
  - [`setup-mcp`](#setup-mcp)
  - [`setup-skills`](#setup-skills)
  - [`remove-plugin`](#remove-plugin)
  - [`status`](#status)
  - [Global Options](#global-options)
- [Full Automation Example](#full-automation-example)
- [How It Works](#how-it-works)
    - [Deterministic Port](#deterministic-port)
    - [Plugin Installation](#plugin-installation)
    - [Configuration File](#configuration-file)
    - [Unity Hub Integration](#unity-hub-integration)

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Commands

## `configure`

Configure MCP tools, prompts, and resources in `UserSettings/AI-Game-Developer-Config.json`.

```bash
unity-mcp-cli configure ./MyGame --list
```

| Option | Required | Description |
|---|---|---|
| `[path]` | Yes | Path to the Unity project (positional or `--path`) |
| `--list` | No | List current configuration and exit |
| `--enable-tools <names>` | No | Enable specific tools (comma-separated) |
| `--disable-tools <names>` | No | Disable specific tools (comma-separated) |
| `--enable-all-tools` | No | Enable all tools |
| `--disable-all-tools` | No | Disable all tools |
| `--enable-prompts <names>` | No | Enable specific prompts (comma-separated) |
| `--disable-prompts <names>` | No | Disable specific prompts (comma-separated) |
| `--enable-all-prompts` | No | Enable all prompts |
| `--disable-all-prompts` | No | Disable all prompts |
| `--enable-resources <names>` | No | Enable specific resources (comma-separated) |
| `--disable-resources <names>` | No | Disable specific resources (comma-separated) |
| `--enable-all-resources` | No | Enable all resources |
| `--disable-all-resources` | No | Disable all resources |

**Example — enable specific tools and disable all prompts:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**Example — enable everything:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `create-project`

Create a new Unity project using the Unity Editor.

```bash
unity-mcp-cli create-project /path/to/new/project
```

| Option | Required | Description |
|---|---|---|
| `[path]` | Yes | Path where the project will be created (positional or `--path`) |
| `--unity <version>` | No | Unity Editor version to use (defaults to highest installed) |

**Example — create a project with a specific editor version:**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

Install the Unity-MCP plugin into a Unity project's `Packages/manifest.json`.

```bash
unity-mcp-cli install-plugin ./MyGame
```

| Option | Required | Description |
|---|---|---|
| `[path]` | Yes | Path to the Unity project (positional or `--path`) |
| `--plugin-version <version>` | No | Plugin version to install (defaults to latest from [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)) |

This command:
1. Adds the **OpenUPM scoped registry** with all required scopes
2. Adds `com.ivanmurzak.unity.mcp` to `dependencies`
3. **Never downgrades** — if a higher version is already installed, it is preserved

**Example — install a specific plugin version:**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

> After running this command, open the project in Unity Editor to complete the package installation.

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-unity`

Install a Unity Editor version via Unity Hub CLI.

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| Argument / Option | Required | Description |
|---|---|---|
| `[version]` | No | Unity Editor version to install (e.g. `6000.3.1f1`) |
| `--path <path>` | No | Read the required version from an existing project |

If neither argument nor option is provided, the command installs the latest stable release from Unity Hub's releases list.

**Example — install the editor version that a project needs:**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

Open a Unity project in the Unity Editor. By default, sets MCP connection environment variables if connection options are provided. Use `--no-connect` to open without MCP connection.

```bash
# Explicit path
unity-mcp-cli open ./MyGame

# From inside the Unity project folder — path defaults to the current directory
cd ./MyGame && unity-mcp-cli open
```

| Option | Env Variable | Required | Description |
|---|---|---|---|
| `[path]` | — | No | Path to the Unity project (positional or `--path`). Defaults to the current working directory. |
| `--unity <version>` | — | No | Specific Unity Editor version to use (defaults to version from project settings, falls back to highest installed) |
| `--no-connect` | — | No | Open without MCP connection environment variables |
| `--url <url>` | `UNITY_MCP_HOST` | No | MCP server URL to connect to |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | No | Force keep the connection alive |
| `--token <token>` | `UNITY_MCP_TOKEN` | No | Authentication token |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | No | Auth mode: `none` or `required` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | No | Comma-separated list of tools to enable |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | No | Transport method: `streamableHttp` or `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | No | Set to `true` or `false` to control MCP server auto-start |

The editor process is spawned in detached mode — the CLI returns immediately.

**Example — open with MCP connection:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**Example — open without MCP connection (simple open):**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**Example — open with authentication and specific tools:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --tools gameobject-create,gameobject-find
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `run-tool`

Execute an MCP tool directly via the HTTP API. The server URL and authorization token are **automatically resolved** from the project's config file (`UserSettings/AI-Game-Developer-Config.json`), based on the current connection mode (Custom or Cloud).

```bash
unity-mcp-cli run-tool gameobject-create ./MyGame --input '{"name":"Cube"}'
```

| Option | Required | Description |
|---|---|---|
| `<tool-name>` | Yes | Name of the MCP tool to execute |
| `[path]` | No | Unity project path (positional or `--path`) — used to read config and detect port |
| `--url <url>` | No | Direct server URL override (bypasses config) |
| `--token <token>` | No | Bearer token override (bypasses config) |
| `--input <json>` | No | JSON string of tool arguments (defaults to `{}`) |
| `--input-file <file>` | No | Read JSON arguments from a file |
| `--raw` | No | Output raw JSON (no formatting, no spinner) |
| `--timeout <ms>` | No | Request timeout in milliseconds (default: 60000) |

**URL resolution priority:**
1. `--url` → use directly
2. Config file → `host` (Custom mode) or hardcoded cloud endpoint (Cloud mode)
3. Deterministic port from project path

**Authorization** is read automatically from the project config (`token` in Custom mode, `cloudToken` in Cloud mode). Use `--token` to override the config-derived token explicitly.

**Example — call a tool (URL and auth from config):**

```bash
unity-mcp-cli run-tool gameobject-find ./MyGame --input '{"query":"Player"}'
```

**Example — explicit URL override:**

```bash
unity-mcp-cli run-tool scene-save --url http://localhost:8080
```

**Example — pipe raw JSON output:**

```bash
unity-mcp-cli run-tool assets-list ./MyGame --raw | jq '.results'
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `wait-for-ready`

Wait until Unity Editor and MCP server are connected and ready to accept tool calls. Polls the server at a configurable interval until it responds successfully or the timeout is reached. Useful for automation scripts and AI agent orchestration where `open` launches Unity but the agent needs to know when it can start calling tools.

```bash
unity-mcp-cli wait-for-ready ./MyGame
```

| Option | Required | Description |
|---|---|---|
| `[path]` | No | Unity project path (positional or `--path`) — used to read config and detect port |
| `--url <url>` | No | Direct server URL override (bypasses config) |
| `--token <token>` | No | Bearer token override (bypasses config) |
| `--timeout <ms>` | No | Maximum time to wait in milliseconds (default: 120000) |
| `--interval <ms>` | No | Polling interval in milliseconds (default: 3000) |

**Example — wait with default timeout (120s):**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
```

**Example — shorter timeout for CI:**

```bash
unity-mcp-cli wait-for-ready ./MyGame --timeout 60000 --interval 2000
```

**Example — explicit server URL:**

```bash
unity-mcp-cli wait-for-ready --url http://localhost:8080 --timeout 30000
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `team`

Manage a local **Unity project session lifecycle**. The command surface is **runtime-backed** so Unity-MCP can keep one project-facing lifecycle contract while choosing an OS-appropriate local backend internally. The product goal is **standalone Unity lifecycle management first**, not OMX parity or a generic orchestration replacement.

What this feature is for:

- launch and inspect a project-scoped local session
- persist project-local session state under `.unity-mcp/team-state/`
- reconcile saved state with live runtime state
- keep the core lifecycle usable **without OMX installed**

What this feature is not:

- a replacement for `omx team`
- a generic worker task-dispatch/mailbox system
- remote/cloud or multi-machine orchestration

Helpful references:

- manual verification: [`cli/docs/team-testing-guide.md`](docs/team-testing-guide.md)
- runtime architecture + Windows backend evaluation: [`cli/docs/team-runtime-architecture.md`](docs/team-runtime-architecture.md)

```bash
unity-mcp-cli team launch ./MyGame
```

### `team launch`

Create a local Unity project session with the built-in `leader`, `builder`, `verifier`, and `notes` roles. The runtime chooses the local backend automatically; tmux remains the current pane-oriented backend, while a standalone process backend can satisfy the same lifecycle contract without pane semantics.

| Option | Required | Description |
|---|---|---|
| `[path]` | No | Unity project path (defaults to current directory) |
| `--path <path>` | No | Unity project path (defaults to current directory) |
| `--layout <name>` | No | Layout preset name (`default` only in milestone 1) |
| `--session-name <name>` | No | Override the generated team session name |

### `team status`

Inspect persisted project session state and reconcile it with the live runtime session.

```bash
unity-mcp-cli team status ./MyGame
unity-mcp-cli team status mygame-team-20260423t050000z --path ./MyGame
```

### `team list`

List saved project sessions and summarize whether each session is `ready`, `degraded`, or `stopped`.

```bash
unity-mcp-cli team list ./MyGame
```

### `team stop`

Stop a project session and mark its saved state as `stopped`.

```bash
unity-mcp-cli team stop mygame-team-20260423t050000z --path ./MyGame
```

If the currently selected runtime is not installed or is missing from `PATH`, the launcher exits with an actionable error. On tmux-oriented systems that usually means `tmux` must be installed before using that backend. OMX remains optional: use it only if you want additional generic multi-agent coordination on top of Unity-MCP's standalone lifecycle flow.

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-mcp`

Write MCP config files for AI agents, enabling headless/CI setup without the Unity Editor UI. Supports all 14 agents (Claude Code, Cursor, Gemini, Codex, etc.).

Note: Codex currently reads MCP configuration from `~/.codex/config.toml`, so `setup-mcp codex` writes to that user-scoped file rather than a project-local `.codex/config.toml`.

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| Option | Required | Description |
|---|---|---|
| `[agent-id]` | Yes | Agent to configure (use `--list` to see all) |
| `[path]` | No | Unity project path (defaults to cwd) |
| `--transport <transport>` | No | Transport method: `stdio` or `http` (default: `http`) |
| `--url <url>` | No | Server URL override (for http transport) |
| `--token <token>` | No | Auth token override |
| `--list` | No | List all available agent IDs |

**Example — list all supported agents:**

```bash
unity-mcp-cli setup-mcp --list
```

**Example — configure Cursor with stdio transport:**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `setup-skills`

Generate skill files for an AI agent by calling the MCP server's system tool API. Requires Unity Editor to be running with the MCP plugin installed.

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| Option | Required | Description |
|---|---|---|
| `[agent-id]` | Yes | Agent to generate skills for (use `--list` to see all) |
| `[path]` | No | Unity project path (defaults to cwd) |
| `--url <url>` | No | Server URL override |
| `--token <token>` | No | Auth token override |
| `--list` | No | List all agents with skills support status |
| `--timeout <ms>` | No | Request timeout in milliseconds (default: 60000) |

**Example — list agents with skills support:**

```bash
unity-mcp-cli setup-skills --list
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `remove-plugin`

Remove the Unity-MCP plugin from a Unity project's `Packages/manifest.json`.

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| Option | Required | Description |
|---|---|---|
| `[path]` | Yes | Path to the Unity project (positional or `--path`) |

This command:
1. Removes `com.ivanmurzak.unity.mcp` from `dependencies`
2. **Preserves scoped registries and scopes** — other packages may depend on them
3. **No-op** if the plugin is not installed

> After running this command, open the project in Unity Editor to apply the change.

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `status`

Check Unity Editor and MCP server connection status. Shows whether Unity is running, whether the local MCP server is reachable, and whether the config-resolved server (e.g., cloud) is reachable.

```bash
unity-mcp-cli status ./MyGame
```

| Option | Required | Description |
|---|---|---|
| `[path]` | No | Unity project path (positional or `--path`) |
| `--url <url>` | No | Direct server URL override (bypasses config) |
| `--token <token>` | No | Bearer token override (bypasses config) |
| `--timeout <ms>` | No | Probe timeout in milliseconds (default: 5000) |

**Example output:**

```
Unity-MCP Status
  Project: /path/to/MyGame
──────────────────────────────────────────────────
Unity Editor Process
✔  Unity is running (PID: 53740)
Local MCP Server
  URL: http://localhost:22958
✖  Not available (connection refused)
Config Server
  URL: https://ai-game.dev/mcp
✔  Connected
──────────────────────────────────────────────────
✔  MCP server is reachable — ready for tool calls
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## Global Options

These options are available on all commands:

| Option | Description |
|---|---|
| `-v, --verbose` | Enable verbose diagnostic output for troubleshooting |
| `--version` | Display CLI version |
| `--help` | Display help for the command |

**Example — run any command with verbose output:**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Full Automation Example

Set up a complete Unity MCP project from scratch in one script:

```bash
# 1. Create a new Unity project
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Install the Unity-MCP plugin
unity-mcp-cli install-plugin ./MyAIGame

# 3. Enable all MCP tools
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Login to cloud server (authenticates and saves token)
unity-mcp-cli login ./MyAIGame

# 5. Open the project (auto-connects and generates skills for claude-code)
unity-mcp-cli open ./MyAIGame

# 6. Wait for Unity Editor and MCP server to be ready
unity-mcp-cli wait-for-ready ./MyAIGame

# 7. Run tests to verify everything works
unity-mcp-cli run-tool tests-run ./MyAIGame --input '{"testMode":"EditMode"}'
```

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# How It Works

### Deterministic Port

The CLI generates a **deterministic port** for each Unity project based on its directory path (SHA256 hash mapped to port range 20000–29999). This matches the port generation in the Unity plugin, ensuring the server and plugin automatically agree on the same port without manual configuration.

### Plugin Installation

The `install-plugin` command modifies `Packages/manifest.json` directly:
- Adds the [OpenUPM](https://openupm.com/) scoped registry (`package.openupm.com`)
- Registers all required scopes (`com.ivanmurzak`, `extensions.unity`, `org.nuget.*`)
- Adds the `com.ivanmurzak.unity.mcp` dependency with version-aware updates (never downgrades)

### Configuration File

The `configure` command reads and writes `UserSettings/AI-Game-Developer-Config.json`, which controls:
- **Tools** — MCP tools available to AI agents
- **Prompts** — pre-defined prompts injected into LLM conversations
- **Resources** — read-only data exposed to AI agents
- **Connection settings** — host URL, auth token, transport method, timeouts

### Unity Hub Integration

Commands that manage editors or create projects use the **Unity Hub CLI** (`--headless` mode). If Unity Hub is not installed, the CLI **downloads and installs it automatically**:
- **Windows** — silent install via `UnityHubSetup.exe /S` (may require administrator privileges)
- **macOS** — downloads the DMG, mounts it, and copies `Unity Hub.app` to `/Applications`
- **Linux** — downloads `UnityHub.AppImage` to `~/Applications/`

> For the full Unity-MCP project documentation, see the [main README](https://github.com/IvanMurzak/Unity-MCP/blob/main/README.md).

![AI Game Developer — Unity SKILLS and MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
