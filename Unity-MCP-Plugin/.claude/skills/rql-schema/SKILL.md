---
name: rql-schema
description: List RQL sources and the component types present in the loaded scenes; pass a type name for its members.
metadata:
  generated-by: mcp-plugin-dotnet
---

# RQL / Schema

Without a type: RQL sources, asset functions, and the component types present in the loaded scenes (with counts), plus GameObject and Transform members. With a type name (e.g. "Light", "Enemy", "Vector3"): that type's members (name type; ro = read-only) and methods. Use it before querying an unfamiliar component.

## How to Call

```bash
unity-mcp-cli run-tool rql-schema --input '{
  "type": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool rql-schema --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool rql-schema --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `type` | `string` | No | Optional type name, e.g. "Light", "Rigidbody", "Enemy", "Vector3" |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "type": {
      "type": "string"
    }
  }
}
```

## Output

This tool does not return structured output.

