# Unity MCP VS Code Extension

This package is the isolated VS Code extension workspace for GitHub issue `#613`.

## Current Slice

Slice 1 adds:

- extension scaffold
- output channel logging
- Workspace Trust awareness
- Unity project detection
- Unity MCP plugin detection
- `.vscode/mcp.json` detection
- `Unity MCP: Check Status`
- `Unity MCP: Show Output`

Slice 2 adds:

- `Unity MCP: Configure Project`
- shared `unity-mcp-cli` adapter usage for VS Code MCP config generation
- trusted-workspace guard for write actions
- transport picker for `HTTP` or `STDIO`

Slice 3 adds:

- `Unity MCP: Install Plugin`
- shared `unity-mcp-cli` adapter usage for Unity package installation
- explicit confirmation before mutating `Packages/manifest.json`

Slice 4 adds:

- `Unity MCP: Open Unity`
- plain launch or MCP-connected launch
- reuse of shared `openProject()` behavior from `unity-mcp-cli`

Slice 5 adds:

- actionable status recommendations
- guided next-step buttons from `Unity MCP: Check Status`
- clearer first-run setup hints in the output report

## Local Development

```bash
cd vscode-extension
npm install
npm run build
```

Then open the `vscode-extension/` folder in VS Code and press `F5`.

## Manual Validation

In the Extension Development Host:

1. Open a non-Unity folder.
2. Run `Unity MCP: Check Status`.
3. Confirm the output channel reports `Unity project detected: no`.
4. Open a Unity project folder.
5. Run `Unity MCP: Check Status`.
6. Confirm the output channel reports:
   - trust state
   - Unity markers
   - plugin installed or missing
   - `.vscode/mcp.json` present or missing

Then:

7. Run `Unity MCP: Configure Project`.
8. Choose `HTTP` unless you specifically want to test `STDIO`.
9. Confirm `.vscode/mcp.json` is created.
10. Run `Unity MCP: Check Status` again and confirm the MCP server now appears as configured.

Then:

11. Run `Unity MCP: Install Plugin`.
12. Confirm the warning about updating `Packages/manifest.json`.
13. Approve the install.
14. Confirm the manifest is updated and the status report now shows the plugin as installed.

Then:

15. Run `Unity MCP: Open Unity`.
16. First test `Open Unity`.
17. Then test `Open Unity With MCP Connection`.
18. If the plugin was just installed and the project has not initialized yet, the extension should offer a guided fallback to `Open Without MCP`.
19. After Unity imports and creates `UserSettings/AI-Game-Developer-Config.json`, retry `Open Unity With MCP Connection`.
20. Confirm the output channel shows the open-project progress events.

Then:

21. Run `Unity MCP: Check Status` in projects representing different setup states.
22. Confirm the status report includes `Recommended next actions`.
23. Confirm the status notification offers relevant next-step buttons such as `Install Plugin`, `Configure Project`, `Open Unity`, or `Show Output`.

Unity itself is not required for this slice.
