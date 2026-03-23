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
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class ToolSetEnabledStateTests : BaseTest
    {
        string? _testToolName;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            toolManager!.SetToolEnabled(Tool_Tool.ToolSetEnabledStateId, true);
            UnityMcpPluginEditor.Instance.Save();

            _testToolName = toolManager.GetAllTools()
                .FirstOrDefault(t => toolManager.IsToolEnabled(t.Name) && t.Name != Tool_Tool.ToolSetEnabledStateId)
                ?.Name;

            Assert.IsNotNull(_testToolName, "Should have at least one enabled tool for testing");
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null)
            {
                if (_testToolName != null)
                    toolManager.SetToolEnabled(_testToolName, true);

                toolManager.SetToolEnabled(Tool_Tool.ToolSetEnabledStateId, false);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        static bool GetSuccessValue(string json, string toolName)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.IsTrue(
                root.TryGetProperty("Success", out var success) || root.TryGetProperty("success", out success),
                "No 'Success' property found in JSON");

            Assert.IsTrue(success.TryGetProperty(toolName, out var value), $"No entry for tool '{toolName}' in Success");
            return value.GetBoolean();
        }

        static int GetSuccessCount(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            if (!root.TryGetProperty("Success", out var success) && !root.TryGetProperty("success", out success))
                return 0;

            var count = 0;
            foreach (var _ in success.EnumerateObject())
                count++;
            return count;
        }

        [UnityTest]
        public IEnumerator SetEnabled_SingleTool_ShouldDisable()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": false }}],
                ""includeLogs"": false
            }}").Value!.GetMessage()!;

            Assert.IsTrue(GetSuccessValue(json, _testToolName!), $"Should successfully disable tool '{_testToolName}'");

            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            Assert.IsFalse(toolManager.IsToolEnabled(_testToolName!), $"Tool '{_testToolName}' should be disabled");
        }

        [UnityTest]
        public IEnumerator SetEnabled_SingleTool_ShouldEnable()
        {
            yield return null;

            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            toolManager.SetToolEnabled(_testToolName!, false);
            UnityMcpPluginEditor.Instance.Save();

            var json = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": true }}],
                ""includeLogs"": false
            }}").Value!.GetMessage()!;

            Assert.IsTrue(GetSuccessValue(json, _testToolName!), $"Should successfully enable tool '{_testToolName}'");
            Assert.IsTrue(toolManager.IsToolEnabled(_testToolName!), $"Tool '{_testToolName}' should be enabled");
        }

        [UnityTest]
        public IEnumerator SetEnabled_AlreadyInDesiredState_ShouldReturnTrue()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": true }}],
                ""includeLogs"": false
            }}").Value!.GetMessage()!;

            Assert.IsTrue(GetSuccessValue(json, _testToolName!), "Should return true when tool is already in desired state");
        }

        [UnityTest]
        public IEnumerator SetEnabled_NonExistentTool_ShouldReturnFalse()
        {
            yield return null;

            var fakeName = "non-existent-tool-xyz-12345";
            var json = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{fakeName}"", ""enabled"": true }}],
                ""includeLogs"": false
            }}").Value!.GetMessage()!;

            Assert.IsFalse(GetSuccessValue(json, fakeName), "Should return false for non-existent tool");
        }

        [UnityTest]
        public IEnumerator SetEnabled_CaseInsensitive_ShouldResolve()
        {
            yield return null;

            var wrongCase = _testToolName!.ToUpperInvariant();
            var json = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{wrongCase}"", ""enabled"": false }}],
                ""includeLogs"": false
            }}").Value!.GetMessage()!;

            Assert.IsTrue(GetSuccessValue(json, _testToolName!), $"Should resolve '{wrongCase}' to '{_testToolName}'");
        }

        [UnityTest]
        public IEnumerator SetEnabled_MultipleTools_ShouldProcessAll()
        {
            yield return null;

            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            var secondTool = toolManager.GetAllTools()
                .FirstOrDefault(t => t.Name != _testToolName && t.Name != Tool_Tool.ToolSetEnabledStateId);

            Assert.IsNotNull(secondTool, "Should have at least two tools for testing");

            var originalSecondState = toolManager.IsToolEnabled(secondTool!.Name);

            try
            {
                var json = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                    ""tools"": [
                        {{ ""name"": ""{_testToolName}"", ""enabled"": false }},
                        {{ ""name"": ""{secondTool.Name}"", ""enabled"": false }}
                    ],
                    ""includeLogs"": false
                }}").Value!.GetMessage()!;

                Assert.AreEqual(2, GetSuccessCount(json), "Should have results for both tools");
                Assert.IsTrue(GetSuccessValue(json, _testToolName!), "Should have result for first tool");
                Assert.IsTrue(GetSuccessValue(json, secondTool.Name), "Should have result for second tool");
            }
            finally
            {
                toolManager.SetToolEnabled(secondTool.Name, originalSecondState);
                UnityMcpPluginEditor.Instance.Save();
            }
        }

        [UnityTest]
        public IEnumerator SetEnabled_WithLogs_ShouldIncludeLogs()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": false }}],
                ""includeLogs"": true
            }}").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.IsTrue(
                root.TryGetProperty("Logs", out var logs) || root.TryGetProperty("logs", out logs),
                "Should have Logs property");
            Assert.AreNotEqual(JsonValueKind.Null, logs.ValueKind, "Logs should not be null when includeLogs is true");
        }

        [UnityTest]
        public IEnumerator SetEnabled_WithoutLogs_ShouldExcludeLogs()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": false }}],
                ""includeLogs"": false
            }}").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            var hasLogs = root.TryGetProperty("Logs", out var logs) || root.TryGetProperty("logs", out logs);
            Assert.IsTrue(!hasLogs || logs.ValueKind == JsonValueKind.Null,
                "Logs should be null or absent when includeLogs is false");
        }

        [UnityTest]
        public IEnumerator SetEnabled_EmptyArray_ShouldReturnError()
        {
            yield return null;

            LogAssert.Expect(LogType.Exception, new Regex("ArgumentException"));
            LogAssert.Expect(LogType.Error, new Regex("Tool execution failed"));
            LogAssert.Expect(LogType.Error, new Regex("Error Response to AI"));

            var jsonResult = RunToolRaw(Tool_Tool.ToolSetEnabledStateId, @"{
                ""tools"": [],
                ""includeLogs"": false
            }");

            StringAssert.Contains("Tools array is null or empty", jsonResult);
        }

        [UnityTest]
        public IEnumerator SetEnabled_NullArray_ShouldReturnError()
        {
            yield return null;

            LogAssert.Expect(LogType.Exception, new Regex("ArgumentException"));
            LogAssert.Expect(LogType.Error, new Regex("Tool execution failed"));
            LogAssert.Expect(LogType.Error, new Regex("Error Response to AI"));

            var jsonResult = RunToolRaw(Tool_Tool.ToolSetEnabledStateId, @"{
                ""tools"": null,
                ""includeLogs"": false
            }");

            StringAssert.Contains("Tools array is null or empty", jsonResult);
        }
    }
}
