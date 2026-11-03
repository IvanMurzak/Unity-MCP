# AI Agent Configurators

Auto-configuration system generating MCP config files for AI clients. Each configurator has platform-specific and transport variants.

**Key patterns**:
- Fluent builder: `.SetProperty(key, value, requiredForConfiguration, comparison).SetPropertyToRemove(key)`
- `ValueComparisonMode`: `Exact`, `Path` (normalized separators), `Url` (case-insensitive scheme+host)
- Duplicate detection via identity keys. Deprecated section cleanup for old "Unity-MCP" entries.
- `AiAgentConfiguratorView` switches on `Describe().SetupKind`: `ConfigFile` (Configure / Reconfigure / Remove),
  `Manual` (Claude Desktop in Cloud mode: the add-a-Claude-connector steps, a neutral status, no Configure), `Unsupported`
  (Claude Desktop over local HTTP: the alert panel). Both offer Remove only for a stale entry, never the working stdio one. Writes stay guarded by
  `SupportsTransport(transport)`, which is false for Claude Desktop over HTTP in both modes.
- `GetRestartNotice(transport)` / `RestartNotice` (stdio agents): shown after Configure / Reconfigure / Remove
  (a dialog, logged in batch mode) and as a hint in the stdio panel.
