# Connection & Transport

- **Port**: Deterministic — SHA256 of project path, mapped to 20000–29999
- **Server Binary**: Downloaded from GitHub releases to `Library/mcp-server/{platform}/`. Version tracked in a `version` file alongside binary.
- **Process Lifecycle** (`McpServerStatus`): `Stopped` → `Starting` → `Running` → `Stopping` → `Stopped`, plus `External`. PID persisted in EditorPrefs for domain reload resilience.
- **Domain Reload**: Disconnects before reload (only if `Connected`), rebuilds and reconnects after. Play mode transitions trigger delayed reconnection.
