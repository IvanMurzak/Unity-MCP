---
name: package-search
description: "Search for packages in both Unity Package Manager registry and installed packages. Use this to find packages by name before installing them. Returns available versions and installation status. Searches both the Unity registry and locally installed packages (including Git, local, and embedded sources). Results are prioritized: exact name match, exact display name match, name substring, display name substring, description substring. Note: Online mode fetches exact matches from live registry, then supplements with cached substring matches."
---

# Package Manager / Search

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool package-search --input '{
  "query": "string_value",
  "maxResults": 0,
  "offlineMode": false
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `query` | `string` | Yes | The package id, name, or description. Can be: Full package id 'com.unity.textmeshpro', Full package name 'TextMesh Pro', Partial name 'TextMesh' (will search in Unity registry and installed packages), Description keyword 'rendering' (searches in package descriptions). |
| `maxResults` | `integer` | No | Maximum number of results to return. Default: 10 |
| `offlineMode` | `boolean` | No | Whether to perform the search in offline mode (uses cached registry data only). Default: true. Set to false to fetch latest exact matches from Unity registry. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "query": {
      "type": "string"
    },
    "maxResults": {
      "type": "integer"
    },
    "offlineMode": {
      "type": "boolean"
    }
  },
  "required": [
    "query"
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
      "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package+PackageSearchResult>"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package+PackageSearchResult": {
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
        "LatestVersion": {
          "type": "string",
          "description": "The latest version available in the registry."
        },
        "Description": {
          "type": "string",
          "description": "A brief description of the package."
        },
        "IsInstalled": {
          "type": "boolean",
          "description": "Whether this package is already installed in the project."
        },
        "InstalledVersion": {
          "type": "string",
          "description": "The currently installed version (if installed)."
        },
        "AvailableVersions": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "Available versions of this package (up to 5 most recent)."
        }
      },
      "required": [
        "IsInstalled"
      ],
      "description": "Package search result with available versions."
    },
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package+PackageSearchResult>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Package+PackageSearchResult",
        "description": "Package search result with available versions."
      }
    }
  },
  "required": [
    "result"
  ]
}
```

