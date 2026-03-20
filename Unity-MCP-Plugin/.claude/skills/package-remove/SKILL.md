---
name: package-remove
description: "Remove (uninstall) a package from the Unity project. This removes the package from the project's manifest.json and triggers package resolution. Note: Built-in packages and packages that are dependencies of other installed packages cannot be removed. Note: Package removal may trigger a domain reload. The result will be sent after the reload completes. Use 'package-list' tool to list installed packages first."
---

# Package Manager / Remove

## How to Call

```bash
unity-mcp-cli run-tool package-remove --input '{
  "packageId": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool package-remove --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool package-remove --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If you encounter issues, such as `unity-mcp-cli` not being found:
- Read the /unity-initial-setup for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `packageId` | `string` | Yes | The ID of the package to remove. Example: 'com.unity.textmeshpro'. Do not include version number. |

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

