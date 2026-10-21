---
name: editor-session-quit
description: Save and quit an editor that a CLI session started (`unity-mcp-cli exec`, `open --headless`). Needs the session lease nonce; an editor the user opened is never closed. Used by `unity-mcp-cli close`.
metadata:
  generated-by: mcp-plugin-dotnet
---

# Editor Session / Quit

Save and quit an editor that a CLI session started.

## Inputs

- `nonce` — the `nonce` of the project's session lease (`.ai-game-dev/session.json`), which the CLI also passed to the editor as `AIGD_SESSION_NONCE`.

## Behavior

Refuses unless this editor was started by a CLI session (`AIGD_SESSION_OWNER=cli`) with the same nonce. Otherwise answers right away, then saves changed scenes that have a file and changed assets and exits — as the idle shutdown does. Unlike the OS polite-quit signal it also reaches a headless (`-batchmode`) editor. Prefer `unity-mcp-cli close --owned-only` over calling it directly.

## How to Call

```bash
unity-mcp-cli run-system-tool editor-session-quit --input '{
  "nonce": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-system-tool editor-session-quit --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-system-tool editor-session-quit --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `nonce` | `string` | Yes | The nonce of the project's CLI session lease (.ai-game-dev/session.json). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "nonce": {
      "type": "string"
    }
  },
  "required": [
    "nonce"
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
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

