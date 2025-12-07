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
using UnityEngine;
using UnityEngine.Profiling;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_GetScriptStats",
            Title = "Get Script Statistics"
        )]
        [Description(@"Gets current script execution statistics from the Unity Profiler.
Returns frame time, time scale, frame count, and memory usage.
Note: Detailed script profiling requires deep profiling mode in Unity's Profiler window.")]
        public ResponseCallValueTool<ScriptStatsData?> GetScriptStats()
        {
            return MainThread.Instance.Run(() =>
            {
                if (!profilerEnabled)
                    return ResponseCallValueTool<ScriptStatsData?>.Error(Error.ProfilerNotEnabled());

                var data = new ScriptStatsData
                {
                    FrameTimeMs = Time.deltaTime * 1000f,
                    FixedDeltaTimeMs = Time.fixedDeltaTime * 1000f,
                    TimeScale = Time.timeScale,
                    FrameCount = Time.frameCount,
                    RealtimeSinceStartup = Time.realtimeSinceStartup,
                    MonoMemoryUsageMB = Profiler.GetMonoUsedSizeLong() / 1048576f,
                    GCMemoryUsageMB = GC.GetTotalMemory(false) / 1048576f
                };

                var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance
                    ?? throw new InvalidOperationException("MCP Plugin instance is not available.");
                var jsonNode = mcpPlugin.McpManager.Reflector.JsonSerializer.SerializeToNode(data);
                var jsonString = jsonNode?.ToJsonString();
                return ResponseCallValueTool<ScriptStatsData?>.SuccessStructured(jsonNode, jsonString);
            });
        }
    }
}

