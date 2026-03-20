---
name: scene-save
description: Save Opened scene to the asset file. Use 'scene-list-opened' tool to get the list of all opened scenes.
---

# Scene / Save

## How to Call

```bash
unity-mcp-cli run-tool scene-save --input '{
  "openedSceneName": "string_value",
  "path": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool scene-save --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool scene-save --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If you encounter issues, such as `unity-mcp-cli` not being found:
- Read the /unity-initial-setup for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `openedSceneName` | `string` | No | Name of the opened scene that should be saved. Could be empty if need to save the current active scene. |
| `path` | `string` | No | Path to the scene file. Should end with ".unity". If null or empty save to the existed scene asset file. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "openedSceneName": {
      "type": "string"
    },
    "path": {
      "type": "string"
    }
  }
}
```

## Output

This tool does not return structured output.

