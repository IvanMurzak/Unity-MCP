# Unity MCP VS Code Extension

This folder contains the VS Code extension for Unity MCP. The extension is designed to reduce manual project setup inside VS Code while keeping file writes and Unity launch behavior explicit and easy to debug.

## What The Extension Does

- shows a `Unity MCP` dashboard in the Activity Bar
- shows project readiness in the VS Code status bar
- detects whether the current workspace looks like a Unity project
- detects whether the Unity MCP package is installed
- detects whether the Unity MCP project config exists
- generates or updates `.vscode/mcp.json`
- installs the Unity MCP package into `Packages/manifest.json`
- launches Unity in plain mode or with MCP connection settings

## Safety Model

- untrusted workspaces are read-only
- file mutations only happen after an explicit command or button click
- Unity launch is explicit and never automatic
- logs are structured and do not include tokens or generated config bodies

## Install A Local VSIX

```bash
cd vscode-extension
npm install
npm run build
npm test
npm run package:vsix
```

Then install `unity-mcp-vscode-0.0.1.vsix` in VS Code:

1. Open the Extensions view.
2. Open the `...` menu in the top-right.
3. Choose `Install from VSIX...`.
4. Select the generated `.vsix` file from this folder.

## Normal Usage

### Recommended First-Time Flow

1. Open a Unity project in VS Code.
2. Trust the workspace if VS Code asks.
3. Open the `Unity MCP` view from the Activity Bar or click the bottom status bar item.
4. If the dashboard says `Install`, run `Install Plugin`.
5. Open Unity once and let the package import and compile.
6. If the dashboard says `Configure`, run `Configure Project`.
7. Run `Check Status` or use the dashboard refresh action.
8. When the dashboard says `Ready`, use `Open Unity With MCP`.

### Dashboard States

- `Install`
  The Unity MCP package is not installed in `Packages/manifest.json`.
- `Init`
  The package is installed, but `UserSettings/AI-Game-Developer-Config.json` does not exist yet. Open Unity once without MCP so the package can initialize.
- `Configure`
  The Unity-side config exists, but `.vscode/mcp.json` is missing or incomplete.
- `Ready`
  The project looks ready for connected Unity MCP use.

### Commands

- `Unity MCP: Check Status`
  Recomputes the current workspace status and prints a full report to the `Unity MCP` output channel.
- `Unity MCP: Configure Project`
  Writes or updates `.vscode/mcp.json` using the shared `unity-mcp-cli` setup logic.
- `Unity MCP: Install Plugin`
  Updates `Packages/manifest.json` to include `com.ivanmurzak.unity.mcp`.
- `Unity MCP: Open Unity`
  Lets you choose plain launch or connected launch.
- `Unity MCP: Show Dashboard`
  Opens the Activity Bar dashboard.
- `Unity MCP: Show Output`
  Opens the `Unity MCP` output channel.

## Files The Extension Touches

Read-only checks:

- `Packages/manifest.json`
- `.vscode/mcp.json`
- `UserSettings/AI-Game-Developer-Config.json`
- workspace trust state
- Unity project markers such as `Assets/` and `ProjectSettings/`

Mutations:

- `Install Plugin` updates `Packages/manifest.json`
- `Configure Project` writes `.vscode/mcp.json`
- `Open Unity` launches the Unity editor but does not rewrite project files by itself

## Debugging And Troubleshooting

Start here:

1. Run `Unity MCP: Show Output`.
2. Set `unityMcp.logLevel` to `debug` if you need more detail.
3. Re-run the failing action.
4. Capture the `Unity MCP` output channel logs.

Common logs:

- `status:*`
  workspace detection and diagnostics
- `configure:*`
  VS Code MCP config generation
- `pluginInstall:*`
  Unity package installation
- `openUnity:*`
  Unity launch flow
- `dashboard:*`
  Activity Bar dashboard lifecycle and button actions
- `cliAdapter:*`
  calls into the shared `unity-mcp-cli` library

If Unity behaves unexpectedly, also capture Unity editor logs. On macOS, the main editor log is usually:

```text
~/Library/Logs/Unity/Editor.log
```

More debugging guidance and issue-report checklists are in [SUPPORT.md](./SUPPORT.md).

## Change The Extension

For local development:

```bash
cd vscode-extension
npm install
npm run build
npm test
```

Then open the `vscode-extension/` folder in VS Code and press `F5` to launch an Extension Development Host.

Detailed maintainer guidance is in:

- [DEVELOPMENT.md](./DEVELOPMENT.md)
- [SUPPORT.md](./SUPPORT.md)
- [PUBLISHING.md](./PUBLISHING.md)

## Release Handoff

This repo is prepared for local VSIX packaging, but not for publishing from a personal account. The current `publisher` value is a placeholder for packaging and handoff only.
