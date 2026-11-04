# Startup Flow

Triggered by `[InitializeOnLoad]` static constructor in `Startup.cs`. An asset import worker
(`AssetDatabase.IsAssetImportWorkerProcess()`) skips all of it, and so do `McpServerManager`, `ServerBinaryUpdateWatcher` and `MainWindowInitializer`: workers
load the editor assemblies too, and a worker connecting replaced the editor's connection on a no-auth
server (#1050).

1. Build `IMcpPlugin` instance (scan assemblies for tools/prompts/resources)
2. Add `BufferedFileLogStorage` log collector for early log capture
3. **Deferred** connection via `EditorApplication.delayCall` (prevents Unity freeze on async SignalR)
4. Start server binary download asynchronously
5. **Deferred** server auto-start via `EditorApplication.delayCall`
6. Validate project path (no spaces)
7. Subscribe to editor lifecycle events (domain reload, play mode transitions)
8. CI environment detection — skips connection and server start in CI

## Explicit CI opt-out

`UNITY_MCP_DISABLE=1` (also `true`, ignoring case/whitespace) skips automatic MCP initialization,
including independent editor hooks and dependency resolution. `McpDisable` is a BCL-only check;
startup logging must remain lazy so disabled launches do not initialize the logging graph first.
It does not disable compilation. `UNITY_MCP_EXCLUDE_FROM_COMPILATION` is a separate scripting
symbol used by all shipped assembly definitions. See [CI setup](../../../docs/ci-disable.md).
