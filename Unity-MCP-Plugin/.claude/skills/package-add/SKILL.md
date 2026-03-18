---
name: package-add
description: "Install a package from the Unity Package Manager registry, Git URL, or local path. This operation modifies the project's manifest.json and triggers package resolution. Note: Package installation may trigger a domain reload. The result will be sent after the reload completes. Use 'package-search' tool to search for packages and 'package-list' to list installed packages."
---

# Package Manager / Add

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool package-add --input '{
  "packageId": "string_value"
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `packageId` | `string` | Yes | The package ID to install. Formats: Package ID 'com.unity.textmeshpro' (installs latest compatible version), Package ID with version 'com.unity.textmeshpro@3.0.6', Git URL 'https://github.com/user/repo.git', Git URL with branch/tag 'https://github.com/user/repo.git#v1.0.0', Local path 'file:../MyPackage'. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "packageId": {
      "type": "string"
    }
  },
  "required": [
    "packageId"
  ]
}
```

## Output

This tool does not return structured output.

