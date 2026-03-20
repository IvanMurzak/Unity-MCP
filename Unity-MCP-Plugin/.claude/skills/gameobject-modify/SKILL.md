---
name: gameobject-modify
description: Modify GameObject fields and properties in opened Prefab or in a Scene. You can modify multiple GameObjects at once. Just provide the same number of GameObject references and SerializedMember objects.
---

# GameObject / Modify

## How to Call

```bash
unity-mcp-cli run-tool gameobject-modify --input '{
  "gameObjectRefs": "string_value",
  "gameObjectDiffs": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool gameobject-modify --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool gameobject-modify --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRefs` | `any` | Yes | Array of GameObjects in opened Prefab or in the active Scene. |
| `gameObjectDiffs` | `any` | Yes | Each item in the array represents a GameObject modification of the 'gameObjectRefs' at the same index. Usually a GameObject is a container for components. Each component may have fields and properties for modification. If you need to modify components of a GameObject, please use 'gameobject-component-modify' tool. Ignore values that should not be modified. Any unknown or wrong located fields and properties will be ignored. Check the result of this command to see what was changed. The ignored fields and properties will be listed. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRefs": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRefList"
    },
    "gameObjectDiffs": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMemberList"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If it is '0' and 'path', 'name', 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'. Priority: 1 (Recommended)"
        },
        "path": {
          "type": "string",
          "description": "Path of a GameObject in the hierarchy Sample 'character/hand/finger/particle'. Priority: 2."
        },
        "name": {
          "type": "string",
          "description": "Name of a GameObject in hierarchy. Priority: 3."
        },
        "assetType": {
          "$ref": "#/$defs/System.Type",
          "description": "Type of the asset."
        },
        "assetPath": {
          "type": "string",
          "description": "Path to the asset within the project. Starts with 'Assets/'"
        },
        "assetGuid": {
          "type": "string",
          "description": "Unique identifier for the asset."
        }
      },
      "required": [
        "instanceID"
      ],
      "description": "Find GameObject in opened Prefab or in the active Scene."
    },
    "System.Type": {
      "type": "string"
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRefList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
        "description": "Find GameObject in opened Prefab or in the active Scene."
      },
      "description": "Array of GameObjects in opened Prefab or in the active Scene."
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
    "gameObjectRefs",
    "gameObjectDiffs"
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
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.Logs"
    }
  },
  "$defs": {
    "com.IvanMurzak.ReflectorNet.Model.LogEntry": {
      "type": "object",
      "properties": {
        "Depth": {
          "type": "integer"
        },
        "Message": {
          "type": "string"
        },
        "Type": {
          "type": "string",
          "enum": [
            "Trace",
            "Debug",
            "Info",
            "Success",
            "Warning",
            "Error",
            "Critical"
          ]
        }
      },
      "required": [
        "Depth",
        "Type"
      ]
    },
    "com.IvanMurzak.ReflectorNet.Model.Logs": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.LogEntry"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

