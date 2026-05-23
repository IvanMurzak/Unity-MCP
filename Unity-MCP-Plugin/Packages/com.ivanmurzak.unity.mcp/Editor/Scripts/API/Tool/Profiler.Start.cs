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
using UnityEngine;
using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        public const string ProfilerStartToolId = "profiler-start";
        [AiTool
        (
            ProfilerStartToolId,
            Title = "Profiler / Start",
            IdempotentHint = true,
            Enabled = false
        )]
        [AiSkillDescription("Enable Unity's runtime profiler and open the Profiler window. " +
            "Idempotent: calling when already enabled returns the current enabled state without error.")]
        [AiSkillBody("Enables `UnityEngine.Profiling.Profiler.enabled = true` and opens " +
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
                // ExecuteMenuItem returns false silently when the menu path is renamed
                // or missing — log a warning so a broken UI hook does not look like a
                // successful Start() (the profiler-enabled flag flips regardless).
                if (!EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler"))
                    Debug.LogWarning("[Tool_Profiler] Could not open menu 'Window/Analysis/Profiler'. The runtime profiler is still enabled.");
                return UnityProfiler.enabled;
            });
        }
    }
}
