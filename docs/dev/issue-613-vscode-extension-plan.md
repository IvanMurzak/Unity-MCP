# Issue 613 Plan: VS Code Extension Marketplace

## Goal

Create a VS Code extension package for Unity MCP that is safe to install from the VS Code Extensions Marketplace, reuses existing repo logic where practical, and reduces the current manual `.vscode/mcp.json` setup burden without widening the Unity MCP trust surface unnecessarily.

## Current Findings

- Issue [`#613`](https://github.com/IvanMurzak/Unity-MCP/issues/613) is minimal and open. Its only explicit acceptance signal today is: make a VS Code extension users can install from the marketplace.
- The repo is Apache-2.0 licensed. No `CONTRIBUTING*`, CLA, or DCO files were found in the repository root.
- The CLI already exposes a side-effect-free library API in [cli/src/lib.ts](/Users/suporte/Unity-MCP/cli/src/lib.ts) and documents it in [cli/README.md](/Users/suporte/Unity-MCP/cli/README.md).
- `setupMcp()` already knows how to generate VS Code MCP config for agent id `vscode-copilot` and targets `.vscode/mcp.json` via [cli/src/utils/agents.ts](/Users/suporte/Unity-MCP/cli/src/utils/agents.ts) and [cli/src/lib/setup-mcp.ts](/Users/suporte/Unity-MCP/cli/src/lib/setup-mcp.ts).
- The Unity-side VS Code configurator in [VisualStudioCodeCopilotConfigurator.cs](/Users/suporte/Unity-MCP/Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/UI/AiAgentConfigurators/Impl/VisualStudioCodeCopilotConfigurator.cs) still tells users to create `.vscode/mcp.json` manually and start the `ai-game-developer` server manually after each VS Code restart.
- Current VS Code docs show:
  - MCP servers are managed through `mcp.json`, the Extensions view, and inline code lenses.
  - VS Code has a current MCP developer guide and workspace trust guidance.
  - Workspace Trust should be treated explicitly for extensions that touch local projects or execute tools.

## Working Decisions

- Use a new isolated package folder: `vscode-extension/`.
- Reuse `unity-mcp-cli` library functions where possible. Avoid shelling out by default.
- Keep v1 local-first. No telemetry. No publishing flow changes in this repo yet.
- Treat write actions and Unity launch actions as trust-sensitive and opt-in only.
- Start with read-only diagnostics before adding project mutation commands.

## Slice Plan

### Slice 1: Extension scaffold and read-only status

Scope:
- Add a standalone VS Code extension package.
- Add safe activation, output logging, Workspace Trust awareness, Unity project detection, plugin detection, and MCP config detection.
- Add `Unity MCP: Check Status` and `Unity MCP: Show Output`.

Why first:
- Gives a testable extension immediately.
- Builds the logging and trust baseline needed for later write flows.
- Avoids any project mutation while we confirm extension packaging and UX.

Automated tests:
- Unit test Unity project detection.
- Unit test plugin detection from `Packages/manifest.json`.
- Unit test `.vscode/mcp.json` parsing and warning paths.

Debug logging to include:
- `activate:start`
- `activate:complete`
- `workspace:pick`
- `status:computeStart`
- `status:computeResult`
- `status:error`
- `trust:granted`

Manual validation:
1. Install extension package dependencies.
2. Build the extension.
3. Launch the Extension Development Host from VS Code.
4. Open a non-Unity folder and run `Unity MCP: Check Status`.
5. Open a Unity project and run `Unity MCP: Check Status`.
6. Confirm the `Unity MCP` output channel reports trust state, Unity detection, plugin status, and MCP config status.

### Slice 2: Shared CLI adapter for setup flows

Scope:
- Add a small internal adapter that imports typed functions from `unity-mcp-cli`.
- Prove runtime compatibility inside the VS Code extension host.
- Avoid any duplicated MCP config generation logic.

Automated tests:
- Adapter success/failure mapping.
- Progress event forwarding.
- Config generation parity check for `vscode-copilot`.

Debug logging to include:
- `cliAdapter:loadStart`
- `cliAdapter:loadSuccess`
- `cliAdapter:loadFailure`
- `cliAdapter:callStart`
- `cliAdapter:callSuccess`
- `cliAdapter:callFailure`

Manual validation:
1. Run a dry adapter command from the extension without mutating files.
2. Confirm output channel logs progress events.

### Slice 3: Configure Project command

Scope:
- Add `Unity MCP: Configure Project`.
- Generate or update `.vscode/mcp.json` through shared library logic.
- Require trusted workspace and explicit user action.

Automated tests:
- Creates `.vscode/mcp.json` when missing.
- Preserves unrelated config content where appropriate.
- Re-running is idempotent.
- Errors are surfaced clearly when the workspace is not a Unity project.

Debug logging to include:
- `configure:start`
- `configure:precheck`
- `configure:writeSuccess`
- `configure:writeFailure`

Manual validation:
1. Trust the workspace.
2. Run `Unity MCP: Configure Project`.
3. Inspect `.vscode/mcp.json`.
4. Open VS Code MCP server UI and confirm the server entry appears.

### Slice 4: Install Plugin command

Scope:
- Add `Unity MCP: Install Plugin` if `installPlugin()` reuse is clean and safe.
- Keep user confirmation explicit.
- Never silently install anything.

Automated tests:
- Plugin install happy path.
- Already-installed path.
- Failure path for missing or invalid Unity project.

Debug logging to include:
- `pluginInstall:start`
- `pluginInstall:precheck`
- `pluginInstall:result`
- `pluginInstall:error`

Manual validation:
1. Use a Unity project without the package installed.
2. Run `Unity MCP: Install Plugin`.
3. Confirm `Packages/manifest.json` is updated as expected.
4. Open Unity and confirm package import begins.

### Slice 5: Optional convenience actions

Scope:
- Evaluate `Unity MCP: Open Unity`.
- Evaluate minimal safe guidance for starting or managing the MCP server.
- Only include commands that do not hide risky behavior.

Automated tests:
- Command registration and trust gating.
- Error handling for missing Unity/editor discovery issues.

Debug logging to include:
- `openUnity:start`
- `openUnity:result`
- `openUnity:error`

Manual validation:
1. Run the command from a trusted Unity workspace.
2. Confirm it only performs the documented action.

### Slice 6: Packaging, docs, and hardening

Scope:
- Add extension README, local install docs, release notes stub, and manual test matrix.
- Add extension-host smoke tests if needed.
- Prepare for future marketplace packaging without publishing.

Automated tests:
- Build.
- Unit tests.
- Extension smoke test.
- Package smoke test.

Debug logging to review:
- Ensure all logs stay structured and redact secrets.

Manual validation:
1. Package the extension locally.
2. Install the VSIX in a clean VS Code profile.
3. Re-run slice 1 to 5 validation steps.

## Security, Privacy, and Workspace Trust Rules

- No telemetry in v1.
- Never log auth tokens, headers, prompts, file contents, or generated config bodies.
- Default to read-only behavior in untrusted workspaces.
- Require trusted workspace for any file mutation or Unity launch action.
- Do not auto-start destructive or code-executing flows.
- Keep all networked or editor-mutating behavior explicit and user-initiated.

## Slice 1 Validation Steps

### Validate in VS Code

1. Open the `vscode-extension/` folder in VS Code.
2. Run `npm install`.
3. Run `npm run build`.
4. Press `F5` to start the Extension Development Host.
5. In the Extension Development Host:
   - Open any non-Unity folder and run `Unity MCP: Check Status`.
   - Open a Unity project folder and run `Unity MCP: Check Status`.
   - Run `Unity MCP: Show Output`.
6. Confirm the output channel shows:
   - workspace trust state
   - selected workspace folder
   - Unity markers detected
   - plugin installed or missing
   - `.vscode/mcp.json` present or missing

### Validate with Unity

Unity is not required for Slice 1.

If you want a realistic signal:
1. Open a Unity project that already has `Assets/`, `ProjectSettings/`, and `Packages/manifest.json`.
2. If the Unity MCP package is installed, confirm the status command reports it.
3. If the package is not installed, confirm the status command reports it as missing.
