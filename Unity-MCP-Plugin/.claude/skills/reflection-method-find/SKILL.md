---
name: reflection-method-find
description: Find C# methods across every loaded assembly by name / type / parameters — including private methods. Returns serialized `MethodData` entries usable as schemas for 'reflection-method-call'.
---

# Method C# / Find

Find method in the project using C# Reflection. It looks for all assemblies in the project and finds method by its name, class name and parameters. Even private methods are available. Use 'reflection-method-call' to call the method after finding it.

## Match levels (apply to `typeName`, `MethodName`, `Parameters`)

- `typeNameMatchLevel` / `methodNameMatchLevel` (default 1 — contains ignoring case): `0` ignore filter, `1` contains-ic, `2` contains-cs, `3` starts-with-ic, `4` starts-with-cs, `5` equals-ic, `6` equals-cs.
- `parametersMatchLevel` (default 0 — ignore filter): `0` ignore, `1` count matches, `2` equals.

## Output

On match, returns a `[Success] Found N method(s):` line followed by a JSON dump of every method's `MethodData`. Pass any single entry to 'reflection-method-call' as its `filter`.

## How to Call

```bash
unity-mcp-cli run-tool reflection-method-find --input '{
  "filter": "string_value",
  "knownNamespace": false,
  "typeNameMatchLevel": 0,
  "methodNameMatchLevel": 0,
  "parametersMatchLevel": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool reflection-method-find --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool reflection-method-find --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filter` | `any` | Yes | Method reference. Used to find method in codebase of the project. |
| `knownNamespace` | `boolean` | No | Set to true if 'Namespace' is known and full namespace name is specified in the 'filter.Namespace' property. Otherwise, set to false. |
| `typeNameMatchLevel` | `integer` | No | Minimal match level for 'typeName'. 0 - ignore 'filter.typeName', 1 - contains ignoring case (default value), 2 - contains case sensitive, 3 - starts with ignoring case, 4 - starts with case sensitive, 5 - equals ignoring case, 6 - equals case sensitive. |
| `methodNameMatchLevel` | `integer` | No | Minimal match level for 'MethodName'. 0 - ignore 'filter.MethodName', 1 - contains ignoring case (default value), 2 - contains case sensitive, 3 - starts with ignoring case, 4 - starts with case sensitive, 5 - equals ignoring case, 6 - equals case sensitive. |
| `parametersMatchLevel` | `integer` | No | Minimal match level for 'Parameters'. 0 - ignore 'filter.Parameters' (default value), 1 - parameters count is the same, 2 - equals. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filter": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.MethodRef"
    },
    "knownNamespace": {
      "type": "boolean"
    },
    "typeNameMatchLevel": {
      "type": "integer"
    },
    "methodNameMatchLevel": {
      "type": "integer"
    },
    "parametersMatchLevel": {
      "type": "integer"
    }
  },
  "$defs": {
    "System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.Model.MethodRef+Parameter>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.MethodRef%2BParameter",
        "description": "Parameter of a method. Contains type and name of the parameter."
      }
    },
    "com.IvanMurzak.ReflectorNet.Model.MethodRef+Parameter": {
      "type": "object",
      "properties": {
        "typeName": {
          "type": "string",
          "description": "Type of the parameter including namespace. Sample: 'System.String', 'System.Int32', 'UnityEngine.GameObject', etc."
        },
        "name": {
          "type": "string",
          "description": "Name of the parameter. It may be empty if the name is unknown."
        }
      },
      "description": "Parameter of a method. Contains type and name of the parameter."
    },
    "com.IvanMurzak.ReflectorNet.Model.MethodRef": {
      "type": "object",
      "properties": {
        "namespace": {
          "type": "string",
          "description": "Namespace of the class. It may be empty if the class is in the global namespace or the namespace is unknown."
        },
        "typeName": {
          "type": "string",
          "description": "Class name, or substring a class name. It may be empty if the class is unknown."
        },
        "methodName": {
          "type": "string",
          "description": "Method name, or substring of the method name. It may be empty if the method is unknown."
        },
        "inputParameters": {
          "$ref": "#/$defs/System.Collections.Generic.List%3Ccom.IvanMurzak.ReflectorNet.Model.MethodRef%2BParameter%3E",
          "description": "List of input parameters. Can be null if the method has no parameters or the parameters are unknown."
        }
      },
      "description": "Method reference. Used to find method in codebase of the project."
    }
  },
  "required": [
    "filter"
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
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

