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
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
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
                {
                    toolManager.SetToolEnabled(_testToolName, true);
                }
                toolManager.SetToolEnabled(Tool_Tool.ToolSetEnabledStateId, false);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        Tool_Tool.ResultData? ParseResult(string jsonMessage)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var doc = JsonDocument.Parse(jsonMessage);
            if (doc.RootElement.TryGetProperty("result", out var resultProp))
                return JsonSerializer.Deserialize<Tool_Tool.ResultData>(resultProp.GetRawText(), options);
            return JsonSerializer.Deserialize<Tool_Tool.ResultData>(jsonMessage, options);
        }

        [UnityTest]
        public IEnumerator SetEnabled_SingleTool_ShouldDisable()
        {
            yield return null;

            var response = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": false }}],
                ""includeLogs"": true
            }}");

            var result = ParseResult(response.Value!.GetMessage()!);
            Assert.IsNotNull(result);
            Assert.IsTrue(result!.Success.ContainsKey(_testToolName!), $"Should have result for tool '{_testToolName}'");
            Assert.IsTrue(result.Success[_testToolName!], $"Should successfully disable tool '{_testToolName}'");

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

            var response = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": true }}],
                ""includeLogs"": true
            }}");

            var result = ParseResult(response.Value!.GetMessage()!);
            Assert.IsNotNull(result);
            Assert.IsTrue(result!.Success.ContainsKey(_testToolName!), $"Should have result for tool '{_testToolName}'");
            Assert.IsTrue(result.Success[_testToolName!], $"Should successfully enable tool '{_testToolName}'");
            Assert.IsTrue(toolManager.IsToolEnabled(_testToolName!), $"Tool '{_testToolName}' should be enabled");
        }

        [UnityTest]
        public IEnumerator SetEnabled_AlreadyInDesiredState_ShouldReturnTrue()
        {
            yield return null;

            var response = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": true }}],
                ""includeLogs"": true
            }}");

            var jsonMessage = response.Value!.GetMessage()!;
            var result = ParseResult(jsonMessage);
            Assert.IsNotNull(result);
            Assert.IsTrue(result!.Success.ContainsKey(_testToolName!), "Should have result for tool");
            Assert.IsTrue(result.Success[_testToolName!], "Should return true when tool is already in desired state");
            StringAssert.Contains("already", jsonMessage);
        }

        [UnityTest]
        public IEnumerator SetEnabled_NonExistentTool_ShouldReturnFalse()
        {
            yield return null;

            var fakeName = "non-existent-tool-xyz-12345";
            var jsonResult = RunToolRaw(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{fakeName}"", ""enabled"": true }}],
                ""includeLogs"": true
            }}");

            var result = ParseResult(jsonResult);
            Assert.IsNotNull(result);
            Assert.IsTrue(result!.Success.ContainsKey(fakeName), "Should have entry for non-existent tool");
            Assert.IsFalse(result.Success[fakeName], "Should return false for non-existent tool");
        }

        [UnityTest]
        public IEnumerator SetEnabled_CaseInsensitive_ShouldResolve()
        {
            yield return null;

            var wrongCase = _testToolName!.ToUpperInvariant();
            var response = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{wrongCase}"", ""enabled"": false }}],
                ""includeLogs"": true
            }}");

            var result = ParseResult(response.Value!.GetMessage()!);
            Assert.IsNotNull(result);
            Assert.IsTrue(result!.Success.ContainsKey(_testToolName!), $"Should resolve '{wrongCase}' to '{_testToolName}'");
            Assert.IsTrue(result.Success[_testToolName!], "Should successfully change state with case-insensitive match");
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
                var response = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                    ""tools"": [
                        {{ ""name"": ""{_testToolName}"", ""enabled"": false }},
                        {{ ""name"": ""{secondTool.Name}"", ""enabled"": false }}
                    ],
                    ""includeLogs"": true
                }}");

                var result = ParseResult(response.Value!.GetMessage()!);
                Assert.IsNotNull(result);
                Assert.AreEqual(2, result!.Success.Count, "Should have results for both tools");
                Assert.IsTrue(result.Success.ContainsKey(_testToolName!), "Should have result for first tool");
                Assert.IsTrue(result.Success.ContainsKey(secondTool.Name), "Should have result for second tool");
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

            var jsonMessage = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": false }}],
                ""includeLogs"": true
            }}").Value!.GetMessage()!;

            var result = ParseResult(jsonMessage);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result!.Logs, "Logs should not be null when includeLogs is true");
        }

        [UnityTest]
        public IEnumerator SetEnabled_WithoutLogs_ShouldExcludeLogs()
        {
            yield return null;

            var jsonMessage = RunTool(Tool_Tool.ToolSetEnabledStateId, $@"{{
                ""tools"": [{{ ""name"": ""{_testToolName}"", ""enabled"": false }}],
                ""includeLogs"": false
            }}").Value!.GetMessage()!;

            var result = ParseResult(jsonMessage);
            Assert.IsNotNull(result);
            Assert.IsNull(result!.Logs, "Logs should be null when includeLogs is false");
        }

        [UnityTest]
        public IEnumerator SetEnabled_EmptyArray_ShouldReturnError()
        {
            yield return null;

            var jsonResult = RunToolRaw(Tool_Tool.ToolSetEnabledStateId, @"{
                ""tools"": [],
                ""includeLogs"": false
            }");

            StringAssert.Contains("[Error]", jsonResult);
        }

        [UnityTest]
        public IEnumerator SetEnabled_NullArray_ShouldReturnError()
        {
            yield return null;

            var jsonResult = RunToolRaw(Tool_Tool.ToolSetEnabledStateId, @"{
                ""tools"": null,
                ""includeLogs"": false
            }");

            StringAssert.Contains("[Error]", jsonResult);
        }
    }
}
