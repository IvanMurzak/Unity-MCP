# AI Agent Configurators

Auto-configuration system generating MCP config files for AI clients. Each configurator has platform-specific and transport variants.

**Key patterns**:
- Fluent builder: `.SetProperty(key, value, requiredForConfiguration, comparison).SetPropertyToRemove(key)`
- `ValueComparisonMode`: `Exact`, `Path` (normalized separators), `Url` (case-insensitive scheme+host)
- Duplicate detection via identity keys. Deprecated section cleanup for old "Unity-MCP" entries.
- `SupportsTransport(transport)` false (Claude Desktop over HTTP, Cloud included): `Describe()` holds only an alert
  (`TransportSupported == false`); `AiAgentConfiguratorView` hides Configure and never writes or rewrites that entry.
- `GetRestartNotice(transport)` / `RestartNotice` (stdio agents): shown after Configure / Reconfigure / Remove
  (a dialog, logged in batch mode) and as a hint in the stdio panel.
