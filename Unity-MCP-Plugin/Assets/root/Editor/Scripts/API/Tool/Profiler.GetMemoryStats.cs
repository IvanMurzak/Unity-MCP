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
using UnityEngine.Profiling;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_GetMemoryStats",
            Title = "Get Memory Statistics"
        )]
        [Description(@"Gets current memory statistics from the Unity Profiler.
Returns detailed memory information including reserved, allocated, mono heap, and graphics memory.
All values are in megabytes (MB).")]
        public ResponseCallValueTool<MemoryStatsData?> GetMemoryStats()
        {
            return MainThread.Instance.Run(() =>
            {
                var data = new MemoryStatsData
                {
                    TotalReservedMemoryMB = Profiler.GetTotalReservedMemoryLong() / 1048576f,
                    TotalAllocatedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f,
                    TotalUnusedReservedMemoryMB = Profiler.GetTotalUnusedReservedMemoryLong() / 1048576f,
                    MonoHeapSizeMB = Profiler.GetMonoHeapSizeLong() / 1048576f,
                    MonoUsedSizeMB = Profiler.GetMonoUsedSizeLong() / 1048576f,
                    TempAllocatorSizeMB = Profiler.GetTempAllocatorSize() / 1048576f,
                    GraphicsMemoryMB = Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f,
                    MaxUsedMemoryMB = Profiler.maxUsedMemory / 1048576f,
                    UsedHeapSizeMB = Profiler.usedHeapSizeLong / 1048576f
                };

                var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance
                    ?? throw new InvalidOperationException("MCP Plugin instance is not available.");
                var jsonNode = mcpPlugin.McpManager.Reflector.JsonSerializer.SerializeToNode(data);
                var jsonString = jsonNode?.ToJsonString();
                return ResponseCallValueTool<MemoryStatsData?>.SuccessStructured(jsonNode, jsonString);
            });
        }
    }
}

