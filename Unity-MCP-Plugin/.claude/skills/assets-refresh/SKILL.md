---
name: assets-refresh
description: Refreshes the AssetDatabase. Use it if any file was added or updated in the project outside of Unity API. Use it if need to force scripts recompilation when '.cs' file changed.
---

# Assets / Refresh

## How to Call

```bash
npx unity-mcp-cli run-tool assets-refresh --input '{
  "options": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> npx unity-mcp-cli run-tool assets-refresh --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> npx unity-mcp-cli run-tool assets-refresh --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```

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

