---
name: assets-shader-list-all
description: List all available shaders in the project assets and packages. Returns their names. Use this to find a shader name for 'assets-material-create' tool.
---

# Assets / List Shaders

## How to Call

```bash
unity-mcp-cli run-tool assets-shader-list-all --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool assets-shader-list-all --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool assets-shader-list-all --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If you encounter issues, such as `unity-mcp-cli` not being found:
- Read the /unity-initial-setup for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `nothing` | `string` | No |  |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "nothing": {
      "type": "string"
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
    "result"
  ]
}
```

