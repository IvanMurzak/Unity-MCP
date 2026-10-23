/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Diagnostics
{
    // TEMPORARY CI timing experiment: what makes EditorSceneManager.NewScene cost seconds on the CI runners. Not for merge.
    public class CiTimingProbeTests
    {
        static readonly StringBuilder Sb = new StringBuilder();

        static double Ms(Action a)
        {
            var sw = Stopwatch.StartNew();
            a();
            return sw.Elapsed.TotalMilliseconds;
        }

        static void NewEmpty() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void Probe()
        {
            Sb.Clear();
            Sb.AppendLine($"gfx={SystemInfo.graphicsDeviceType} '{SystemInfo.graphicsDeviceName}' unity={Application.unityVersion}");
            Sb.AppendLine($"NewScene baseline: {Ms(NewEmpty):F0} / {Ms(NewEmpty):F0} ms");

            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            Sb.AppendLine($"windows ({windows.Length}): " + string.Join(", ", windows.Select(w => w.GetType().Name)));

            foreach (var w in windows.Where(w => w is SceneView || w.GetType().Name == "GameView")) w.Close();
            Sb.AppendLine($"NewScene without Scene/Game views: {Ms(NewEmpty):F0} / {Ms(NewEmpty):F0} ms");

            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>()) w.Close();
            Sb.AppendLine($"windows left: {Resources.FindObjectsOfTypeAll<EditorWindow>().Length}");
            Sb.AppendLine($"NewScene without any window: {Ms(NewEmpty):F0} / {Ms(NewEmpty):F0} ms");

            // Which profiler markers run inside one NewScene, by time.
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var recorders = new List<(string name, ProfilerRecorder rec)>();
            foreach (var h in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(h);
                if (d.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;
                recorders.Add((d.Name, ProfilerRecorder.StartNew(d.Category, d.Name, 1, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame)));
            }
            var before = recorders.Select(r => r.rec.CurrentValue).ToArray();
            var total = Ms(NewEmpty);
            var top = recorders.Select((r, i) => (r.name, ms: (r.rec.CurrentValue - before[i]) / 1e6))
                .Where(x => x.ms > 20 && x.ms <= total + 50).OrderByDescending(x => x.ms).Take(40).ToList();
            foreach (var r in recorders) r.rec.Dispose();
            Sb.AppendLine($"profiled NewScene: {total:F0} ms over {recorders.Count} markers; top:");
            foreach (var (name, ms) in top) Sb.AppendLine($"  {ms,8:F0} ms  {name}");

            var text = Sb.ToString();
            Debug.Log("[CiTimingProbe]\n" + text);
            Assert.Pass(text);
        }
    }
}
