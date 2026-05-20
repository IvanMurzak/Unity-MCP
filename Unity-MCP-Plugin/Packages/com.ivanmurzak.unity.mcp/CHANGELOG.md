# Changelog

## [Unreleased]

### Fixed

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
