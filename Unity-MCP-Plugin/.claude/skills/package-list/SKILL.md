---
name: package-list
description: List all packages installed in the Unity project (UPM packages). Returns information about each installed package including name, version, source, and description. Use this to check which packages are currently installed before adding or removing packages.
---

# Package Manager / List Installed

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool package-list --input '{
  "sourceFilter": "string_value",
  "nameFilter": "string_value",
  "directDependenciesOnly": false
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sourceFilter` | `string` | No | Filter packages by source. |
| `nameFilter` | `string` | No | Filter packages by name, display name, or description (case-insensitive). Results are prioritized: exact name match, exact display name match, name substring, display name substring, description substring. |
| `directDependenciesOnly` | `boolean` | No | Include only direct dependencies (packages in manifest.json). If false, includes all resolved packages. Default: false |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "sourceFilter": {
      "type": "string",
      "enum": [
        "All",
        "Registry",
        "Embedded",
        "Local",
        "Git",
        "BuiltIn",
        "LocalTarball"
      ]
    },
    "nameFilter": {
      "type": "string"
    },
    "directDependenciesOnly": {
      "type": "boolean"
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
      "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package+PackageData>"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package+PackageData": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string",
          "description": "The official Unity name of the package used as the package ID."
        },
        "DisplayName": {
          "type": "string",
          "description": "The display name of the package."
        },
        "Version": {
          "type": "string",
          "description": "The version of the package."
        },
        "Description": {
          "type": "string",
          "description": "A brief description of the package."
        },
        "Source": {
          "type": "string",
          "description": "The source of the package (Registry, Embedded, Local, Git, etc.)."
        },
        "Category": {
          "type": "string",
          "description": "The category of the package."
        }
      },
      "description": "Package information returned from package list operation."
    },
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package+PackageData>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package+PackageData",
        "description": "Package information returned from package list operation."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

