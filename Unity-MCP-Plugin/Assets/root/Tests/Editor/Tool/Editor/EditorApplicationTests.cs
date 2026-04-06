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
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class EditorApplicationTests : BaseTest
    {
        bool? _originalEnabled;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            _originalEnabled = toolManager!.IsToolEnabled(Tool_Editor.EditorApplicationGetStateToolId);
            toolManager.SetToolEnabled(Tool_Editor.EditorApplicationGetStateToolId, true);
            UnityMcpPluginEditor.Instance.Save();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null && _originalEnabled.HasValue)
            {
                toolManager.SetToolEnabled(Tool_Editor.EditorApplicationGetStateToolId, _originalEnabled.Value);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        [UnityTest]
        public IEnumerator GetApplicationState_FromMainThread_ReturnsValidState()
        {
            yield return null;

            var result = RunTool(Tool_Editor.EditorApplicationGetStateToolId, "{}");
            var json = result.Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.IsTrue(
                root.TryGetProperty("isPlaying", out _) ||
                root.TryGetProperty("IsPlaying", out _),
                "Response should contain isPlaying field");
        }

        [UnityTest]
        public IEnumerator GetApplicationState_DirectMethod_ReturnsNonNull()
        {
            yield return null;

            var tool = new Tool_Editor();
            var state = tool.GetApplicationState();

            Assert.IsNotNull(state, "GetApplicationState should return non-null state");
        }

        [UnityTest]
        public IEnumerator GetApplicationState_FromBackgroundThread_ReturnsValidState()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
            {
                var tool = new Tool_Editor();
                var state = tool.GetApplicationState();
                Assert.IsNotNull(state, "GetApplicationState should return non-null state from background thread");
            });
        }

        [UnityTest]
        public IEnumerator GetApplicationState_ViaRunTool_FromBackgroundThread_Succeeds()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Editor.EditorApplicationGetStateToolId, "{}"));
        }

        [UnityTest]
        public IEnumerator GetApplicationState_IsNotInPlayMode_WhenTestRuns()
        {
            yield return null;

            var tool = new Tool_Editor();
            var state = tool.GetApplicationState();

            Assert.IsNotNull(state);
            Assert.IsFalse(state!.IsPlaying, "Tests should not be running in play mode");
        }
    }
}
