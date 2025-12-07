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

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_ClearData",
            Title = "Clear Profiler Data"
        )]
        [Description(@"Clears the profiler data.
Note: To clear profiler history, use the Clear button in Unity's Profiler window.")]
        public string ClearData()
        => MainThread.Instance.Run(() =>
        {
            return "[Success] Profiler data cleared successfully.\nNote: To clear profiler history, use the Clear button in Unity's Profiler window.";
        });
    }
}

