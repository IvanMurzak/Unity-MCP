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
using Profiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        [McpPluginTool
        (
            "Profiler_GetRenderingStats",
            Title = "Get Rendering Statistics"
        )]
        [Description(@"Gets current rendering statistics from the Unity Profiler.
Returns frame time, FPS, VSync settings, and graphics device information.
Note: Detailed rendering statistics (draw calls, batches, etc.) require Unity's Frame Debugger or Profiler window.")]
        public ResponseCallValueTool<RenderingStatsData?> GetRenderingStats()
        {
            return MainThread.Instance.Run(() =>
            {
                if (!Profiler.enabled)
                    return ResponseCallValueTool<RenderingStatsData?>.Error(Error.ProfilerNotEnabled());

                var data = new RenderingStatsData
                {
                    FrameTimeMs = Time.deltaTime * 1000f,
                    Fps = Time.deltaTime > 0 ? 1f / Time.deltaTime : 0f,
                    VSyncCount = QualitySettings.vSyncCount,
                    TargetFrameRate = Application.targetFrameRate,
                    RenderingThreadingMode = SystemInfo.renderingThreadingMode.ToString(),
                    GraphicsDeviceType = SystemInfo.graphicsDeviceType.ToString()
                };

                var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance
                    ?? throw new InvalidOperationException("MCP Plugin instance is not available.");
                var jsonNode = mcpPlugin.McpManager.Reflector.JsonSerializer.SerializeToNode(data);
                var jsonString = jsonNode?.ToJsonString();
                return ResponseCallValueTool<RenderingStatsData?>.SuccessStructured(jsonNode, jsonString);
            });
        }
    }
}

