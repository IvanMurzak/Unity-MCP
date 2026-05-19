---
name: ping
description: Lightweight readiness probe. Returns the input `message` echoed back, or `'pong'` when omitted. Useful for CLI health checks and SignalR connectivity smoke tests.
---

# Ping

Lightweight readiness probe. Returns the input message or 'pong' if omitted.

## Inputs

- `message` (optional) — when present, echoed back verbatim.

## Behavior

No I/O, no Unity API calls — pure echo. Ideal for measuring round-trip latency or confirming the MCP transport is alive before invoking a heavier tool.

## How to Call

```bash
unity-mcp-cli run-system-tool ping --input '{
  "message": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-system-tool ping --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-system-tool ping --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `message` | `string` | No | Optional message to echo back. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "message": {
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
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

