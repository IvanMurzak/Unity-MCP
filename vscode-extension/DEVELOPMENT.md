# Development Guide

This guide is for maintainers changing the VS Code extension itself.

## Local Workflow

Run from the `vscode-extension/` directory:

```bash
npm install
npm run build
npm test
```

To debug the extension host:

1. Open the `vscode-extension/` folder in VS Code.
2. Press `F5`.
3. In the Extension Development Host, open a Unity project.
4. Use the dashboard, status bar, or command palette.
5. Watch the `Unity MCP` output channel.

To package a local VSIX:

```bash
npm run package:vsix
```

## Source Map

- `src/extension.ts`
  Command registration, trust gates, notifications, status bar wiring, and dashboard refresh orchestration.
- `src/dashboard.ts`
  Activity Bar dashboard webview, button layout, and status bar label computation.
- `src/projectStatus.ts`
  Workspace inspection, recommended next actions, and the text status report.
- `src/cliAdapter.ts`
  Shared-library bridge to `unity-mcp-cli`. Prefer extending this instead of duplicating CLI logic in the extension.
- `src/unityConfig.ts`
  Reads and normalizes `UserSettings/AI-Game-Developer-Config.json`.
- `src/workspace.ts`
  Workspace selection and preferred-folder behavior for multi-root workspaces.
- `src/logging.ts`
  Output channel logger and log-level filtering.

## Rules For Changes

- keep write actions explicit
- keep untrusted workspaces read-only
- do not log tokens or generated config bodies
- prefer reusing `unity-mcp-cli` logic through `cliAdapter.ts`
- add or update tests when status logic, dashboard logic, or adapter behavior changes

## How To Change Common Things

### Add A New Command

1. Add the command contribution in `package.json`.
2. Register the command in `src/extension.ts`.
3. If the command should appear in the dashboard, add it in `src/dashboard.ts`.
4. If the command changes setup state, call `refreshUi()` after the operation completes.
5. Add or update tests.

### Change Setup-State Logic

Update `src/projectStatus.ts`.

That file controls:

- Unity project detection
- plugin detection
- Unity MCP project config detection
- `.vscode/mcp.json` detection
- recommended next actions
- the text status report

If you change state logic, also review:

- `buildStatusBarPresentation()` in `src/dashboard.ts`
- dashboard primary actions in `src/dashboard.ts`
- status notification action routing in `src/extension.ts`

### Change Dashboard UX

Update `src/dashboard.ts`.

This file controls:

- `Next Step`
- workspace state cards
- button ordering
- button-to-command wiring
- Activity Bar view rendering

If the dashboard changes, verify:

- it renders after `dashboard:viewResolved`
- buttons still call the expected commands
- the status bar still reflects the right state

### Change File Generation Behavior

Prefer changing the shared library in `unity-mcp-cli` first, then keep `src/cliAdapter.ts` thin.

Avoid copying setup logic into the extension unless the shared library cannot support the needed behavior cleanly.

## Tests

Current test coverage is centered on:

- adapter behavior
- dashboard state mapping
- project status detection
- Unity config parsing

Run:

```bash
npm test
```

When changing packaging behavior, also run:

```bash
npm run package:vsix
```

## Useful Files During Debugging

Inspect these when debugging a real project:

- `Packages/manifest.json`
- `.vscode/mcp.json`
- `UserSettings/AI-Game-Developer-Config.json`

The extension should explain its view of these files through:

- `Unity MCP: Check Status`
- the dashboard `Diagnostics` card
- the `Unity MCP` output channel

## Release Handoff

This extension is ready for local packaging, but Marketplace publishing should be done by the official maintainer account, not a personal account. See [PUBLISHING.md](./PUBLISHING.md).
