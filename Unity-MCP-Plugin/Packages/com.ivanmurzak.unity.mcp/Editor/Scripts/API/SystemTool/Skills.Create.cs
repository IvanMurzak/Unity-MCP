/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Skills
    {
        public const string SkillsCreateToolId = "unity-skill-create";
        [McpPluginTool
        (
            SkillsCreateToolId,
            Title = "Skill (Tool) / Create",
            DestructiveHint = false,
            Enabled = false,
            ToolType = McpToolType.System
        )]
        [McpPluginSkillDescription("Create a new skill (MCP tool) for the Unity Editor by writing a C# (.cs) file " +
            "that Unity compiles into the project. After compilation the new tool becomes callable through MCP. " +
            "The file must be a partial class decorated with [McpPluginToolType], each tool method must be " +
            "decorated with [McpPluginTool], the class name should match the file name, all Unity API calls must " +
            "run via com.IvanMurzak.ReflectorNet.Utils.MainThread.Instance.Run(), and the method should either " +
            "return a structured data model (for parseable output) or void (for side-effect-only operations). " +
            "See the body of this skill for a full sample and best-practice notes.")]
        [McpPluginSkillBody(@"## Full sample

```csharp
#nullable enable
using System;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using AIGD;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Sample
    {
        [McpPluginTool(""sample-get"", Title = ""Sample / Get"")]
        [Description(""Finds a GameObject and returns its ref data."")]
        public GameObjectRef Get
        (
            [Description(""Name of the GameObject to find."")]
            string name
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var go = GameObject.Find(name)
                    ?? throw new ArgumentException($""GameObject '{name}' not found."", nameof(name));

                return new GameObjectRef(go);
            });
        }

        [McpPluginTool(""sample-rename"", Title = ""Sample / Rename"")]
        [Description(""Renames a GameObject."")]
        public void Rename
        (
            [Description(""Current name of the GameObject."")]
            string name,
            [Description(""New name to assign."")]
            string newName
        )
        {
            MainThread.Instance.Run(() =>
            {
                var go = GameObject.Find(name)
                    ?? throw new ArgumentException($""GameObject '{name}' not found."", nameof(name));

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
2. Return `ResponseCallTool.Processing(""..."").SetRequestID(requestId)` immediately.
3. Schedule the actual work asynchronously via `MainThread.Instance.RunAsync(async () => { await Task.Yield(); ... })`.
4. When the operation finishes, send the final result by calling:
```csharp
_ = UnityMcpPluginEditor.NotifyToolRequestCompleted(new RequestToolCompletedData
{
    RequestId = requestId,
    Result = ResponseCallTool.Success(""Operation completed."").SetRequestID(requestId)
});
```
If the operation may survive a domain reload (e.g. a .cs file was saved and Unity will recompile), use `ScriptUtils.SchedulePostCompilationNotification(requestId, filePath, operationType)` instead of calling `NotifyToolRequestCompleted` directly — it persists the pending notification to `SessionState` and sends it automatically after the domain reload completes. For package install/removal or other non-compilation domain reloads use `PackageUtils.SchedulePostDomainReloadNotification(requestId, label, action, expectedResult)` the same way.

### Return structured data with a typed response
Prefer returning a structured data model over a plain string so the AI can parse individual fields. Data models for MCP tools MUST be declared as TOP-LEVEL types in the `AIGD` namespace - never nested inside the tool class. Place each data model in its own `.cs` file (one type per file) and use `ResponseCallValueTool<T>` as the return type. The flat `AIGD` namespace keeps the auto-generated JSON Schema `$defs` keys short and intuitive for AI agents.
```csharp
// Tool file (Tool/MyTool.cs) - references the data model from AIGD:
using AIGD;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Sample
    {
        public ResponseCallValueTool<MyResult> MyTool(...)
        {
            return ResponseCallValueTool<MyResult>.Success(new MyResult
            {
                Name = go.name,
                InstanceID = go.GetInstanceID()
            }).SetRequestID(requestId);
        }
    }
}

// Data model (Tool/Data/MyResult.cs) - top-level, in AIGD namespace, NOT nested:
namespace AIGD
{
    public class MyResult
    {
        [Description(""Name of the GameObject."")]
        public string? Name { get; set; }

        [Description(""Unity instance ID of the GameObject."")]
        public int InstanceID { get; set; }
    }
}
```
For simpler cases that do not need async/processing, you may return the model directly (without `ResponseCallValueTool<T>`) and Unity-MCP will wrap it automatically. The data model itself MUST still live as a top-level type in the `AIGD` namespace.

### Validate inputs early and throw clearly
Always validate required parameters at the top of the method before any Unity API calls. Throw `ArgumentException` or `InvalidOperationException` with descriptive messages so the AI knows exactly what went wrong and can self-correct:
```csharp
if (string.IsNullOrEmpty(name))
    throw new ArgumentException(""Name cannot be null or empty."", nameof(name));
```

### Always use MainThread for Unity API calls
All Unity API calls (including `GameObject.Find`, `AssetDatabase`, `EditorUtility`, etc.) MUST run on the main thread. Wrap them in `MainThread.Instance.Run(() => { ... })` for synchronous operations, or `MainThread.Instance.RunAsync(async () => { ... })` when you need to await inside.")]
        [Description("Create a new skill using C# code. " +
            "It will be added into the project as a .cs file and compiled by Unity. " +
            "The skill will be available for use after compilation.\n" +
            "\n" +
            "It must be a partial class decorated with [McpPluginToolType]. Each tool method must be decorated with [McpPluginTool]. " +
            "The class name should match the file name. " +
            "All Unity API calls must use com.IvanMurzak.ReflectorNet.Utils.MainThread.Instance.Run(). " +
            "Return a data model for structured output, or void for side-effect-only operations. " +
            "\n\nFull sample:\n" +
            "```csharp\n" +
            "#nullable enable\n" +
            "using System;\n" +
            "using System.ComponentModel;\n" +
            "using com.IvanMurzak.McpPlugin;\n" +
            "using com.IvanMurzak.ReflectorNet.Utils;\n" +
            "using com.IvanMurzak.Unity.MCP.Editor.Utils;\n" +
            "using AIGD;\n" +
            "using UnityEditor;\n" +
            "using UnityEngine;\n" +
            "\n" +
            "namespace com.IvanMurzak.Unity.MCP.Editor.API\n" +
            "{\n" +
            "    [McpPluginToolType]\n" +
            "    public partial class Tool_Sample\n" +
            "    {\n" +
            "        [McpPluginTool(\"sample-get\", Title = \"Sample / Get\")]\n" +
            "        [Description(\"Finds a GameObject and returns its ref data.\")]\n" +
            "        public GameObjectRef Get\n" +
            "        (\n" +
            "            [Description(\"Name of the GameObject to find.\")]\n" +
            "            string name\n" +
            "        )\n" +
            "        {\n" +
            "            return MainThread.Instance.Run(() =>\n" +
            "            {\n" +
            "                var go = GameObject.Find(name)\n" +
            "                    ?? throw new ArgumentException($\"GameObject '{name}' not found.\", nameof(name));\n" +
            "\n" +
            "                return new GameObjectRef(go);\n" +
            "            });\n" +
            "        }\n" +
            "\n" +
            "        [McpPluginTool(\"sample-rename\", Title = \"Sample / Rename\")]\n" +
            "        [Description(\"Renames a GameObject.\")]\n" +
            "        public void Rename\n" +
            "        (\n" +
            "            [Description(\"Current name of the GameObject.\")]\n" +
            "            string name,\n" +
            "            [Description(\"New name to assign.\")]\n" +
            "            string newName\n" +
            "        )\n" +
            "        {\n" +
            "            MainThread.Instance.Run(() =>\n" +
            "            {\n" +
            "                var go = GameObject.Find(name)\n" +
            "                    ?? throw new ArgumentException($\"GameObject '{name}' not found.\", nameof(name));\n" +
            "\n" +
            "                go.name = newName;\n" +
            "                EditorUtility.SetDirty(go);\n" +
            "                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);\n" +
            "                EditorUtils.RepaintAllEditorWindows();\n" +
            "            });\n" +
            "        }\n" +
            "    }\n" +
            "}\n" +
            "```" +
            "\n" +
            "## Suggestions\n" +
            "\n" +
            "### Refresh UI after visual changes\n" +
            "If the skill modifies anything visually in the Unity Editor (GameObjects, components, materials, etc.), " +
            "call these two lines at the end of the tool method to apply changes to the UI immediately:\n" +
            "```csharp\n" +
            "AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);\n" +
            "EditorUtils.RepaintAllEditorWindows();\n" +
            "```\n" +
            "\n" +
            "### Refresh AssetDatabase after asset or script changes\n" +
            "If the skill creates, modifies, or deletes any asset file or .cs script on disk outside of Unity API, " +
            "call this inside a `MainThread.Instance.Run()` block to ensure Unity picks up the changes:\n" +
            "```csharp\n" +
            "MainThread.Instance.Run(() =>\n" +
            "{\n" +
            "    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);\n" +
            "});\n" +
            "```\n" +
            "\n" +
            "### Use processing mechanic for long-running or domain-reload operations\n" +
            "Some operations take time to complete and may trigger a Unity domain reload " +
            "(e.g. writing a .cs script, switching play mode, running tests, adding a package). " +
            "In these cases the tool must NOT block and wait — instead it must:\n" +
            "1. Accept a `[RequestID] string? requestId` parameter.\n" +
            "2. Return `ResponseCallTool.Processing(\"...\").SetRequestID(requestId)` immediately.\n" +
            "3. Schedule the actual work asynchronously via `MainThread.Instance.RunAsync(async () => { await Task.Yield(); ... })`.\n" +
            "4. When the operation finishes, send the final result by calling:\n" +
            "```csharp\n" +
            "_ = UnityMcpPluginEditor.NotifyToolRequestCompleted(new RequestToolCompletedData\n" +
            "{\n" +
            "    RequestId = requestId,\n" +
            "    Result = ResponseCallTool.Success(\"Operation completed.\").SetRequestID(requestId)\n" +
            "});\n" +
            "```\n" +
            "If the operation may survive a domain reload (e.g. a .cs file was saved and Unity will recompile), " +
            "use `ScriptUtils.SchedulePostCompilationNotification(requestId, filePath, operationType)` instead of calling " +
            "`NotifyToolRequestCompleted` directly — it persists the pending notification to `SessionState` and " +
            "sends it automatically after the domain reload completes. " +
            "For package install/removal or other non-compilation domain reloads use " +
            "`PackageUtils.SchedulePostDomainReloadNotification(requestId, label, action, expectedResult)` the same way.\n" +
            "\n" +
            "### Return structured data with a typed response\n" +
            "Prefer returning a structured data model over a plain string so the AI can parse individual fields. " +
            "Data models for MCP tools MUST be declared as TOP-LEVEL types in the `AIGD` namespace - " +
            "never nested inside the tool class. Place each data model in its own `.cs` file (one type per file) " +
            "and use `ResponseCallValueTool<T>` as the return type. The flat `AIGD` namespace keeps the " +
            "auto-generated JSON Schema `$defs` keys short and intuitive for AI agents.\n" +
            "```csharp\n" +
            "// Tool file (Tool/MyTool.cs) - references the data model from AIGD:\n" +
            "using AIGD;\n" +
            "\n" +
            "namespace com.IvanMurzak.Unity.MCP.Editor.API\n" +
            "{\n" +
            "    [McpPluginToolType]\n" +
            "    public partial class Tool_Sample\n" +
            "    {\n" +
            "        public ResponseCallValueTool<MyResult> MyTool(...)\n" +
            "        {\n" +
            "            return ResponseCallValueTool<MyResult>.Success(new MyResult\n" +
            "            {\n" +
            "                Name = go.name,\n" +
            "                InstanceID = go.GetInstanceID()\n" +
            "            }).SetRequestID(requestId);\n" +
            "        }\n" +
            "    }\n" +
            "}\n" +
            "\n" +
            "// Data model (Tool/Data/MyResult.cs) - top-level, in AIGD namespace, NOT nested:\n" +
            "namespace AIGD\n" +
            "{\n" +
            "    public class MyResult\n" +
            "    {\n" +
            "        [Description(\"Name of the GameObject.\")]\n" +
            "        public string? Name { get; set; }\n" +
            "\n" +
            "        [Description(\"Unity instance ID of the GameObject.\")]\n" +
            "        public int InstanceID { get; set; }\n" +
            "    }\n" +
            "}\n" +
            "```\n" +
            "For simpler cases that do not need async/processing, you may return the model directly " +
            "(without `ResponseCallValueTool<T>`) and Unity-MCP will wrap it automatically. " +
            "The data model itself MUST still live as a top-level type in the `AIGD` namespace.\n" +
            "\n" +
            "### Validate inputs early and throw clearly\n" +
            "Always validate required parameters at the top of the method before any Unity API calls. " +
            "Throw `ArgumentException` or `InvalidOperationException` with descriptive messages so the AI " +
            "knows exactly what went wrong and can self-correct:\n" +
            "```csharp\n" +
            "if (string.IsNullOrEmpty(name))\n" +
            "    throw new ArgumentException(\"Name cannot be null or empty.\", nameof(name));\n" +
            "```\n" +
            "\n" +
            "### Always use MainThread for Unity API calls\n" +
            "All Unity API calls (including `GameObject.Find`, `AssetDatabase`, `EditorUtility`, etc.) " +
            "MUST run on the main thread. Wrap them in `MainThread.Instance.Run(() => { ... })` for synchronous " +
            "operations, or `MainThread.Instance.RunAsync(async () => { ... })` when you need to await inside.")]
        public ResponseCallTool Create
        (
            [Description("Path for the C# (.cs) file to be created. Sample: \"Assets/Skills/MySkill.cs\".\n" +
                "CRITICAL — Assembly Definition placement: If the project uses Assembly Definition files (.asmdef), " +
                "you MUST place the script inside a folder that belongs to an assembly definition which already references " +
                "all required dependencies (e.g. com.IvanMurzak.McpPlugin, UnityEditor, UnityEngine). " +
                "Placing the file in the wrong assembly will cause compile errors due to missing type references. " +
                "Before choosing a path, inspect existing .asmdef files with the assets-find tool to identify the correct assembly folder.")]
            string path,

            [Description("C# code for the skill tool.")]
            string code,

            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallTool.Error("Original request with valid RequestID must be provided.");

            if (string.IsNullOrEmpty(path))
                return ResponseCallTool.Error("Path is null or empty. Sample: 'Assets/Skills/MySkill.cs'.").SetRequestID(requestId);

            if (!path.EndsWith(".cs"))
                return ResponseCallTool.Error("Path must end with '.cs'.").SetRequestID(requestId);

            if (Path.IsPathRooted(path))
                return ResponseCallTool.Error("Path must be a relative path inside the Unity project (e.g. 'Assets/Skills/MySkill.cs'). Absolute paths are not allowed.").SetRequestID(requestId);

            var normalizedPath = path.Replace('\\', '/');
            if (normalizedPath.Contains("../") || normalizedPath.EndsWith(".."))
                return ResponseCallTool.Error("Path must not contain '..' traversal segments. Use a path relative to the project root starting with 'Assets/'.").SetRequestID(requestId);

            if (!normalizedPath.StartsWith("Assets/"))
                return ResponseCallTool.Error("Path must start with 'Assets/' (e.g. 'Assets/Skills/MySkill.cs'). Writing outside the Assets folder is not allowed.").SetRequestID(requestId);

            if (!ScriptUtils.IsValidCSharpSyntax(code, out var errors))
                return ResponseCallTool.Error($"Invalid C# syntax:\n{string.Join("\n", errors)}").SetRequestID(requestId);

            var dirPath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dirPath))
                return ResponseCallTool.Error("Path must include a directory component (e.g. 'Assets/Skills/MySkill.cs').").SetRequestID(requestId);

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            var exists = File.Exists(path);
            File.WriteAllText(path, code);

            var operationType = exists ? "Skill updated" : "Skill created";

            MainThread.Instance.RunAsync(async () =>
            {
                await Task.Yield();
                ScriptUtils.SchedulePostCompilationNotification(requestId, path, operationType);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            });

            return ResponseCallTool.Processing($"{operationType}. Refreshing AssetDatabase and waiting for compilation...").SetRequestID(requestId);
        }
    }
}