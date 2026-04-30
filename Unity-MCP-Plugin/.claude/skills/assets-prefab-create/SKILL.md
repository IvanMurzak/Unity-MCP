---
name: assets-prefab-create
description: Create a prefab from a GameObject in the current active scene. The prefab will be saved in the project assets at the specified path. Creates folders recursively if they do not exist. If the source GameObject is already a prefab instance and 'connectGameObjectToPrefab' is true, a Prefab Variant is created automatically. To create a Prefab Variant from an existing prefab asset, provide 'sourcePrefabAssetPath' instead of 'gameObjectRef'. Use 'gameobject-find' tool to find the target GameObject first.
---

# Assets / Prefab / Create

## How to Call

```bash
unity-mcp-cli run-tool assets-prefab-create --input '{
  "prefabAssetPath": "string_value",
  "gameObjectRef": "string_value",
  "sourcePrefabAssetPath": "string_value",
  "connectGameObjectToPrefab": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool assets-prefab-create --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool assets-prefab-create --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `prefabAssetPath` | `string` | Yes | Prefab asset path. Should be in the format 'Assets/Path/To/Prefab.prefab'. |
| `gameObjectRef` | `any` | No | Reference to a scene GameObject to create the prefab from. If the GameObject is already a prefab instance, a Prefab Variant is created when 'connectGameObjectToPrefab' is true. Optional if 'sourcePrefabAssetPath' is provided. |
| `sourcePrefabAssetPath` | `string` | No | Path to an existing prefab asset to create a Prefab Variant from (e.g. 'Assets/Prefabs/Base.prefab'). When provided, a temporary instance is created, saved as a Prefab Variant, and cleaned up. Optional if 'gameObjectRef' is provided. |
| `connectGameObjectToPrefab` | `boolean` | No | If true, the scene GameObject will be connected to the new prefab (becoming a prefab instance). If the source is already a prefab instance, this creates a Prefab Variant. If false, the prefab asset is created but the scene GameObject remains unchanged. Ignored when 'sourcePrefabAssetPath' is used (always creates a variant). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "prefabAssetPath": {
      "type": "string"
    },
    "gameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef"
    },
    "sourcePrefabAssetPath": {
      "type": "string"
    },
    "connectGameObjectToPrefab": {
      "type": "boolean"
    }
  },
  "$defs": {
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
    "prefabAssetPath"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef",
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    }
  },
  "$defs": {
    "System.Type": {
      "type": "string"
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0' and 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'."
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
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    }
  },
  "required": [
    "result"
  ]
}
```

