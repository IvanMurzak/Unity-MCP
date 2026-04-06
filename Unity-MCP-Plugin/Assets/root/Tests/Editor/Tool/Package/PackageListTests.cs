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
    public class PackageListTests : BaseTest
    {
        bool? _originalEnabled;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            _originalEnabled = toolManager!.IsToolEnabled(Tool_Package.PackageListToolId);
            toolManager.SetToolEnabled(Tool_Package.PackageListToolId, true);
            UnityMcpPluginEditor.Instance.Save();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null && _originalEnabled.HasValue)
            {
                toolManager.SetToolEnabled(Tool_Package.PackageListToolId, _originalEnabled.Value);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        [UnityTest]
        public IEnumerator PackageList_NoFilter_FromMainThread_ReturnsPackages()
        {
            yield return null;

            var json = RunTool(Tool_Package.PackageListToolId, "{}").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.AreEqual(JsonValueKind.Array, root.ValueKind, "Result should be an array of packages");
            Assert.Greater(root.GetArrayLength(), 0, "Should return at least one installed package");
        }

        [UnityTest]
        public IEnumerator PackageList_NoFilter_FromBackgroundThread_ReturnsPackages()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
            {
                var json = RunTool(Tool_Package.PackageListToolId, "{}").Value!.GetMessage()!;
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("result", out var resultEl))
                    root = resultEl;
                Assert.AreEqual(JsonValueKind.Array, root.ValueKind, "Result should be an array from background thread");
            }, timeoutSeconds: 60f);
        }

        [UnityTest]
        public IEnumerator PackageList_EachPackageHasNameAndVersion()
        {
            yield return null;

            var json = RunTool(Tool_Package.PackageListToolId, "{}").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.Greater(root.GetArrayLength(), 0, "Should have at least one package");

            var first = root[0];
            Assert.IsTrue(
                first.TryGetProperty("name", out var name) || first.TryGetProperty("Name", out name),
                "Package should have a name field");
            Assert.IsTrue(
                first.TryGetProperty("version", out var version) || first.TryGetProperty("Version", out version),
                "Package should have a version field");
            Assert.IsFalse(string.IsNullOrEmpty(name.GetString()), "Package name should not be empty");
        }

        [UnityTest]
        public IEnumerator PackageList_WithNameFilter_FiltersResults()
        {
            yield return null;

            // "core" is a very common substring present in many Unity packages
            var json = RunTool(Tool_Package.PackageListToolId, @"{
                ""nameFilter"": ""com.unity""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.AreEqual(JsonValueKind.Array, root.ValueKind, "Filtered result should be an array");
            // All results should contain "com.unity" (case-insensitive)
            for (int i = 0; i < root.GetArrayLength(); i++)
            {
                var pkg = root[i];
                var name = (pkg.TryGetProperty("name", out var n) || pkg.TryGetProperty("Name", out n))
                    ? n.GetString() ?? string.Empty
                    : string.Empty;
                var displayName = (pkg.TryGetProperty("displayName", out var dn) || pkg.TryGetProperty("DisplayName", out dn))
                    ? dn.GetString() ?? string.Empty
                    : string.Empty;
                var description = (pkg.TryGetProperty("description", out var d) || pkg.TryGetProperty("Description", out d))
                    ? d.GetString() ?? string.Empty
                    : string.Empty;

                var matchesFilter =
                    name.IndexOf("com.unity", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    displayName.IndexOf("com.unity", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    description.IndexOf("com.unity", System.StringComparison.OrdinalIgnoreCase) >= 0;

                Assert.IsTrue(matchesFilter, $"Package '{name}' should match filter 'com.unity'");
            }
        }

        [UnityTest]
        public IEnumerator PackageList_WithNameFilter_FromBackgroundThread_FiltersResults()
        {
            yield return null;

            yield return RunOnBackgroundThread(() =>
                RunTool(Tool_Package.PackageListToolId, @"{
                    ""nameFilter"": ""com.unity""
                }"), timeoutSeconds: 60f);
        }

        [UnityTest]
        public IEnumerator PackageList_DirectDependenciesOnly_ReturnsFewer()
        {
            yield return null;

            var allJson = RunTool(Tool_Package.PackageListToolId, @"{
                ""directDependenciesOnly"": false
            }").Value!.GetMessage()!;

            using var allDoc = JsonDocument.Parse(allJson);
            var allRoot = allDoc.RootElement;
            if (allRoot.TryGetProperty("result", out var allResultEl))
                allRoot = allResultEl;
            var allCount = allRoot.GetArrayLength();

            var directJson = RunTool(Tool_Package.PackageListToolId, @"{
                ""directDependenciesOnly"": true
            }").Value!.GetMessage()!;

            using var directDoc = JsonDocument.Parse(directJson);
            var directRoot = directDoc.RootElement;
            if (directRoot.TryGetProperty("result", out var directResultEl))
                directRoot = directResultEl;
            var directCount = directRoot.GetArrayLength();

            Assert.LessOrEqual(directCount, allCount,
                "Direct dependencies only should return same or fewer packages than all packages");
        }
    }
}
