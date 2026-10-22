# Changelog

## [Unreleased]

### Added

- **Editor-session contract shared by the engine CLIs.** An editor started by a CLI session
  (`AIGD_SESSION_OWNER=cli`) quits after `AIGD_SESSION_IDLE_SECONDS` without tool calls
  (`UNITY_MCP_IDLE_TIMEOUT` still works and is the fallback), saving changed scenes and assets first. The
  editor leaves the CLI's session lease `<project>/.ai-game-dev/session.json` to the CLI, which removes it once
  the editor's process is gone: removed earlier, the still-exiting editor looked like one the CLI did not start.
- **`editor-session-quit` system tool.** Saves and quits an editor a CLI session started, when the caller
  passes the lease nonce; any other editor refuses. `unity-mcp-cli close` uses it, because the Windows
  polite quit (`WM_CLOSE`) never reaches a headless (`-batchmode`) editor.

- **`UNITY_MCP_SERVER_PATH` — dev/CI override for the launched MCP server binary.** When the
  variable resolves to a file that exists, the Editor launches that binary instead of the
  `GameDev-MCP-Server` release pinned by `McpServerManager.ServerVersion`, and skips both the
  download and the version match; the override also becomes the `command` written into generated
  AI-agent configs. Set-but-missing falls through to the pinned release, matching Unreal-MCP's
  `UNREAL_MCP_SERVER_PATH`. Resolved through `DevControlEnv.Resolve`, so it layers
  process env > `<projectRoot>/.env` and therefore works for a GUI/IDE-launched Editor.
  `Tools/AI Game Developer/Server/Download Binaries` still downloads into `Library/mcp-server/<rid>/`
  regardless of the override. See `docs/mcp-server.md`.

- **Tool timeout for the MCP server (McpPlugin 8.11.0).** McpPlugin 8.11.0 servers send a tool call to the
  plugin once (older servers re-sent it up to 10 times, one per `plugin-timeout`, which could run a long tool
  such as `script-execute` or `tests-run` several times) and wait `tool-timeout` for it. The new **Tool timeout
  (ms)** setting in the AI Game Developer window (`ToolTimeoutMs` in the config, default 300000) is passed as
  `tool-timeout=<ms>` when the editor starts the local server, in the Docker command
  (`MCP_PLUGIN_TOOL_TIMEOUT`) and in `McpServerManager.RawJsonConfigurationStdio`; it is never below **Timeout**,
  so a Timeout raised for long tools before this setting existed still applies. **Timeout (ms)** stays
  `plugin-timeout`, the connection timeout, and is passed unchanged, so an older server (which ignores
  `tool-timeout`) behaves as before. The `tests-run` stale-run lease now follows the tool timeout.

### Fixed

- **Changing Timeout in the AI Game Developer window threw a `NullReferenceException`** before saving: it
  refreshed a stdio-config text field the window no longer has.

- **`EntityId` wire format moved from JSON number to JSON string of decimal digits**
  (Unity 6.5+ paths only). Closes #759, resolves #754. JS-based MCP clients
  (Claude Agent SDK, etc.) parse JSON numbers as IEEE-754 doubles, so any
  `EntityId` raw ulong past `2^53 - 1` was rounded on the JS side and the
  rounded value sent back to Unity could not resolve the original object.
  Serialising the value as an opaque JSON string preserves full 64-bit
  precision across any language boundary. Inbound still accepts both forms
  (string preferred, number accepted for back-compat); outbound is always a
  string. Schema is now `{ "type": "string", "pattern": "^[0-9]+$" }`. Affects
  every Unity 6.5+ converter that writes `EntityId` or an `instanceID` field:
  `EntityIdConverter`, `ObjectRefConverter`, `GameObjectRefConverter`,
  `AssetObjectRefConverter`, `ComponentRefConverter`, `SceneRefConverter`,
  and the `Tool_Assets.Modify` `instanceID` injection. Pre-Unity-6.5 paths
  (legacy `int InstanceID`, safely inside the JS-safe range) are untouched.

### Changed

- **McpPlugin 8.9.0 -> 8.11.1** (`com.IvanMurzak.McpPlugin` / `.Common`), **ReflectorNet 5.4.2 -> 5.5.0**, local
  MCP server pin (`McpServerManager.ServerVersion`) **9.2.14**. ReflectorNet 5.5.0 recognizes the main thread by
  its `SynchronizationContext` instead of managed thread id 1: Unity 7 (CoreCLR) on Windows runs the main thread
  with id 2, and main-thread calls hung there. McpPlugin 8.11.1 and GameDev-MCP-Server 9.2.14 are rebuilds on it.
  The NuGet gate generation moves to `UNITY_MCP_DEPS_14`.

### Changed (BREAKING)

- **Namespace flattened to `AIGD`**: AI-facing data model namespace renamed from
  `com.IvanMurzak.Unity.MCP.Runtime.Data` to `AIGD` across all data types
  (`GameObjectRef`, `ComponentRef`, `AssetObjectRef`, `SceneRef`, `ObjectRef`,
  `*Data`, `*DataShallow`, `*Metadata`, `PathPatch`, etc.). Closes #676.
  Replace `using com.IvanMurzak.Unity.MCP.Runtime.Data;` with `using AIGD;`.
  This supersedes reverted PR #701 (which used an intermediate `Unity.MCP.Data` name).
- **Nested-data-model convention REVERSED**: Data classes that were previously
  declared as nested types inside MCP tool `partial` classes (e.g.,
  `Tool_GameObject.DestroyGameObjectResult`, `Tool_Assets.CopyAssetsResponse`,
  `Tool_Tool.InputData/ResultData`) have been EXTRACTED into top-level types under
  `Editor/Scripts/API/Tool/Data/` in the `AIGD` namespace. `Tool_Tool.InputData`
  was renamed to `AIGD.ToolToggleInput`; `Tool_Tool.ResultData` became `AIGD.ToolToggleResult`.
  All other extracted types preserved their original names. New AI-facing data models MUST
  be top-level types in `AIGD` (see constitution Principle IV).
- `unity-skill-create` tool guidance updated to instruct AI agents to declare data
  models as top-level types in `AIGD`, not nested inside the tool class.
- Constitution bumped 1.4.0 → 1.5.0 documenting the rule reversal and the new
  `AIGD` namespace exception in Principle IV.

## [0.17.1] - 2025-01-XX

### Fixed

- **Play Mode Reconnection**: Fixed Unity-MCP-Plugin not reconnecting after exiting Play mode. The plugin now automatically re-establishes connection when returning to Edit mode if "Keep Connected" is enabled.
- Added proper handling for Unity's Play mode state changes (`EditorApplication.playModeStateChanged`)
- Enhanced logging for connection lifecycle debugging

### Added

- Comprehensive test coverage for Play mode reconnection scenarios
- Debug logging for Play mode transitions to help troubleshooting connection issues

## [0.1.0] - 2025-04-01

### Added

- Initial release of the Unity package.
