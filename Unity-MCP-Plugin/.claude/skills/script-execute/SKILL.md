---
name: script-execute
description: "Compiles and runs C# in the Unity Editor (Roslyn): plain statements with the last expression as the result, or a full class with a static method. See the skill body for the modes, usings/include and Unity object parameters."
metadata:
  generated-by: mcp-plugin-dotnet
---

# Script / Execute

## Modes

- **Snippet** (default): plain statements. The last expression is the result (`GameObject.Find("Player").name`). Own `using` lines, local classes and functions are allowed; `await` works. Usings for System, LINQ, UnityEngine, UnityEditor and AIGD are already there; an unknown type name is imported automatically when exactly one namespace has it.
- **Full code**: when `csharpCode` declares class `className` with a static method `methodName`, that method runs.
- `isMethodBody=true` forces snippet mode.

Compile errors point at your own lines (`snippet.cs(2,5)`).

## usings / include

- `usings`: extra namespaces, e.g. `["MyGame.Gameplay"]`.
- `include`: helper `.cs` files or folders (recursive), relative to `<Project>/.ai-game-dev/snippets`, compiled with the snippet.

## Passing Unity objects as parameters

Parameters become the snippet's variables (or the method's arguments, in order):

- `UnityEngine.GameObject` — resolves an actual GameObject from value `{"instanceID": N}`, `{"name": "..."}`, or `{"path": "..."}`.
- `UnityEngine.Component` (or any component subtype) — resolves from `{"instanceID": N}`.
- `AIGD.GameObjectRef` — passes a `GameObjectRef` POCO directly; call `goRef.FindGameObject()` to resolve it.
- `AIGD.ComponentRef` — passes a `ComponentRef` POCO.
- `AIGD.ObjectRef` — passes a base `ObjectRef` POCO.

## How to Call

```bash
unity-mcp-cli run-tool script-execute --input '{
  "csharpCode": "string_value",
  "className": "string_value",
  "methodName": "string_value",
  "parameters": "string_value",
  "isMethodBody": false,
  "usings": [],
  "include": []
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool script-execute --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool script-execute --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `csharpCode` | `string` | Yes | C# code. Snippet: 'var go = GameObject.Find("Player");\ngo.transform.position'. Full: 'public class Script { public static int Main() { return 42; } }'. |
| `className` | `string` | No | Full mode: the class whose static method runs (namespace-qualified or not). |
| `methodName` | `string` | No | Full mode: the static method to run. |
| `parameters` | `string` | No | Values for the snippet's / method's parameters by name, e.g. {go: ref("GameObject#12"), n: 6}. Leave empty if there are none. RQL: SerializedMemberList |
| `isMethodBody` | `boolean` | No | Force snippet mode even when the code declares 'className'. |
| `usings` | `array` | No | Extra namespaces for snippet mode, e.g. ["MyGame.Gameplay"]. |
| `include` | `array` | No | Helper .cs files or folders (recursive) compiled with the code, relative to <Project>/.ai-game-dev/snippets. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "csharpCode": {
      "type": "string"
    },
    "className": {
      "type": "string"
    },
    "methodName": {
      "type": "string"
    },
    "parameters": {
      "type": "string"
    },
    "isMethodBody": {
      "type": "boolean"
    },
    "usings": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "include": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "csharpCode"
  ]
}
```

## Output

This tool does not return structured output.

