---
name: scene-get-data
description: This tool retrieves the list of root GameObjects in the specified scene. Use 'scene-list-opened' tool to get the list of all opened scenes.
---

# Scene / Get Data

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool scene-get-data --input '{
  "openedSceneName": "string_value",
  "includeRootGameObjects": false,
  "includeChildrenDepth": 0,
  "includeBounds": false,
  "includeData": false
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `openedSceneName` | `string` | No | Name of the opened scene. If empty or null, the active scene will be used. |
| `includeRootGameObjects` | `boolean` | No | If true, includes root GameObjects in the scene data. |
| `includeChildrenDepth` | `integer` | No | Determines the depth of the hierarchy to include. |
| `includeBounds` | `boolean` | No | If true, includes bounding box information for GameObjects. |
| `includeData` | `boolean` | No | If true, includes component data for GameObjects. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "openedSceneName": {
      "type": "string"
    },
    "includeRootGameObjects": {
      "type": "boolean"
    },
    "includeChildrenDepth": {
      "type": "integer"
    },
    "includeBounds": {
      "type": "boolean"
    },
    "includeData": {
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.SceneData",
      "description": "Scene reference. Used to find a Scene."
    }
  },
  "$defs": {
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectData>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectData"
      }
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectData": {
      "type": "object",
      "properties": {
        "Reference": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
          "description": "Find GameObject in opened Prefab or in the active Scene."
        },
        "Data": {
          "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
          "description": "GameObject editable data (tag, layer, etc)."
        },
        "Bounds": {
          "$ref": "#/$defs/UnityEngine.Bounds",
          "description": "Bounds of the GameObject."
        },
        "Hierarchy": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata",
          "description": "Hierarchy metadata of the GameObject."
        },
        "Components": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow[]",
          "description": "Attached components shallow data of the GameObject (Read-only, use Component modification tool for modification)."
        }
      }
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If it is '0' and 'path', 'name', 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'. Priority: 1 (Recommended)"
        },
        "path": {
          "type": "string",
          "description": "Path of a GameObject in the hierarchy Sample 'character/hand/finger/particle'. Priority: 2."
        },
        "name": {
          "type": "string",
          "description": "Name of a GameObject in hierarchy. Priority: 3."
        },
        "assetType": {
          "$ref": "#/$defs/System.Type",
          "description": "Type of the asset."
        },
        "assetPath": {
          "type": "string",
          "description": "Path to the asset within the project. Starts with 'Assets/'"
        },
        "assetGuid": {
          "type": "string",
          "description": "Unique identifier for the asset."
        }
      },
      "required": [
        "instanceID"
      ],
      "description": "Find GameObject in opened Prefab or in the active Scene."
    },
    "System.Type": {
      "type": "string"
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMember": {
      "type": "object",
      "properties": {
        "typeName": {
          "type": "string",
          "description": "Full type name. Eg: 'System.String', 'System.Int32', 'UnityEngine.Vector3', etc."
        },
        "name": {
          "type": "string",
          "description": "Object name."
        },
        "value": {
          "description": "Value of the object, serialized as a non stringified JSON element. Can be null if the value is not set. Can be default value if the value is an empty object or array json."
        },
        "fields": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested field value."
          },
          "description": "Fields of the object, serialized as a list of 'SerializedMember'."
        },
        "props": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested property value."
          },
          "description": "Properties of the object, serialized as a list of 'SerializedMember'."
        }
      },
      "required": [
        "typeName"
      ],
      "additionalProperties": false
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
    },
    "UnityEngine.Bounds": {
      "type": "object",
      "properties": {
        "center": {
          "type": "object",
          "properties": {
            "x": {
              "type": "number"
            },
            "y": {
              "type": "number"
            },
            "z": {
              "type": "number"
            }
          },
          "required": [
            "x",
            "y",
            "z"
          ]
        },
        "size": {
          "type": "object",
          "properties": {
            "x": {
              "type": "number"
            },
            "y": {
              "type": "number"
            },
            "z": {
              "type": "number"
            }
          },
          "required": [
            "x",
            "y",
            "z"
          ]
        }
      },
      "required": [
        "center",
        "size"
      ],
      "additionalProperties": false
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer"
        },
        "path": {
          "type": "string"
        },
        "name": {
          "type": "string"
        },
        "sceneName": {
          "type": "string"
        },
        "tag": {
          "type": "string"
        },
        "activeSelf": {
          "type": "boolean"
        },
        "activeInHierarchy": {
          "type": "boolean"
        },
        "children": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata>"
        }
      },
      "required": [
        "instanceID",
        "activeSelf",
        "activeInHierarchy"
      ]
    },
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectMetadata"
      }
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow"
      }
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.ComponentDataShallow": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer"
        },
        "typeName": {
          "type": "string"
        },
        "isEnabled": {
          "type": "string",
          "enum": [
            "False",
            "True",
            "NA"
          ]
        }
      },
      "required": [
        "instanceID",
        "isEnabled"
      ]
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.SceneData": {
      "type": "object",
      "properties": {
        "RootGameObjects": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectData>"
        },
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

