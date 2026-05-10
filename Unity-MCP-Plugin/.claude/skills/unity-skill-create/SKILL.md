---
name: unity-skill-create
description: |-
  Create a new skill using C# code. It will be added into the project as a .cs file and compiled by Unity. The skill will be available for use after compilation.
  
  It must be a partial class decorated with [McpPluginToolType]. Each tool method must be decorated with [McpPluginTool]. The class name should match the file name. All Unity API calls must use com.IvanMurzak.ReflectorNet.Utils.MainThread.Instance.Run(). Return a data model for structured output, or void for side-effect-only operations. 
  
  Full sample:…
---

# Skill (Tool) / Create

## How to Call

```bash
unity-mcp-cli run-system-tool unity-skill-create --input '{
  "path": "string_value",
  "code": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-system-tool unity-skill-create --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-system-tool unity-skill-create --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | `string` | Yes | Path for the C# (.cs) file to be created. Sample: "Assets/Skills/MySkill.cs".
CRITICAL — Assembly Definition placement: If the project uses Assembly Definition files (.asmdef), you MUST place the script inside a folder that belongs to an assembly definition which already references all required dependencies (e.g. com.IvanMurzak.McpPlugin, UnityEditor, UnityEngine). Placing the file in the wrong assembly will cause compile errors due to missing type references. Before choosing a path, inspect existing .asmdef files with the assets-find tool to identify the correct assembly folder. |
| `code` | `string` | Yes | C# code for the skill tool. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "path": {
      "type": "string"
    },
    "code": {
      "type": "string"
    }
  },
  "required": [
    "path",
    "code"
  ]
}
```

## Output

This tool does not return structured output.

