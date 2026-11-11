# Connection & Transport

- **Port**: Deterministic — SHA256 of project path, mapped to 20000–29999
- **Server Binary**: Downloaded from GitHub releases to `Library/mcp-server/{platform}/`. Version tracked in a `version` file alongside binary.
- **Process Lifecycle** (`McpServerStatus`): `Stopped` → `Starting` → `Running` → `Stopping` → `Stopped`, plus `External`. PID persisted in EditorPrefs for domain reload resilience.
- **Domain Reload**: Disconnects before reload (only if `Connected`), rebuilds and reconnects after. Play mode transitions trigger delayed reconnection.
- **Unexpected Exit**: when the server this editor started exits on its own (status `Running`/`Starting`, so no stop was requested), the editor logs a warning and starts it again if the auto-start gates hold (Custom mode, not stdio, Keep Server Running), after 1, 2, 4 … 60 s (reset once a server ran 60 s); the port is checked first, so a server that took it meanwhile (`External`) is left alone. The connection loop reconnects on its own.
