# Changelog

## [Unreleased]

### Changed (BREAKING)

- AI-facing data model types under `Runtime/Data/*` (`ObjectRef`, `AssetObjectRef`, `GameObjectRef`, `ComponentRef`, `SceneRef`, `GameObjectData`, `ComponentData`, `SceneData`, `GameObjectMetadata`, `*Shallow`, `*List`, `PathPatch`, …) moved from namespace `com.IvanMurzak.Unity.MCP.Runtime.Data` to `Unity.MCP.Data`. External code that imports these types must update its `using` directives. The rename keeps MCP tool JSON Schema `$defs` keys and `$ref` paths short and intuitive for AI agents (issue #676).

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
