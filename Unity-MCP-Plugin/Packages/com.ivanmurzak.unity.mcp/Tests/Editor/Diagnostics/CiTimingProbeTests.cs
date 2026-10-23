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
using System.Diagnostics;
using System.Text;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Diagnostics
{
    // TEMPORARY CI timing experiment: where a typical 3.6 s RQL adapter test spends its time on CI. Not for merge.
    public class CiTimingProbeTests
    {
        [Test]
        public void Probe()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"gfx={SystemInfo.graphicsDeviceType} '{SystemInfo.graphicsDeviceName}' batch={Application.isBatchMode}");
            void T(string name, Action a)
            {
                var sw = Stopwatch.StartNew();
                a();
                sb.AppendLine($"{name}: {sw.Elapsed.TotalMilliseconds:F0} ms");
            }

            for (var i = 0; i < 3; i++)
                T($"NewScene Empty Single #{i}", () => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single));
            T("NewScene Default Single", () => EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single));
            T("NewScene Empty Single after Default", () => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single));

            // The same with the environment lighting switched off on the active scene first.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            T("NewScene Empty Single after flat env", () => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single));

            T("UnloadUnusedAssetsImmediate", () => EditorUtility.UnloadUnusedAssetsImmediate());
            T("GC.Collect", () => GC.Collect());
            T("RqlUnity.Reset + Engine", () => { RqlUnity.Reset(); _ = RqlUnity.Engine; });
            var go = new GameObject("Toggle");
            T("query", () => RqlUnity.Engine.Execute("objects | where name == \"Toggle\" | select active"));
            T("mutation", () => RqlUnity.Engine.Execute("objects | where name == \"Toggle\" | set active = false", null, new QueryOptions { AllowMutation = true }));
            T("Undo.PerformUndo", () => Undo.PerformUndo());
            T("new GameObject x100", () => { for (var i = 0; i < 100; i++) new GameObject("g" + i); });
            T("Object.Destroy roots", () =>
            {
                foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                    UnityEngine.Object.DestroyImmediate(root);
            });

            var text = sb.ToString();
            Debug.Log("[CiTimingProbe]\n" + text);
            Assert.Pass(text);
        }

        // Mirrors AdapterTestBase: is the per-test cost in SetUp (NewScene + Reset) or somewhere around the test?
        public class SetUpOnly : Rql.Adapters.AdapterTestBase
        {
            [Test] public void Empty1() { }
            [Test] public void Empty2() { }
            [Test] public void Empty3() { }
        }

        public class NoSetUp
        {
            [Test] public void Empty1() { }
            [Test] public void Empty2() { }
            [Test] public void Empty3() { }
        }
    }
}
