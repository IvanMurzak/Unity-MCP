---
name: editor-application-set-state
description: Control the Unity Editor application state. You can start, stop, or pause the 'playmode'. Use 'editor-application-get-state' tool to get the current state first.
---

# Editor / Application / Set State

## How to Call

```bash
unity-mcp-cli run-tool editor-application-set-state --input '{
  "isPlaying": false,
  "isPaused": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool editor-application-set-state --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool editor-application-set-state --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `isPlaying` | `boolean` | No | If true, the 'playmode' will be started. If false, the 'playmode' will be stopped. |
| `isPaused` | `boolean` | No | If true, the 'playmode' will be paused. If false, the 'playmode' will be resumed. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "isPlaying": {
      "type": "boolean"
    },
    "isPaused": {
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Editor+EditorStatsData",
      "description": "Available information about 'UnityEditor.EditorApplication'."
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Editor+EditorStatsData": {
      "type": "object",
      "properties": {
        "IsPlaying": {
          "type": "boolean",
          "description": "Whether the Editor is in Play mode."
        },
        "IsPaused": {
          "type": "boolean",
          "description": "Whether the Editor is paused."
        },
        "IsCompiling": {
          "type": "boolean",
          "description": "Is editor currently compiling scripts? (Read Only)"
        },
        "IsPlayingOrWillChangePlaymode": {
          "type": "boolean",
          "description": "Editor application state which is true only when the Editor is currently in or about to enter Play mode. (Read Only)"
        },
        "IsUpdating": {
          "type": "boolean",
          "description": "True if the Editor is currently refreshing the AssetDatabase. (Read Only)"
        },
        "ApplicationContentsPath": {
          "type": "string",
          "description": "Path to the Unity editor contents folder. (Read Only)"
        },
        "ApplicationPath": {
          "type": "string",
          "description": "Gets the path to the Unity Editor application. (Read Only)"
        },
        "TimeSinceStartup": {
          "type": "number",
          "description": "The time since the editor was started. (Read Only)"
        }
      },
      "required": [
        "IsPlaying",
        "IsPaused",
        "IsCompiling",
        "IsPlayingOrWillChangePlaymode",
        "IsUpdating",
        "TimeSinceStartup"
      ],
      "description": "Available information about 'UnityEditor.EditorApplication'."
    }
  },
  "required": [
    "result"
  ]
}
```

