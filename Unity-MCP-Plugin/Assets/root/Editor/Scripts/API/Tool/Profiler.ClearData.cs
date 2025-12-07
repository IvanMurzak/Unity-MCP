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
using UnityEditorInternal;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_ClearData",
            Title = "Clear Profiler Data"
        )]
        [Description("Clears all recorded profiler frames and data.")]
        public string ClearData()
        => MainThread.Instance.Run(() =>
        {
            ProfilerDriver.ClearAllFrames();
            return "[Success] Profiler data cleared successfully. All recorded frames have been removed.";
        });
    }
}

