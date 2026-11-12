# Changelog

All notable changes to `unity-mcp-cli` will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed — Node.js 22.14 or later is required

`engines.node` is now `>=22.14.0` (was `^20.19.0 || >=22.12.0`): Node.js 20 is no longer supported. chalk 6, commander 15,
vitest 5 and `@baizor/gamedev-cli-core` 0.11.1 already require Node.js 22 or later (cli-core: 22.14). CI tests the CLI on
Node.js 22, 24 and 26.

### Changed — dependencies: chalk 6, commander 15, TypeScript 7, vitest 5

Help text, colours, exit codes and the JSON envelope are unchanged, except that a "too many arguments" usage error now
names the extra arguments (`Expected 1 argument but got 3: a, b, c.`, on stderr and in the envelope `message`; still
exit 2 `USAGE`), and a negative number such as `-1` given as a command argument (or as `rql --schema -1`) is now read
as that value instead of being rejected as `unknown option '-1'`. chalk 6 and commander 15 declare Node.js >= 22.

### Fixed — a connection dropped mid-call exits 7 `UNREACHABLE`

A socket the server took and then closed — before the answer's headers, in the middle of its body, a reset (`ECONNRESET`,
`EPIPE`, undici's `UND_ERR_SOCKET` "other side closed" / "terminated") — now fails `run-tool`, `run-system-tool`, `exec`,
`rql` and `tools` with exit **7** `UNREACHABLE` (`connection-reset`), the same in all three engine CLIs (one helper,
`networkFailureReason` of `@baizor/gamedev-cli-core` ^0.11.1). It was exit 1 `ERROR` (only an `ECONNRESET` on the first
cause was recognised), and a body cut off after a 200 was read as an empty answer. `tools` exited 7 for most of these
already (an error answer cut off mid-body gave its HTTP status's code, e.g. 6 `TOOL_FAILED` for a 500); it now classifies them the same way. A connect timeout or an unreachable
host (`ETIMEDOUT`, `EHOSTUNREACH`, `ENETUNREACH`) is exit 7 too (was 1).

### Changed — one failure table shared with `unreal-mcp-cli` and `godot-cli`

A failed `run-tool`, `run-system-tool`, `exec`, `rql` or `tools` call is classified by `classifyToolFailure` of
`@baizor/gamedev-cli-core` ^0.11.0, so all three engine CLIs give the same exit code and error code (table in the README,
`errorCodes` in `commands --format json`). The envelope error carries the server's HTTP status:
`errors[{code, message, httpStatus}]`. Behaviour changes:

- `tools <name>` with no matching tool, and `tools --cached` with no saved list: exit **2** `NOT_FOUND` (was 6).
- A failed `tools` read is classified by the same table as `run-tool` (401 → 6 `NOT_AUTHENTICATED`, 503 → 17 `BUSY`,
  a timeout → 14, the cloud's "no game engine connected" → 7, ...) instead of always 7 `UNREACHABLE`.
- HTTP 503 whose error says no engine plugin is connected: exit **7** `UNREACHABLE` (was 17 `BUSY`).
- HTTP 408: exit 14 `TIMEOUT`; HTTP 429: exit 17 `BUSY`; any other 4xx: exit 2 `USAGE` (were 6 `TOOL_FAILED`).
- A call that failed for an unclassified reason: exit **1** `ERROR` (was 6 `TOOL_FAILED`).
- A `--path` / path argument that does not exist or is not a Unity project (no `Assets` folder): exit **2** `NOT_FOUND`
  (was 1), as in the other two CLIs.

### Fixed

- **A fresh project no longer hangs `tools` / `exec` / `run-tool` / `compile`.** A project the plugin has not configured
  yet (no `UserSettings/AI-Game-Developer-Config.json`, or one without `connectionMode`) is in the plugin's default Cloud
  mode, but it was resolved as a local server with no fallback: on a machine never signed in, the started editor dialed
  ai-game.dev and the CLI waited on a local port nobody served until `--ready-timeout`. It now takes the same local-server
  fallback as a project saved in Cloud mode.
- **`dialog --press` checks that the dialog closed.** A click sent to an inactive dialog could be lost while the command
  reported success; it now falls back to the dialog's own button notification and fails when the dialog stays open.
- The launch-error auto-dismiss runs PowerShell with the user's temp folder, like `dialog` (`Add-Type` fails under
  `C:\Windows\Temp`).

### Changed

- **`setup-skills` works without an editor.** It always writes the `unity-mcp-cli` skill offline (cold-start
  workflow, every `rql` recipe, `compile` / `test`, `--format json`, exit codes and `dialog`; `references/` for depth;
  rendered from `src/lib/cli-guide.ts`, the source of the root `--help`) and `ai-game-dev-app`, then asks a running
  editor for the plugin's per-tool skills as before. A closed editor, a rejected token or an old plugin is now a
  note, not exit 1; `--offline` skips the editor. It never starts a Cloud login any more (that opened a browser
  and signed the machine in from a setup step): a Cloud project without a sign-in gets the CLI skill and the
  steps for the plugin's skills. `references/rql.md` holds the stages, operators, sources and members; the skill's
  description says to read it before the first editor command and that editing scripts alone needs only `compile`.
- **The exit codes, `--format`, the result envelope, the `commands` manifest and the local-server fallback rule
  now come from `@baizor/gamedev-cli-core` 0.9.0** (one contract for the three engine CLIs). `commands --format json` names the exit codes by their `ExitCode` key (`CommandFailed`, was `COMMAND_FAILED`) and adds option `choices`; a failure envelope no command handled names the program when no command is on the command line. An unreadable
  sign-in no longer prints PowerShell's DPAPI exception, and a forced close of an editor unity-mcp-cli started also kills its
  child processes.

### Added

- **`rql '<query>'`: one RQL query without JSON quoting.** A query ending in `set` / `call` goes to `rql-mutate`
  (`--dry-run`, `--max-targets`), any other to `rql-query`; `--schema [type]` runs `rql-schema`; `-f <file>` / `-f -`
  reads the query (a shell may strip the inner quotes). After a change it lists the scene / asset / settings files
  written (Unity saves at once), and an operator spelled as in another language (`startsWith`, `like`) gets a hint.
- **A shorter root `--help` (4.4 KB -> 1.8 KB)**: the everyday commands, the other command names and six RQL recipes;
  a subcommand's help starts with a plain line instead of a box.
- **`test`: Unity tests in a batch-mode editor, no plugin needed.** Runs Unity's own `-runTests` (`--mode`,
  `--filter`, `--category`, `--assembly`, extra editor args after `--`) and takes the verdict from the NUnit report:
  exit 0 passed, 8 failed, 6 no verdict (compile errors listed as `file(line,col): error CS…`, a crash, the project
  already open in an editor), 14 timeout (the editor tree is killed; Ctrl+C stops it too). `--junit-output` writes
  JUnit; `--format github` writes `::error` annotations at the failing test's source line.
- **`compile [--strict]`**: refreshes assets in the project's editor (started headless when none runs), waits for the
  compile and its domain reload, and reports compiler errors/warnings with file, line and column (exit 0 / 6 / 7 / 14).
- **`dialog [--press <button>]`** (Windows, macOS, Linux): lists the modal dialogs blocking the editor and presses a
  button. `status`, `run-tool`, `exec`, `compile` and `wait-for-ready` report a blocking dialog as exit 18
  (`AWAITING_INPUT`). macOS uses System Events (`osascript`, dialog windows and sheets) and needs the Accessibility
  permission of the terminal or agent app; without it `dialog` says where to grant it (System Settings → Privacy &
  Security → Accessibility). Linux reads the AT-SPI accessibility bus through a `python3` helper (PyGObject), on X11
  and Wayland, for GTK dialogs such as Unity's `EditorUtility.DisplayDialog`; it needs `python3-gi` and
  `gir1.2-atspi-2.0` (Debian/Ubuntu names) and a running accessibility bus, and names the missing package or setting
  when one is absent.
- **`tools [name]`**: the AI tools the project's editor offers (one line each with their inputs, or one tool in detail
  with an example call and its schema), read over MCP `tools/list` plus the hidden tools; starts a headless editor when
  needed and saves the list in the project (git-ignored) for `--cached` / `--no-start`. The root `--help` now ends with
  the everyday workflow (`tools`, `run-tool`, `exec`, `compile`, `test`, `status`, `close`). A call to an unknown tool
  points at `tools`, invalid arguments at `tools <name>`.
- **Works without an ai-game.dev sign-in:** in Cloud mode with no sign-in, `exec`, `run-tool`, `compile` and `tools`
  use the project's local server (a started editor serves it, in memory) instead of failing with "not logged in".
- **A headless editor survives script compile errors** (`-ignorecompilererrors`) and logs to
  `Logs/unity-mcp-cli-editor.log`; an editor that exits before it is ready names that log and the compile errors.
- **`commands --format json`**: a manifest of every command, argument, option and exit code, read from the CLI.
- **`--format json|ndjson`** on `run-tool`, `run-system-tool`, `exec`, `status`, `wait-for-ready` and the new
  commands: one result envelope `{success, command, data, errors[{code, message}], warnings}` on stdout, progress and
  logs on stderr; a command-line error still prints an envelope.
- **`run-tool` / `run-system-tool` start a headless editor** like `exec` when none answers (only for a project with
  `ProjectSettings/ProjectVersion.txt`; `--no-start` turns it off), and **`--once`** (also on `exec` and `compile`)
  closes an editor the call started, after the call or when it never became ready.
- Library: `ensureEditor` (the shared start-or-reuse policy), `runTests`, `compileProject`, `ExitCode`; `execCode` takes `once`.

### Changed

- `run-tool` prints the tool's own answer (not a banner plus an escaped JSON envelope; that stays `--raw` /
  `--format json`), a failed call prints the server's error text, and `test` writes its reports to
  `<project>/Logs/` by default. Spinner and progress lines go to stderr.

- **Exit codes follow one table shared with `unreal-mcp-cli` and `godot-cli`:** 2 bad arguments (commander parse
  errors and bad flag values, previously 1), 6 the command ran and failed, 7 editor unreachable, 8 tests failed,
  14 timeout, 17 busy, 18 awaiting input. Notably `run-tool` / `exec` now exit 6 (not 0) when the tool answers with an
  error, `wait-for-ready` exits 14 on timeout and `status` 7 when the server does not answer.

- **`update` updates everything.** `update [path]` brings the project's MCP plugin and installed extensions to their
  latest versions, then updates the CLI itself. From a subfolder it finds the project; outside a project it updates only
  the CLI. `--check` reports (exit 2 when something is outdated); `--no-self`, `--no-project` and `--no-extensions` skip a
  step. `file:`/git installs are left alone and nothing is downgraded. Library: `updateProject`.
- **`openProject` `spawnImpl`** (type `EditorSpawnImpl`): an embedder starts the editor its own way. It gets the
  editor path, arguments and full environment, and returns the started process or a promise of the editor's pid,
  which `openProject` waits for before `editor-launched` and `editorPid`; a rejection fails the open. Not used by
  `headless`. The `editor-launched` line (and `open`'s output) names the pid once known and never says
  `PID: unknown`.

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

- `install-plugin --with-server` fetches GameDev-MCP-Server 9.3.0 by default (`DEFAULT_SERVER_VERSION`, was
  9.2.17), in lockstep with the plugin's `ServerVersion`: built on McpPlugin 8.14.0 / ReflectorNet 5.5.1.

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
