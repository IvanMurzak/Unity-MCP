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
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class EditorSelectionTests : BaseTest
    {
        bool? _originalGetEnabled;
        bool? _originalSetEnabled;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            _originalGetEnabled = toolManager!.IsToolEnabled(Tool_Editor_Selection.EditorSelectionGetToolId);
            _originalSetEnabled = toolManager.IsToolEnabled(Tool_Editor_Selection.EditorSelectionSetToolId);

            toolManager.SetToolEnabled(Tool_Editor_Selection.EditorSelectionGetToolId, true);
            toolManager.SetToolEnabled(Tool_Editor_Selection.EditorSelectionSetToolId, true);
            UnityMcpPluginEditor.Instance.Save();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null)
            {
                if (_originalGetEnabled.HasValue)
                    toolManager.SetToolEnabled(Tool_Editor_Selection.EditorSelectionGetToolId, _originalGetEnabled.Value);
                if (_originalSetEnabled.HasValue)
                    toolManager.SetToolEnabled(Tool_Editor_Selection.EditorSelectionSetToolId, _originalSetEnabled.Value);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        // ── editor-selection-get ──────────────────────────────────────────

        [UnityTest]
        public IEnumerator SelectionGet_FromMainThread_ReturnsData()
        {
            yield return null;
            RunTool(Tool_Editor_Selection.EditorSelectionGetToolId, "{}");
        }

        [UnityTest]
        public IEnumerator SelectionGet_DirectMethod_ReturnsData()
        {
            yield return null;

            var tool = new Tool_Editor_Selection();
            var result = tool.Get();
            Assert.IsNotNull(result, "Get() should return a non-null SelectionData");
        }

        [UnityTest]
        public IEnumerator SelectionGet_FromBackgroundThread_ReturnsData()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
            {
                var tool = new Tool_Editor_Selection();
                var result = tool.Get();
                Assert.IsNotNull(result, "Get() should return non-null from background thread");
            });
        }

        [UnityTest]
        public IEnumerator SelectionGet_ViaRunTool_FromBackgroundThread_Succeeds()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Editor_Selection.EditorSelectionGetToolId, "{}"));
        }

        [UnityTest]
        public IEnumerator SelectionGet_WithIncludeOptions_FromMainThread_Succeeds()
        {
            yield return null;

            RunTool(Tool_Editor_Selection.EditorSelectionGetToolId, @"{
                ""includeGameObjects"": true,
                ""includeTransforms"": true,
                ""includeInstanceIDs"": true,
                ""includeAssetGUIDs"": true,
                ""includeActiveObject"": true,
                ""includeActiveTransform"": true
            }");
        }

        [UnityTest]
        public IEnumerator SelectionGet_WithIncludeOptions_FromBackgroundThread_Succeeds()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Editor_Selection.EditorSelectionGetToolId, @"{
                    ""includeGameObjects"": true,
                    ""includeInstanceIDs"": true,
                    ""includeActiveObject"": true
                }"));
        }

        // ── editor-selection-set ──────────────────────────────────────────

        [UnityTest]
        public IEnumerator SelectionSet_WithGameObject_FromMainThread_Succeeds()
        {
            yield return null;

            var go = new GameObject("SelectionTestGO");

            RunTool(Tool_Editor_Selection.EditorSelectionSetToolId, $@"{{
                ""select"": [{{
                    ""instanceID"": {go.GetInstanceID()}
                }}]
            }}");
        }

        [UnityTest]
        public IEnumerator SelectionSet_WithGameObject_FromBackgroundThread_Succeeds()
        {
            yield return null;

            var go = new GameObject("SelectionTestGO_BG");

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Editor_Selection.EditorSelectionSetToolId, $@"{{
                    ""select"": [{{
                        ""instanceID"": {go.GetInstanceID()}
                    }}]
                }}"));
        }
    }
}
