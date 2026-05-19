---
name: assets-prefab-open
description: Open the prefab edit stage for a prefab instance or prefab asset GameObject. Modifications inside the edit stage propagate to all instances. Pair with 'assets-prefab-close' to exit the stage when done.
---

# Assets / Prefab / Open

Open prefab edit mode for a specific GameObject. In the Edit mode you can modify the prefab. The modification will be applied to all instances of the prefab across the project. Note: Please use 'assets-prefab-close' tool later to exit prefab editing mode.

## Inputs

- `gameObjectRef` — reference to a scene prefab instance OR a prefab asset GameObject. The tool resolves the prefab asset path via `PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot` and opens the appropriate prefab stage.

## Behavior

Asset-side GameObjects open via the simple `OpenPrefab(path)` overload. Scene-instance GameObjects open via `OpenPrefab(path, gameObject)` so the editor remembers which instance prompted the edit. Editor windows are repainted before returning. Throws when the GameObject cannot be resolved or the stage fails to open.

## How to Call

```bash
unity-mcp-cli run-tool assets-prefab-open --input '{
  "gameObjectRef": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool assets-prefab-open --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool assets-prefab-open --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | GameObject that represents prefab instance of an original prefab GameObject. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
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
    "gameObjectRef"
  ]
}
```

## Output

This tool does not return structured output.

