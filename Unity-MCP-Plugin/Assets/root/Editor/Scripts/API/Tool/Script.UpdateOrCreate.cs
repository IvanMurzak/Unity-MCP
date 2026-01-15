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
    public static partial class Tool_Script
    {
        public const string ScriptUpdateOrCreateToolId = "script-update-or-create";
        [McpPluginTool
        (
            ScriptUpdateOrCreateToolId,
            Title = "Script / Update or Create"
        )]
        [Description("Updates or creates script file with the provided C# code. Does AssetDatabase.Refresh() at the end. Provides compilation error details if the code has syntax errors.")]
        public static ResponseCallTool UpdateOrCreate
        (
            [Description("The path to the file. Sample: \"Assets/Scripts/MyScript.cs\".")]
            string filePath,
            [Description("C# code - content of the file.")]
            string content,
            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallTool.Error("[Error] Original request with valid RequestID must be provided.");

            if (string.IsNullOrEmpty(filePath))
                return ResponseCallTool.Error(Error.ScriptPathIsEmpty()).SetRequestID(requestId);

            if (!filePath.EndsWith(".cs"))
                return ResponseCallTool.Error(Error.FilePathMustEndsWithCs()).SetRequestID(requestId);

            if (!ScriptUtils.IsValidCSharpSyntax(content, out var errors))
                return ResponseCallTool.Error($"[Error] Invalid C# syntax:\n{string.Join("\n", errors)}").SetRequestID(requestId);

            var dirPath = Path.GetDirectoryName(filePath)!;
            if (Directory.Exists(dirPath) == false)
                Directory.CreateDirectory(dirPath);

            var exists = File.Exists(filePath);

            File.WriteAllText(filePath, content);

            var scriptWord = exists
                ? "Script updated"
                : "Script created";

            MainThread.Instance.RunAsync(async () =>
            {
                await Task.Yield();

                // Schedule notification to be sent after compilation completes (survives domain reload)
                ScriptUtils.SchedulePostCompilationNotification(requestId, filePath, $"{scriptWord}");

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            });

            return ResponseCallTool.Processing($"{scriptWord}. Refreshing AssetDatabase and waiting for compilation to complete...").SetRequestID(requestId);
        }
    }
}
