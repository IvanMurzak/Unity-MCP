---
name: object-modify
description: Modify the specified Unity Object. Allows direct modification of object fields and properties. Use 'object-get-data' first to inspect the object structure before modifying.
---

# Object / Modify

## How to Call

```bash
unity-mcp-cli run-tool object-modify --input '{
  "objectRef": "string_value",
  "objectDiff": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool object-modify --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool object-modify --input-file - <<'EOF'
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
| `objectDiff` | `any` | Yes | The object data to apply. Should contain 'fields' and/or 'props' with the values to modify.
Only include the fields/properties you want to change.
Any unknown or invalid fields and properties will be reported in the response. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "objectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ObjectRef"
    },
    "objectDiff": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ObjectRef": {
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
    "objectRef",
    "objectDiff"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Object+ModifyObjectResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ObjectRef": {
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
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
    },
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Object+ModifyObjectResponse": {
      "type": "object",
      "properties": {
        "Success": {
          "type": "boolean",
          "description": "Whether the modification was successful."
        },
        "Reference": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ObjectRef",
          "description": "Reference to the modified object."
        },
        "Data": {
          "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
          "description": "Updated object data after modification."
        },
        "Logs": {
          "$ref": "#/$defs/System.String[]",
          "description": "Log of modifications made and any warnings/errors encountered."
        }
      },
      "required": [
        "Success"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

