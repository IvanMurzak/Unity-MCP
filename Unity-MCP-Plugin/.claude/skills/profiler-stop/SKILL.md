---
name: profiler-stop
description: Disable Unity's runtime profiler. Idempotent — calling when already disabled returns the current disabled state.
---

# Profiler / Stop

Sets `UnityEngine.Profiling.Profiler.enabled = false`. Returns the post-call value of `Profiler.enabled` (expected `false`).

## Behavior

Uses only built-in Unity APIs (`UnityEngine.Profiling`). No external Unity package is required.

## How to Call

```bash
unity-mcp-cli run-tool profiler-stop --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool profiler-stop --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool profiler-stop --input-file - <<'EOF'
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

