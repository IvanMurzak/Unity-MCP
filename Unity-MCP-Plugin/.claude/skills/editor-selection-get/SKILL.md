---
name: editor-selection-get
description: Get information about the current Selection in the Unity Editor. Use 'editor-selection-set' tool to set the selection.
---

# Editor / Selection / Get

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool editor-selection-get --input '{
  "includeGameObjects": false,
  "includeTransforms": false,
  "includeInstanceIDs": false,
  "includeAssetGUIDs": false,
  "includeActiveObject": false,
  "includeActiveTransform": false
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `includeGameObjects` | `boolean` | No |  |
| `includeTransforms` | `boolean` | No |  |
| `includeInstanceIDs` | `boolean` | No |  |
| `includeAssetGUIDs` | `boolean` | No |  |
| `includeActiveObject` | `boolean` | No |  |
| `includeActiveTransform` | `boolean` | No |  |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "includeGameObjects": {
      "type": "boolean"
    },
    "includeTransforms": {
      "type": "boolean"
    },
    "includeInstanceIDs": {
      "type": "boolean"
    },
    "includeAssetGUIDs": {
      "type": "boolean"
    },
    "includeActiveObject": {
      "type": "boolean"
    },
    "includeActiveTransform": {
      "type": "boolean"
    }
  }
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Editor_Selection+SelectionData"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
        "description": "Find GameObject in opened Prefab or in the active Scene."
      }
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
    },
    "System.Type": {
      "type": "string"
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef",
        "description": "Component reference. Used to find a Component at GameObject."
      }
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef": {
      "type": "object",
      "properties": {
        "index": {
          "type": "integer",
          "description": "Component 'index' attached to a gameObject. The first index is '0' and that is usually Transform or RectTransform. Priority: 2. Default value is -1."
        },
        "typeName": {
          "type": "string",
          "description": "Component type full name. Sample 'UnityEngine.Transform'. If the gameObject has two components of the same type, the output component is unpredictable. Priority: 3. Default value is null."
        },
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'."
        }
      },
      "required": [
        "index",
        "instanceID"
      ],
      "description": "Component reference. Used to find a Component at GameObject."
    },
    "System.Int32[]": {
      "type": "array",
      "items": {
        "type": "integer"
      }
    },
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
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
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Editor_Selection+SelectionData": {
      "type": "object",
      "properties": {
        "GameObjects": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef[]",
          "description": "Returns the actual game object selection. Includes Prefabs, non-modifiable objects."
        },
        "Transforms": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef[]",
          "description": "Returns the top level selection, excluding Prefabs."
        },
        "InstanceIDs": {
          "$ref": "#/$defs/System.Int32[]",
          "description": "The actual unfiltered selection from the Scene returned as instance ids instead of objects."
        },
        "AssetGUIDs": {
          "$ref": "#/$defs/System.String[]",
          "description": "Returns the guids of the selected assets."
        },
        "ActiveGameObject": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
          "description": "Returns the active game object. (The one shown in the inspector)."
        },
        "ActiveInstanceID": {
          "type": "integer",
          "description": "Returns the instanceID of the actual object selection. Includes Prefabs, non-modifiable objects"
        },
        "ActiveObject": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ObjectRef",
          "description": "Returns the actual object selection. Includes Prefabs, non-modifiable objects."
        },
        "ActiveTransform": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentRef",
          "description": "Returns the active transform. (The one shown in the inspector)."
        }
      },
      "required": [
        "ActiveInstanceID"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

