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
            "Profiler_CaptureFrame",
            Title = "Capture Frame Data"
        )]
        [Description(@"Captures current frame data from the Unity Profiler.
Returns frame timing information, FPS, and frame counts.
Note: This captures a snapshot of the current frame only. Historical frame data requires using Unity's Profiler window.")]
        public ResponseCallValueTool<FrameCaptureData?> CaptureFrame()
        {
            return MainThread.Instance.Run(() =>
            {
                if (!Profiler.enabled)
                    return ResponseCallValueTool<FrameCaptureData?>.Error(Error.ProfilerNotEnabled());

                var data = new FrameCaptureData
                {
                    FrameTimeMs = Time.deltaTime * 1000f,
                    Fps = Time.deltaTime > 0 ? 1f / Time.deltaTime : 0f,
                    TotalFrameCount = Time.frameCount,
                    RealtimeSinceStartup = Time.realtimeSinceStartup,
                    RenderedFrameCount = Time.renderedFrameCount
                };

                var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance
                    ?? throw new InvalidOperationException("MCP Plugin instance is not available.");
                var jsonNode = mcpPlugin.McpManager.Reflector.JsonSerializer.SerializeToNode(data);
                var jsonString = jsonNode?.ToJsonString();
                return ResponseCallValueTool<FrameCaptureData?>.SuccessStructured(jsonNode, jsonString);
            });
        }
    }
}

