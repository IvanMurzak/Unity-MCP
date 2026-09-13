# Support And Debugging

Use this document when the extension behaves incorrectly, Unity behaves unexpectedly during setup, or you need to prepare a good bug report.

## Collect The Right Information First

Always capture:

- VS Code version
- extension version
- OS version
- whether the workspace was trusted
- whether the Unity MCP package was already installed
- the relevant `Unity MCP` output channel logs

If the problem involves Unity launch or Unity closing unexpectedly, also capture:

- Unity editor version
- whether the launch was plain or MCP-connected
- whether it was the first Unity open after installing the package
- Unity `Editor.log`

On macOS, Unity `Editor.log` is usually:

```text
~/Library/Logs/Unity/Editor.log
```

## Extension Logs

Open the logs with:

- `Unity MCP: Show Output`

Increase detail with the VS Code setting:

- `unityMcp.logLevel = debug`

Important log groups:

- `activate:*`
  extension activation and command registration
- `status:*`
  workspace inspection and diagnostics
- `dashboard:*`
  dashboard resolve, render, refresh, and button routing
- `configure:*`
  `.vscode/mcp.json` generation and write results
- `pluginInstall:*`
  `Packages/manifest.json` install flow
- `openUnity:*`
  Unity launch mode, warnings, and user-facing fallbacks
- `cliAdapter:*`
  calls into the shared `unity-mcp-cli` implementation

## Common Problems

### The Dashboard Is Empty Or Missing

Check:

1. The `Unity MCP` Activity Bar icon exists.
2. `Unity MCP: Show Output` shows `dashboard:viewResolved`.
3. The output also shows `dashboard:render`.

If the icon exists but the dashboard still does not render, reload the VS Code window and retry. Capture the `dashboard:*` log lines.

### The Workspace Is Read-Only

If the extension refuses to install, configure, or launch Unity, the workspace is probably not trusted. Trust the workspace in VS Code and retry.

Typical log hint:

- `workspace-not-trusted`

### Install Plugin Worked But Connected Launch Does Not

This usually means Unity has not created `UserSettings/AI-Game-Developer-Config.json` yet.

Do this:

1. Open Unity once without MCP.
2. Let package import and compilation finish.
3. Retry `Open Unity With MCP`.

Typical log hint:

- `project-config-missing`

### Configure Project Worked But MCP Is Still Not Ready

Inspect:

- `.vscode/mcp.json`
- `UserSettings/AI-Game-Developer-Config.json`
- the `Unity MCP Status` report in the output channel

The most common causes are:

- Unity project config not initialized yet
- stale workspace state before a refresh
- editing the generated config manually into an invalid state

Run `Unity MCP: Check Status` again after refreshing or reopening the workspace.

### Unity Closed Unexpectedly During Setup

This extension should not need Unity to stay open while writing `.vscode/mcp.json`, so an editor close during `Configure Project` is suspicious and worth reporting if it repeats.

If it happens again:

1. Capture the `Unity MCP` output channel logs.
2. Capture Unity `Editor.log`.
3. Note the exact action:
   - `Install Plugin`
   - `Configure Project`
   - `Open Unity`
   - `Open Unity With MCP`
4. Note whether Unity had just finished importing the package.

If it only happens once and the project then behaves normally, keep the note but do not assume the extension caused it directly.

## Files To Inspect Manually

If you need to verify state by hand, check:

- `Packages/manifest.json`
- `.vscode/mcp.json`
- `UserSettings/AI-Game-Developer-Config.json`

## Reporting A Bug

Open an issue in the main repository:

- [Unity-MCP Issues](https://github.com/IvanMurzak/Unity-MCP/issues)

A strong bug report includes:

- exact repro steps
- exact command or dashboard button used
- output channel logs
- Unity `Editor.log` if Unity was involved
- whether the failure reproduces after reloading VS Code or reopening Unity
