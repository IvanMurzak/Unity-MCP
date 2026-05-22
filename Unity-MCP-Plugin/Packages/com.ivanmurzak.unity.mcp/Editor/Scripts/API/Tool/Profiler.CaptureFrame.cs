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
        public const string ProfilerCaptureFrameToolId = "profiler-capture-frame";
        [McpPluginTool
        (
            ProfilerCaptureFrameToolId,
            Title = "Profiler / Capture Frame",
            ReadOnlyHint = true,
            Enabled = false
        )]
        [McpPluginSkillDescription("Capture the current frame's timing info (delta time, FPS, frame counts, runtime). Snapshot only — historical frames live in Unity's Profiler window.")]
        [McpPluginSkillBody("Reads `UnityEngine.Time` fields and returns them in a single struct. " +
            "This tool is intentionally a single-frame snapshot — Unity's runtime API does not expose historical " +
            "frame-data outside the Profiler window.\n\n" +
            "## Fields\n\n" +
            "- `FrameTimeMs` — `Time.deltaTime * 1000f`.\n" +
            "- `Fps` — `1 / Time.deltaTime` (0 when delta is zero).\n" +
            "- `TotalFrameCount` — `Time.frameCount` (includes skipped renders).\n" +
            "- `RealtimeSinceStartup` — `Time.realtimeSinceStartup`.\n" +
            "- `RenderedFrameCount` — `Time.renderedFrameCount`.\n\n" +
            "## Behavior\n\n" +
            "Uses only built-in Unity APIs (`UnityEngine.Time`). No external Unity package is required.")]
        [Description("Captures current frame timing data (delta time, FPS, total + rendered frame counts, runtime).")]
        public FrameCaptureData CaptureFrame
        (
            [Description("Reserved for future per-N-frames capture. Currently ignored — only the current frame is captured.")]
            int frameCount = 1
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var delta = Time.deltaTime;
                return new FrameCaptureData
                {
                    FrameTimeMs = delta * 1000f,
                    Fps = delta > 0f ? 1f / delta : 0f,
                    TotalFrameCount = Time.frameCount,
                    RealtimeSinceStartup = Time.realtimeSinceStartup,
                    RenderedFrameCount = Time.renderedFrameCount
                };
            });
        }
    }
}
