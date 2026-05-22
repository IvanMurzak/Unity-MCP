---
name: profiler-clear-data
description: Discard all frames currently held by the Editor Profiler (UnityEditorInternal.ProfilerDriver.ClearAllFrames). Cannot be undone.
---

# Profiler / Clear Data

Invokes `UnityEditorInternal.ProfilerDriver.ClearAllFrames()` on the main thread. `UnityEditorInternal` is a built-in editor namespace — no external Unity package is required.

## Behavior

After this call, the Profiler window's frame history is empty; subsequent recording starts from frame 0. Returns `true` on success.

## How to Call

```bash
unity-mcp-cli run-tool profiler-clear-data --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool profiler-clear-data --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool profiler-clear-data --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

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
      "type": "boolean"
    }
  },
  "required": [
    "result"
  ]
}
```

