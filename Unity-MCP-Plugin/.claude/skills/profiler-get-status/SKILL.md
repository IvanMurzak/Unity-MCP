---
name: profiler-get-status
description: Return the Unity profiler's current enabled state, active modules, max-used memory, and platform support flag. Read-only.
---

# Profiler / Get Status

Snapshots `UnityEngine.Profiling.Profiler` state and returns it in a single response.

## Fields

- `ProfilerEnabled` — `Profiler.enabled`.
- `ActiveModules` — names of modules this wrapper considers enabled (local bookkeeping).
- `MaxUsedMemoryMB` — `Profiler.maxUsedMemory / 1048576f`.
- `Supported` — `Profiler.supported`.

## Behavior

Uses only built-in Unity APIs. No external Unity package is required.

## How to Call

```bash
unity-mcp-cli run-tool profiler-get-status --input '{
  "nothing": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool profiler-get-status --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool profiler-get-status --input-file - <<'EOF'
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler%2BProfilerStatusData",
      "description": "Profiler status data including memory and module information."
    }
  },
  "$defs": {
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Profiler+ProfilerStatusData": {
      "type": "object",
      "properties": {
        "ProfilerEnabled": {
          "type": "boolean",
          "description": "Whether Unity's runtime profiler is currently enabled (UnityEngine.Profiling.Profiler.enabled)."
        },
        "ActiveModules": {
          "$ref": "#/$defs/System.Collections.Generic.List%3CSystem.String%3E",
          "description": "List of profiler modules this wrapper considers active. Local bookkeeping only."
        },
        "MaxUsedMemoryMB": {
          "type": "number",
          "description": "Maximum used memory recorded by the profiler, in megabytes."
        },
        "Supported": {
          "type": "boolean",
          "description": "Whether profiling is supported on this platform (UnityEngine.Profiling.Profiler.supported)."
        }
      },
      "required": [
        "ProfilerEnabled",
        "MaxUsedMemoryMB",
        "Supported"
      ],
      "description": "Profiler status data including memory and module information."
    }
  },
  "required": [
    "result"
  ]
}
```

