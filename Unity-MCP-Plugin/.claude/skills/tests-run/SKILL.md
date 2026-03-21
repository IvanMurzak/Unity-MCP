---
name: tests-run
description: Execute Unity tests and return detailed results. Supports filtering by test mode, assembly, namespace, class, and method. Recommended to use 'EditMode' for faster iteration during development.
---

# Tests / Run

## How to Call

```bash
unity-mcp-cli run-tool tests-run --input '{
  "testMode": "string_value",
  "testAssembly": "string_value",
  "testNamespace": "string_value",
  "testClass": "string_value",
  "testMethod": "string_value",
  "includePassingTests": false,
  "includeMessages": false,
  "includeStacktrace": false,
  "includeLogs": false,
  "logType": "string_value",
  "includeLogsStacktrace": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool tests-run --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool tests-run --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `testMode` | `string` | No | Test mode to run. Options: 'EditMode', 'PlayMode'. Default: 'EditMode' |
| `testAssembly` | `string` | No | Specific test assembly name to run (optional). Example: 'Assembly-CSharp-Editor-testable' |
| `testNamespace` | `string` | No | Specific test namespace to run (optional). Example: 'MyTestNamespace' |
| `testClass` | `string` | No | Specific test class name to run (optional). Example: 'MyTestClass' |
| `testMethod` | `string` | No | Specific fully qualified test method to run (optional). Example: 'MyTestNamespace.FixtureName.TestName' |
| `includePassingTests` | `boolean` | No | Include details for all tests, both passing and failing (default: false). If you just need details for failing tests, set to false. |
| `includeMessages` | `boolean` | No | Include test result messages in the test results (default: true). If you just need pass/fail status, set to false. |
| `includeStacktrace` | `boolean` | No | Include stack traces in the test results (default: false). |
| `includeLogs` | `boolean` | No | Include console logs in the test results (default: false). |
| `logType` | `string` | No | Log type filter for console logs. Options: 'Log', 'Warning', 'Assert', 'Error', 'Exception'. (default: 'Warning') |
| `includeLogsStacktrace` | `boolean` | No | Include stack traces for console logs in the test results (default: false). This is huge amount of data, use only if really needed. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "testMode": {
      "type": "string",
      "enum": [
        "EditMode",
        "PlayMode"
      ]
    },
    "testAssembly": {
      "type": "string"
    },
    "testNamespace": {
      "type": "string"
    },
    "testClass": {
      "type": "string"
    },
    "testMethod": {
      "type": "string"
    },
    "includePassingTests": {
      "type": "boolean"
    },
    "includeMessages": {
      "type": "boolean"
    },
    "includeStacktrace": {
      "type": "boolean"
    },
    "includeLogs": {
      "type": "boolean"
    },
    "logType": {
      "type": "string",
      "enum": [
        "Error",
        "Assert",
        "Warning",
        "Log",
        "Exception"
      ]
    },
    "includeLogsStacktrace": {
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestRunResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestSummaryData": {
      "type": "object",
      "properties": {
        "Status": {
          "type": "string",
          "enum": [
            "Unknown",
            "Passed",
            "Failed"
          ]
        },
        "TotalTests": {
          "type": "integer"
        },
        "PassedTests": {
          "type": "integer"
        },
        "FailedTests": {
          "type": "integer"
        },
        "SkippedTests": {
          "type": "integer"
        },
        "Duration": {
          "type": "string"
        }
      },
      "required": [
        "Status",
        "TotalTests",
        "PassedTests",
        "FailedTests",
        "SkippedTests",
        "Duration"
      ]
    },
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestResultData>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestResultData"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestResultData": {
      "type": "object",
      "properties": {
        "Name": {
          "type": "string"
        },
        "Status": {
          "type": "string",
          "enum": [
            "Passed",
            "Failed",
            "Skipped"
          ]
        },
        "Duration": {
          "type": "string"
        },
        "Message": {
          "type": "string"
        },
        "StackTrace": {
          "type": "string"
        }
      },
      "required": [
        "Status",
        "Duration"
      ]
    },
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestLogEntry>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestLogEntry"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestLogEntry": {
      "type": "object",
      "properties": {
        "Condition": {
          "type": "string"
        },
        "StackTrace": {
          "type": "string"
        },
        "Type": {
          "type": "string",
          "enum": [
            "Error",
            "Assert",
            "Warning",
            "Log",
            "Exception"
          ]
        },
        "Timestamp": {
          "type": "string",
          "format": "date-time"
        },
        "LogLevel": {
          "type": "integer"
        }
      },
      "required": [
        "Type",
        "Timestamp"
      ]
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestRunResponse": {
      "type": "object",
      "properties": {
        "Summary": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestSummaryData",
          "description": "Summary of the test run including total, passed, failed, and skipped counts."
        },
        "Results": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestResultData>",
          "description": "List of individual test results with details about each test."
        },
        "Logs": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.TestRunner.TestLogEntry>",
          "description": "Log entries captured during test execution."
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

