---
name: script-update-or-create
description: Updates or creates script file with the provided C# code. Does AssetDatabase.Refresh() at the end. Provides compilation error details if the code has syntax errors. Use 'script-read' tool to read existing script files first.
---

# Script / Update or Create

## How to Call

```bash
npx unity-mcp-cli run-tool script-update-or-create --input '{
  "filePath": "string_value",
  "content": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> npx unity-mcp-cli run-tool script-update-or-create --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> npx unity-mcp-cli run-tool script-update-or-create --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | The path to the file. Sample: "Assets/Scripts/MyScript.cs". |
| `content` | `string` | Yes | C# code - content of the file. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filePath": {
      "type": "string"
    },
    "content": {
      "type": "string"
    }
  },
  "required": [
    "filePath",
    "content"
  ]
}
```

## Output

This tool does not return structured output.

