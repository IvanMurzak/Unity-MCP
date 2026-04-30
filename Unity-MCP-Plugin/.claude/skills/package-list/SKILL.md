---
name: package-list
description: List all packages installed in the Unity project (UPM packages). Returns information about each installed package including name, version, source, and description. Use this to check which packages are currently installed before adding or removing packages.
---

# Package Manager / List Installed

## How to Call

```bash
unity-mcp-cli run-tool package-list --input '{
  "sourceFilter": "string_value",
  "nameFilter": "string_value",
  "directDependenciesOnly": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool package-list --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool package-list --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

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
      "$ref": "#/$defs/System.Collections.Generic.List<AIGD.PackageData>"
    }
  },
  "$defs": {
    "AIGD.PackageData": {
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
    "System.Collections.Generic.List<AIGD.PackageData>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.PackageData",
        "description": "Package information returned from package list operation."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

