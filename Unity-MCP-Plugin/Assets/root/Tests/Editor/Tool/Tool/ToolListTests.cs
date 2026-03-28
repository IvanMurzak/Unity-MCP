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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class ToolListTests : BaseTest
    {
        bool? _originalToolListEnabled;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            _originalToolListEnabled = toolManager!.IsToolEnabled(Tool_Tool.ToolListId);
            toolManager.SetToolEnabled(Tool_Tool.ToolListId, true);
            UnityMcpPluginEditor.Instance.Save();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null && _originalToolListEnabled.HasValue)
            {
                toolManager.SetToolEnabled(Tool_Tool.ToolListId, _originalToolListEnabled.Value);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        [UnityTest]
        public IEnumerator List_NoArgs_ReturnsAllTools()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, "{}").Value!.GetMessage()!;
            using var doc = JsonDocument.Parse(json);
            var root = GetResultArray(doc);

            Assert.IsTrue(root.GetArrayLength() > 0, "Should return at least one tool");
        }

        [UnityTest]
        public IEnumerator List_NoArgs_ContainsOnlyNames()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, "{}").Value!.GetMessage()!;
            using var doc = JsonDocument.Parse(json);
            var first = GetResultArray(doc)[0];

            Assert.IsTrue(first.TryGetProperty("name", out _), "Each tool should have a name");

            var hasDescription = first.TryGetProperty("description", out var desc) &&
                                 desc.ValueKind != JsonValueKind.Null;
            Assert.IsFalse(hasDescription, "Description should not be included by default");

            var hasInputs = first.TryGetProperty("inputs", out var inputs) &&
                            inputs.ValueKind != JsonValueKind.Null;
            Assert.IsFalse(hasInputs, "Inputs should not be included by default");
        }

        [UnityTest]
        public IEnumerator List_IncludeDescription_ReturnsDescriptions()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""includeDescription"": true
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            var hasAnyDescription = false;
            for (int i = 0; i < arr.GetArrayLength(); i++)
            {
                if (arr[i].TryGetProperty("description", out var desc) &&
                    desc.ValueKind == JsonValueKind.String &&
                    desc.GetString()!.Length > 0)
                {
                    hasAnyDescription = true;
                    break;
                }
            }

            Assert.IsTrue(hasAnyDescription, "At least one tool should have a description");
        }

        [UnityTest]
        public IEnumerator List_IncludeInputs_ReturnsInputNames()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""includeInputs"": ""Inputs""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            var hasAnyInputs = false;
            for (int i = 0; i < arr.GetArrayLength(); i++)
            {
                if (arr[i].TryGetProperty("inputs", out var inputs) &&
                    inputs.ValueKind == JsonValueKind.Array &&
                    inputs.GetArrayLength() > 0)
                {
                    var firstInput = inputs[0];
                    Assert.IsTrue(firstInput.TryGetProperty("name", out _), "Input should have name");

                    var hasInputDesc = firstInput.TryGetProperty("description", out var desc) &&
                                       desc.ValueKind != JsonValueKind.Null;
                    Assert.IsFalse(hasInputDesc, "Input description should not be included with 'Inputs' mode");

                    hasAnyInputs = true;
                    break;
                }
            }

            Assert.IsTrue(hasAnyInputs, "At least one tool should have inputs");
        }

        [UnityTest]
        public IEnumerator List_IncludeInputsWithDescription_ReturnsInputDescriptions()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""includeInputs"": ""InputsWithDescription""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            var hasAnyInputDescription = false;
            for (int i = 0; i < arr.GetArrayLength(); i++)
            {
                if (arr[i].TryGetProperty("inputs", out var inputs) &&
                    inputs.ValueKind == JsonValueKind.Array)
                {
                    for (int j = 0; j < inputs.GetArrayLength(); j++)
                    {
                        if (inputs[j].TryGetProperty("description", out var desc) &&
                            desc.ValueKind == JsonValueKind.String &&
                            desc.GetString()!.Length > 0)
                        {
                            hasAnyInputDescription = true;
                            break;
                        }
                    }
                }
                if (hasAnyInputDescription) break;
            }

            Assert.IsTrue(hasAnyInputDescription, "At least one input should have a description");
        }

        [UnityTest]
        public IEnumerator List_RegexSearch_FiltersByToolName()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""regexSearch"": ""^tool-list$""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            Assert.AreEqual(1, arr.GetArrayLength(), "Exact regex for 'tool-list' should match one tool");
            Assert.AreEqual("tool-list", arr[0].GetProperty("name").GetString());
        }

        [UnityTest]
        public IEnumerator List_RegexSearch_NoMatch_ReturnsEmpty()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""regexSearch"": ""^nonexistent-tool-xyz-99999$""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            Assert.AreEqual(0, arr.GetArrayLength(), "Non-matching regex should return empty array");
        }

        [UnityTest]
        public IEnumerator List_RegexSearch_MatchesDescription()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""regexSearch"": ""Enable or disable MCP tools""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            Assert.IsTrue(arr.GetArrayLength() > 0, "Should match tool by description content");

            var names = Enumerable.Range(0, arr.GetArrayLength())
                .Select(i => arr[i].GetProperty("name").GetString())
                .ToList();
            Assert.Contains("tool-set-enabled-state", names,
                "tool-set-enabled-state description contains 'Enable or disable MCP tools'");
        }

        [UnityTest]
        public IEnumerator List_RegexSearch_MatchesInputName()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""regexSearch"": ""^regexSearch$""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            var names = Enumerable.Range(0, arr.GetArrayLength())
                .Select(i => arr[i].GetProperty("name").GetString())
                .ToList();
            Assert.Contains("tool-list", names, "tool-list has input named 'regexSearch'");
        }

        [UnityTest]
        public IEnumerator List_RegexSearch_MatchesInputDescription()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""regexSearch"": ""regex pattern to filter tools""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            var names = Enumerable.Range(0, arr.GetArrayLength())
                .Select(i => arr[i].GetProperty("name").GetString())
                .ToList();
            Assert.Contains("tool-list", names, "tool-list input description contains 'regex pattern to filter tools'");
        }

        [UnityTest]
        public IEnumerator List_RegexSearch_CaseInsensitive()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""regexSearch"": ""^TOOL-LIST$""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            Assert.AreEqual(1, arr.GetArrayLength(), "Regex should match case-insensitively");
            Assert.AreEqual("tool-list", arr[0].GetProperty("name").GetString());
        }

        [UnityTest]
        public IEnumerator List_RegexWithIncludeOptions_ReturnsCombinedResult()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""regexSearch"": ""^tool-list$"",
                ""includeDescription"": true,
                ""includeInputs"": ""InputsWithDescription""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var arr = GetResultArray(doc);

            Assert.AreEqual(1, arr.GetArrayLength());
            var tool = arr[0];

            Assert.AreEqual("tool-list", tool.GetProperty("name").GetString());

            Assert.IsTrue(
                tool.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String,
                "Description should be included");

            Assert.IsTrue(
                tool.TryGetProperty("inputs", out var inputs) &&
                inputs.ValueKind == JsonValueKind.Array &&
                inputs.GetArrayLength() > 0,
                "Inputs should be included");

            var firstInput = inputs[0];
            Assert.IsTrue(firstInput.TryGetProperty("name", out _), "Input should have name");
            Assert.IsTrue(
                firstInput.TryGetProperty("description", out var inputDesc) &&
                inputDesc.ValueKind == JsonValueKind.String,
                "Input description should be included with InputsWithDescription");
        }

        [UnityTest]
        public IEnumerator List_RegexSearch_InvalidPattern_ReturnsError()
        {
            yield return null;

            var originalLogLevel = UnityMcpPluginEditor.LogLevel;
            try
            {
                if (originalLogLevel == LogLevel.None)
                    UnityMcpPluginEditor.LogLevel = LogLevel.Error;

                LogAssert.Expect(LogType.Exception, new Regex("ArgumentException"));
                LogAssert.Expect(LogType.Error, new Regex("Tool execution failed"));
                LogAssert.Expect(LogType.Error, new Regex("Error Response to AI"));

                var json = RunToolRaw(Tool_Tool.ToolListId, @"{
                    ""regexSearch"": ""[invalid""
                }");

                StringAssert.Contains("Invalid regex pattern", json);
            }
            finally
            {
                UnityMcpPluginEditor.LogLevel = originalLogLevel;
            }
        }

        static JsonElement GetResultArray(JsonDocument doc)
        {
            var root = doc.RootElement;

            if (root.TryGetProperty("result", out var resultEl))
                root = resultEl;

            Assert.AreEqual(JsonValueKind.Array, root.ValueKind, "Result should be an array");
            return root;
        }
    }
}
