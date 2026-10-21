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
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Lighting
{
    /// <summary>
    /// A fresh untitled scene per test (its own RenderSettings, no Lighting Settings asset). Tests that need a saved scene
    /// call <see cref="SaveScene"/>; everything written under <see cref="TestFolder"/> is deleted afterwards.
    /// </summary>
    public abstract class LightingTestBase
    {
        protected const string TestFolder = "Assets/RqlLightingTests";
        protected const string Lights = "objects | get components | where $ is Light";

        [SetUp]
        public void SetUpScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // A run the Editor did not finish (a crash, a killed process) leaves its assets behind.
            if (AssetDatabase.IsValidFolder(TestFolder)) AssetDatabase.DeleteAsset(TestFolder);
            RqlUnity.Reset();
        }

        [TearDown]
        public void TearDownScene()
        {
            if (Lightmapping.isRunning) Lightmapping.Cancel();
            // Leave the saved test scene before deleting it and its baked lighting.
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
            var settings = new LightingSettings { lightmapper = LightingSettings.Lightmapper.ProgressiveCPU };
            try { return settings.lightmapper == LightingSettings.Lightmapper.ProgressiveCPU; }
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

        protected static QueryResult Q(string rql) => RqlUnity.Engine.Execute(rql);

        protected static QueryResult M(string rql) => RqlUnity.Engine.Execute(rql, null, new QueryOptions { AllowMutation = true });

        protected static void Ok(QueryResult r) => Assert.IsTrue(r.Ok, Table(r));

        protected static void Fails(QueryResult r, string expected)
        {
            Assert.IsFalse(r.Ok, "expected the query to fail:\n" + Table(r));
            StringAssert.Contains(expected, Table(r));
        }

        protected static string Table(QueryResult r) => AgentFormat.Render(r, OutputFormat.Table);

        /// <summary>The first cell of a successful result as text (strings unquoted, "null" for null).</summary>
        protected static string Cell(QueryResult r)
        {
            Ok(r);
            Assert.IsNotEmpty(r.Rows!, Table(r));
            var node = r.Rows![0][0];
            return Text(node);
        }

        /// <summary>
        /// What a <c>call</c> returned, as text: a call's receipt row starts with the item it was called on, and the method's
        /// return value is in its "result" column.
        /// </summary>
        protected static string CallResult(QueryResult r)
        {
            Ok(r);
            Assert.IsNotEmpty(r.Rows!, Table(r));
            var column = r.Columns?.FindIndex(c => string.Equals(c, "result", System.StringComparison.OrdinalIgnoreCase)) ?? -1;
            Assert.GreaterOrEqual(column, 0, "no \"result\" column in the receipt:\n" + Table(r));
            return Text(r.Rows![0][column]);
        }

        private static string Text(System.Text.Json.Nodes.JsonNode? node) => node == null ? "null" : node.ToJsonString().Trim('"');
    }
}
