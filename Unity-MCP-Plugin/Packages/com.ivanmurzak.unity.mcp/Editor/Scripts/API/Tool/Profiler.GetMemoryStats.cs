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
using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        public const string ProfilerGetMemoryStatsToolId = "profiler-get-memory-stats";
        [McpPluginTool
        (
            ProfilerGetMemoryStatsToolId,
            Title = "Profiler / Get Memory Stats",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [McpPluginSkillDescription("Return memory statistics snapshot from UnityEngine.Profiling.Profiler — reserved, allocated, mono heap, graphics, etc. (in MB).")]
        [McpPluginSkillBody("Reads UnityEngine.Profiling.Profiler scalar memory counters and converts each from bytes to megabytes (`/1048576f`).\n\n" +
            "## Fields\n\n" +
            "- `TotalReservedMemoryMB` — `GetTotalReservedMemoryLong()`.\n" +
            "- `TotalAllocatedMemoryMB` — `GetTotalAllocatedMemoryLong()`.\n" +
            "- `TotalUnusedReservedMemoryMB` — `GetTotalUnusedReservedMemoryLong()`.\n" +
            "- `MonoHeapSizeMB` — `GetMonoHeapSizeLong()`.\n" +
            "- `MonoUsedSizeMB` — `GetMonoUsedSizeLong()`.\n" +
            "- `TempAllocatorSizeMB` — `GetTempAllocatorSize()`.\n" +
            "- `GraphicsMemoryMB` — `GetAllocatedMemoryForGraphicsDriver()`.\n" +
            "- `MaxUsedMemoryMB` — `maxUsedMemory`.\n" +
            "- `UsedHeapSizeMB` — `usedHeapSizeLong`.\n\n" +
            "## Behavior\n\n" +
            "Uses only built-in Unity APIs (`UnityEngine.Profiling.Profiler`). No external Unity package is required.")]
        [Description("Returns memory statistics from the Unity Profiler (all values in MB).")]
        public MemoryStatsData GetMemoryStats(string? nothing = null)
        {
            return MainThread.Instance.Run(() => new MemoryStatsData
            {
                TotalReservedMemoryMB = UnityProfiler.GetTotalReservedMemoryLong() / 1048576f,
                TotalAllocatedMemoryMB = UnityProfiler.GetTotalAllocatedMemoryLong() / 1048576f,
                TotalUnusedReservedMemoryMB = UnityProfiler.GetTotalUnusedReservedMemoryLong() / 1048576f,
                MonoHeapSizeMB = UnityProfiler.GetMonoHeapSizeLong() / 1048576f,
                MonoUsedSizeMB = UnityProfiler.GetMonoUsedSizeLong() / 1048576f,
                TempAllocatorSizeMB = UnityProfiler.GetTempAllocatorSize() / 1048576f,
                GraphicsMemoryMB = UnityProfiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f,
                MaxUsedMemoryMB = UnityProfiler.maxUsedMemory / 1048576f,
                UsedHeapSizeMB = UnityProfiler.usedHeapSizeLong / 1048576f
            });
        }
    }
}
