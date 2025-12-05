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
using System;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Editor
    {
        [McpPluginTool
        (
            "Editor_SetApplicationState",
            Title = "Set Unity Editor application state"
        )]
        [Description("Control the Unity Editor application state. You can start, stop, or pause the 'playmode'.")]
        public ResponseCallValueTool<EditorStatsData?> SetApplicationState
        (
            [Description("If true, the 'playmode' will be started. If false, the 'playmode' will be stopped.")]
            bool isPlaying = false,
            [Description("If true, the 'playmode' will be paused. If false, the 'playmode' will be resumed.")]
            bool isPaused = false
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (UnityEditor.EditorUtility.scriptCompilationFailed)
                {
                    var compilationErrorDetails = ScriptUtils.GetCompilationErrorDetails();
                    return ResponseCallValueTool<EditorStatsData?>
                        .Error($"Unity project has compilation error. Please fix all compilation errors before doing this operation.\n{compilationErrorDetails}");
                }
                EditorApplication.isPlaying = isPlaying;
                EditorApplication.isPaused = isPaused;

                var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance ?? throw new InvalidOperationException("MCP Plugin instance is not available.");
                var jsonNode = mcpPlugin.McpManager.Reflector.JsonSerializer.SerializeToNode(EditorStatsData.FromEditor());
                var jsonString = jsonNode?.ToJsonString();
                return ResponseCallValueTool<EditorStatsData?>.SuccessStructured(jsonNode, jsonString);
            });
        }
    }
}
