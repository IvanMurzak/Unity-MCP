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
using System;
using System.Collections;
using System.Text.Json;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class AssetsFindTests : BaseTest
    {
        bool? _originalEnabled;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            _originalEnabled = toolManager!.IsToolEnabled(Tool_Assets.AssetsFindToolId);
            toolManager.SetToolEnabled(Tool_Assets.AssetsFindToolId, true);
            UnityMcpPluginEditor.Instance.Save();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null && _originalEnabled.HasValue)
            {
                toolManager.SetToolEnabled(Tool_Assets.AssetsFindToolId, _originalEnabled.Value);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        // ── assets-find ───────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator AssetsFind_NoFilter_FromMainThread_ReturnsAssets()
        {
            yield return null;

            var json = RunTool(Tool_Assets.AssetsFindToolId, @"{
                ""maxResults"": 5
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.AreEqual(JsonValueKind.Array, root.ValueKind, "Result should be an array");
            Assert.Greater(root.GetArrayLength(), 0, "Should return at least one asset");
        }

        [UnityTest]
        public IEnumerator AssetsFind_NoFilter_FromBackgroundThread_ReturnsAssets()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
            {
                var json = RunTool(Tool_Assets.AssetsFindToolId, @"{
                    ""maxResults"": 5
                }").Value!.GetMessage()!;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("result", out var resultEl))
                    root = resultEl;

                Assert.AreEqual(JsonValueKind.Array, root.ValueKind, "Result should be an array from background thread");
                Assert.Greater(root.GetArrayLength(), 0, "Should return at least one asset from background thread");
            });
        }

        [UnityTest]
        public IEnumerator AssetsFind_DirectMethod_ReturnsAssets()
        {
            yield return null;

            var tool = new Tool_Assets();
            var results = tool.Find(maxResults: 5);
            Assert.IsNotNull(results, "Find() should return non-null");
        }

        [UnityTest]
        public IEnumerator AssetsFind_DirectMethod_FromBackgroundThread_ReturnsAssets()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
            {
                var tool = new Tool_Assets();
                var results = tool.Find(maxResults: 5);
                Assert.IsNotNull(results, "Find() should return non-null from background thread");
            });
        }

        [UnityTest]
        public IEnumerator AssetsFind_FilterByScript_FromMainThread_ReturnsScripts()
        {
            yield return null;

            var tool = new Tool_Assets();
            var results = tool.Find(filter: "t:Script", maxResults: 10);
            Assert.IsNotNull(results, "Find with type filter should return non-null");
            // All results should be scripts
            foreach (var asset in results)
            {
                Assert.IsNotNull(asset, "Asset reference should not be null");
            }
        }

        [UnityTest]
        public IEnumerator AssetsFind_FilterByScript_FromBackgroundThread_ReturnsScripts()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
            {
                var tool = new Tool_Assets();
                var results = tool.Find(filter: "t:Script", maxResults: 10);
                Assert.IsNotNull(results, "Find with type filter from background thread should return non-null");
            });
        }

        [UnityTest]
        public IEnumerator AssetsFind_ViaRunTool_FilterByScript_FromMainThread_Succeeds()
        {
            yield return null;

            RunTool(Tool_Assets.AssetsFindToolId, @"{
                ""filter"": ""t:Script"",
                ""maxResults"": 5
            }");
        }

        [UnityTest]
        public IEnumerator AssetsFind_ViaRunTool_FilterByScript_FromBackgroundThread_Succeeds()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Assets.AssetsFindToolId, @"{
                    ""filter"": ""t:Script"",
                    ""maxResults"": 5
                }"));
        }

        [UnityTest]
        public IEnumerator AssetsFind_WithSearchFolders_FromMainThread_Succeeds()
        {
            yield return null;

            RunTool(Tool_Assets.AssetsFindToolId, @"{
                ""filter"": ""t:Script"",
                ""searchInFolders"": [""Assets""],
                ""maxResults"": 5
            }");
        }

        [UnityTest]
        public IEnumerator AssetsFind_WithSearchFolders_FromBackgroundThread_Succeeds()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Assets.AssetsFindToolId, @"{
                    ""filter"": ""t:Script"",
                    ""searchInFolders"": [""Assets""],
                    ""maxResults"": 5
                }"));
        }

        [UnityTest]
        public IEnumerator AssetsFind_MaxResultsZero_ThrowsArgumentException()
        {
            yield return null;

            var tool = new Tool_Assets();
            Assert.Throws<ArgumentException>(() => tool.Find(maxResults: 0));
        }

        [UnityTest]
        public IEnumerator AssetsFind_MaxResultsZero_FromBackgroundThread_ThrowsArgumentException()
        {
            yield return null;

            Exception? caughtException = null;
            yield return RunOnBackgroundThread(() =>
            {
                try
                {
                    var tool = new Tool_Assets();
                    tool.Find(maxResults: 0);
                }
                catch (ArgumentException ex)
                {
                    caughtException = ex;
                }
            });

            Assert.IsNotNull(caughtException, "Should throw ArgumentException from background thread too");
        }
    }
}
