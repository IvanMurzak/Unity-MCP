---
name: profiler-get-script-stats
description: Return script execution timing (frame time, fixed dt, time scale, frame count, runtime) plus Mono / GC memory usage in MB.
---

# Profiler / Get Script Stats

Snapshots fields from `UnityEngine.Time`, `UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()` and `System.GC.GetTotalMemory(false)`. All values are produced by built-in Unity APIs.

## Fields

- `FrameTimeMs` — `Time.deltaTime * 1000f`.
- `FixedDeltaTimeMs` — `Time.fixedDeltaTime * 1000f`.
- `TimeScale` — `Time.timeScale`.
- `TotalFrameCount` — `Time.frameCount`.
- `RealtimeSinceStartup` — `Time.realtimeSinceStartup`.
- `MonoMemoryUsageMB` — `Profiler.GetMonoUsedSizeLong() / 1048576f`.
- `GCMemoryUsageMB` — `GC.GetTotalMemory(false) / 1048576f`.

## How to Call

```bash
unity-mcp-cli run-tool profiler-get-script-stats --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool profiler-get-script-stats --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool profiler-get-script-stats --input-file - <<'EOF'
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-ScriptStatsData",
      "description": "Script statistics derived from UnityEngine.Time + UnityEngine.Profiling.Profiler."
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler-ScriptStatsData": {
      "type": "object",
      "properties": {
        "FrameTimeMs": {
          "type": "number",
          "description": "Time.deltaTime in milliseconds."
        },
        "FixedDeltaTimeMs": {
          "type": "number",
          "description": "Time.fixedDeltaTime in milliseconds."
        },
        "TimeScale": {
          "type": "number",
          "description": "Time.timeScale."
        },
        "TotalFrameCount": {
          "type": "integer",
          "description": "Total frame count since application start (Time.frameCount)."
        },
        "RealtimeSinceStartup": {
          "type": "number",
          "description": "Time.realtimeSinceStartup, in seconds."
        },
        "MonoMemoryUsageMB": {
          "type": "number",
          "description": "Mono used size (Profiler.GetMonoUsedSizeLong / 1048576), in megabytes."
        },
        "GCMemoryUsageMB": {
          "type": "number",
          "description": "System.GC.GetTotalMemory(false) / 1048576, in megabytes."
        }
      },
      "required": [
        "FrameTimeMs",
        "FixedDeltaTimeMs",
        "TimeScale",
        "TotalFrameCount",
        "RealtimeSinceStartup",
        "MonoMemoryUsageMB",
        "GCMemoryUsageMB"
      ],
      "description": "Script statistics derived from UnityEngine.Time + UnityEngine.Profiling.Profiler."
    }
  },
  "required": [
    "result"
  ]
}
```

