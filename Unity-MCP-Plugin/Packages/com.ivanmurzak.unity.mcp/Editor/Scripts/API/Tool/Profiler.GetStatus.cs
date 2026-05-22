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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        public const string ProfilerGetStatusToolId = "profiler-get-status";
        [McpPluginTool
        (
            ProfilerGetStatusToolId,
            Title = "Profiler / Get Status",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [McpPluginSkillDescription("Return the Unity profiler's current enabled state, active modules, max-used memory, and platform support flag. Read-only.")]
        [McpPluginSkillBody("Snapshots `UnityEngine.Profiling.Profiler` state and returns it in a single response.\n\n" +
            "## Fields\n\n" +
            "- `ProfilerEnabled` — `Profiler.enabled`.\n" +
            "- `ActiveModules` — names of modules this wrapper considers enabled (local bookkeeping).\n" +
            "- `MaxUsedMemoryMB` — `Profiler.maxUsedMemory / 1048576f`.\n" +
            "- `Supported` — `Profiler.supported`.\n\n" +
            "## Behavior\n\n" +
            "Uses only built-in Unity APIs. No external Unity package is required.")]
        [Description("Returns the current state of the Unity Profiler (enabled flag, active modules, max-used memory, platform support).")]
        public ProfilerStatusData GetStatus(string? nothing = null)
        {
            // Divide in double precision then cast — see GetMemoryStats for the rationale.
            return MainThread.Instance.Run(() => new ProfilerStatusData
            {
                ProfilerEnabled = UnityProfiler.enabled,
                ActiveModules = EnabledModules.OrderBy(name => name).ToList(),
                MaxUsedMemoryMB = (float)(UnityProfiler.maxUsedMemory / 1048576.0),
                Supported = UnityProfiler.supported
            });
        }
    }
}
