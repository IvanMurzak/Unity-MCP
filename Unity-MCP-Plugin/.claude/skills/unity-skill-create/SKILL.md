---
name: unity-skill-create
description: |-
  Create a new skill using C# code. It will be added into the project as a .cs file and compiled by Unity. The skill will be available for use after compilation.
  
  It must be a partial class decorated with [McpPluginToolType]. Each tool method must be decorated with [McpPluginTool]. The class name should match the file name. All Unity API calls must use com.IvanMurzak.ReflectorNet.Utils.MainThread.Instance.Run(). Return a data model for structured output, or void for side-effect-only operations. 
  
  Full sample:
  ```csharp
  #nullable enable
  using System;
  using System.ComponentModel;
  using com.IvanMurzak.McpPlugin;
  using com.IvanMurzak.ReflectorNet.Utils;
  using com.IvanMurzak.Unity.MCP.Editor.Utils;
  using com.IvanMurzak.Unity.MCP.Runtime.Data;
  using UnityEditor;
  using UnityEngine;
  
  namespace com.IvanMurzak.Unity.MCP.Editor.API
  {
      [McpPluginToolType]
      public partial class Tool_Sample
      {
          [McpPluginTool("sample-get", Title = "Sample / Get")]
          [Description("Finds a GameObject and returns its ref data.")]
          public GameObjectRef Get
          (
              [Description("Name of the GameObject to find.")]
              string name
          )
          {
              return MainThread.Instance.Run(() =>
              {
                  var go = GameObject.Find(name)
                      ?? throw new ArgumentException($"GameObject '{name}' not found.", nameof(name));
  
                  return new GameObjectRef(go);
              });
          }
  
          [McpPluginTool("sample-rename", Title = "Sample / Rename")]
          [Description("Renames a GameObject.")]
          public void Rename
          (
              [Description("Current name of the GameObject.")]
              string name,
              [Description("New name to assign.")]
              string newName
          )
          {
              MainThread.Instance.Run(() =>
              {
                  var go = GameObject.Find(name)
                      ?? throw new ArgumentException($"GameObject '{name}' not found.", nameof(name));
  
                  go.name = newName;
                  EditorUtility.SetDirty(go);
                  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                  EditorUtils.RepaintAllEditorWindows();
              });
          }
      }
  }
  ```
  ## Suggestions
  
  ### Refresh UI after visual changes
  If the skill modifies anything visually in the Unity Editor (GameObjects, components, materials, etc.), call these two lines at the end of the tool method to apply changes to the UI immediately:
  ```csharp
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  EditorUtils.RepaintAllEditorWindows();
  ```
  
  ### Refresh AssetDatabase after asset or script changes
  If the skill creates, modifies, or deletes any asset file or .cs script on disk outside of Unity API, call this inside a `MainThread.Instance.Run()` block to ensure Unity picks up the changes:
  ```csharp
  MainThread.Instance.Run(() =>
  {
      AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  });
  ```
  
  ### Use processing mechanic for long-running or domain-reload operations
  Some operations take time to complete and may trigger a Unity domain reload (e.g. writing a .cs script, switching play mode, running tests, adding a package). In these cases the tool must NOT block and wait — instead it must:
  1. Accept a `[RequestID] string? requestId` parameter.
  2. Return `ResponseCallTool.Processing("...").SetRequestID(requestId)` immediately.
  3. Schedule the actual work asynchronously via `MainThread.Instance.RunAsync(async () => { await Task.Yield(); ... })`.
  4. When the operation finishes, send the final result by calling:
  ```csharp
  _ = UnityMcpPluginEditor.NotifyToolRequestCompleted(new RequestToolCompletedData
  {
      RequestId = requestId,
      Result = ResponseCallTool.Success("Operation completed.").SetRequestID(requestId)
  });
  ```
  If the operation may survive a domain reload (e.g. a .cs file was saved and Unity will recompile), use `ScriptUtils.SchedulePostCompilationNotification(requestId, filePath, operationType)` instead of calling `NotifyToolRequestCompleted` directly — it persists the pending notification to `SessionState` and sends it automatically after the domain reload completes. For package install/removal or other non-compilation domain reloads use `PackageUtils.SchedulePostDomainReloadNotification(requestId, label, action, expectedResult)` the same way.
  
  ### Return structured data with a typed response
  Prefer returning a structured data model over a plain string so the AI can parse individual fields. Declare a nested class with `[Description]` on each property and use `ResponseCallValueTool<T>` as return type:
  ```csharp
  // Return type:
  public ResponseCallValueTool<MyResult> MyTool(...)
  {
      return ResponseCallValueTool<MyResult>.Success(new MyResult
      {
          Name = go.name,
          InstanceID = go.GetInstanceID()
      }).SetRequestID(requestId);
  }
  
  // Data model:
  public class MyResult
  {
      [Description("Name of the GameObject.")]
      public string? Name { get; set; }
  
      [Description("Unity instance ID of the GameObject.")]
      public int InstanceID { get; set; }
  }
  ```
  For simpler cases that do not need async/processing, you may return the model directly (without `ResponseCallValueTool<T>`) and Unity-MCP will wrap it automatically.
  
  ### Validate inputs early and throw clearly
  Always validate required parameters at the top of the method before any Unity API calls. Throw `ArgumentException` or `InvalidOperationException` with descriptive messages so the AI knows exactly what went wrong and can self-correct:
  ```csharp
  if (string.IsNullOrEmpty(name))
      throw new ArgumentException("Name cannot be null or empty.", nameof(name));
  ```
  
  ### Always use MainThread for Unity API calls
  All Unity API calls (including `GameObject.Find`, `AssetDatabase`, `EditorUtility`, etc.) MUST run on the main thread. Wrap them in `MainThread.Instance.Run(() => { ... })` for synchronous operations, or `MainThread.Instance.RunAsync(async () => { ... })` when you need to await inside.
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

