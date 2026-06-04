# Unity MCP VS Code Extension

This package is the isolated VS Code extension workspace for GitHub issue `#613`.

It is currently a preview extension focused on local project setup and diagnostics for Unity MCP. It does not publish telemetry and it only performs file mutations or Unity launch actions after explicit user action in a trusted workspace.

## Current Capabilities

- `Unity MCP` Activity Bar dashboard with:
  - current workspace setup state
  - recommended next step
  - action buttons for install, configure, launch, refresh, and logs
- bottom status bar item that reflects project readiness and opens the dashboard
- `Unity MCP: Check Status`
- `Unity MCP: Configure Project`
- `Unity MCP: Install Plugin`
- `Unity MCP: Open Unity`
- `Unity MCP: Show Output`

## Safety Rules

- Untrusted workspaces stay read-only.
- File mutations always require an explicit command or button click.
- Unity launch is explicit and never automatic.
- Logs are structured and do not include auth tokens or generated config bodies.

## Local Development

```bash
cd vscode-extension
npm install
npm run build
npm test
```

Then open `/Users/suporte/Unity-MCP/vscode-extension` in VS Code and press `F5`.

## Package A VSIX Locally

```bash
cd vscode-extension
npm install
npm run package:vsix
```

This produces a local `.vsix` package in the extension folder.

To install it in VS Code:

1. Open the Extensions view.
2. Click the `...` menu in the top-right.
3. Choose `Install from VSIX...`.
4. Select the generated `unity-mcp-vscode-0.0.1.vsix`.

## Release Handoff

This repo is prepared for local VSIX packaging, but not for publishing from a personal account.

- Maintainer handoff steps: [PUBLISHING.md](/Users/suporte/Unity-MCP/vscode-extension/PUBLISHING.md)
- Support guidance: [SUPPORT.md](/Users/suporte/Unity-MCP/vscode-extension/SUPPORT.md)

## Manual Validation Matrix

### Dashboard and Status Bar

1. Open a Unity project in VS Code.
2. Confirm the `Unity MCP` icon appears in the Activity Bar.
3. Confirm the bottom status bar item appears and changes by project state:
   - `Install`
   - `Init`
   - `Configure`
   - `Ready`
4. Click the status bar item and confirm it opens the dashboard.
5. Confirm the dashboard shows:
   - `Next Step`
   - `Workspace Status`
   - `Actions`
   - `Diagnostics`

### Setup Flows

1. In a project without the package, run `Install Plugin`.
2. Confirm `Packages/manifest.json` is updated.
3. Open Unity and allow package import to complete.
4. Run `Configure Project`.
5. Confirm `.vscode/mcp.json` is created or updated.
6. Run `Check Status`.
7. Confirm the report shows:
   - plugin installed
   - Unity MCP project config present
   - VS Code MCP configured

### Launch Flows

1. Run `Open Unity`.
2. Validate plain launch.
3. Run `Open Unity With MCP` from the dashboard or command flow.
4. If the project has not initialized yet, confirm the extension offers `Open Without MCP`.
5. After initialization, confirm connected launch succeeds.

### Clean VSIX Install

1. Install the VSIX in a normal VS Code window, not the Extension Development Host.
2. Open a Unity project.
3. Use only the Activity Bar dashboard and status bar entry.
4. Confirm the full setup workflow still works without launching the extension from source.

## Notes

- `HTTP` is the recommended transport when configuring VS Code MCP.
- The current package metadata uses a preview publisher id for local packaging. Marketplace publisher identity can change later without affecting the extension architecture.
