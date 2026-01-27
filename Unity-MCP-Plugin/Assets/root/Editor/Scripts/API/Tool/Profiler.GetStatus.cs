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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Profiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_GetStatus",
            Title = "Get Unity Profiler Status"
        )]
        [Description(@"Gets the current status of the Unity Profiler.
Returns information about whether the profiler is enabled, active modules, and memory usage.")]
        public ResponseCallValueTool<ProfilerStatusData?> GetStatus()
        {
            return MainThread.Instance.Run(() =>
            {
                var data = new ProfilerStatusData
                {
                    Enabled = Profiler.enabled,
                    RuntimeProfilerEnabled = Profiler.enabled,
                    ActiveModules = enabledModules.ToList(),
                    MaxUsedMemoryMB = Profiler.maxUsedMemory / 1048576f,
                    Supported = Profiler.supported
                };

                var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance
                    ?? throw new InvalidOperationException("MCP Plugin instance is not available.");
                var jsonNode = mcpPlugin.McpManager.Reflector.JsonSerializer.SerializeToNode(data);
                var jsonString = jsonNode?.ToJsonString();
                return ResponseCallValueTool<ProfilerStatusData?>.SuccessStructured(jsonNode, jsonString);
            });
        }
    }
}

