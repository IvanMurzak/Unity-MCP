---
name: gameobject-set-parent
description: Reparent a batch of GameObjects under a new parent in the currently opened Prefab or active Scene. Per-item failures are reported in the returned status string instead of aborting the batch. Use 'gameobject-find' to locate the GameObjects first.
---

# GameObject / Set Parent

Set parent GameObject to list of GameObjects in opened Prefab or in a Scene. Use 'gameobject-find' tool to find the target GameObjects first.

## Inputs

- `gameObjectRefs` — list of children to reparent.
- `parentGameObjectRef` — new parent. Must resolve, otherwise the call returns early with an error string.
- `worldPositionStays` (default `true`) — preserve world-space transform when reparenting (passed to `Transform.SetParent`).

## Behavior

Iterates `gameObjectRefs` and reparents each one independently; per-item resolve errors are appended to the returned status string instead of throwing. After the loop, if at least one reparent succeeded, marks the active scene dirty and repaints editor windows.

## How to Call

```bash
unity-mcp-cli run-tool gameobject-set-parent --input '{
  "gameObjectRefs": "string_value",
  "parentGameObjectRef": "string_value",
  "worldPositionStays": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool gameobject-set-parent --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool gameobject-set-parent --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRefs` | `any` | Yes | List of references to the GameObjects to set new parent. |
| `parentGameObjectRef` | `any` | Yes | Reference to the parent GameObject. |
| `worldPositionStays` | `boolean` | No | A boolean flag indicating whether the GameObject's world position should remain unchanged when setting its parent. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRefs": {
      "$ref": "#/$defs/AIGD.GameObjectRefList"
    },
    "parentGameObjectRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
    },
    "worldPositionStays": {
      "type": "boolean"
    }
  },
  "$defs": {
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
    },
    "System.Type": {
      "type": "string"
    },
    "AIGD.GameObjectRefList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.GameObjectRef",
        "description": "Find GameObject in opened Prefab or in the active Scene."
      },
      "description": "Array of GameObjects in opened Prefab or in the active Scene."
    }
  },
  "required": [
    "gameObjectRefs",
    "parentGameObjectRef"
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

