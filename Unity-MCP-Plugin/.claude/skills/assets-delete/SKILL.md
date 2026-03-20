---
name: assets-delete
description: Delete the assets at paths from the project. Does AssetDatabase.Refresh() at the end. Use 'assets-find' tool to find assets before deleting.
---

# Assets / Delete

## How to Call

```bash
npx unity-mcp-cli run-tool assets-delete --input '{
  "paths": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> npx unity-mcp-cli {command} {tool.Name} --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> npx unity-mcp-cli run-tool assets-delete --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `paths` | `any` | Yes | The paths of the assets |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "paths": {
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
    "paths"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+DeleteAssetsResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+DeleteAssetsResponse": {
      "type": "object",
      "properties": {
        "DeletedPaths": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "List of paths of deleted assets."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "List of errors encountered during delete operations."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

