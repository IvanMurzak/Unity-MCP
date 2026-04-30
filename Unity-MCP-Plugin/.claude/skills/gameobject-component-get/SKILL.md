---
name: gameobject-component-get
description: |-
  Get detailed information about a specific Component on a GameObject. Returns component type, enabled state, and optionally serialized fields and properties. Use this to inspect component data before modifying it. Use 'gameobject-find' tool to get the list of all components on the GameObject.
  
  Path-scoped reads (token-saving): supply 'paths' (a list of paths) to read only the listed fields/elements via Reflector.TryReadAt, or 'viewQuery' (a ViewQuery) to navigate to a subtree and/or filter by name regex / max depth / type via Reflector.View. The result is returned in the 'View' field of the response. These two parameters are mutually exclusive — supply at most one.
  Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.
---

# GameObject / Component / Get

## How to Call

```bash
unity-mcp-cli run-tool gameobject-component-get --input '{
  "gameObjectRef": "string_value",
  "componentRef": "string_value",
  "includeFields": false,
  "includeProperties": false,
  "deepSerialization": false,
  "paths": "string_value",
  "viewQuery": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool gameobject-component-get --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool gameobject-component-get --input-file - <<'EOF'
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
| `componentRef` | `any` | Yes | Component reference. Used to find a Component at GameObject. |
| `includeFields` | `boolean` | No | Include serialized fields of the component. |
| `includeProperties` | `boolean` | No | Include serialized properties of the component. |
| `deepSerialization` | `boolean` | No | Performs deep serialization including all nested objects. Otherwise, only serializes top-level members. |
| `paths` | `any` | No | Optional. List of paths to read individually via Reflector.TryReadAt. When supplied, the legacy 'Fields'/'Properties' lists are skipped and the result is returned in 'View'. Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Mutually exclusive with 'viewQuery'. |
| `viewQuery` | `any` | No | Optional. View-query filter routed through Reflector.View. When supplied, the legacy 'Fields'/'Properties' lists are skipped and the filtered subtree is returned in 'View'. Mutually exclusive with 'paths'. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/AIGD.GameObjectRef"
    },
    "componentRef": {
      "$ref": "#/$defs/AIGD.ComponentRef"
    },
    "includeFields": {
      "type": "boolean"
    },
    "includeProperties": {
      "type": "boolean"
    },
    "deepSerialization": {
      "type": "boolean"
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
    "AIGD.ComponentRef": {
      "type": "object",
      "properties": {
        "index": {
          "type": "integer",
          "description": "Component 'index' attached to a gameObject. The first index is '0' and that is usually Transform or RectTransform. Priority: 2. Default value is -1."
        },
        "typeName": {
          "type": "string",
          "description": "Component type full name. Sample 'UnityEngine.Transform'. If the gameObject has two components of the same type, the output component is unpredictable. Priority: 3. Default value is null."
        },
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'."
        }
      },
      "required": [
        "index",
        "instanceID"
      ],
      "description": "Component reference. Used to find a Component at GameObject."
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
    "gameObjectRef",
    "componentRef"
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
      "$ref": "#/$defs/AIGD.GetComponentResponse"
    }
  },
  "$defs": {
    "AIGD.ComponentRef": {
      "type": "object",
      "properties": {
        "index": {
          "type": "integer",
          "description": "Component 'index' attached to a gameObject. The first index is '0' and that is usually Transform or RectTransform. Priority: 2. Default value is -1."
        },
        "typeName": {
          "type": "string",
          "description": "Component type full name. Sample 'UnityEngine.Transform'. If the gameObject has two components of the same type, the output component is unpredictable. Priority: 3. Default value is null."
        },
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0', then it will be used as 'null'."
        }
      },
      "required": [
        "index",
        "instanceID"
      ],
      "description": "Component reference. Used to find a Component at GameObject."
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
    "System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.Model.SerializedMember>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
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
    "AIGD.GetComponentResponse": {
      "type": "object",
      "properties": {
        "Reference": {
          "$ref": "#/$defs/AIGD.ComponentRef",
          "description": "Reference to the component for future operations."
        },
        "Index": {
          "type": "integer",
          "description": "Index of the component in the GameObject's component list."
        },
        "Component": {
          "$ref": "#/$defs/AIGD.ComponentDataShallow",
          "description": "Basic component information (type, enabled state)."
        },
        "Fields": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.Model.SerializedMember>",
          "description": "Serialized fields of the component. Populated only on the legacy code path (no 'paths' / no 'viewQuery')."
        },
        "Properties": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.Model.SerializedMember>",
          "description": "Serialized properties of the component. Populated only on the legacy code path (no 'paths' / no 'viewQuery')."
        },
        "View": {
          "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
          "description": "Path-scoped read or view-query result, populated when 'paths' or 'viewQuery' was supplied. Null otherwise."
        }
      },
      "required": [
        "Index"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

