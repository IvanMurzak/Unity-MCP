---
name: script-execute
description: "Compiles and runs C# in the Unity Editor (Roslyn): plain statements with the last expression as the result, or a full class with a static method. See the skill body for the modes, usings/include and Unity object parameters."
metadata:
  generated-by: mcp-plugin-dotnet
---

# Script / Execute

## Modes

- **Snippet** (default): plain statements. The last expression is the result (`GameObject.Find("Player").name`). Own `using` lines, local classes and functions are allowed; `await` works. Usings for System, LINQ, UnityEngine, UnityEditor and AIGD are already there (`Object` and `Random` mean UnityEngine's); an unknown type name is imported automatically when exactly one namespace has it.
- **Full code**: when `csharpCode` declares class `className` with a static method `methodName`, that method runs.
- `isMethodBody=true` forces snippet mode.

Compile errors point at your own lines (`snippet.cs(2,5)`).

## Result and output

- The result is bounded: a string to 20000 characters, a collection to 1000 items; a result over 131072 characters as JSON comes back as the first 20000 characters of that JSON; errors to 20000 characters and 20 compile errors (a note says what was cut). Return a selection or a summary instead of everything.
- `Debug.Log` / `Console.WriteLine` output is not part of the result: `Debug.Log` goes to the Unity Console (read it with rql-query `console | sort sequence desc | first 10 | select logType message`).
- Code runs on the main thread and is not aborted: an endless loop freezes the Editor. An `await` gets at most 120 s.
- Every run starts fresh: static fields of your own types do not keep values from an earlier call, except a `static readonly` field of an immutable type (string, number, enum, read-only collection): identical code sets it once per domain, so read live Editor state in a method or property, not in such an initializer.

## usings / include

- `usings`: extra namespaces, e.g. `["MyGame.Gameplay"]`.
- `include`: helper `.cs` files or folders (recursive) inside `<Project>/.ai-game-dev/snippets` (paths relative to it), compiled with the snippet.

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
  "usings": "string_value",
  "include": "string_value"
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
| `parameters` | `any` | No | Parameters, each with 'name' and 'typeName': the snippet's variables or the method's arguments in order. Unity object types and value formats: see the skill. Leave empty when none. |
| `isMethodBody` | `boolean` | No | Force snippet mode even when the code declares 'className'. |
| `usings` | `any` | No | Extra namespaces for snippet mode, e.g. ["MyGame.Gameplay"]. |
| `include` | `any` | No | Helper .cs files or folders (recursive) compiled with the code, relative to <Project>/.ai-game-dev/snippets. |

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
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMemberList"
    },
    "isMethodBody": {
      "type": "boolean"
    },
    "usings": {
      "$ref": "#/$defs/System.String-1"
    },
    "include": {
      "$ref": "#/$defs/System.String-1"
    }
  },
  "$defs": {
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMember": {
      "type": "object",
      "properties": {
        "typeName": {
          "type": "string",
          "description": "Full type name. Eg: 'System.String', 'System.Int32', 'UnityEngine.Vector3', etc."
        },
        "name": {
          "type": "string",
          "description": "Object name."
        },
        "value": {
          "description": "Value of the object, serialized as a non stringified JSON element. Can be null if the value is not set. Can be default value if the value is an empty object or array json."
        },
        "fields": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested field value."
          },
          "description": "Fields of the object, serialized as a list of 'SerializedMember'."
        },
        "props": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested property value."
          },
          "description": "Properties of the object, serialized as a list of 'SerializedMember'."
        }
      },
      "required": [
        "typeName"
      ],
      "additionalProperties": false
    },
    "System.String-1": {
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

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
    }
  },
  "$defs": {
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMember": {
      "type": "object",
      "properties": {
        "typeName": {
          "type": "string",
          "description": "Full type name. Eg: 'System.String', 'System.Int32', 'UnityEngine.Vector3', etc."
        },
        "name": {
          "type": "string",
          "description": "Object name."
        },
        "value": {
          "description": "Value of the object, serialized as a non stringified JSON element. Can be null if the value is not set. Can be default value if the value is an empty object or array json."
        },
        "fields": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested field value."
          },
          "description": "Fields of the object, serialized as a list of 'SerializedMember'."
        },
        "props": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested property value."
          },
          "description": "Properties of the object, serialized as a list of 'SerializedMember'."
        }
      },
      "required": [
        "typeName"
      ],
      "additionalProperties": false
    }
  },
  "required": [
    "result"
  ]
}
```

