---
name: console-get-logs
description: Retrieves Unity Editor logs. Useful for debugging and monitoring Unity Editor activity.
---

# Console / Get Logs

## How to Call

```bash
unity-mcp-cli run-tool console-get-logs --input '{
  "maxEntries": 0,
  "logTypeFilter": "string_value",
  "includeStackTrace": false,
  "lastMinutes": 0
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool console-get-logs --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool console-get-logs --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `maxEntries` | `integer` | No | Maximum number of log entries to return. Minimum: 1. Default: 100 |
| `logTypeFilter` | `any` | No | Filter by log type. 'null' means All. |
| `includeStackTrace` | `boolean` | No | Include stack traces in the output. Default: false |
| `lastMinutes` | `integer` | No | Return logs from the last N minutes. If 0, returns all available logs. Default: 0 |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "maxEntries": {
      "type": "integer"
    },
    "logTypeFilter": {
      "$ref": "#/$defs/UnityEngine.LogType"
    },
    "includeStackTrace": {
      "type": "boolean"
    },
    "lastMinutes": {
      "type": "integer"
    }
  },
  "$defs": {
    "UnityEngine.LogType": {
      "type": "string",
      "enum": [
        "Error",
        "Assert",
        "Warning",
        "Log",
        "Exception"
      ]
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.LogEntry[]"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.LogEntry": {
      "type": "object",
      "properties": {
        "LogType": {
          "type": "string",
          "enum": [
            "Error",
            "Assert",
            "Warning",
            "Log",
            "Exception"
          ]
        },
        "Message": {
          "type": "string"
        },
        "Timestamp": {
          "type": "string",
          "format": "date-time"
        },
        "StackTrace": {
          "type": "string"
        }
      },
      "required": [
        "LogType",
        "Timestamp"
      ]
    },
    "com.IvanMurzak.Unity.MCP.LogEntry[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.LogEntry"
      }
    }
  },
  "required": [
    "result"
  ]
}
```

