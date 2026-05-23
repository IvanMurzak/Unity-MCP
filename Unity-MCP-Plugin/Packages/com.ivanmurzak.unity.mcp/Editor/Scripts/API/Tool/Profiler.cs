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
    [AiToolType]
    public partial class Tool_Profiler
    {
        /// <summary>
        /// Default-enabled subset of <see cref="AvailableModules"/>. Mutated by
        /// <c>profiler-enable-module</c> and read by <c>profiler-get-status</c> /
        /// <c>profiler-list-modules</c>. Unity's runtime Profiler API does not expose
        /// per-module enable/disable, so this is purely a bookkeeping helper for callers;
        /// actual module visibility is controlled from the Profiler window.
        /// </summary>
        internal static readonly HashSet<string> EnabledModules = new HashSet<string>()
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
        /// Canonical list of profiler module names this tool surface understands. Derived
        /// from the names Unity uses in its built-in Profiler window — kept here as a
        /// constant so the wrapper is independent of the optional
        /// `com.unity.profiling.core` package (per task scope, the core tool must rely on
        /// built-in Unity APIs only). Exposed as <see cref="IReadOnlyList{T}"/> so callers
        /// cannot accidentally mutate the canonical list via <c>.Add()</c>.
        /// </summary>
        internal static readonly IReadOnlyList<string> AvailableModules = new List<string>()
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
        }.AsReadOnly();

        public static class Error
        {
            public static string ModuleNameIsRequired()
                => "[Error] Module name is required.";

            public static string UnknownModule(string moduleName)
                => $"[Error] Unknown profiler module: '{moduleName}'. Available modules: {string.Join(", ", AvailableModules)}";

            public static string FilePathIsRequired()
                => "[Error] File path is required.";

            public static string FileNotFound(string filePath)
                => $"[Error] Profiler data file not found: '{filePath}'.";

            public static string FileTooLarge(string filePath, long fileBytes, long maxBytes)
                => $"[Error] Profiler data file '{filePath}' is {fileBytes} bytes, exceeding the {maxBytes}-byte cap.";

            public static string FailedToSaveData(string message)
                => $"[Error] Failed to save profiler data: {message}";

            public static string FailedToLoadData(string message)
                => $"[Error] Failed to load profiler data: {message}";
        }

        [Description("Profiler status data including memory and module information.")]
        public class ProfilerStatusData
        {
            [Description("Whether Unity's runtime profiler is currently enabled (UnityEngine.Profiling.Profiler.enabled).")]
            public bool ProfilerEnabled { get; set; }

            [Description("List of profiler modules this wrapper considers active. Local bookkeeping only.")]
            public List<string>? ActiveModules { get; set; }

            [Description("Maximum used memory recorded by the profiler, in megabytes.")]
            public float MaxUsedMemoryMB { get; set; }

            [Description("Whether profiling is supported on this platform (UnityEngine.Profiling.Profiler.supported).")]
            public bool Supported { get; set; }
        }

        [Description("Memory statistics from the Unity Profiler. All values in megabytes unless otherwise noted.")]
        public class MemoryStatsData
        {
            [Description("Total reserved memory (UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong / 1048576).")]
            public float TotalReservedMemoryMB { get; set; }

            [Description("Total allocated memory (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong / 1048576).")]
            public float TotalAllocatedMemoryMB { get; set; }

            [Description("Total unused reserved memory (UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong / 1048576).")]
            public float TotalUnusedReservedMemoryMB { get; set; }

            [Description("Mono heap size (UnityEngine.Profiling.Profiler.GetMonoHeapSizeLong / 1048576).")]
            public float MonoHeapSizeMB { get; set; }

            [Description("Mono used size (UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong / 1048576).")]
            public float MonoUsedSizeMB { get; set; }

            [Description("Temp allocator size (UnityEngine.Profiling.Profiler.GetTempAllocatorSize / 1048576).")]
            public float TempAllocatorSizeMB { get; set; }

            [Description("Graphics memory reserved by the driver (UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver / 1048576).")]
            public float GraphicsMemoryMB { get; set; }

            [Description("Maximum used memory observed (UnityEngine.Profiling.Profiler.maxUsedMemory / 1048576).")]
            public float MaxUsedMemoryMB { get; set; }

            [Description("Used heap size (UnityEngine.Profiling.Profiler.usedHeapSizeLong / 1048576).")]
            public float UsedHeapSizeMB { get; set; }
        }

        [Description("Rendering statistics from Unity's Time / QualitySettings / SystemInfo. No external package required.")]
        public class RenderingStatsData
        {
            [Description("Last reported frame time (Time.deltaTime * 1000) in milliseconds.")]
            public float FrameTimeMs { get; set; }

            [Description("Frames per second derived from Time.deltaTime.")]
            public float Fps { get; set; }

            [Description("QualitySettings.vSyncCount.")]
            public int VSyncCount { get; set; }

            [Description("Application.targetFrameRate.")]
            public int TargetFrameRate { get; set; }

            [Description("SystemInfo.renderingThreadingMode.")]
            public string? RenderingThreadingMode { get; set; }

            [Description("SystemInfo.graphicsDeviceType.")]
            public string? GraphicsDeviceType { get; set; }
        }

        [Description("Script statistics derived from UnityEngine.Time + UnityEngine.Profiling.Profiler.")]
        public class ScriptStatsData
        {
            [Description("Time.deltaTime in milliseconds.")]
            public float FrameTimeMs { get; set; }

            [Description("Time.fixedDeltaTime in milliseconds.")]
            public float FixedDeltaTimeMs { get; set; }

            [Description("Time.timeScale.")]
            public float TimeScale { get; set; }

            [Description("Total frame count since application start (Time.frameCount).")]
            public int TotalFrameCount { get; set; }

            [Description("Time.realtimeSinceStartup, in seconds.")]
            public float RealtimeSinceStartup { get; set; }

            [Description("Mono used size (Profiler.GetMonoUsedSizeLong / 1048576), in megabytes.")]
            public float MonoMemoryUsageMB { get; set; }

            [Description("System.GC.GetTotalMemory(false) / 1048576, in megabytes.")]
            public float GCMemoryUsageMB { get; set; }
        }

        [Description("Single-frame snapshot of timing information from UnityEngine.Time. No historical frame data; use the Profiler window for that.")]
        public class FrameCaptureData
        {
            [Description("Time.deltaTime in milliseconds at the moment of capture.")]
            public float FrameTimeMs { get; set; }

            [Description("Frames per second derived from Time.deltaTime.")]
            public float Fps { get; set; }

            [Description("Total frame count since application start (Time.frameCount). May include skipped renders.")]
            public int TotalFrameCount { get; set; }

            [Description("Time.realtimeSinceStartup, in seconds.")]
            public float RealtimeSinceStartup { get; set; }

            [Description("Frames actually rendered (Time.renderedFrameCount).")]
            public int RenderedFrameCount { get; set; }
        }

        [Description("Profiler module entry returned by 'profiler-list-modules'.")]
        public class ProfilerModuleInfo
        {
            [Description("Module name (e.g. 'CPU', 'Memory').")]
            public string? Name { get; set; }

            [Description("Whether the wrapper considers this module enabled. Local bookkeeping only.")]
            public bool Enabled { get; set; }
        }

        [Description("Container for 'profiler-list-modules' output.")]
        public class ProfilerModulesData
        {
            [Description("All known profiler modules and their wrapper-side enabled flag.")]
            public List<ProfilerModuleInfo>? Modules { get; set; }
        }
    }
}
