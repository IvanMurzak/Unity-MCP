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
using System.Linq;
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
        [McpPluginTool
        (
            "Script_Delete",
            Title = "Delete Script content"
        )]
        [Description("Delete the script file(s). Does AssetDatabase.Refresh() and waits for Unity compilation to complete before reporting results.")]
        public static ResponseCallTool Delete
        (
            [Description("File paths to the files. Sample: \"Assets/Scripts/MyScript.cs\".")]
            string[] files,
            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallTool.Error("Original request with valid RequestID must be provided.");

            if (files == null || files.Length == 0)
                return ResponseCallTool.Error(Error.ScriptPathIsEmpty()).SetRequestID(requestId);

            if (files.Any(f => string.IsNullOrEmpty(f)))
                return ResponseCallTool.Error(Error.ScriptPathIsEmpty()).SetRequestID(requestId);

            if (!files.All(f => f.EndsWith(".cs")))
                return ResponseCallTool.Error(Error.FilePathMustEndsWithCs()).SetRequestID(requestId);

            var invalidFiles = files.Where(f => !File.Exists(f)).ToArray();
            if (invalidFiles.Length > 0)
                return ResponseCallTool.Error(Error.ScriptFileNotFound(invalidFiles)).SetRequestID(requestId);

            foreach (var f in files)
            {
                File.Delete(f);
                if (File.Exists(f + ".meta"))
                    File.Delete(f + ".meta");
            }

            MainThread.Instance.RunAsync(async () =>
            {
                await Task.Yield();
                // Schedule notification to be sent after compilation completes (survives domain reload)
                ScriptUtils.SchedulePostCompilationNotification(requestId, string.Join(",", files), "Script deletion");

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            });

            var scriptWord = files.Length > 1 ? "Scripts" : "Script";
            return ResponseCallTool.Processing($"{scriptWord} deleted. Refreshing AssetDatabase and waiting for compilation to complete...").SetRequestID(requestId);
        }
    }
}
