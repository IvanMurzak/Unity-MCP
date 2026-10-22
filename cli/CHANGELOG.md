# Changelog

All notable changes to `unity-mcp-cli` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Editor sessions (lease + start lock) for `exec` and `open --headless`**, on `@baizor/gamedev-cli-core`'s
  `EditorSession` (dependency bumped to `^0.7.1`). An editor the CLI starts is recorded in
  `<project>/.ai-game-dev/session.json` (PID, process start time, nonce) under the file lock
  `.ai-game-dev/session.lock`, so parallel cold `exec` calls start exactly one editor and share it. A running editor
  of the project (the user's GUI included) is still reused and never closed. The started editor gets the shared
  session env `AIGD_SESSION_IDLE_SECONDS` / `AIGD_SESSION_OWNER=cli` / `AIGD_SESSION_NONCE`.
- **`close --owned-only`**, and `close` asks an editor the CLI started to save and quit through its plugin (the new
  `editor-session-quit` system tool, checked against the lease nonce). This reaches a headless editor, which the
  Windows polite quit (`WM_CLOSE`) never does; an editor opened by the user is never closed by `--owned-only`.
- **`open` with a window closes the CLI's own headless editor first** (it saves), so the window can open.
- **The lease records the server URL** the editor was started against (and its connection mode), so `close` asks
  the plugin there without resolving the connection again (then at whichever resolved URL answers; the call's token
  is never sent to a remote server it was not resolved for). A lease whose editor has exited no longer hides the
  user's editor from `open --headless`. The session files are kept out of git by
  `.ai-game-dev/.gitignore`.
- **`exec` fails fast on an editor it did not start that never answers** (default 60 s instead of the full
  `--ready-timeout`): e.g. the user's editor whose plugin is disconnected or connected to the ai-game.dev cloud. The
  message says what to do (connect the plugin, switch it to Custom, or pass `--url`); `ExecCodeFailure.editorSource`
  is `foreign` then, and `ExecCodeOptions.foreignReadyTimeoutMs` sets the wait. If that editor exits while `exec`
  waits, `exec` starts its own.
- **`open --headless` waits until the editor's MCP server answers** (`--ready-timeout`, default 300000 ms) and
  reports the URL; its `--idle-timeout` is sent as `AIGD_SESSION_IDLE_SECONDS`. A windowed `open --idle-timeout`
  still sets `UNITY_MCP_IDLE_TIMEOUT`.

### Changed

- `install-plugin --with-server` fetches GameDev-MCP-Server 9.2.14 by default (`DEFAULT_SERVER_VERSION`, was
  9.2.13), in lockstep with the plugin's `ServerVersion`: a rebuild on ReflectorNet 5.5.0.

- `exec` waits 310000 ms by default (was 180000; `--timeout` and `ExecCodeOptions.timeoutMs`; exported as
  `DEFAULT_EXEC_TIMEOUT_MS`): longer than the MCP server's default `tool-timeout` (300000, McpPlugin 8.11.0+), so
  the CLI never gives up on a call the server is still waiting for.

- `wait-for-ready` / `waitForReady` use cli-core's readiness probe: a 401/403 now counts as a server that is up (the
  next call reports the auth problem). `WaitForReadyOptions.probeImpl` is replaced by `fetchImpl`.
- `execCode`'s test seam `deps` is now `{ session, invoke }`; failures carry `sessionReason`.

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
