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
using UnityEditor;
using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        public const string ProfilerStartToolId = "profiler-start";
        [McpPluginTool
        (
            ProfilerStartToolId,
            Title = "Profiler / Start",
            IdempotentHint = true,
            Enabled = false
        )]
        [McpPluginSkillDescription("Enable Unity's runtime profiler and open the Profiler window. " +
            "Idempotent: calling when already enabled returns the current enabled state without error.")]
        [McpPluginSkillBody("Enables `UnityEngine.Profiling.Profiler.enabled = true` and opens " +
            "`Window > Analysis > Profiler` via `EditorApplication.ExecuteMenuItem`. " +
            "Returns `true` once the profiler is enabled.\n\n" +
            "## Behavior\n\n" +
            "Uses only built-in Unity APIs (`UnityEngine.Profiling`, `UnityEditor.EditorApplication`). " +
            "No external Unity package is required.\n\n" +
            "Snapshot-based: this tool does not stream historical frame data — use Unity's Profiler window directly " +
            "for that.")]
        [Description("Enable the Unity Profiler and open the Profiler window. Returns true once enabled.")]
        public bool Start(string? nothing = null)
        {
            return MainThread.Instance.Run(() =>
            {
                UnityProfiler.enabled = true;
                EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
                return UnityProfiler.enabled;
            });
        }
    }
}
