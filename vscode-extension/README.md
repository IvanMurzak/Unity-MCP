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

Unity itself is not required for this slice.
