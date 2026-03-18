---
name: reflection-method-call
description: Call C# method. Any method could be called, even private methods. It requires to receive proper method schema. Use 'reflection-method-find' to find available method before using it. Receives input parameters and returns result.
---

# Method C# / Call

## How to Call

### CLI (Direct Tool Execution)

Execute this tool directly via command line:

```bash
npx unity-mcp-cli run-tool reflection-method-call --input '{
  "filter": "string_value",
  "knownNamespace": false,
  "typeNameMatchLevel": 0,
  "methodNameMatchLevel": 0,
  "parametersMatchLevel": 0,
  "targetObject": "string_value",
  "inputParameters": "string_value",
  "executeInMainThread": false
}'
```

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filter` | `any` | Yes | Method reference. Used to find method in codebase of the project. |
| `knownNamespace` | `boolean` | No | Set to true if 'Namespace' is known and full namespace name is specified in the 'filter.Namespace' property. Otherwise, set to false. |
| `typeNameMatchLevel` | `integer` | No | Minimal match level for 'typeName'. 0 - ignore 'filter.typeName', 1 - contains ignoring case (default value), 2 - contains case sensitive, 3 - starts with ignoring case, 4 - starts with case sensitive, 5 - equals ignoring case, 6 - equals case sensitive. |
| `methodNameMatchLevel` | `integer` | No | Minimal match level for 'MethodName'. 0 - ignore 'filter.MethodName', 1 - contains ignoring case (default value), 2 - contains case sensitive, 3 - starts with ignoring case, 4 - starts with case sensitive, 5 - equals ignoring case, 6 - equals case sensitive. |
| `parametersMatchLevel` | `integer` | No | Minimal match level for 'Parameters'. 0 - ignore 'filter.Parameters', 1 - parameters count is the same, 2 - equals (default value). |
| `targetObject` | `any` | No | Specify target object to call method on. Should be null if the method is static or if there is no specific target instance. New instance of the specified class will be created if the method is instance method and the targetObject is null. Required: type - full type name of the object to call method on, value - serialized object value (it will be deserialized to the specified type). |
| `inputParameters` | `any` | No | Method input parameters. Per each parameter specify: type - full type name of the object to call method on, name - parameter name, value - serialized object value (it will be deserialized to the specified type). |
| `executeInMainThread` | `boolean` | No | Set to true if the method should be executed in the main thread. Otherwise, set to false. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "filter": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.MethodRef"
    },
    "knownNamespace": {
      "type": "boolean"
    },
    "typeNameMatchLevel": {
      "type": "integer"
    },
    "methodNameMatchLevel": {
      "type": "integer"
    },
    "parametersMatchLevel": {
      "type": "integer"
    },
    "targetObject": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
    },
    "inputParameters": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMemberList"
    },
    "executeInMainThread": {
      "type": "boolean"
    }
  },
  "$defs": {
    "System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.Model.MethodRef+Parameter>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.MethodRef+Parameter",
        "description": "Parameter of a method. Contains type and name of the parameter."
      }
    },
    "com.IvanMurzak.ReflectorNet.Model.MethodRef+Parameter": {
      "type": "object",
      "properties": {
        "typeName": {
          "type": "string",
          "description": "Type of the parameter including namespace. Sample: 'System.String', 'System.Int32', 'UnityEngine.GameObject', etc."
        },
        "name": {
          "type": "string",
          "description": "Name of the parameter. It may be empty if the name is unknown."
        }
      },
      "description": "Parameter of a method. Contains type and name of the parameter."
    },
    "com.IvanMurzak.ReflectorNet.Model.MethodRef": {
      "type": "object",
      "properties": {
        "namespace": {
          "type": "string",
          "description": "Namespace of the class. It may be empty if the class is in the global namespace or the namespace is unknown."
        },
        "typeName": {
          "type": "string",
          "description": "Class name, or substring a class name. It may be empty if the class is unknown."
        },
        "methodName": {
          "type": "string",
          "description": "Method name, or substring of the method name. It may be empty if the method is unknown."
        },
        "inputParameters": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.Model.MethodRef+Parameter>",
          "description": "List of input parameters. Can be null if the method has no parameters or the parameters are unknown."
        }
      },
      "description": "Method reference. Used to find method in codebase of the project."
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
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
    }
  },
  "required": [
    "filter"
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
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
    }
  },
  "$defs": {
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
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
    }
  },
  "required": [
    "result"
  ]
}
```

