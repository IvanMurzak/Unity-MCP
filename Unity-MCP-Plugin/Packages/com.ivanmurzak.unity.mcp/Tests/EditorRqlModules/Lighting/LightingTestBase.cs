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
using com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters;
using NUnit.Framework;
using Rql;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Lighting
{
    /// <summary>
    /// A fresh untitled scene and engine per test (<see cref="AdapterTestBase"/>: its own RenderSettings, no Lighting Settings
    /// asset). Tests that need a saved scene call <see cref="SaveScene"/>; everything under <see cref="TestFolder"/> is deleted.
    /// </summary>
    public abstract class LightingTestBase : AdapterTestBase
    {
        protected const string TestFolder = "Assets/RqlLightingTests";

        // After the base SetUp's new scene: a run the Editor did not finish (a crash, a killed process) leaves its assets behind.
        [SetUp]
        public void DeleteLeftovers()
        {
            if (AssetDatabase.IsValidFolder(TestFolder)) AssetDatabase.DeleteAsset(TestFolder);
        }

        [TearDown]
        public void TearDownScene()
        {
            if (Lightmapping.isRunning) Lightmapping.Cancel();
            // Leave a saved test scene before deleting it and its baked lighting. Only then: a new scene costs seconds on CI,
            // and the next SetUp makes one anyway.
            if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (AssetDatabase.IsValidFolder(TestFolder)) AssetDatabase.DeleteAsset(TestFolder);
        }

        /// <summary>Save the active scene as "Assets/RqlLightingTests/&lt;name&gt;.unity" (baking needs a saved scene).</summary>
        protected static Scene SaveScene(string name = "LightingTest")
        {
            EnsureTestFolder();
            var scene = SceneManager.GetActiveScene();
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, $"{TestFolder}/{name}.unity"), "could not save the test scene");
            return scene;
        }

        protected static void EnsureTestFolder()
        {
            if (!AssetDatabase.IsValidFolder(TestFolder)) AssetDatabase.CreateFolder("Assets", "RqlLightingTests");
        }

        /// <summary>
        /// False where Unity runs the GPU lightmapper instead of ProgressiveCPU (e.g. Apple silicon in Unity 2022.3): the
        /// setting then reads back ProgressiveGPU.
        /// </summary>
        protected static bool CpuLightmapperSupported()
        {
#pragma warning disable CS0618 // ProgressiveCPU (deprecated in Unity 6.6) is the only lightmapper that bakes without a graphics device
            var settings = new LightingSettings { lightmapper = LightingSettings.Lightmapper.ProgressiveCPU };
            try { return settings.lightmapper == LightingSettings.Lightmapper.ProgressiveCPU; }
#pragma warning restore CS0618
            finally { Object.DestroyImmediate(settings); }
        }

        protected static bool HasGraphicsDevice => SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

        protected static Light CreateLight(string name, LightType type, LightmapBakeType bakeType = LightmapBakeType.Realtime)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = type;
            light.lightmapBakeType = bakeType;
            return light;
        }

        /// <summary>The query failed and its output mentions <paramref name="expected"/>.</summary>
        protected static void Fails(QueryResult r, string expected)
        {
            Assert.IsFalse(r.Ok, "expected the query to fail:\n" + Table(r));
            StringAssert.Contains(expected, Table(r));
        }

        /// <summary>
        /// What a <c>call</c> returned, as text: a call's receipt row starts with the item it was called on, and the method's
        /// return value is in its "result" column.
        /// </summary>
        protected static string CallResult(QueryResult r)
        {
            AssertOk(r);
            var column = r.Columns?.FindIndex(c => string.Equals(c, "result", System.StringComparison.OrdinalIgnoreCase)) ?? -1;
            Assert.GreaterOrEqual(column, 0, "no \"result\" column in the receipt:\n" + Table(r));
            return Cell(r, 0, column);
        }
    }
}
