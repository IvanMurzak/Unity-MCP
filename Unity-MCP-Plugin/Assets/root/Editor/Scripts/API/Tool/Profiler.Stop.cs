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
using Profiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_Stop",
            Title = "Stop Unity Profiler"
        )]
        [Description("Stops the Unity Profiler and disables data collection.")]
        public string Stop()
        => MainThread.Instance.Run(() =>
        {
            if (!Profiler.enabled)
                return "[Success] Profiler is already stopped.";

            Profiler.enabled = false;

            return "[Success] Profiler stopped successfully.";
        });
    }
}

