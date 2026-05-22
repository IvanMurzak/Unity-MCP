---
name: script-update-or-create
description: Write a `.cs` script file (create or overwrite) with the provided C# code. Validates syntax via Roslyn before write — invalid code is rejected with error details and the file is left untouched. Refreshes the AssetDatabase and delivers the final result via `requestId` after Unity finishes the triggered compilation. Use 'script-read' to inspect existing content first.
---

# Script / Update or Create

Updates or creates script file with the provided C# code. Does AssetDatabase.Refresh() at the end. Provides compilation error details if the code has syntax errors. Use 'script-read' tool to read existing script files first.

## Inputs

- `filePath` — required `.cs` path.
- `content` — C# source. MUST pass `ScriptUtils.IsValidCSharpSyntax`.
- `requestId` — required for the processing/delivered-later contract.

## Behavior

Creates any missing parent directories, writes the file, then calls `AssetDatabase.Refresh` and schedules a post-compilation notification so the final response is delivered after Unity finishes the recompile.

## How to Call

```bash
unity-mcp-cli run-tool script-update-or-create --input '{
  "filePath": "string_value",
  "content": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool script-update-or-create --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool script-update-or-create --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filePath` | `string` | Yes | The path to the file. Sample: "Assets/Scripts/MyScript.cs". |
| `content` | `string` | Yes | C# code - content of the file. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filePath": {
      "type": "string"
    },
    "content": {
      "type": "string"
    }
  },
  "required": [
    "filePath",
    "content"
  ]
}
```

## Output

This tool does not return structured output.

