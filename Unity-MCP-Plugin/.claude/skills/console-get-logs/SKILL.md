---
name: console-get-logs
description: Retrieve Unity Editor logs from the MCP plugin's `LogCollector`, optionally filtered by log type or time window. Useful for debugging and monitoring Editor activity.
---

# Console / Get Logs

Retrieves Unity Editor logs. Useful for debugging and monitoring Unity Editor activity.

## Inputs

- `maxEntries` (default 100, minimum 1) — caps the size of the returned array.
- `logTypeFilter` — Unity `LogType` filter; `null` returns all severities.
- `includeStackTrace` (default `false`) — include stack-trace strings in each entry.
- `lastMinutes` (default 0) — when non-zero, only logs from the last N minutes are returned.
- `sinceSequence` (default 0) — every entry carries a `sequence` number. When greater than 0, only entries with a higher `sequence` are returned, oldest first, up to `maxEntries`.

## Polling

To read only what is new, pass the highest `sequence` you have received as `sinceSequence`; if the result has `maxEntries` entries, call again with the new highest `sequence` to continue. If returned sequences are lower than your cursor, the log restarted.

## How to Call

```bash
unity-mcp-cli run-tool console-get-logs --input '{
  "maxEntries": 0,
  "logTypeFilter": "string_value",
  "includeStackTrace": false,
  "lastMinutes": 0,
  "sinceSequence": 0
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
| `sinceSequence` | `integer` | No | Return only entries whose `sequence` is greater than this value, oldest first. If 0, returns the most recent entries. Default: 0 |

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
    },
    "sinceSequence": {
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.LogEntry-1"
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
        },
        "sequence": {
          "type": "integer"
        }
      },
      "required": [
        "LogType",
        "Timestamp",
        "sequence"
      ]
    },
    "com.IvanMurzak.Unity.MCP.LogEntry-1": {
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

