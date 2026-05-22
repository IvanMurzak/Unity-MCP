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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Profiler
    {
        public const string ProfilerGetRenderingStatsToolId = "profiler-get-rendering-stats";
        [McpPluginTool
        (
            ProfilerGetRenderingStatsToolId,
            Title = "Profiler / Get Rendering Stats",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = false
        )]
        [McpPluginSkillDescription("Return current frame timing, FPS, vsync, target frame rate, threading mode, and graphics device type from Unity Time / QualitySettings / SystemInfo.")]
        [McpPluginSkillBody("Snapshots rendering-related fields from `UnityEngine.Time`, `UnityEngine.QualitySettings`, " +
            "`UnityEngine.Application` and `UnityEngine.SystemInfo`. All values are read from built-in Unity APIs " +
            "so no external Unity package is required.\n\n" +
            "## Fields\n\n" +
            "- `FrameTimeMs` — `Time.deltaTime * 1000f`.\n" +
            "- `Fps` — `1 / Time.deltaTime` (0 when delta is zero).\n" +
            "- `VSyncCount` — `QualitySettings.vSyncCount`.\n" +
            "- `TargetFrameRate` — `Application.targetFrameRate`.\n" +
            "- `RenderingThreadingMode` — `SystemInfo.renderingThreadingMode.ToString()`.\n" +
            "- `GraphicsDeviceType` — `SystemInfo.graphicsDeviceType.ToString()`.")]
        [Description("Returns rendering statistics: frame time, FPS, vsync, target frame rate, threading mode, graphics device type.")]
        public RenderingStatsData GetRenderingStats(string? nothing = null)
        {
            return MainThread.Instance.Run(() =>
            {
                var delta = Time.deltaTime;
                return new RenderingStatsData
                {
                    FrameTimeMs = delta * 1000f,
                    Fps = delta > 0f ? 1f / delta : 0f,
                    VSyncCount = QualitySettings.vSyncCount,
                    TargetFrameRate = Application.targetFrameRate,
                    RenderingThreadingMode = SystemInfo.renderingThreadingMode.ToString(),
                    GraphicsDeviceType = SystemInfo.graphicsDeviceType.ToString()
                };
            });
        }
    }
}
