---
name: assets-create-folder
description: Creates a new folder in the specified parent folder. The parent folder string must start with the 'Assets' folder, and all folders within the parent folder string must already exist. For example, when specifying 'Assets/ParentFolder1/ParentFolder2/', the new folder will be created in 'ParentFolder2' only if ParentFolder1 and ParentFolder2 already exist. Use it to organize scripts and assets in the project. Does AssetDatabase.Refresh() at the end. Returns the GUID of the newly created folder, if successful.
---

# Assets / Create Folder

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool assets-create-folder --input '{
  "inputs": "string_value"
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `inputs` | `any` | Yes | The paths for the folders to create. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "inputs": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+CreateFolderInput[]"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+CreateFolderInput": {
      "type": "object",
      "properties": {
        "ParentFolderPath": {
          "type": "string",
          "description": "The parent folder path where the new folder will be created."
        },
        "NewFolderName": {
          "type": "string",
          "description": "The name of the new folder to create."
        }
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+CreateFolderInput[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+CreateFolderInput"
      }
    }
  },
  "required": [
    "inputs"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+CreateFolderResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Assets+CreateFolderResponse": {
      "type": "object",
      "properties": {
        "CreatedFolderGuids": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "List of GUIDs of created folders."
        },
        "Errors": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "List of errors encountered during folder creation."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

