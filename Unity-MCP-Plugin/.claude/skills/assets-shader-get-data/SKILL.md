---
name: assets-shader-get-data
description: |-
  Get detailed data about a shader asset in the Unity project. Returns shader properties, subshaders, passes, compilation errors, and supported status. Use 'assets-find' tool with filter 't:Shader' to find shaders, or 'assets-shader-list-all' tool to list all shader names.
  
  Path-scoped reads (token-saving): supply 'paths' (a list of paths) to read only the listed fields/elements via Reflector.TryReadAt, or 'viewQuery' (a ViewQuery) to navigate to a subtree and/or filter by name regex / max depth / type via Reflector.View. The result populates 'View' on the returned ShaderData. These two parameters are mutually exclusive.
  Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.
---

# Assets / Shader / Get Data

## How to Call

```bash
unity-mcp-cli run-tool assets-shader-get-data --input '{
  "assetRef": "string_value",
  "includeMessages": "string_value",
  "includeProperties": "string_value",
  "includeSubshaders": "string_value",
  "includeSourceCode": "string_value",
  "paths": "string_value",
  "viewQuery": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool assets-shader-get-data --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool assets-shader-get-data --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `assetRef` | `any` | Yes | Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders. |
| `includeMessages` | `any` | No | Include compilation error and warning messages. Default: true |
| `includeProperties` | `any` | No | Include shader properties (uniforms) list. Default: false |
| `includeSubshaders` | `any` | No | Include subshader and pass structure. Default: false |
| `includeSourceCode` | `any` | No | Include pass source code in subshader data. Requires 'includeSubshaders' to be true. Can produce very large responses. Default: false |
| `paths` | `any` | No | Optional. List of paths to read individually via Reflector.TryReadAt against the underlying Shader asset. Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Mutually exclusive with 'viewQuery'. |
| `viewQuery` | `any` | No | Optional. View-query filter routed through Reflector.View against the underlying Shader asset. Mutually exclusive with 'paths'. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "assetRef": {
      "$ref": "#/$defs/AIGD.AssetObjectRef"
    },
    "includeMessages": {
      "$ref": "#/$defs/System.Boolean"
    },
    "includeProperties": {
      "$ref": "#/$defs/System.Boolean"
    },
    "includeSubshaders": {
      "$ref": "#/$defs/System.Boolean"
    },
    "includeSourceCode": {
      "$ref": "#/$defs/System.Boolean"
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
    "AIGD.AssetObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0' and 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'."
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
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    },
    "System.Boolean": {
      "type": "boolean"
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
    "assetRef"
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
      "$ref": "#/$defs/AIGD.ShaderData"
    }
  },
  "$defs": {
    "AIGD.AssetObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If this is '0' and 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'."
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
      "description": "Reference to UnityEngine.Object asset instance. It could be Material, ScriptableObject, Prefab, and any other Asset. Anything located in the Assets and Packages folders."
    },
    "System.Type": {
      "type": "string"
    },
    "System.Collections.Generic.List<AIGD.ShaderMessageData>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ShaderMessageData"
      }
    },
    "AIGD.ShaderMessageData": {
      "type": "object",
      "properties": {
        "Message": {
          "type": "string",
          "description": "The error or warning message text."
        },
        "Line": {
          "type": "integer",
          "description": "The line number in the shader source where the issue occurs."
        },
        "Severity": {
          "type": "string",
          "description": "Severity level (e.g. 'Error', 'Warning')."
        },
        "Platform": {
          "type": "string",
          "description": "The platform on which the error occurs (e.g. 'OpenGLCore', 'D3D11')."
        }
      },
      "required": [
        "Line"
      ]
    },
    "System.Collections.Generic.List<AIGD.ShaderPropertyData>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.ShaderPropertyData"
      }
    },
    "AIGD.ShaderPropertyData": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string",
          "description": "Property name as used in shader code (e.g. '_MainTex', '_Color')."
        },
        "Description": {
          "type": "string",
          "description": "Human-readable description/display name of the property."
        },
        "Type": {
          "type": "string",
          "description": "Property type (e.g. 'Color', 'Float', 'Range', 'Texture', 'Vector', 'Int')."
        },
        "Flags": {
          "type": "string",
          "description": "Property flags (e.g. 'None', 'HideInInspector', 'PerRendererData')."
        },
        "NameId": {
          "type": "integer",
          "description": "The unique name ID for this property."
        },
        "RangeMin": {
          "type": "number",
          "description": "Minimum value for Range properties. Null for non-range properties."
        },
        "RangeMax": {
          "type": "number",
          "description": "Maximum value for Range properties. Null for non-range properties."
        },
        "DefaultTextureName": {
          "type": "string",
          "description": "Default texture name for Texture properties. Null if not applicable."
        },
        "Attributes": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>",
          "description": "Custom attributes applied to this property. Null if none."
        }
      },
      "required": [
        "NameId"
      ]
    },
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "System.Collections.Generic.List<AIGD.SubshaderData>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.SubshaderData"
      }
    },
    "AIGD.SubshaderData": {
      "type": "object",
      "properties": {
        "Index": {
          "type": "integer",
          "description": "Index of this subshader within the shader."
        },
        "PassCount": {
          "type": "integer",
          "description": "Number of passes in this subshader."
        },
        "Passes": {
          "$ref": "#/$defs/System.Collections.Generic.List<AIGD.PassData>",
          "description": "List of passes in this subshader. Null if no passes."
        }
      },
      "required": [
        "Index",
        "PassCount"
      ]
    },
    "System.Collections.Generic.List<AIGD.PassData>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/AIGD.PassData"
      }
    },
    "AIGD.PassData": {
      "type": "object",
      "properties": {
        "Index": {
          "type": "integer",
          "description": "Index of this pass within the subshader."
        },
        "Name": {
          "type": "string",
          "description": "Name of the pass. Null if unnamed."
        },
        "SourceCode": {
          "type": "string",
          "description": "Source code of the pass. Null if unavailable."
        }
      },
      "required": [
        "Index"
      ]
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
    "AIGD.ShaderData": {
      "type": "object",
      "properties": {
        "Reference": {
          "$ref": "#/$defs/AIGD.AssetObjectRef",
          "description": "Reference to the shader asset for future operations."
        },
        "Name": {
          "type": "string",
          "description": "Full name of the shader (e.g. 'Standard', 'Universal Render Pipeline/Lit')."
        },
        "IsSupported": {
          "type": "boolean",
          "description": "Whether the shader is supported on the current GPU and platform."
        },
        "RenderQueue": {
          "type": "integer",
          "description": "The render queue value of the shader."
        },
        "HasErrors": {
          "type": "boolean",
          "description": "Whether the shader has any compilation errors."
        },
        "PropertyCount": {
          "type": "integer",
          "description": "Number of properties exposed by the shader."
        },
        "PassCount": {
          "type": "integer",
          "description": "Total number of passes in the shader."
        },
        "RenderType": {
          "type": "string",
          "description": "The RenderType tag value from the first pass, if set."
        },
        "Messages": {
          "$ref": "#/$defs/System.Collections.Generic.List<AIGD.ShaderMessageData>",
          "description": "Compilation messages including errors and warnings. Null if no messages."
        },
        "Properties": {
          "$ref": "#/$defs/System.Collections.Generic.List<AIGD.ShaderPropertyData>",
          "description": "List of shader properties (uniforms). Null if the shader has no properties."
        },
        "Subshaders": {
          "$ref": "#/$defs/System.Collections.Generic.List<AIGD.SubshaderData>",
          "description": "List of subshaders with their passes. Null if shader data is unavailable."
        },
        "View": {
          "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
          "description": "Path-scoped read or view-query result, populated when 'paths' or 'viewQuery' is supplied. Null otherwise."
        }
      },
      "required": [
        "IsSupported",
        "RenderQueue",
        "HasErrors",
        "PropertyCount",
        "PassCount"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

