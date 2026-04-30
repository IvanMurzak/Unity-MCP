---
name: assets-create-folder
description: Creates a new folder in the specified parent folder. The parent folder string must start with the 'Assets' folder, and all folders within the parent folder string must already exist. For example, when specifying 'Assets/ParentFolder1/ParentFolder2/', the new folder will be created in 'ParentFolder2' only if ParentFolder1 and ParentFolder2 already exist. Use it to organize scripts and assets in the project. Does AssetDatabase.Refresh() at the end. Returns the GUID of the newly created folder, if successful.
---

# Assets / Create Folder

## How to Call

```bash
unity-mcp-cli run-tool assets-create-folder --input '{
  "inputs": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool assets-create-folder --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool assets-create-folder --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

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
      "$ref": "#/$defs/AIGD.CreateFolderInput[]"
    }
  },
  "$defs": {
    "AIGD.CreateFolderInput": {
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
    "AIGD.CreateFolderInput[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.CreateFolderInput"
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
      "$ref": "#/$defs/AIGD.CreateFolderResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "AIGD.CreateFolderResponse": {
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

