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
using System.Collections;
using System.Text.Json;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class SceneTests : BaseTest
    {
        bool? _originalListOpenedEnabled;
        bool? _originalGetDataEnabled;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            _originalListOpenedEnabled = toolManager!.IsToolEnabled(Tool_Scene.SceneListOpenedToolId);
            _originalGetDataEnabled = toolManager.IsToolEnabled(Tool_Scene.SceneGetDataToolId);

            toolManager.SetToolEnabled(Tool_Scene.SceneListOpenedToolId, true);
            toolManager.SetToolEnabled(Tool_Scene.SceneGetDataToolId, true);
            UnityMcpPluginEditor.Instance.Save();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null)
            {
                if (_originalListOpenedEnabled.HasValue)
                    toolManager.SetToolEnabled(Tool_Scene.SceneListOpenedToolId, _originalListOpenedEnabled.Value);
                if (_originalGetDataEnabled.HasValue)
                    toolManager.SetToolEnabled(Tool_Scene.SceneGetDataToolId, _originalGetDataEnabled.Value);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        // ── scene-list-opened ─────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SceneListOpened_FromMainThread_ReturnsAtLeastOneScene()
        {
            yield return null;

            var json = RunTool(Tool_Scene.SceneListOpenedToolId, "{}").Value!.GetMessage()!;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.AreEqual(JsonValueKind.Array, root.ValueKind, "Result should be an array");
            Assert.Greater(root.GetArrayLength(), 0, "Should have at least one opened scene");
        }

        [UnityTest]
        public IEnumerator SceneListOpened_DirectMethod_ReturnsScenes()
        {
            yield return null;

            var tool = new Tool_Scene();
            var scenes = tool.ListOpened();
            Assert.IsNotNull(scenes, "ListOpened() should return non-null");
        }

        [UnityTest]
        public IEnumerator SceneListOpened_FromBackgroundThread_ReturnsScenes()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
            {
                var tool = new Tool_Scene();
                var scenes = tool.ListOpened();
                Assert.IsNotNull(scenes, "ListOpened() should return non-null from background thread");
            });
        }

        [UnityTest]
        public IEnumerator SceneListOpened_ViaRunTool_FromBackgroundThread_Succeeds()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Scene.SceneListOpenedToolId, "{}"));
        }

        // ── scene-get-data ────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SceneGetData_ActiveScene_FromMainThread_ReturnsData()
        {
            yield return null;

            var json = RunTool(Tool_Scene.SceneGetDataToolId, @"{
                ""includeRootGameObjects"": true,
                ""includeChildrenDepth"": 1
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.IsTrue(
                root.TryGetProperty("name", out _) || root.TryGetProperty("Name", out _),
                "Scene data should contain a name field");
        }

        [UnityTest]
        public IEnumerator SceneGetData_DirectMethod_ReturnsData()
        {
            yield return null;

            var tool = new Tool_Scene();
            var data = tool.GetData(includeRootGameObjects: false);
            Assert.IsNotNull(data, "GetData() should return non-null");
        }

        [UnityTest]
        public IEnumerator SceneGetData_FromBackgroundThread_ReturnsData()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
            {
                var tool = new Tool_Scene();
                var data = tool.GetData(includeRootGameObjects: false);
                Assert.IsNotNull(data, "GetData() should return non-null from background thread");
            });
        }

        [UnityTest]
        public IEnumerator SceneGetData_ViaRunTool_FromBackgroundThread_Succeeds()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Scene.SceneGetDataToolId, @"{
                    ""includeRootGameObjects"": false
                }"));
        }

        [UnityTest]
        public IEnumerator SceneGetData_ByName_ActiveScene_Succeeds()
        {
            yield return null;

            var activeSceneName = SceneManager.GetActiveScene().name;
            RunTool(Tool_Scene.SceneGetDataToolId, $@"{{
                ""openedSceneName"": ""{activeSceneName}"",
                ""includeRootGameObjects"": false
            }}");
        }

        [UnityTest]
        public IEnumerator SceneGetData_ByName_ActiveScene_FromBackgroundThread_Succeeds()
        {
            yield return null;

            var activeSceneName = SceneManager.GetActiveScene().name;
            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Scene.SceneGetDataToolId, $@"{{
                    ""openedSceneName"": ""{activeSceneName}"",
                    ""includeRootGameObjects"": false
                }}"));
        }

        // ── scene-list-opened + scene-get-data combined background tests ──

        [UnityTest]
        public IEnumerator SceneListAndGetData_Sequential_FromBackgroundThread_Succeeds()
        {
            yield return null;

            // List scenes, then get data for the active one — both from background thread
            yield return RunOnBackgroundThread(() =>
            {
                var listTool = new Tool_Scene();
                var scenes = listTool.ListOpened();
                Assert.IsNotNull(scenes, "ListOpened should return non-null");
                Assert.Greater(scenes.Length, 0, "Should have at least one scene");

                var dataTool = new Tool_Scene();
                var data = dataTool.GetData(includeRootGameObjects: false);
                Assert.IsNotNull(data, "GetData should return non-null");
            });
        }

        [UnityTest]
        public IEnumerator SceneListOpened_ReturnsSceneWithValidName()
        {
            yield return null;

            var tool = new Tool_Scene();
            var scenes = tool.ListOpened();
            Assert.IsNotNull(scenes);
            Assert.Greater(scenes.Length, 0);

            foreach (var scene in scenes)
                Assert.IsNotNull(scene, "Each scene data entry should be non-null");
        }
    }
}
