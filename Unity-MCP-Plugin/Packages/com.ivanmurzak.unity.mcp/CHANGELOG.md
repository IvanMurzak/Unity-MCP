# Changelog

## [Unreleased]

### Changed

- **Tools, prompts and resources are "Visible", not "Enabled".** The switch only decides whether MCP clients see
  an item listed (`tools/list`, which keeps their context small); it is not an access control: a hidden tool still
  runs when called by name. The Tools / Prompts / Resources windows filter All / Visible / Hidden. The config file
  writes `"visible"`; a file saved with `"enabled"` still loads. The `tool-set-visibility` tool (input
  `{ Name, Visible }`) replaces `tool-set-enabled-state`, which keeps working as a deprecated alias until the next
  major release. `unity-mcp-cli configure` reads both keys and writes `visible`; `--list` prints `[visible]` /
  `[hidden]`. Use a `unity-mcp-cli` of this release with this plugin: an older CLI does not know `"visible"` and
  drops those entries when it edits the file, and an older plugin ignores entries a new CLI wrote.
- **A player connects as a runtime.** `UnityMcpPluginRuntime` declares McpPlugin's `ClientRole.Runtime` (sent at
  connect), so GameDev-MCP-Server keeps a running player beside the Editor. The `" (runtime)"` handshake suffix is
  gone. McpPlugin 8.14.0-dev.e8985d5 (temporary pre-release DLLs, gate `UNITY_MCP_DEPS_23`).
- **RQL arguments by name, side-effect-free overloads, compact receipts** (RQL main 2877579, temporary
  pre-release DLLs 0.3.0-dev.2877579, gate `UNITY_MCP_DEPS_24`). A lone object literal binds parameters by name:
  `editor | call CreateGameObject({name: "Crate", primitive: "Cube", position: [1, 0, 2]})`, also in a bulk create
  (`range(0, 300) | call editor.CreateGameObject({name: "C" + $, ...})`), validated before the first call and undone
  as one step. Trying an overload no longer creates anything, dry runs included, and a typo inside a placement
  literal is reported against the placement overload. Unity objects are never built from object literals; RQL now
  refuses that itself (`… is an engine object …`), so the plugin's own refusal is gone. Receipts of 2 or more rows
  print a shared method once, drop a `$ref` column equal to the result, and list 50 rows plus a count.
- **RQL literal kinds, vector functions, key warnings** (RQL main aacfbba, temporary pre-release DLLs
  0.3.0-dev.aacfbba, gate `UNITY_MCP_DEPS_25`). A Material or a data ScriptableObject is built from a typed literal:
  `set sharedMaterial = {$type: "Material", color: "#ff8800"}` (the render pipeline's default shader), `set item =
  {$type: "ItemData", price: 3}`; written into a scene object it is saved with the scene, into an asset or the open prefab it is refused
  (the asset would lose it). GameObjects, components and other assets stay refused, now with Unity's way to pass an
  existing one (`unity.find("Name")`, `ref(…)`, an asset path). Lists of numbers are vectors (`[x, 0, z] * k`), and
  `length` / `normalize` / `distance` / `dot` / `lerp` work on lists and vectors. A failing sort key and a sparse
  group-by key warn; a misspelt parameter name gets a suggestion. Sources declare their types, so typo warnings never
  read a source the query does not name. A cut receipt ends with a note that the rest was applied, not a pointer to a
  json format the tools do not have. The guide adds the agent rules: delete in two steps (list, then remove what was
  shown), say when a misspelt name was mapped, Unity units.
- **`script-execute` takes a `timeout` (seconds) and stops the threads a snippet starts.** The default budget is 30 s
  (was 120 s), `timeout` sets another, at most 290 s and 10 s below the tool timeout. Threads and tasks the code starts
  (`new Thread`, `Task.Run`, `ThreadPool`, `TaskFactory.StartNew`) stop at their next loop iteration or call once the
  run ends, with a Console warning that counts them; a `using var x = …;` first statement compiles. Vendored Code-Exec
  main 2556a2f.
- **McpPlugin 8.14.0-dev.42786f9 and ReflectorNet 5.5.1-dev.ec3702e** (temporary pre-release DLLs). Tools marked
  `[AiToolType(Target = ToolTarget.Runtime)]` are registered only by a runtime (player) connection, and the server
  routes their calls to the game while the Editor is connected; mark game-only tools this way. A call the plugin did
  not run (`NotExecuted`) is re-sent and runs; `string?` / `int?` parameters without a default are optional.
- **License metadata.** Every UPM package.json (this package, the extensions and the extension template) has a
  `license` field matching its LICENSE file. The vendored Code-Exec sources (`Editor/Scripts/CodeExec`) carry a
  header naming their license (PolyForm Shield 1.0.0, not this package's Apache-2.0), and the folder has a
  `THIRD-PARTY-NOTICES.md` with the source repository, commit, required notice and the license terms.

### Added

- **`rql-schema` lists the installed extensions and their guides.** Without a type it now names every installed
  Unity-MCP extension package (always active: there is nothing to turn on) with the sources and functions it adds,
  and the guide topics; `rql-schema "guide:<topic>"` prints that guide (the text the unity-rql skill carries). In
  wave-6 testing an agent asked to "turn on every extension" spent 60 turns reflecting over assemblies instead.
- **`unity-tool-list` shows `Visible`.** Hidden tools are listed with `Visible: false`, and the tool says plainly that
  they are not offered to MCP clients: ask the user to show one in the MCP Tools window (or with
  `unity-mcp-cli configure --enable-tools <name>`).
- **`screenshot-isolated` frames a Terrain.** A Terrain has no Renderer component, so the tool reported "No Renderers
  found"; its TerrainData bounds now count (when the Terrain module is present).

- **`staticFlags` on GameObject (RQL).** The per-flag Static Editor Flags behind `isStatic`
  (ContributeGI, OccluderStatic, OccludeeStatic, BatchingStatic, ReflectionProbeStatic): filter with
  `where staticFlags contains "ContributeGI"`, assign a list (`set staticFlags = ["ContributeGI"]`);
  validated all-or-nothing and undoable; obsolete flags a GameObject still carries are kept.

- **Lighting RQL module.** The `lighting` source reads and edits the active scene's environment (ambient,
  fog, skybox, sun, default reflections; undoable) and its Lighting Settings asset (`CreateSettings`,
  `UseSettings`; undoable) and drives the bake (`Bake` in the background or blocking, `isBaking` /
  `bakeProgress`, `CancelBake`, `ClearBakedData`, `BakeReflectionProbes`; refused without a graphics device
  unless the CPU lightmapper is set, instead of crashing the Editor). `LightProbeGroup` gains
  `PlaceGrid(size, spacing)`, `ReflectionProbe` gains `Bake()` (Custom probes; Baked ones bake with the lighting). The `unity-rql` skill lists the source in one
  line; members and calls are read on demand with `rql-schema "lighting"`.

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

- **`script-execute` takes a `null` parameter** (`parameters = "{label: null}"`): it failed with a
  NullReferenceException. The package's own C# warnings (CS0414, CS0114, CS8600, CS8602) are fixed too.
- **No connection and log-storage warnings on every Editor start and recompile** (#1039). The plugin no longer tells
  the server about tool / prompt changes while it is not connected and not reconnecting (e.g. with
  `UNITY_MCP_KEEP_CONNECTED=false`), so `EnsureConnection … auto-reconnect disabled` is gone. The log cache flushes before it closes, so
  `BufferedFileLogStorage Flush called but already disposed` is gone too. A PlayMode test run (`tests-run`) no longer
  logs `InternalConnect Connection was canceled`. With the AI Game Developer window open, toggling Play Mode no longer
  logs `EnsureConnection Failed to establish connection`: the window's server and agent status queries do not
  connect. McpPlugin 8.14.0-dev.71fa5c1 and ReflectorNet 5.5.1-dev.ec3702e (temporary pre-release DLLs, gate
  `UNITY_MCP_DEPS_27`).
- **No connection errors when the Editor starts or quits its own local server.** With *Keep server running* the
  Editor now connects once the server it launched has passed its startup check, not before, so
  `Failed to start connection` and `Connection refused` are gone from every start. On quit it disconnects before it
  stops the server, so `The server connection was terminated with an error` is gone too.
- **Polling with `script-execute` no longer holds a script reload forever.** Each call holds the domain reload until
  1 s after its reply, so a client calling every half second after writing a script (waiting for its new type) kept
  `isCompiling` true for minutes. While a compile that our lock held waits, `script-execute` now answers at once that the
  snippet did not run (for at most 60 s; never in Play Mode), and `unity-mcp-cli exec` re-sends it after the reload.
- **The Input System extension compiles on Unity 6.6.** Its native floor `com.unity.inputsystem` 1.8.2 fails there
  (`CS0619`: `TreeViewState` / `TreeViewItem` are obsolete as errors, about 260 errors; a batchmode editor exits).
  On Unity 6000.6 and later, the editor's extension panel, `unity-mcp-cli`, the self-contained installer and
  `commands/extensions.py prepare` now raise an older Input System pin to 1.19.0, as they already did for Navigation,
  ProBuilder and Tilemap. Newer or custom pins are kept.
- **`tests-run` filters: the check before the run now counts exactly what Unity runs** (#969). It matched any one
  filter while Unity requires all of them, so a filter selecting nothing (an assembly with a class that is not in it)
  answered with the summary of an empty run instead of "no tests found"; a parameterized `testMethod`, a
  wrong-case `testMethod` and a class name equal to a namespace segment were also miscounted. The check now
  matches the very filter Unity runs, the way Unity's Test Runner matches it, and a run that executes no test is
  answered with an error.
- **`tests-run` answered before Unity's run was over, and never when the run failed** (#1049). The answer and
  the run lock now wait until Unity has left Play Mode and restored the scenes, so the next `tests-run` cannot
  start on top of the previous run; `tests-run` also refuses to start while the Editor is in Play Mode. A run
  Unity aborts with an error (a throwing `IPrebuildSetup`, a failing test-runner task) is answered with the
  error and the errors logged during the run, and frees the lock, instead of holding it for the whole lease.

- **Asset import workers disconnected the Editor from MCP** (#1050). Every `AssetImportWorker` process
  (model and texture imports, Entities SubScene baking) ran the plugin's startup and connected to the same
  server; with no-auth, which keeps one plugin connection, a worker could replace the Editor's connection.
  Workers now skip the plugin startup and the server manager; headless CLI/CI editors are unaffected.

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
  MCP server pin (`McpServerManager.ServerVersion`) **9.2.14**. ReflectorNet 5.5.0's default `MainThread`
  recognizes the main thread by its `SynchronizationContext` instead of managed thread id 1 (Unity 7 CoreCLR on
  Windows runs the main thread with id 2); the plugin keeps its own `UnityMainThread`, so editor behavior does not
  change. McpPlugin 8.11.1 and GameDev-MCP-Server 9.2.14 are rebuilds on it. The NuGet gate generation moves to
  `UNITY_MCP_DEPS_14`.

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
