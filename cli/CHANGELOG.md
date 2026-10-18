# Changelog

All notable changes to `unity-mcp-cli` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **`exec` command.** `unity-mcp-cli exec '<C#>'` runs a snippet through the `script-execute` tool and prints the
  result. It reuses an editor that is already open for the project (window or headless) and never closes it;
  otherwise it starts a headless editor (`--window` for a windowed one) that stays warm and quits by itself after
  `--idle` seconds (default 300) without tool calls. Code comes from the argument, `-f <file>` or stdin;
  `--using` and `--include` are repeatable.
- **`open --headless` and `open --idle-timeout <seconds>`.** `--headless` launches with `-batchmode` (graphics stay
  on, so cameras render) and implies `--keep-connected`; `--idle-timeout` sets `UNITY_MCP_IDLE_TIMEOUT`, which the
  plugin uses to quit after that many idle seconds (it saves changed scenes and assets first).
- **`execCode` and `waitForReady` library functions** (exported from the package root), the code behind `exec` and
  `wait-for-ready`. `runTool` accepts `tokenFromCloudStore` so a caller-resolved machine-store Bearer is refreshed once
  on 401, as when the library resolves the connection itself.

- **`runTool` / `runSystemTool` library functions.** The HTTP-tool
  invocation paths previously available only as the `run-tool` /
  `run-system-tool` CLI commands are now first-class library exports:

  ```ts
  import { runTool, runSystemTool } from 'unity-mcp-cli';

  const result = await runTool({
    toolName: 'tool-list',
    unityProjectPath: '/path/to/Unity/project',
    input: { regexSearch: 'gameobject', includeDescription: true },
  });
  if (result.kind === 'success') {
    // result.data === parsed JSON body from the Unity plugin
  }
  ```

  Same URL/token resolution priority as the CLI commands (explicit
  override → project config → deterministic localhost port). Returns
  a discriminated `{ kind: 'success' | 'failure', ... }` union; never
  throws past the public boundary; emits no console output.

### Changed

- **`run-tool` / `run-system-tool` commands** now delegate to the new
  library functions internally. CLI behaviour is unchanged.

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
  An `import 'unity-mcp-cli'` or dynamic `import('unity-mcp-cli')`
  call is safe to do anywhere.

[0.67.0]: https://github.com/IvanMurzak/Unity-MCP/releases/tag/cli-0.67.0
