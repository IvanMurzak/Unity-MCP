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

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Editor
    {
        [McpPluginTool
        (
            "Editor_GetApplicationInformation",
            Title = "Get Unity Editor application information"
        )]
        [Description(@"Returns available information about 'UnityEditor.EditorApplication'.
Use it to get information about the current state of the Unity Editor application. Such as: playmode, paused state, compilation state, etc.")]
        public ResponseCallValueTool<EditorStatsData?> GetApplicationInformation()
        {
            return MainThread.Instance.Run(() =>
            {
                var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance ?? throw new InvalidOperationException("MCP Plugin instance is not available.");
                var jsonNode = mcpPlugin.McpManager.Reflector.JsonSerializer.SerializeToNode(EditorStatsData.FromEditor());
                var jsonString = jsonNode?.ToJsonString();
                return ResponseCallValueTool<EditorStatsData?>.SuccessStructured(jsonNode, jsonString);
            });
        }
    }
}
