---
name: assets-refresh
description: Refreshes the AssetDatabase. Use it if any file was added or updated in the project outside of Unity API. Use it if need to force scripts recompilation when '.cs' file changed.
---

# Assets / Refresh

## How to Call

```bash
unity-mcp-cli run-tool assets-refresh --input '{
  "options": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool assets-refresh --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool assets-refresh --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If you encounter issues, such as `unity-mcp-cli` not being found:
- Read the /unity-initial-setup for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `options` | `any` | No |  |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "options": {
      "$ref": "#/$defs/UnityEditor.ImportAssetOptions"
    }
  },
  "$defs": {
    "UnityEditor.ImportAssetOptions": {
      "type": "string",
      "enum": [
        "Default",
        "ForceUpdate",
        "ForceSynchronousImport",
        "ImportRecursive",
        "DontDownloadFromCacheServer",
        "ForceUncompressedImport"
      ]
    }
  }
}
```

## Output

This tool does not return structured output.

