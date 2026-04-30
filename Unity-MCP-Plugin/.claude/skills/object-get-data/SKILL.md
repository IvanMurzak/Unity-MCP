---
name: object-get-data
description: |-
  Get data of the specified Unity Object. Returns serialized data of the object including its properties and fields. If need to modify the data use 'object-modify' tool.
  
  Path-scoped reads (token-saving): supply 'paths' (a list of paths) to read only the listed fields/elements via Reflector.TryReadAt, or 'viewQuery' (a ViewQuery) to navigate to a subtree and/or filter by name regex / max depth / type via Reflector.View. These two parameters are mutually exclusive — supply at most one. When neither is supplied the full object is serialized as before (backwards compatible).
  Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.
---

# Object / Get Data

## How to Call

```bash
unity-mcp-cli run-tool object-get-data --input '{
  "objectRef": "string_value",
  "paths": "string_value",
  "viewQuery": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool object-get-data --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool object-get-data --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `objectRef` | `any` | Yes | Reference to UnityEngine.Object instance. It could be GameObject, Component, Asset, etc. Anything extended from UnityEngine.Object. |
| `paths` | `any` | No | Optional. List of paths to read individually via Reflector.TryReadAt. Each path may target a different depth. Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Mutually exclusive with 'viewQuery'. |
| `viewQuery` | `any` | No | Optional. View-query filter routed through Reflector.View — combines a starting Path, a case-insensitive NamePattern regex, MaxDepth, and an optional TypeFilter. Mutually exclusive with 'paths'. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "objectRef": {
      "$ref": "#/$defs/AIGD.ObjectRef"
    },
    "paths": {
      "$ref": "#/$defs/System.Collections.Generic.List<System.String>"
    },
    "viewQuery": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.ViewQuery"
    }
  },
  "$defs": {
    "AIGD.ObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'."
        }
      },
      "required": [
        "instanceID"
      ],
      "description": "Reference to UnityEngine.Object instance. It could be GameObject, Component, Asset, etc. Anything extended from UnityEngine.Object."
    },
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "System.Type": {
      "type": "string"
    },
    "com.IvanMurzak.ReflectorNet.Model.ViewQuery": {
      "type": "object",
      "properties": {
        "Path": {
          "type": "string",
          "description": "Navigate to this path first, then serialize only that subtree. Path segments are separated by '/'. Use '[i]' for array/list index (e.g. 'users/[2]/name') and '[key]' for dictionary entry (e.g. 'config/[timeout]'). A leading '#/' is stripped automatically. Examples: 'admin/name', 'users/[0]/email', 'config/[timeout]'. Leave null to start from the root object."
        },
        "NamePattern": {
          "type": "string",
          "description": "Case-insensitive .NET regex pattern matched against field and property names. Only branches containing at least one match are kept in the result tree. Examples: 'orbitRadius' (exact name), 'orbit.*' (prefix match), 'radius|speed' (either name). When nothing matches, the root envelope is returned with empty fields/props. Leave null to return all fields and properties without filtering."
        },
        "MaxDepth": {
          "type": "integer",
          "description": "Maximum nesting depth of the returned serialized tree. 0 = root type name and value only — no nested fields or properties. 1 = one level of fields/props visible, their children stripped. 2 = two levels visible, and so on. Leave null (default) for unlimited depth."
        },
        "TypeFilter": {
          "$ref": "#/$defs/System.Type",
          "description": "When set, prunes the result tree to members whose runtime type is assignable to this type. Non-matching branches are removed; the root envelope is always preserved. Examples: typeof(float) keeps only float fields, typeof(IEnumerable) keeps only collections. Leave null to include members of any type."
        }
      }
    }
  },
  "required": [
    "objectRef"
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

