---
name: gameobject-find
description: |-
  Finds specific GameObject by provided information in opened Prefab or in a Scene. First it looks for the opened Prefab, if any Prefab is opened it looks only there ignoring a scene. If no opened Prefab it looks into current active scene. Returns GameObject information and its children. Also, it returns Components preview just for the target GameObject.
  
  Path-scoped reads (token-saving): supply 'paths' (a list of paths) to read only the listed fields/elements via Reflector.TryReadAt, or 'viewQuery' (a ViewQuery) to navigate to a subtree and/or filter by name regex / max depth / type via Reflector.View. When either is supplied, the result populates 'Data' on the returned GameObjectData and overrides 'includeData' (which would otherwise produce a full recursive serialization). These two parameters are mutually exclusive — supply at most one.
  Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.
---

# GameObject / Find

## How to Call

```bash
unity-mcp-cli run-tool gameobject-find --input '{
  "gameObjectRef": "string_value",
  "includeData": false,
  "includeComponents": false,
  "includeBounds": false,
  "includeHierarchy": false,
  "hierarchyDepth": 0,
  "paths": "string_value",
  "viewQuery": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool gameobject-find --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool gameobject-find --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Find GameObject in opened Prefab or in the active Scene. |
| `includeData` | `boolean` | No | Include editable GameObject data (tag, layer, etc). |
| `includeComponents` | `boolean` | No | Include attached components references. |
| `includeBounds` | `boolean` | No | Include 3D bounds of the GameObject. |
| `includeHierarchy` | `boolean` | No | Include hierarchy metadata. |
| `hierarchyDepth` | `integer` | No | Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below. |
| `paths` | `any` | No | Optional. List of paths to read individually via Reflector.TryReadAt. When supplied, replaces 'includeData'-style full serialization with a path-scoped aggregate. Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Mutually exclusive with 'viewQuery'. |
| `viewQuery` | `any` | No | Optional. View-query filter routed through Reflector.View. When supplied, replaces 'includeData'-style full serialization with the filtered subtree. Mutually exclusive with 'paths'. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
    },
    "includeData": {
      "type": "boolean"
    },
    "includeComponents": {
      "type": "boolean"
    },
    "includeBounds": {
      "type": "boolean"
    },
    "includeHierarchy": {
      "type": "boolean"
    },
    "hierarchyDepth": {
      "type": "integer"
    },
    "paths": {
      "$ref": "#/$defs/System.Collections.Generic.List<System.String>"
    },
    "viewQuery": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.ViewQuery"
    }
  },
  "$defs": {
    "System.Type": {
      "type": "string"
    },
    "AIGD.GameObjectRef": {
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
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.ReflectorNet.Model.ViewQuery": {
      "type": "object",
      "properties": {
        "Path": {
          "type": "string",
          "description": "Navigate to this path first, then serialize only that subtree. Path segments are separated by '/'. Use '[i]' for array/list index (e.g. 'users/[2]/name') and '[key]' for dictionary entry (e.g. 'config/[timeout]'). A leading '#/' is stripped automatically. Examples: 'admin/name', 'users/[0]/email', 'config/[timeout]'. Leave null to start from the root object."
        },
        "NamePattern": {
          "type": "string",
          "description": "Case-insensitive .NET regex pattern matched against field and property names. Only branches containing at least one match are kept in the result tree. Examples: 'orbitRadius' (exact name), 'orbit.*' (prefix match), 'radius|speed' (either name). When nothing matches, the root envelope is returned with empty fields/props. Leave null to return all fields and properties without filtering."
        },
        "MaxDepth": {
          "type": "integer",
          "description": "Maximum nesting depth of the returned serialized tree. 0 = root type name and value only — no nested fields or properties. 1 = one level of fields/props visible, their children stripped. 2 = two levels visible, and so on. Leave null (default) for unlimited depth."
        },
        "TypeFilter": {
          "$ref": "#/$defs/System.Type",
          "description": "When set, prunes the result tree to members whose runtime type is assignable to this type. Non-matching branches are removed; the root envelope is always preserved. Examples: typeof(float) keeps only float fields, typeof(IEnumerable) keeps only collections. Leave null to include members of any type."
        }
      }
    }
  },
  "required": [
    "gameObjectRef"
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
      "$ref": "#/$defs/AIGD.GameObjectData"
    }
  },
  "$defs": {
    "AIGD.GameObjectRef": {
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
    "AIGD.GameObjectMetadata": {
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
          "$ref": "#/$defs/System.Collections.Generic.List<AIGD.GameObjectMetadata>"
        }
      },
      "required": [
        "instanceID",
        "activeSelf",
        "activeInHierarchy"
      ]
    },
    "System.Collections.Generic.List<AIGD.GameObjectMetadata>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.GameObjectMetadata"
      }
    },
    "AIGD.ComponentDataShallow[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ComponentDataShallow"
      }
    },
    "AIGD.ComponentDataShallow": {
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
    "AIGD.GameObjectData": {
      "type": "object",
      "properties": {
        "Reference": {
          "$ref": "#/$defs/AIGD.GameObjectRef",
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
          "$ref": "#/$defs/AIGD.GameObjectMetadata",
          "description": "Hierarchy metadata of the GameObject."
        },
        "Components": {
          "$ref": "#/$defs/AIGD.ComponentDataShallow[]",
          "description": "Attached components shallow data of the GameObject (Read-only, use Component modification tool for modification)."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

