---
name: assets-move
description: Move the assets at paths in the project. Should be used for asset rename. Does AssetDatabase.Refresh() at the end. Use 'assets-find' tool to find assets before moving.
---

# Assets / Move

## How to Call

```bash
unity-mcp-cli run-tool assets-move --input '{
  "sourcePaths": "string_value",
  "destinationPaths": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool assets-move --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool assets-move --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sourcePaths` | `any` | Yes | The paths of the assets to move. |
| `destinationPaths` | `any` | Yes | The paths of moved assets. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "sourcePaths": {
      "$ref": "#/$defs/System.String[]"
    },
    "destinationPaths": {
      "$ref": "#/$defs/System.String[]"
    }
  },
  "$defs": {
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "sourcePaths",
    "destinationPaths"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+MoveAssetsResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+MoveAssetsResponse": {
      "type": "object",
      "properties": {
        "MovedPaths": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "List of destination paths of successfully moved assets."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "List of errors encountered during move operations."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

