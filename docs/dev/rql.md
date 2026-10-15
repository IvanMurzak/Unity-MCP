# RQL in Unity-MCP (experimental branch `experiment/rql`)

Local experiment, not for release. The data layer of the Editor tools moves from ReflectorNet-based tools to RQL
(Reflection Query Language, private repo `IvanMurzak/RQL`): one query language instead of one tool per operation.
McpPlugin and its tool plumbing (ReflectorNet for tool parameters and the Runtime path) are unchanged.

## Tools

| Before | Now |
|---|---|
| `gameobject-*`, `gameobject-component-*`, `object-*` | `rql-query` / `rql-mutate` on `objects`, `roots`, `selection` (`set`, `call AddComponent / Destroy / Duplicate`) |
| `assets-find`, `-get-data`, `-modify`, `-shader-*` | `unity.assets("t:Material")`, `unity.asset(path)`, `shaders` |
| `assets-copy / move / delete / create-folder / refresh / material-create` | `editor \| call CopyAsset / MoveAsset / DeleteAsset / CreateFolder / RefreshAssets / CreateMaterial` |
| `assets-prefab-*` | `editor \| call InstantiatePrefab / OpenPrefab / SavePrefab / ClosePrefab`, `prefab` source, `call SaveAsPrefab(path)` |
| `scene-*` | `scenes` source (`call Save / SaveAs / SetActive / Close`), `editor \| call OpenScene / NewScene / SaveScenes` |
| `editor-application-*`, `editor-selection-*` | `editor` members `isPlaying`, `isPaused`, `selection` (writable) |
| `console-get-logs / clear-logs` | `console` source (`where logType == "Error"`, `sequence > N`), `editor \| call ClearConsole()` |
| `reflection-method-*`, `type-get-json-schema` | RQL `call`, `rql-schema` |
| `profiler-*` (12 tools) | `profiler` source: `enabled`, `supported`, `memory`, `rendering`, `script`, `frame`, `modules`; `profiler \| call Start / Stop / ClearFrames / EnableModule / Save / Load` |
| `tests-run` filters | `tests` source (`fullName name fixture namespace assembly mode categories parameterized explicit ignored`); `tests-run query` runs an RQL selection as one run, `result` filters `testResults` |

Kept as tools: `script-*`, `tests-run`, `screenshot-*`, `package-*`, `unity-tool-list`,
`tool-set-enabled-state`, `ping`, skills tools.

## Code

- `Editor/Plugins/Rql/` — RQL DLLs (netstandard2.1, Editor-only). Rebuild with `shared/RQL/unity/build-rql-dlls.sh` and copy.
- `Editor/Scripts/Rql/` — engine (`RqlUnity`: Undo group per mutation, dirty marking; `RqlUnity.Mutate` saves assets,
  scenes and the open prefab after every `rql-mutate`, modules flush deferred file writes on `RqlUnity.Saving`), `RqlEditorApi` (the `editor`
  source), `RqlProfiler` (the `profiler` source), `RqlTests` (`tests` / `testResults`), schema, type index, guide texts, `Modules/UnityCoreModule.cs`, one adapter per file in `Adapters/`.
- `Editor/Scripts/API/Tool/Rql*.cs` — the tools; `Editor/Scripts/Skills/Skill_Rql.cs` — the `unity-rql` skill.
- `Editor/RqlModules/<Feature>/` — optional modules in their own asmdef, compiled only when the Unity module exists
  (`versionDefines` + `defineConstraints`): Physics. Feature modules live in the extensions (e.g. the Particle System
  module and its playback methods are in `Unity-AI-ParticleSystem`).

## Extensions

An extension contributes an `IRqlModule` (discovered through `TypeCache`): adapters for its types, sources / functions,
and `ctx.AddGuide(topic, markdown)`. The guide is rendered into the `unity-rql` skill, so agents learn the extension's
sources and literals as soon as it is installed.

## Tests

`Tests/Editor/Rql/` (adapters one file each, Editor API, skill), `Tests/EditorRqlModules/<Feature>/`. Local run
(macOS, Unity 6000.6.4f1, project `Unity-Tests/6000.6.3f1`):

```bash
U=/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity
"$U" -batchmode -nographics -projectPath Unity-Tests/6000.6.3f1 -runTests -testPlatform EditMode -testResults results.xml
```

`-nographics` fails 16 existing screenshot / overlay / profiler tests (no graphics device); they pass with graphics.
Chain fixtures (`tests/chain-fixtures/`) are not re-recorded in this branch (they record the tool list per Unity version).
