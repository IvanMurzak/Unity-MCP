---
name: screenshot-scene-view
description: Captures a screenshot from the Unity Editor Scene View and returns it as an image. Returns the image directly for visual inspection by the LLM.
---

# Screenshot / Scene View

## How to Call

```bash
unity-mcp-cli run-tool screenshot-scene-view --input '{
  "width": 0,
  "height": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool screenshot-scene-view --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool screenshot-scene-view --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If you encounter issues, such as `unity-mcp-cli` not being found:
- Read the /unity-initial-setup for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `width` | `integer` | No | Width of the screenshot in pixels. |
| `height` | `integer` | No | Height of the screenshot in pixels. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "width": {
      "type": "integer"
    },
    "height": {
      "type": "integer"
    }
  }
}
```

## Output

This tool does not return structured output.

