<div align="center" width="100%">
  <h1>Unity MCP — <i>CLI</i></h1>

[![npm](https://img.shields.io/npm/v/unity-mcp-cli?label=npm&labelColor=333A41 'npm package')](https://www.npmjs.com/package/unity-mcp-cli)
[![Node.js](https://img.shields.io/badge/Node.js-%3E%3D18-5FA04E?logo=nodedotjs&labelColor=333A41 'Node.js')](https://nodejs.org/)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

  <img src="https://github.com/IvanMurzak/Unity-MCP/raw/main/docs/img/promo/ai-developer-banner-glitch.gif" alt="AI Game Developer" title="Unity MCP CLI" width="100%">

</div>

Cross-platform CLI tool for **[Unity MCP](https://github.com/IvanMurzak/Unity-MCP)** — create projects, install plugins, configure MCP tools, and launch Unity with active MCP connections. All from a single command line.

## ![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-features.svg?raw=true)

- :white_check_mark: **Create projects** — scaffold new Unity projects via Unity Hub
- :white_check_mark: **Install editors** — install any Unity Editor version from the command line
- :white_check_mark: **Install plugin** — add Unity-MCP plugin to `manifest.json` with all required scoped registries
- :white_check_mark: **Configure** — enable/disable MCP tools, prompts, and resources
- :white_check_mark: **Connect** — launch Unity with MCP environment variables for automated server connection
- :white_check_mark: **Cross-platform** — Windows, macOS, and Linux
- :white_check_mark: **Version-aware** — never downgrades plugin versions, resolves latest from OpenUPM

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Quick Start

Run any command instantly with `npx` — no installation required:

```bash
npx unity-mcp-cli install-plugin --project-path /path/to/unity/project
```

Or install globally:

```bash
npm install -g unity-mcp-cli
unity-mcp install-plugin --project-path /path/to/unity/project
```

> **Requirements:** [Node.js](https://nodejs.org/) >= 18 and [Unity Hub](https://unity.com/download) (for project/editor commands)

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Contents

- [Quick Start](#quick-start)
- [Commands](#commands)
  - [`create-project`](#create-project) — Create a new Unity project
  - [`install-editor`](#install-editor) — Install Unity Editor via Unity Hub
  - [`open`](#open) — Open a Unity project in the Editor
  - [`install-plugin`](#install-plugin) — Install Unity-MCP plugin into a project
  - [`configure`](#configure) — Configure MCP tools, prompts, and resources
  - [`connect`](#connect) — Launch Unity with MCP connection
- [Full Automation Example](#full-automation-example)
- [How It Works](#how-it-works)

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Commands

## `create-project`

Create a new Unity project using Unity Hub.

```bash
npx unity-mcp-cli create-project --path /path/to/new/project
```

| Option | Required | Description |
|---|---|---|
| `--path <path>` | Yes | Path where the project will be created |
| `--editor-version <version>` | No | Unity Editor version to use (defaults to latest installed) |

**Example — create a project with a specific editor version:**

```bash
npx unity-mcp-cli create-project --path ./MyGame --editor-version 2022.3.62f1
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-editor`

Install a Unity Editor version via Unity Hub CLI.

```bash
npx unity-mcp-cli install-editor --version 6000.3.1f1
```

| Option | Required | Description |
|---|---|---|
| `--version <version>` | No | Unity Editor version to install |
| `--project-path <path>` | No | Read the required version from an existing project |

If neither option is provided, the command lists currently installed editors.

**Example — install the editor version that a project needs:**

```bash
npx unity-mcp-cli install-editor --project-path ./MyGame
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `open`

Open a Unity project in the Unity Editor.

```bash
npx unity-mcp-cli open --project-path ./MyGame
```

| Option | Required | Description |
|---|---|---|
| `--project-path <path>` | Yes | Path to the Unity project |
| `--editor-version <version>` | No | Specific Unity Editor version to use (defaults to version from project settings) |

The editor process is spawned in detached mode — the CLI returns immediately.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `install-plugin`

Install the Unity-MCP plugin into a Unity project's `Packages/manifest.json`.

```bash
npx unity-mcp-cli install-plugin --project-path ./MyGame
```

| Option | Required | Description |
|---|---|---|
| `--project-path <path>` | Yes | Path to the Unity project |
| `--plugin-version <version>` | No | Plugin version to install (defaults to latest from [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)) |

This command:
1. Adds the **OpenUPM scoped registry** with all required scopes
2. Adds `com.ivanmurzak.unity.mcp` to `dependencies`
3. **Never downgrades** — if a higher version is already installed, it is preserved

**Example — install a specific plugin version:**

```bash
npx unity-mcp-cli install-plugin --project-path ./MyGame --plugin-version 0.51.6
```

> After running this command, open the project in Unity Editor to complete the package installation.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `configure`

Configure MCP tools, prompts, and resources in `UserSettings/AI-Game-Developer-Config.json`.

```bash
npx unity-mcp-cli configure --project-path ./MyGame --list
```

| Option | Required | Description |
|---|---|---|
| `--project-path <path>` | Yes | Path to the Unity project |
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
npx unity-mcp-cli configure --project-path ./MyGame \
  --enable-tools gameobject-create,gameobject-find \
  --disable-all-prompts
```

**Example — enable everything:**

```bash
npx unity-mcp-cli configure --project-path ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

## `connect`

Open a Unity project and connect it to a specific MCP server via environment variables.

```bash
npx unity-mcp-cli connect \
  --project-path ./MyGame \
  --url http://localhost:8080
```

| Option | Required | Description |
|---|---|---|
| `--project-path <path>` | Yes | Path to the Unity project |
| `--url <url>` | Yes | MCP server URL to connect to |
| `--tools <names>` | No | Comma-separated list of tools to enable |
| `--token <token>` | No | Authentication token |
| `--auth <option>` | No | Auth mode: `none` or `required` |
| `--keep-connected` | No | Force keep the connection alive |
| `--editor-version <version>` | No | Specific Unity Editor version to use |

This command launches the Unity Editor with MCP environment variables (`UNITY_MCP_HOST`, `UNITY_MCP_TOOLS`, `UNITY_MCP_TOKEN`, etc.) so the plugin connects automatically on startup.

**Example — connect with authentication and specific tools:**

```bash
npx unity-mcp-cli connect \
  --project-path ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth required \
  --keep-connected \
  --tools gameobject-create,gameobject-find,script-execute
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# Full Automation Example

Set up a complete Unity MCP project from scratch in one script:

```bash
# 1. Create a new Unity project
npx unity-mcp-cli create-project --path ./MyAIGame --editor-version 6000.3.1f1

# 2. Install the Unity-MCP plugin
npx unity-mcp-cli install-plugin --project-path ./MyAIGame

# 3. Enable all MCP tools
npx unity-mcp-cli configure --project-path ./MyAIGame --enable-all-tools

# 4. Open the project with MCP connection
npx unity-mcp-cli connect \
  --project-path ./MyAIGame \
  --url http://localhost:8080 \
  --keep-connected
```

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)

# How It Works

### Deterministic Port

The CLI generates a **deterministic port** for each Unity project based on its directory path (SHA256 hash mapped to port range 50000–59999). This matches the port generation in the Unity plugin, ensuring the server and plugin automatically agree on the same port without manual configuration.

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

Commands that manage editors or create projects use the **Unity Hub CLI** (`--headless` mode). The CLI automatically detects Unity Hub's installation path across all platforms.

![AI Game Developer — Unity MCP](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/promo/hazzard-divider.svg?raw=true)
