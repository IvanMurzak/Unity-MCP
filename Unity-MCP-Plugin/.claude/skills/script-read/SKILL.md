---
name: script-read
description: Reads the content of a script file and returns it as a string. Use 'script-update-or-create' tool to update or create script files.
---

# Script / Read

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool script-read --input '{
  "filePath": "string_value",
  "lineFrom": 0,
  "lineTo": 0
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | The path to the file. Sample: "Assets/Scripts/MyScript.cs". |
| `lineFrom` | `integer` | No | The line number to start reading from (1-based). |
| `lineTo` | `integer` | No | The line number to stop reading at (1-based, -1 for all lines). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filePath": {
      "type": "string"
    },
    "lineFrom": {
      "type": "integer"
    },
    "lineTo": {
      "type": "integer"
    }
  },
  "required": [
    "filePath"
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

