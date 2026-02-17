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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Profiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_Start",
            Title = "Start Unity Profiler"
        )]
        [Description(@"Starts the Unity Profiler for performance analysis.
Opens the Profiler window and enables data collection.
Note: For detailed, historical profiling, use the Unity Profiler window directly.")]
        public string Start()
        => MainThread.Instance.Run(() =>
        {
            if (Profiler.enabled)
                return "[Success] Profiler is already running.";

            Profiler.enabled = true;
            #if UNITY_EDITOR
            EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
            #endif

            var data = new
            {
                enabled = true,
                targetFrameRate = Application.targetFrameRate,
                note = "Profiler window has been opened. Use Unity's Profiler window for detailed analysis."
            };

            var jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var json = System.Text.Json.JsonSerializer.Serialize(data, jsonOptions);

            return $"[Success] Profiler started successfully.\n{json}";
        });
    }
}

