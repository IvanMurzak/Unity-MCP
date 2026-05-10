---
name: tool-list
description: List all available MCP tools. Optionally filter by regex across tool names, descriptions, and arguments.
---

# Tool / List

## How to Call

```bash
unity-mcp-cli run-tool tool-list --input '{
  "regexSearch": "string_value",
  "includeDescription": "string_value",
  "includeInputs": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool tool-list --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool tool-list --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `regexSearch` | `string` | No | Regex pattern to filter tools. Matches against tool name, description, and argument names and descriptions. |
| `includeDescription` | `any` | No | Include tool descriptions in the result. Default: false |
| `includeInputs` | `any` | No | Include input arguments in the result. Default: None |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "regexSearch": {
      "type": "string"
    },
    "includeDescription": {
      "$ref": "#/$defs/System.Boolean"
    },
    "includeInputs": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+InputRequest"
    }
  },
  "$defs": {
    "System.Boolean": {
      "type": "boolean"
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+InputRequest": {
      "type": "string",
      "enum": [
        "None",
        "Inputs",
        "InputsWithDescription"
      ],
      "description": "Specifies what to include for tool input arguments."
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
      "$ref": "#/$defs/AIGD.ToolInfoData[]"
    }
  },
  "$defs": {
    "AIGD.ToolInfoData": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "description": "Tool name."
        },
        "description": {
          "type": "string",
          "description": "Tool description."
        },
        "inputs": {
          "$ref": "#/$defs/AIGD.ToolInputData[]",
          "description": "Tool input arguments."
        }
      },
      "description": "MCP tool information."
    },
    "AIGD.ToolInputData[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ToolInputData",
        "description": "MCP tool input argument."
      }
    },
    "AIGD.ToolInputData": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "description": "Argument name."
        },
        "description": {
          "type": "string",
          "description": "Argument description."
        }
      },
      "description": "MCP tool input argument."
    },
    "AIGD.ToolInfoData[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ToolInfoData",
        "description": "MCP tool information."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

