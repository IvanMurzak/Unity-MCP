# Changelog

## [0.51.6] - 2025-03-10

### Changed

- **Package tests hidden from consumers**: The package tests folder was renamed from `Tests` to `Tests~`. Unity treats folders ending with `~` as hidden, so consumer projects that install the package via OpenUPM no longer see the package’s unit tests in the Test Runner. The repo and CI continue to run tests from `Tests~` when opening the Plugin or Installer project.

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
