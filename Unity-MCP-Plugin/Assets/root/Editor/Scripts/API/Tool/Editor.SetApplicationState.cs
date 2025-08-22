/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common;
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
        public string SetApplicationState
        (
            [Description("If true, the 'playmode' will be started. If false, the 'playmode' will be stopped.")]
            bool isPlaying = false,
            [Description("If true, the 'playmode' will be paused. If false, the 'playmode' will be resumed.")]
            bool isPaused = false
        )
        {
            return MainThread.Instance.Run(() =>
            {
                EditorApplication.isPlaying = isPlaying;
                EditorApplication.isPaused = isPaused;
                return $"[Success] {EditorStats}";
            });
        }
    }
}
