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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Profiler
    {
        /// <summary>
        /// Tracks profiler enabled state locally.
        /// </summary>
        private static bool profilerEnabled = false;

        /// <summary>
        /// Set of enabled profiler modules.
        /// </summary>
        private static readonly HashSet<string> enabledModules = new HashSet<string>()
        {
            "CPU",
            "GPU",
            "Rendering",
            "Memory",
            "Audio",
            "Video",
            "Physics",
            "Physics2D",
            "UI"
        };

        /// <summary>
        /// List of all available profiler modules.
        /// </summary>
        public static readonly List<string> AvailableModules = new List<string>()
        {
            "CPU",
            "GPU",
            "Rendering",
            "Memory",
            "Audio",
            "Video",
            "Physics",
            "Physics2D",
            "NetworkMessages",
            "NetworkOperations",
            "UI",
            "UIDetails",
            "GlobalIllumination",
            "VirtualTexturing"
        };

        public static class Error
        {
            public static string ProfilerNotEnabled()
                => "[Error] Profiler must be enabled to perform this operation. Use 'Profiler_Start' first.";

            public static string ModuleNameIsRequired()
                => "[Error] Module name is required.";

            public static string UnknownModule(string moduleName)
                => $"[Error] Unknown profiler module: '{moduleName}'. Available modules: {string.Join(", ", AvailableModules)}";

            public static string FilePathIsRequired()
                => "[Error] File path is required.";

            public static string FileNotFound(string filePath)
                => $"[Error] Profiler data file not found: '{filePath}'.";

            public static string FailedToSaveData(string message)
                => $"[Error] Failed to save profiler data: {message}";

            public static string FailedToLoadData(string message)
                => $"[Error] Failed to load profiler data: {message}";
        }

        [Description("Profiler status data including memory and module information.")]
        public class ProfilerStatusData
        {
            [Description("Whether the profiler is enabled.")]
            public bool Enabled { get; set; }

            [Description("Whether Unity's runtime profiler is enabled.")]
            public bool RuntimeProfilerEnabled { get; set; }

            [Description("List of active profiler modules.")]
            public List<string>? ActiveModules { get; set; }

            [Description("Maximum used memory in MB.")]
            public float MaxUsedMemoryMB { get; set; }

            [Description("Whether profiling is supported on this platform.")]
            public bool Supported { get; set; }
        }

        [Description("Memory statistics from the Unity Profiler.")]
        public class MemoryStatsData
        {
            [Description("Total reserved memory in MB.")]
            public float TotalReservedMemoryMB { get; set; }

            [Description("Total allocated memory in MB.")]
            public float TotalAllocatedMemoryMB { get; set; }

            [Description("Total unused reserved memory in MB.")]
            public float TotalUnusedReservedMemoryMB { get; set; }

            [Description("Mono heap size in MB.")]
            public float MonoHeapSizeMB { get; set; }

            [Description("Mono used size in MB.")]
            public float MonoUsedSizeMB { get; set; }

            [Description("Temp allocator size in MB.")]
            public float TempAllocatorSizeMB { get; set; }

            [Description("Graphics memory for driver in MB.")]
            public float GraphicsMemoryMB { get; set; }

            [Description("Maximum used memory in MB.")]
            public float MaxUsedMemoryMB { get; set; }

            [Description("Used heap size in MB.")]
            public float UsedHeapSizeMB { get; set; }
        }

        [Description("Rendering statistics from the Unity Profiler.")]
        public class RenderingStatsData
        {
            [Description("Frame time in milliseconds.")]
            public float FrameTimeMs { get; set; }

            [Description("Frames per second.")]
            public float Fps { get; set; }

            [Description("VSync count setting.")]
            public int VSyncCount { get; set; }

            [Description("Target frame rate.")]
            public int TargetFrameRate { get; set; }

            [Description("Rendering threading mode.")]
            public string? RenderingThreadingMode { get; set; }

            [Description("Graphics device type.")]
            public string? GraphicsDeviceType { get; set; }
        }

        [Description("Script statistics from the Unity Profiler.")]
        public class ScriptStatsData
        {
            [Description("Frame time in milliseconds.")]
            public float FrameTimeMs { get; set; }

            [Description("Fixed delta time in milliseconds.")]
            public float FixedDeltaTimeMs { get; set; }

            [Description("Time scale.")]
            public float TimeScale { get; set; }

            [Description("Current frame count.")]
            public int FrameCount { get; set; }

            [Description("Real time since startup in seconds.")]
            public float RealtimeSinceStartup { get; set; }

            [Description("Mono memory usage in MB.")]
            public float MonoMemoryUsageMB { get; set; }

            [Description("GC memory usage in MB.")]
            public float GCMemoryUsageMB { get; set; }
        }

        [Description("Frame capture data.")]
        public class FrameCaptureData
        {
            [Description("Frame time in milliseconds.")]
            public float FrameTimeMs { get; set; }

            [Description("Frames per second.")]
            public float Fps { get; set; }

            [Description("Current frame count.")]
            public int FrameCount { get; set; }

            [Description("Real time since startup in seconds.")]
            public float RealtimeSinceStartup { get; set; }

            [Description("Rendered frame count.")]
            public int RenderedFrameCount { get; set; }
        }

        [Description("Profiler module information.")]
        public class ProfilerModuleInfo
        {
            [Description("Module name.")]
            public string? Name { get; set; }

            [Description("Whether the module is enabled.")]
            public bool Enabled { get; set; }
        }
    }
}

