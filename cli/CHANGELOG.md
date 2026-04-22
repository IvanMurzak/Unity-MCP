# Changelog

All notable changes to `unity-mcp-cli` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.67.0] - 2026-04-21

### Added

- **Library API.** The package now exposes its core commands as a typed,
  side-effect-free library alongside the existing CLI binary.
  Consumers can do:

  ```ts
  import { installPlugin, removePlugin, configure, setupMcp } from 'unity-mcp-cli';
  ```

  Available functions:
  - `installPlugin(opts)` — install the Unity-MCP plugin into a Unity project
  - `removePlugin(opts)` — remove the Unity-MCP plugin from a Unity project
  - `configure(opts)` — enable/disable MCP tools, prompts, resources
  - `setupMcp(opts)` — write an agent MCP config file
  - `listAgentIds()` — list every supported agent id

  All functions return a typed `{ success, ... }` result (never throw past the
  public boundary) and accept an optional `onProgress(event)` callback that
  fires for `start`, `dependencies-resolved`, `manifest-patched`, and
  `done` phases.

- **`package.json` `exports` field.** The package now ships with a
  conditional-exports map:
  - `"."` — the library entry (`dist/lib.js`)
  - `"./cli"` — the CLI entry (`dist/cli.js`), identical behaviour to
    the `unity-mcp-cli` binary

### Changed

- Command handlers (`install-plugin`, `remove-plugin`, `configure`,
  `setup-mcp`) are now thin Commander wrappers that delegate to the
  library functions. Terminal output and exit codes are unchanged from
  the user's perspective.

- `utils/manifest.ts` functions now accept an optional `logger` argument
  so the library can run silently while the CLI preserves its chalk-
  styled output. `addPluginToManifest` and `removePluginFromManifest`
  also return structured result objects; return values are purely
  additive and the existing call sites are unaffected.

### Notes

- No breaking changes to the CLI binary. `unity-mcp-cli install-plugin
  ./MyProject` and friends still work exactly as before.
- The library entry point has no top-level side effects: importing
  `unity-mcp-cli` never writes to stdout/stderr and never parses argv.
  A `require('unity-mcp-cli')` call is safe to do anywhere.

[0.67.0]: https://github.com/IvanMurzak/Unity-MCP/releases/tag/cli-0.67.0
