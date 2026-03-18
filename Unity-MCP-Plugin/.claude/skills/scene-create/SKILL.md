---
name: scene-create
description: Create new scene in the project assets. Use 'scene-list-opened' tool to list all opened scenes after creation.
---

# Scene / Create

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool scene-create --input '{
  "path": "string_value",
  "newSceneSetup": "string_value",
  "newSceneMode": "string_value"
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | `string` | Yes | Path to the scene file. Should end with ".unity" extension. |
| `newSceneSetup` | `any` | No |  |
| `newSceneMode` | `any` | No |  |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "path": {
      "type": "string"
    },
    "newSceneSetup": {
      "$ref": "#/$defs/UnityEditor.SceneManagement.NewSceneSetup"
    },
    "newSceneMode": {
      "$ref": "#/$defs/UnityEditor.SceneManagement.NewSceneMode"
    }
  },
  "$defs": {
    "UnityEditor.SceneManagement.NewSceneSetup": {
      "type": "string",
      "enum": [
        "EmptyScene",
        "DefaultGameObjects"
      ]
    },
    "UnityEditor.SceneManagement.NewSceneMode": {
      "type": "string",
      "enum": [
        "Single",
        "Additive"
      ]
    }
  },
  "required": [
    "path"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.SceneDataShallow",
      "description": "Scene reference. Used to find a Scene."
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Runtime.Data.SceneDataShallow": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string"
        },
        "IsLoaded": {
          "type": "boolean"
        },
        "IsDirty": {
          "type": "boolean"
        },
        "IsSubScene": {
          "type": "boolean"
        },
        "IsValidScene": {
          "type": "boolean",
          "description": "Whether this is a valid Scene. A Scene may be invalid if, for example, you tried to open a Scene that does not exist. In this case, the Scene returned from EditorSceneManager.OpenScene would return False for IsValid."
        },
        "RootCount": {
          "type": "integer"
        },
        "path": {
          "type": "string",
          "description": "Path to the Scene within the project. Starts with 'Assets/'"
        },
        "buildIndex": {
          "type": "integer",
          "description": "Build index of the Scene in the Build Settings."
        },
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'."
        }
      },
      "required": [
        "IsLoaded",
        "IsDirty",
        "IsSubScene",
        "IsValidScene",
        "RootCount",
        "buildIndex",
        "instanceID"
      ],
      "description": "Scene reference. Used to find a Scene."
    }
  },
  "required": [
    "result"
  ]
}
```

