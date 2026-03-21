---
name: gameobject-component-add
description: Add Component to GameObject in opened Prefab or in a Scene. Use 'gameobject-find' tool to find the target GameObject first. Use 'gameobject-component-list-all' tool to find the component type names to add.
---

# GameObject / Component / Add

## How to Call

```bash
unity-mcp-cli run-tool gameobject-component-add --input '{
  "componentNames": "string_value",
  "gameObjectRef": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool gameobject-component-add --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool gameobject-component-add --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `componentNames` | `any` | Yes | Full name of the Component. It should include full namespace path and the class name. |
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "componentNames": {
      "$ref": "#/$defs/System.String[]"
    },
    "gameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef"
    }
  },
  "$defs": {
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "System.Type": {
      "type": "string"
    },
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
    }
  },
  "required": [
    "componentNames",
    "gameObjectRef"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_GameObject+AddComponentResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow"
      }
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer"
        },
        "typeName": {
          "type": "string"
        },
        "isEnabled": {
          "type": "string",
          "enum": [
            "False",
            "True",
            "NA"
          ]
        }
      },
      "required": [
        "instanceID",
        "isEnabled"
      ]
    },
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_GameObject+AddComponentResponse": {
      "type": "object",
      "properties": {
        "AddedComponents": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow>",
          "description": "List of successfully added components."
        },
        "Messages": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "List of success messages for added components."
        },
        "Warnings": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "List of warnings encountered during component addition."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "List of errors encountered during component addition."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

