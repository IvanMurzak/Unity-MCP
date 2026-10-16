---
name: rql-query
description: "Query the live Unity scene (GameObjects, components, assets) with RQL in ONE call: filter, project, sort, count, walk hierarchies. Far fewer calls/tokens than reading objects one by one."
metadata:
  generated-by: mcp-plugin-dotnet
---

# RQL / Query

Run a read-only RQL query against the live Unity Editor scene and return compact rows. One precise query beats many exploratory calls. Changes need rql-mutate.

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

Examples:
  objects | where name contains "Enemy" and active | select $ref name transform.position
  objects | get components | where $ is Light and intensity > 2 | select gameObject.name intensity color
  roots | recurse children | where components.count() == 1 | select $path name
  unity.assets("t:Material") | select name shader.name properties["_Color"]
  console | where logType == "Error" | sort sequence desc | first 5 | select message
  scenes | select name path isDirty rootCount
  componentTypes | where name contains "Collider" | select name
  editor | select isPlaying isPaused selection

## How to Call

```bash
unity-mcp-cli run-tool rql-query --input '{
  "query": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool rql-query --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool rql-query --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `query` | `string` | Yes | RQL query, e.g. objects | where $ is ... | select $ref name |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "query": {
      "type": "string"
    }
  },
  "required": [
    "query"
  ]
}
```

## Output

This tool does not return structured output.

