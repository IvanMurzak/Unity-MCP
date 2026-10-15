---
name: rql-mutate
description: "Change the live Unity scene with RQL set/call: bulk edits in one all-or-nothing, undoable call, saved to disk; dry_run previews."
metadata:
  generated-by: mcp-plugin-dotnet
---

# RQL / Mutate

Change the live Unity Editor scene with an RQL query whose last stage is set or call. All-or-nothing: every target and value is checked before anything is written; a failure changes nothing. Undoable in the Editor (Ctrl+Z). Changed assets, scenes and the open prefab are saved to disk automatically; an untitled scene needs SaveAs(path). dry_run=true returns the receipt ($ref, member, old, new) without changing anything. Do exactly the change requested; a read-only member fails with readonly-member instead of being approximated.
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
Object literals build values: set settings = {speed: 2, "$type": "FastSettings"}. At most 20 targets per call.

Query part: the RQL of rql-query (full guide and the sources in its description); unity.assets("t:Material"), unity.asset(path), unity.find("Name"), ref("Light#1234"). Stages: where, select, sort, first N, get <path>, recurse children. Operators: == != < > and or not, contains, starts-with, $ is Light. Paths a.b.c, list[0], map["key"]. Vectors/colors as "(1, 2, 3)" or "#ff8800"; enums as strings.

## How to Call

```bash
unity-mcp-cli run-tool rql-mutate --input '{
  "query": "string_value",
  "dry_run": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool rql-mutate --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool rql-mutate --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `query` | `string` | Yes | RQL query ending with a set or call stage, e.g. objects | where name == "Player" | set active = false |
| `dry_run` | `boolean` | No | true = preview the receipt without changing anything |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "query": {
      "type": "string"
    },
    "dry_run": {
      "type": "boolean"
    }
  },
  "required": [
    "query"
  ]
}
```

## Output

This tool does not return structured output.

