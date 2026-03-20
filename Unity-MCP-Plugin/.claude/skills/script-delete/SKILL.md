---
name: script-delete
description: Delete the script file(s). Does AssetDatabase.Refresh() and waits for Unity compilation to complete before reporting results. Use 'script-read' tool to read existing script files first.
---

# Script / Delete

## How to Call

```bash
npx unity-mcp-cli run-tool script-delete --input '{
  "files": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> npx unity-mcp-cli {command} {tool.Name} --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> npx unity-mcp-cli run-tool script-delete --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `files` | `any` | Yes | File paths to the files. Sample: "Assets/Scripts/MyScript.cs". |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "files": {
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
    "files"
  ]
}
```

## Output

This tool does not return structured output.

