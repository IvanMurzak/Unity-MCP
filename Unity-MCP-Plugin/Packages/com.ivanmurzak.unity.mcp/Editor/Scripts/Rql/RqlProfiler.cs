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
using UnityEngine.Profiling;
using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>
    /// The <c>profiler</c> source: Unity's built-in profiler state and snapshots (memory, rendering, script timing, the
    /// current frame), read with rql-query and driven with rql-mutate calls, e.g.
    /// <c>profiler | select memory.totalAllocatedMB, frame.fps</c>, <c>profiler | call Start()</c>.
    /// Every member is read live; values are megabytes / milliseconds as named. Built-in Unity APIs only.
    /// </summary>
    public sealed class RqlProfiler
    {
        public static readonly RqlProfiler Instance = new RqlProfiler();

        /// <summary>Largest snapshot file <see cref="Load"/> reads back.</summary>
        public const long MaxLoadFileSizeBytes = 10L * 1024L * 1024L;

        static readonly JsonSerializerOptions Indented = new JsonSerializerOptions { WriteIndented = true };

        [RqlDescription("Unity's runtime profiler is recording (Profiler.enabled); assign to switch it, or call Start() / Stop()")]
        public bool enabled { get => UnityProfiler.enabled; set => UnityProfiler.enabled = value; }

        [RqlDescription("profiling is supported on this platform (Profiler.supported)")]
        public bool supported => UnityProfiler.supported;

        [RqlDescription("memory, megabytes")]
        public RqlProfilerMemory memory => RqlProfilerMemory.Instance;

        [RqlDescription("the current frame: frame time, fps, frame counts (no history; the Profiler window has it)")]
        public RqlProfilerFrame frame => RqlProfilerFrame.Instance;

        [RqlDescription("rendering settings: vsync, target frame rate, threading mode, graphics device")]
        public RqlProfilerRendering rendering => RqlProfilerRendering.Instance;

        [RqlDescription("script timing: fixed delta time, time scale, GC memory")]
        public RqlProfilerScript script => RqlProfilerScript.Instance;

        [RqlDescription("profiler areas (Profiler.GetAreaEnabled): CPU, GPU, Rendering, Memory …")]
        public List<RqlProfilerModule> modules => Areas().Select(a => new RqlProfilerModule(a.ToString(), UnityProfiler.GetAreaEnabled(a))).ToList();

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

        [RqlCallable("enable or disable a profiler area (CPU, GPU, Rendering, Memory …, Profiler.SetAreaEnabled); returns its new state")]
        public bool EnableModule(string name, bool enabled = true)
        {
            if (!Enum.TryParse<ProfilerArea>(name, ignoreCase: true, out var area) || !Enum.IsDefined(typeof(ProfilerArea), area))
                throw new ArgumentException($"unknown profiler module '{name}'; known: {string.Join(", ", Areas())}");
            UnityProfiler.SetAreaEnabled(area, enabled);
            return UnityProfiler.GetAreaEnabled(area);
        }

        [RqlCallable("save a JSON snapshot (status, modules, memory, frame, rendering, script) to a file; returns the full path")]
        public string Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("a file path is required");
            var full = Path.GetFullPath(path);
            var snapshot = new
            {
                savedAt = DateTime.UtcNow.ToString("O"),
                enabled,
                supported,
                modules = Areas().Where(UnityProfiler.GetAreaEnabled).Select(a => a.ToString()).ToList(),
                memory,
                frame,
                rendering,
                script
            };
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(full, JsonSerializer.Serialize(snapshot, Indented));
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

        internal static IEnumerable<ProfilerArea> Areas() => ((ProfilerArea[])Enum.GetValues(typeof(ProfilerArea))).Distinct();

        // Divide in double precision, then round: a long / 1048576f loses precision above ~16 MB, and the extra digits
        // cost the agent tokens without telling it anything.
        internal static double MB(long bytes) => Math.Round(bytes / 1048576.0, 2);
        internal static double Ms(float seconds) => Math.Round(seconds * 1000.0, 3);
        internal static double Fps(float deltaTime) => deltaTime > 0f ? Math.Round(1.0 / deltaTime, 1) : 0;
        internal static double Seconds(double seconds) => Math.Round(seconds, 3);
    }

    [RqlDescription("a profiler area and whether it is enabled")]
    public sealed class RqlProfilerModule
    {
        public RqlProfilerModule(string name, bool enabled) { this.name = name; this.enabled = enabled; }
        public string name { get; }
        public bool enabled { get; }
    }

    // The snapshot members below are read live, one Unity call each: a query pays only for the members it selects.

    [RqlDescription("memory from UnityEngine.Profiling.Profiler, megabytes")]
    public sealed class RqlProfilerMemory
    {
        internal static readonly RqlProfilerMemory Instance = new RqlProfilerMemory();
        public double totalReservedMB => RqlProfiler.MB(UnityProfiler.GetTotalReservedMemoryLong());
        public double totalAllocatedMB => RqlProfiler.MB(UnityProfiler.GetTotalAllocatedMemoryLong());
        public double totalUnusedReservedMB => RqlProfiler.MB(UnityProfiler.GetTotalUnusedReservedMemoryLong());
        public double monoHeapSizeMB => RqlProfiler.MB(UnityProfiler.GetMonoHeapSizeLong());
        public double monoUsedSizeMB => RqlProfiler.MB(UnityProfiler.GetMonoUsedSizeLong());
        public double tempAllocatorSizeMB => RqlProfiler.MB(UnityProfiler.GetTempAllocatorSize());
        [RqlDescription("memory the graphics driver reserved")]
        public double graphicsMB => RqlProfiler.MB(UnityProfiler.GetAllocatedMemoryForGraphicsDriver());
        [RqlDescription("size limit of the profiler's own frame buffer (Profiler.maxUsedMemory)")]
        public double maxUsedMB => RqlProfiler.MB(UnityProfiler.maxUsedMemory);
        public double usedHeapSizeMB => RqlProfiler.MB(UnityProfiler.usedHeapSizeLong);
    }

    [RqlDescription("the current frame from Time (no history)")]
    public sealed class RqlProfilerFrame
    {
        internal static readonly RqlProfilerFrame Instance = new RqlProfilerFrame();
        [RqlDescription("Time.deltaTime")]
        public double frameTimeMs => RqlProfiler.Ms(Time.deltaTime);
        public double fps => RqlProfiler.Fps(Time.deltaTime);
        public int frameCount => Time.frameCount;
        [RqlDescription("frames actually rendered (Time.renderedFrameCount)")]
        public int renderedFrameCount => Time.renderedFrameCount;
        [RqlDescription("seconds")]
        public double realtimeSinceStartup => RqlProfiler.Seconds(Time.realtimeSinceStartupAsDouble);
    }

    [RqlDescription("rendering settings from QualitySettings / Application / SystemInfo")]
    public sealed class RqlProfilerRendering
    {
        internal static readonly RqlProfilerRendering Instance = new RqlProfilerRendering();
        public int vSyncCount => QualitySettings.vSyncCount;
        public int targetFrameRate => Application.targetFrameRate;
        public string renderingThreadingMode => SystemInfo.renderingThreadingMode.ToString();
        public string graphicsDeviceType => SystemInfo.graphicsDeviceType.ToString();
    }

    [RqlDescription("script timing from Time, plus managed memory")]
    public sealed class RqlProfilerScript
    {
        internal static readonly RqlProfilerScript Instance = new RqlProfilerScript();
        public double fixedDeltaTimeMs => RqlProfiler.Ms(Time.fixedDeltaTime);
        public float timeScale => Time.timeScale;
        [RqlDescription("System.GC.GetTotalMemory(false)")]
        public double gcMemoryMB => RqlProfiler.MB(GC.GetTotalMemory(false));
    }
}
