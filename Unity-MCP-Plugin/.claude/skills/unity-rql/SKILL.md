---
name: unity-rql
description: |-
  How to read and change anything in the Unity Editor with RQL (rql-query / rql-mutate / rql-schema): the query
  language, operators, sources (scene objects, assets, scenes, prefabs, console logs, Editor state), Editor operations
  (create, add components, scenes, prefabs, assets, selection, Play Mode) and what installed extensions add.
metadata:
  generated-by: mcp-plugin-dotnet
---
# RQL — query and change the Unity Editor

RQL is one pipeline language for the whole Editor: find objects, read members, filter, aggregate, and change them in one
call. Prefer one precise query over many exploratory calls.

- `rql-query` — read-only queries, compact table results.
- `rql-mutate` — queries ending with `set` / `call`; all-or-nothing, undoable (Ctrl+Z), `dry_run: true` previews.
- `rql-schema` — sources and component types in the scene; `rql-schema(type)` lists one type's members and methods.

```bash
unity-mcp-cli run-tool rql-query --input '{"query": "objects | where name contains \"Enemy\" | select $ref name"}'
unity-mcp-cli run-tool rql-mutate --input '{"query": "objects | where name == \"Player\" | set active = false"}'
```

## Language

```
RQL: source | stage | stage ...
Sources: objects (every GameObject in the loaded scenes, inactive included), roots, scenes, selection, prefab (the prefab open in prefab editing mode), console (Editor log entries), shaders, componentTypes, profiler (memory / rendering / frame snapshots; call Start / Stop / Save), tests (test methods; run a selection with tests-run query), testResults (last run), editor (Editor state and operations). Assets: unity.assets("t:Material") or unity.assets("t:Prefab", "Assets/Enemies"), unity.asset("Assets/Mats/Red.mat"). ref("Light#1234") resolves a $ref; unity.find("Player") or unity.find("Level/Pillar_1") a GameObject by unique name or path.
Stages:
  where <predicate>                  filter
  select a b.c alias=(expr)          project columns (wrap computed expressions in parentheses)
  sort <expr> [desc], ...            stable sort
  first N | skip N | count
  get <path>                         replace each item by a member (lists are flattened)
  recurse children [max-depth=N] [include-self=false]   walk the hierarchy (input items included by default)
  distinct [expr] | group-by expr | sum expr | min expr | max expr | avg expr
Expressions: == != < <= > >= and or not + - * / %; contains, starts-with, ends-with (case-insensitive); matches "regex"; x in [a, b]; x is null; x is not null; $ is Light (includes subclasses); has(member). Literals: numbers, "strings", true/false/null, lists [a, b], objects {x: 1, y: 2}.
Paths: a.b.c, list[0], map["key"]; .member on a list maps over its elements. List methods: .any(pred) .all(pred) .count([pred]) .sum(e) .min(e) .max(e) .first([pred]); inside them bare names refer to the element, falling back to the outer item.
Meta: $ (current item), $ref (stable id such as Light#1234), $type, $path, $index, $depth. Enums compare to strings (type == "Spot"); == on strings is case-sensitive.
Unity: GameObject has components (list), children (list), active, name, tag, layer, transform (position, localPosition, eulerAngles, localScale, parent). Component members are the C# members (public + [SerializeField]); find them via objects | get components | where $ is Light. Vectors/colors print as (x, y, z); write "(1, 2, 3)", [1, 2, 3] or {x: 1, y: 2, z: 3}; colors also "#ff8800". Material shader properties: properties["_Color"]. Destroyed objects read as null (missing references: where target == null). Navigate with paths or get; select only projects output columns.
Results come back as compact rows (header line, then col|col). Long results are truncated — refine with where/first or use count. Unknown members produce warnings with suggestions; rql-schema(type) lists a type's members.
```

## Changing things (rql-mutate)

```
  set path = expr, ...   assign members (paths may go through structs: set transform.position.y = transform.position.y + 1)
  call Method(args)      invoke a method the tools expose
Examples:
  objects | where name == "Player" | set transform.position = "(0, 1, 0)"
  objects | get components | where $ is Light | set intensity = intensity * 2, color = "#ffcc88"
  objects | where tag == "Enemy" | set active = false
  unity.asset("Assets/Mats/Red.mat") | set properties["_Color"] = "(1, 0, 0, 1)"
  objects | where name == "Spawner" | set targets = append(targets, ref("GameObject#1234"))
Create / add / remove (call; the receipt's result column holds the new object's $ref for follow-up queries):
  editor | call CreateGameObject("Props")                                   new root GameObject
  editor | call CreateGameObject("Crate", ref("GameObject#1234"), "Cube")    child of a parent, optional primitive
  objects | where name == "Crate" | call AddComponent("Rigidbody")           add a component by type name
  objects | where name == "Old" | call Destroy()                             remove a GameObject (components: get components | where $ is X | call Destroy())
  objects | where name == "Lamp" | set transform.parent = ref("GameObject#1234")   reparent (keeps the world position)
  objects | where name == "Crate" | call Duplicate()                          copy next to it (returns the copy)
Scenes, prefabs, assets, Editor state:
  editor | call OpenScene("Assets/Scenes/Level.unity")          editor | call NewScene("Assets/Scenes/New.unity")
  scenes | where path == "" | call SaveAs("Assets/Scenes/New.unity")   scenes | where name == "Level" | call SetActive()
  editor | call InstantiatePrefab("Assets/Prefabs/Enemy.prefab", ref("GameObject#1234"), "(0, 0, 5)")
  objects | where name == "Crate" | call SaveAsPrefab("Assets/Prefabs/Crate.prefab")
  editor | call OpenPrefab("Assets/Prefabs/Enemy.prefab")  then  prefab | recurse children | …  then  editor | call ClosePrefab()
  editor | call CreateMaterial("Assets/Mats/Glow.mat")        editor | call MoveAsset("Assets/A.mat", "Assets/Mats/A.mat")
  editor | set selection = [ref("GameObject#1234")]         editor | set isPlaying = true
  profiler | call Start()      profiler | call Save("Logs/profile.json")      (read: profiler | select memory frame)
```

## Sources

| Source | What |
|---|---|
| `componentTypes` | component types that can be added (name fullName namespace) |
| `console` | captured Editor log entries, oldest first (last 1000): logType message timestamp stackTrace sequence |
| `editor` | Editor state (isPlaying, isPaused, selection …) and operations: CreateGameObject, OpenScene, NewScene, SaveScenes, InstantiatePrefab, OpenPrefab / SavePrefab / ClosePrefab, CopyAsset / MoveAsset / DeleteAsset / CreateFolder, CreateMaterial, SaveAssets, RefreshAssets, ClearConsole, ExecuteMenuItem (rql-schema "editor" for signatures) |
| `objects` | every GameObject in the loaded scenes, depth-first, inactive ones included |
| `prefab` | root GameObject of the prefab open in prefab editing mode (none otherwise) |
| `profiler` | Unity profiler: enabled supported memory rendering script frame modules (live snapshots, MB / ms); calls Start, Stop, ClearFrames, EnableModule, Save, Load (rql-schema "profiler" for signatures) |
| `roots` | root GameObjects of the loaded scenes |
| `scenes` | loaded scenes |
| `selection` | objects selected in the Editor |
| `shaders` | every shader the Editor knows (built-in and project): name supported hasErrors |
| `testResults` | results of the last tests-run: name status durationMs message stackTrace |
| `tests` | test methods (fullName name fixture namespace assembly mode categories parameterized explicit ignored); run a selection with tests-run query, e.g. tests \| where fixture == "PlayerTests" |
| `unity.asset(…)` | unity.asset(path): the main asset at a path, e.g. "Assets/Mats/Red.mat" |
| `unity.assets(…)` | unity.assets(filter, folder?): AssetDatabase search, e.g. "t:Material", "t:Prefab Enemy" (at most 200, loaded as needed) |
| `unity.find(…)` | unity.find(name): the GameObject with that unique name or hierarchy path ("Level/Pillar_1"), e.g. set Follow = unity.find("Player") |

## Installed RQL modules

- `com.IvanMurzak.Unity.MCP.Editor.Rql.Modules.UnityCoreModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.ToolTypesModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Mathematics.MathematicsModule`
- `com.IvanMurzak.Unity.MCP.Editor.Rql.Physics3D.PhysicsModule`
