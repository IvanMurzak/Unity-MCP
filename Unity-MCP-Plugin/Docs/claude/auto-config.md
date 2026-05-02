# AI Agent Configurators

Auto-configuration system generating MCP config files for AI clients. Each configurator has platform-specific and transport variants.

**Key patterns**:
- Fluent builder: `.SetProperty(key, value, requiredForConfiguration, comparison).SetPropertyToRemove(key)`
- `ValueComparisonMode`: `Exact`, `Path` (normalized separators), `Url` (case-insensitive scheme+host)
- Duplicate detection via identity keys. Deprecated section cleanup for old "Unity-MCP" entries.
