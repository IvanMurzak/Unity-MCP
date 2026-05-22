---
name: profiler-capture-frame
description: Capture the current frame's timing info (delta time, FPS, frame counts, runtime). Snapshot only — historical frames live in Unity's Profiler window.
---

# Profiler / Capture Frame

Reads `UnityEngine.Time` fields and returns them in a single struct. This tool is intentionally a single-frame snapshot — Unity's runtime API does not expose historical frame-data outside the Profiler window.

## Fields

- `FrameTimeMs` — `Time.deltaTime * 1000f`.
- `Fps` — `1 / Time.deltaTime` (0 when delta is zero).
- `TotalFrameCount` — `Time.frameCount` (includes skipped renders).
- `RealtimeSinceStartup` — `Time.realtimeSinceStartup`.
- `RenderedFrameCount` — `Time.renderedFrameCount`.

## Behavior

Uses only built-in Unity APIs (`UnityEngine.Time`). No external Unity package is required.

## How to Call

```bash
unity-mcp-cli run-tool profiler-capture-frame --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool profiler-capture-frame --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool profiler-capture-frame --input-file - <<'EOF'
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler%2BFrameCaptureData",
      "description": "Single-frame snapshot of timing information from UnityEngine.Time. No historical frame data; use the Profiler window for that."
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler+FrameCaptureData": {
      "type": "object",
      "properties": {
        "FrameTimeMs": {
          "type": "number",
          "description": "Time.deltaTime in milliseconds at the moment of capture."
        },
        "Fps": {
          "type": "number",
          "description": "Frames per second derived from Time.deltaTime."
        },
        "TotalFrameCount": {
          "type": "integer",
          "description": "Total frame count since application start (Time.frameCount). May include skipped renders."
        },
        "RealtimeSinceStartup": {
          "type": "number",
          "description": "Time.realtimeSinceStartup, in seconds."
        },
        "RenderedFrameCount": {
          "type": "integer",
          "description": "Frames actually rendered (Time.renderedFrameCount)."
        }
      },
      "required": [
        "FrameTimeMs",
        "Fps",
        "TotalFrameCount",
        "RealtimeSinceStartup",
        "RenderedFrameCount"
      ],
      "description": "Single-frame snapshot of timing information from UnityEngine.Time. No historical frame data; use the Profiler window for that."
    }
  },
  "required": [
    "result"
  ]
}
```

