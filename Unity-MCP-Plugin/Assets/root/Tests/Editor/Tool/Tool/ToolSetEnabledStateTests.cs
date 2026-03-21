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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class ToolSetEnabledStateTests : BaseTest
    {
        Tool_Tool _tool = null!;
        string? _testToolName;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();
            _tool = new Tool_Tool();

            // Find a tool that is enabled by default to use in tests
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            _testToolName = toolManager!.GetAllTools()
                .FirstOrDefault(t => toolManager.IsToolEnabled(t.Name) && t.Name != Tool_Tool.ToolSetEnabledStateId)
                ?.Name;

            Assert.IsNotNull(_testToolName, "Should have at least one enabled tool for testing");
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            // Restore test tool to enabled state if it was changed
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null && _testToolName != null)
            {
                toolManager.SetToolEnabled(_testToolName, true);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        [UnityTest]
        public IEnumerator SetEnabled_SingleTool_ShouldDisable()
        {
            yield return null;

            var result = _tool.SetEnabledState(
                new[] { new Tool_Tool.InputData { Name = _testToolName!, Enabled = false } },
                includeLogs: true
            );

            Assert.IsTrue(result.Success[_testToolName!], $"Should successfully disable tool '{_testToolName}'");

            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            Assert.IsFalse(toolManager.IsToolEnabled(_testToolName!), $"Tool '{_testToolName}' should be disabled");
        }

        [UnityTest]
        public IEnumerator SetEnabled_SingleTool_ShouldEnable()
        {
            yield return null;

            // First disable it
            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            toolManager.SetToolEnabled(_testToolName!, false);

            // Then enable via our tool
            var result = _tool.SetEnabledState(
                new[] { new Tool_Tool.InputData { Name = _testToolName!, Enabled = true } },
                includeLogs: true
            );

            Assert.IsTrue(result.Success[_testToolName!], $"Should successfully enable tool '{_testToolName}'");
            Assert.IsTrue(toolManager.IsToolEnabled(_testToolName!), $"Tool '{_testToolName}' should be enabled");
        }

        [UnityTest]
        public IEnumerator SetEnabled_AlreadyInDesiredState_ShouldReturnTrue()
        {
            yield return null;

            // Tool is already enabled by default
            var result = _tool.SetEnabledState(
                new[] { new Tool_Tool.InputData { Name = _testToolName!, Enabled = true } },
                includeLogs: true
            );

            Assert.IsTrue(result.Success[_testToolName!], "Should return true when tool is already in desired state");
            Assert.IsNotNull(result.Logs, "Logs should be included");
            Assert.IsTrue(result.Logs!.Any(l => l.ToString().Contains("already")), "Should log that tool is already in desired state");
        }

        [UnityTest]
        public IEnumerator SetEnabled_NonExistentTool_ShouldReturnFalse()
        {
            yield return null;

            var fakeName = "non-existent-tool-xyz-12345";
            var result = _tool.SetEnabledState(
                new[] { new Tool_Tool.InputData { Name = fakeName, Enabled = true } },
                includeLogs: true
            );

            Assert.IsTrue(result.Success.ContainsKey(fakeName), "Should have entry for non-existent tool");
            Assert.IsFalse(result.Success[fakeName], "Should return false for non-existent tool");
        }

        [UnityTest]
        public IEnumerator SetEnabled_CaseInsensitive_ShouldResolve()
        {
            yield return null;

            // Send tool name in wrong case
            var wrongCase = _testToolName!.ToUpperInvariant();
            var result = _tool.SetEnabledState(
                new[] { new Tool_Tool.InputData { Name = wrongCase, Enabled = false } },
                includeLogs: true
            );

            // The resolved name (correct case) should be the key
            Assert.IsTrue(result.Success.ContainsKey(_testToolName!), $"Should resolve '{wrongCase}' to '{_testToolName}'");
            Assert.IsTrue(result.Success[_testToolName!], "Should successfully change state with case-insensitive match");
        }

        [UnityTest]
        public IEnumerator SetEnabled_MultipleTools_ShouldProcessAll()
        {
            yield return null;

            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            var allTools = toolManager.GetAllTools().ToList();
            var secondTool = allTools
                .FirstOrDefault(t => t.Name != _testToolName && t.Name != Tool_Tool.ToolSetEnabledStateId);

            Assert.IsNotNull(secondTool, "Should have at least two tools for testing");

            var originalSecondState = toolManager.IsToolEnabled(secondTool!.Name);

            try
            {
                var result = _tool.SetEnabledState(
                    new[]
                    {
                        new Tool_Tool.InputData { Name = _testToolName!, Enabled = false },
                        new Tool_Tool.InputData { Name = secondTool.Name, Enabled = false }
                    },
                    includeLogs: true
                );

                Assert.AreEqual(2, result.Success.Count, "Should have results for both tools");
                Assert.IsTrue(result.Success.ContainsKey(_testToolName!), "Should have result for first tool");
                Assert.IsTrue(result.Success.ContainsKey(secondTool.Name), "Should have result for second tool");
            }
            finally
            {
                // Restore second tool state
                toolManager.SetToolEnabled(secondTool.Name, originalSecondState);
                UnityMcpPluginEditor.Instance.Save();
            }
        }

        [UnityTest]
        public IEnumerator SetEnabled_WithLogs_ShouldIncludeLogs()
        {
            yield return null;

            var result = _tool.SetEnabledState(
                new[] { new Tool_Tool.InputData { Name = _testToolName!, Enabled = false } },
                includeLogs: true
            );

            Assert.IsNotNull(result.Logs, "Logs should not be null when includeLogs is true");
            Assert.IsTrue(result.Logs!.Count > 0, "Logs should contain entries");
        }

        [UnityTest]
        public IEnumerator SetEnabled_WithoutLogs_ShouldExcludeLogs()
        {
            yield return null;

            var result = _tool.SetEnabledState(
                new[] { new Tool_Tool.InputData { Name = _testToolName!, Enabled = false } },
                includeLogs: false
            );

            Assert.IsNull(result.Logs, "Logs should be null when includeLogs is false");
        }

        [UnityTest]
        public IEnumerator SetEnabled_EmptyArray_ShouldThrow()
        {
            yield return null;

            Assert.Throws<ArgumentException>(() =>
            {
                _tool.SetEnabledState(new Tool_Tool.InputData[0], includeLogs: false);
            });
        }

        [UnityTest]
        public IEnumerator SetEnabled_NullArray_ShouldThrow()
        {
            yield return null;

            Assert.Throws<ArgumentException>(() =>
            {
                _tool.SetEnabledState(null!, includeLogs: false);
            });
        }
    }
}
