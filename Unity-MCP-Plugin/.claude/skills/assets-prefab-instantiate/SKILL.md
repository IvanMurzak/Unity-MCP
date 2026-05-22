---
name: assets-prefab-instantiate
description: Instantiate a prefab into the currently active scene at an optional position/rotation/scale, parented under an optional scene GameObject path. Use 'assets-find' to locate the prefab asset first.
---

# Assets / Prefab / Instantiate

Instantiates prefab in the current active scene. Use 'assets-find' tool to find prefab assets in the project.

## Inputs

- `prefabAssetPath` — project asset path of the prefab to instantiate.
- `gameObjectPath` — destination path in the scene; the last segment becomes the new GameObject's name, any prefix is looked up as the parent (must already exist).
- `position` / `rotation` / `scale` — optional transform; default to zero / zero / one.
- `isLocalSpace` — when `true`, applies the transform in local space relative to the parent.

## How to Call

```bash
unity-mcp-cli run-tool assets-prefab-instantiate --input '{
  "prefabAssetPath": "string_value",
  "gameObjectPath": "string_value",
  "position": "string_value",
  "rotation": "string_value",
  "scale": "string_value",
  "isLocalSpace": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool assets-prefab-instantiate --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool assets-prefab-instantiate --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `prefabAssetPath` | `string` | Yes | Prefab asset path. |
| `gameObjectPath` | `string` | Yes | GameObject path in the current active scene. |
| `position` | `any` | No | Transform position of the GameObject. |
| `rotation` | `any` | No | Transform rotation of the GameObject. Euler angles in degrees. |
| `scale` | `any` | No | Transform scale of the GameObject. |
| `isLocalSpace` | `boolean` | No | World or Local space of transform. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "prefabAssetPath": {
      "type": "string"
    },
    "gameObjectPath": {
      "type": "string"
    },
    "position": {
      "$ref": "#/$defs/UnityEngine.Vector3"
    },
    "rotation": {
      "$ref": "#/$defs/UnityEngine.Vector3"
    },
    "scale": {
      "$ref": "#/$defs/UnityEngine.Vector3"
    },
    "isLocalSpace": {
      "type": "boolean"
    }
  },
  "$defs": {
    "UnityEngine.Vector3": {
      "type": "object",
      "properties": {
        "x": {
          "type": "number"
        },
        "y": {
          "type": "number"
        },
        "z": {
          "type": "number"
        }
      },
      "required": [
        "x",
        "y",
        "z"
      ],
      "additionalProperties": false
    }
  },
  "required": [
    "prefabAssetPath",
    "gameObjectPath"
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
      "$ref": "#/$defs/AIGD.GameObjectRef",
      "description": "Find GameObject in opened Prefab or in the active Scene."
    }
  },
  "$defs": {
    "System.Type": {
      "type": "string"
    },
    "AIGD.GameObjectRef": {
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
    "result"
  ]
}
```

