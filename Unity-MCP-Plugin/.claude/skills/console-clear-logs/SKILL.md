---
name: console-clear-logs
description: Clears the MCP log cache (used by console-get-logs) and the Unity Editor Console window. Useful for isolating errors related to a specific action by clearing logs before performing the action.
---

# Console / Clear Logs

## How to Call

```bash
unity-mcp-cli run-tool console-clear-logs --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool console-clear-logs --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool console-clear-logs --input-file - <<'EOF'
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

This tool does not return structured output.

