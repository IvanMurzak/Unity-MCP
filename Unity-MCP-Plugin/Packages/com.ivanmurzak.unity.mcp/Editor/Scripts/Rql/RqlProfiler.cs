/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Rql;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// The <c>profiler</c> source: Unity's built-in profiler state and snapshots (memory, rendering, script timing, the
    /// current frame), read with rql-query and driven with rql-mutate calls, e.g.
    /// <c>profiler | select memory.totalAllocatedMB, memory.monoUsedSizeMB</c>, <c>profiler | call Start()</c>.
    /// Every snapshot member is read live; values are megabytes / milliseconds as named. Built-in Unity APIs only.
    /// </summary>
    public sealed class RqlProfiler
    {
        public static readonly RqlProfiler Instance = new RqlProfiler();

        /// <summary>Largest snapshot file <see cref="Load"/> reads back.</summary>
        public const long MaxLoadFileSizeBytes = 10L * 1024L * 1024L;

        /// <summary>Profiler module names this source knows (the names of Unity's Profiler window).</summary>
        public static readonly IReadOnlyList<string> AvailableModules = new[]
        {
            "CPU", "GPU", "Rendering", "Memory", "Audio", "Video", "Physics", "Physics2D", "NetworkMessages",
            "NetworkOperations", "UI", "UIDetails", "GlobalIllumination", "VirtualTexturing"
        };

        // Unity's runtime Profiler API has no per-module switch: this is bookkeeping for callers; the Profiler window
        // controls what is actually shown.
        private static readonly HashSet<string> EnabledModules = new HashSet<string>
        {
            "CPU", "GPU", "Rendering", "Memory", "Audio", "Video", "Physics", "Physics2D", "UI"
        };

        [RqlDescription("Unity's runtime profiler is recording (Profiler.enabled); assign to switch it, or call Start() / Stop()")]
        public bool enabled { get => UnityProfiler.enabled; set => UnityProfiler.enabled = value; }

        [RqlDescription("profiling is supported on this platform (Profiler.supported)")]
        public bool supported => UnityProfiler.supported;

        [RqlDescription("memory snapshot, megabytes")]
        public RqlProfilerMemory memory => RqlProfilerMemory.Now();

        [RqlDescription("rendering snapshot: frame time, fps, vsync, target frame rate, graphics device")]
        public RqlProfilerRendering rendering => RqlProfilerRendering.Now();

        [RqlDescription("script timing snapshot plus Mono / GC memory, milliseconds and megabytes")]
        public RqlProfilerScript script => RqlProfilerScript.Now();

        [RqlDescription("the current frame: frame time, fps, frame counts (no history; the Profiler window has it)")]
        public RqlProfilerFrame frame => RqlProfilerFrame.Now();

        [RqlDescription("known profiler modules and their enabled flag (bookkeeping; the Profiler window controls visibility)")]
        public List<RqlProfilerModule> modules => AvailableModules.Select(n => new RqlProfilerModule(n, EnabledModules.Contains(n))).ToList();

        [RqlCallable("enable the runtime profiler and (by default) open the Profiler window; returns whether it is enabled")]
        public bool Start(bool openWindow = true)
        {
            UnityProfiler.enabled = true;
            if (openWindow && !EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler"))
                Debug.LogWarning("[RQL] could not open 'Window/Analysis/Profiler'; the runtime profiler is enabled anyway");
            return UnityProfiler.enabled;
        }

        [RqlCallable("disable the runtime profiler; returns whether it is still enabled (false)")]
        public bool Stop()
        {
            UnityProfiler.enabled = false;
            return UnityProfiler.enabled;
        }

        [RqlCallable("discard every frame the Editor profiler holds")]
        public bool ClearFrames()
        {
            ProfilerDriver.ClearAllFrames();
            return true;
        }

        [RqlCallable("mark a module (CPU, GPU, Rendering, Memory …) enabled or disabled in the bookkeeping; returns its new flag")]
        public bool EnableModule(string name, bool enabled = true)
        {
            var known = AvailableModules.FirstOrDefault(m => string.Equals(m, name, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"unknown profiler module '{name}'; known: {string.Join(", ", AvailableModules)}");
            if (enabled) EnabledModules.Add(known);
            else EnabledModules.Remove(known);
            return enabled;
        }

        [RqlCallable("save a JSON snapshot (status, memory, rendering, script, frame) to a file; returns the full path")]
        public string Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("a file path is required");
            var full = Path.GetFullPath(path);
            var snapshot = new
            {
                savedAt = DateTime.UtcNow.ToString("O"),
                enabled,
                supported,
                modules = modules.Where(m => m.enabled).Select(m => m.name).ToList(),
                memory,
                rendering,
                script,
                frame
            };
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(full, JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }));
            return full;
        }

        [RqlCallable("read back a snapshot written by Save (at most 10 MB) as text")]
        public string Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("a file path is required");
            var info = new FileInfo(path);
            if (!info.Exists) throw new ArgumentException($"no profiler snapshot at '{path}'");
            if (info.Length > MaxLoadFileSizeBytes)
                throw new ArgumentException($"'{path}' is {info.Length} bytes, more than the {MaxLoadFileSizeBytes}-byte limit");
            return File.ReadAllText(path);
        }

        /// <summary>Reset the module bookkeeping to its defaults (tests).</summary>
        internal static void ResetModules()
        {
            EnabledModules.Clear();
            foreach (var m in new[] { "CPU", "GPU", "Rendering", "Memory", "Audio", "Video", "Physics", "Physics2D", "UI" })
                EnabledModules.Add(m);
        }

        // Divide in double precision, then round to 0.01 MB: a long / 1048576f loses precision above ~16 MB, and the
        // extra digits cost the agent tokens without telling it anything.
        internal static double MB(long bytes) => Math.Round(bytes / 1048576.0, 2);
        internal static double Ms(float seconds) => Math.Round(seconds * 1000.0, 3);
    }

    [RqlDescription("a profiler module and its enabled flag")]
    public sealed class RqlProfilerModule
    {
        public RqlProfilerModule(string name, bool enabled) { this.name = name; this.enabled = enabled; }
        public string name { get; }
        public bool enabled { get; }
    }

    [RqlDescription("memory snapshot from UnityEngine.Profiling.Profiler, megabytes")]
    public sealed class RqlProfilerMemory
    {
        public double totalReservedMB { get; private set; }
        public double totalAllocatedMB { get; private set; }
        public double totalUnusedReservedMB { get; private set; }
        public double monoHeapSizeMB { get; private set; }
        public double monoUsedSizeMB { get; private set; }
        public double tempAllocatorSizeMB { get; private set; }
        [RqlDescription("memory the graphics driver reserved")]
        public double graphicsMB { get; private set; }
        [RqlDescription("highest used memory the profiler recorded")]
        public double maxUsedMB { get; private set; }
        public double usedHeapSizeMB { get; private set; }

        internal static RqlProfilerMemory Now() => new RqlProfilerMemory
        {
            totalReservedMB = RqlProfiler.MB(UnityProfiler.GetTotalReservedMemoryLong()),
            totalAllocatedMB = RqlProfiler.MB(UnityProfiler.GetTotalAllocatedMemoryLong()),
            totalUnusedReservedMB = RqlProfiler.MB(UnityProfiler.GetTotalUnusedReservedMemoryLong()),
            monoHeapSizeMB = RqlProfiler.MB(UnityProfiler.GetMonoHeapSizeLong()),
            monoUsedSizeMB = RqlProfiler.MB(UnityProfiler.GetMonoUsedSizeLong()),
            tempAllocatorSizeMB = RqlProfiler.MB(UnityProfiler.GetTempAllocatorSize()),
            graphicsMB = RqlProfiler.MB(UnityProfiler.GetAllocatedMemoryForGraphicsDriver()),
            maxUsedMB = RqlProfiler.MB(UnityProfiler.maxUsedMemory),
            usedHeapSizeMB = RqlProfiler.MB(UnityProfiler.usedHeapSizeLong),
        };
    }

    [RqlDescription("rendering snapshot from Time / QualitySettings / SystemInfo")]
    public sealed class RqlProfilerRendering
    {
        public double frameTimeMs { get; private set; }
        public double fps { get; private set; }
        public int vSyncCount { get; private set; }
        public int targetFrameRate { get; private set; }
        public string renderingThreadingMode { get; private set; } = "";
        public string graphicsDeviceType { get; private set; } = "";

        internal static RqlProfilerRendering Now() => new RqlProfilerRendering
        {
            frameTimeMs = RqlProfiler.Ms(Time.deltaTime),
            fps = Time.deltaTime > 0f ? Math.Round(1.0 / Time.deltaTime, 1) : 0,
            vSyncCount = QualitySettings.vSyncCount,
            targetFrameRate = Application.targetFrameRate,
            renderingThreadingMode = SystemInfo.renderingThreadingMode.ToString(),
            graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
        };
    }

    [RqlDescription("script timing snapshot from Time, plus Mono / GC memory")]
    public sealed class RqlProfilerScript
    {
        public double frameTimeMs { get; private set; }
        public double fixedDeltaTimeMs { get; private set; }
        public float timeScale { get; private set; }
        public int frameCount { get; private set; }
        public double realtimeSinceStartup { get; private set; }
        public double monoUsedMB { get; private set; }
        [RqlDescription("System.GC.GetTotalMemory(false)")]
        public double gcMemoryMB { get; private set; }

        internal static RqlProfilerScript Now() => new RqlProfilerScript
        {
            frameTimeMs = RqlProfiler.Ms(Time.deltaTime),
            fixedDeltaTimeMs = RqlProfiler.Ms(Time.fixedDeltaTime),
            timeScale = Time.timeScale,
            frameCount = Time.frameCount,
            realtimeSinceStartup = Math.Round(Time.realtimeSinceStartupAsDouble, 3),
            monoUsedMB = RqlProfiler.MB(UnityProfiler.GetMonoUsedSizeLong()),
            gcMemoryMB = RqlProfiler.MB(GC.GetTotalMemory(false)),
        };
    }

    [RqlDescription("the current frame from Time (no history)")]
    public sealed class RqlProfilerFrame
    {
        public double frameTimeMs { get; private set; }
        public double fps { get; private set; }
        public int frameCount { get; private set; }
        [RqlDescription("frames actually rendered (Time.renderedFrameCount)")]
        public int renderedFrameCount { get; private set; }
        public double realtimeSinceStartup { get; private set; }

        internal static RqlProfilerFrame Now() => new RqlProfilerFrame
        {
            frameTimeMs = RqlProfiler.Ms(Time.deltaTime),
            fps = Time.deltaTime > 0f ? Math.Round(1.0 / Time.deltaTime, 1) : 0,
            frameCount = Time.frameCount,
            renderedFrameCount = Time.renderedFrameCount,
            realtimeSinceStartup = Math.Round(Time.realtimeSinceStartupAsDouble, 3),
        };
    }
}
