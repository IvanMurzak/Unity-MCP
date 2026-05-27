---
name: gameobject-component-add
description: Add one or more Components to a GameObject in the opened Prefab or active Scene. Component types are looked up by full name (with namespace) or by class-name fallback. Use 'gameobject-find' to locate the host GameObject and 'gameobject-component-list-all' to discover valid component type names.
---

# GameObject / Component / Add

Add Component to GameObject in opened Prefab or in a Scene. Use 'gameobject-find' tool to find the target GameObject first. Use 'gameobject-component-list-all' tool to find the component type names to add.

## Inputs

- `componentNames` — list of component type names. Each entry may be a fully-qualified type name (preferred) or a bare class name (resolved via fallback to `AllComponentTypes`).
- `gameObjectRef` — the target GameObject. Required.

## Behavior

Per-name errors (unknown type, type not assignable to `UnityEngine.Component`, add-failed/duplicate) are accumulated in `response.Errors` / `response.Warnings` instead of throwing, so a single bad name does not abort the whole batch. Successful additions populate `response.AddedComponents` with `ComponentDataShallow` snapshots.

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
      "$ref": "#/$defs/System.String-1"
    },
    "gameObjectRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
    }
  },
  "$defs": {
    "System.String-1": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
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
      "$ref": "#/$defs/AIGD.AddComponentResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List(AIGD.ComponentDataShallow)": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ComponentDataShallow"
      }
    },
    "AIGD.ComponentDataShallow": {
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
    "System.Collections.Generic.List(System.String)": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "AIGD.AddComponentResponse": {
      "type": "object",
      "properties": {
        "AddedComponents": {
          "$ref": "#/$defs/System.Collections.Generic.List(AIGD.ComponentDataShallow)",
          "description": "List of successfully added components."
        },
        "Messages": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of success messages for added components."
        },
        "Warnings": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
          "description": "List of warnings encountered during component addition."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List(System.String)",
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

