---
name: skill-generate
description: Generate all skills from the existed Tools in the Unity Project.
---

# Skill (Tool) / Generate All

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-system-tool skill-generate --input '{
  "path": "string_value"
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | `string` | No | Path to the skills folder. If null or empty, the default path will be used. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "path": {
      "type": "string"
    }
  }
}
```

## Output

This tool does not return structured output.

