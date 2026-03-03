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
using com.IvanMurzak.Unity.MCP.Runtime.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ToolListTests : BaseTest
    {
        private Tool_Tool _tool = null!;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();
            _tool = new Tool_Tool();
        }

        [UnityTest]
        public IEnumerator ListTools_NoFilter_ReturnsNonEmptyArray()
        {
            var result = _tool.ListTools();

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsNotEmpty(result, "Result should contain at least one tool.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_NoFilter_ContainsListToolsItself()
        {
            var result = _tool.ListTools();

            Assert.IsTrue(
                result.Any(t => t.Name == Tool_Tool.ListToolsToolId),
                $"Result should contain the '{Tool_Tool.ListToolsToolId}' tool itself.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_RegexFilter_ByExactToolName_ReturnsSingleMatch()
        {
            var result = _tool.ListTools(regexSearch: $"^{Tool_Tool.ListToolsToolId}$");

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.AreEqual(1, result.Length, "Exact-name regex should match exactly one tool.");
            Assert.AreEqual(Tool_Tool.ListToolsToolId, result[0].Name);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_RegexFilter_NoMatch_ReturnsEmpty()
        {
            var result = _tool.ListTools(regexSearch: "this-tool-name-does-not-exist-xyz123");

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsEmpty(result, "Non-matching regex should return an empty array.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_RegexFilter_MatchesArgumentName()
        {
            // 'regexSearch' is a parameter name of the ListTools tool itself
            var result = _tool.ListTools(regexSearch: "regexSearch", includeInputs: Tool_Tool.InputRequest.Inputs);

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsTrue(
                result.Any(t => t.Name == Tool_Tool.ListToolsToolId),
                "Filter on argument name 'regexSearch' should match the tool-list tool.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_RegexFilter_MatchesToolDescription()
        {
            // The ListTools tool description contains "list of all available MCP tools"
            var result = _tool.ListTools(regexSearch: "list of all available");

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsTrue(
                result.Any(t => t.Name == Tool_Tool.ListToolsToolId),
                "Filter on description text should match the tool-list tool.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_WithDescription_IncludesDescription()
        {
            var result = _tool.ListTools(
                regexSearch: $"^{Tool_Tool.ListToolsToolId}$",
                includeDescription: true);

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.AreEqual(1, result.Length);
            Assert.IsNotNull(result[0].Description,
                "Description should be included when includeDescription is true.");
            Assert.IsNotEmpty(result[0].Description,
                "Description should not be empty.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_WithoutDescription_ExcludesDescription()
        {
            var result = _tool.ListTools(
                regexSearch: $"^{Tool_Tool.ListToolsToolId}$",
                includeDescription: false);

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.AreEqual(1, result.Length);
            Assert.IsNull(result[0].Description,
                "Description should be null when includeDescription is false.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_DefaultDescription_ExcludesDescription()
        {
            // Default value of includeDescription is false
            var result = _tool.ListTools(regexSearch: $"^{Tool_Tool.ListToolsToolId}$");

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.AreEqual(1, result.Length);
            Assert.IsNull(result[0].Description,
                "Description should be null by default.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_InputRequestNone_InputsAreNull()
        {
            var result = _tool.ListTools(
                regexSearch: $"^{Tool_Tool.ListToolsToolId}$",
                includeInputs: Tool_Tool.InputRequest.None);

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.AreEqual(1, result.Length);
            Assert.IsNull(result[0].Inputs,
                "Inputs should be null when InputRequest.None is used.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_InputRequestInputs_IncludesInputsWithoutDescriptions()
        {
            var result = _tool.ListTools(
                regexSearch: $"^{Tool_Tool.ListToolsToolId}$",
                includeInputs: Tool_Tool.InputRequest.Inputs);

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.AreEqual(1, result.Length);
            Assert.IsNotNull(result[0].Inputs,
                "Inputs should not be null when InputRequest.Inputs is used.");
            Assert.IsNotEmpty(result[0].Inputs,
                "Inputs array should contain entries for the tool-list tool.");

            // All input descriptions should be null in Inputs mode (no descriptions)
            foreach (var input in result[0].Inputs!)
                Assert.IsNull(input.Description,
                    $"Input '{input.Name}' should have null description in InputRequest.Inputs mode.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_InputRequestInputsWithDescription_IncludesInputsWithDescriptions()
        {
            var result = _tool.ListTools(
                regexSearch: $"^{Tool_Tool.ListToolsToolId}$",
                includeInputs: Tool_Tool.InputRequest.InputsWithDescription);

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.AreEqual(1, result.Length);
            Assert.IsNotNull(result[0].Inputs,
                "Inputs should not be null when InputRequest.InputsWithDescription is used.");
            Assert.IsNotEmpty(result[0].Inputs,
                "Inputs array should contain entries for the tool-list tool.");

            // At least some inputs should have descriptions
            Assert.IsTrue(
                result[0].Inputs!.Any(i => !string.IsNullOrEmpty(i.Description)),
                "At least one input should have a description in InputRequest.InputsWithDescription mode.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_InputRequestInputs_InputNamesAreCorrect()
        {
            var result = _tool.ListTools(
                regexSearch: $"^{Tool_Tool.ListToolsToolId}$",
                includeInputs: Tool_Tool.InputRequest.Inputs);

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.AreEqual(1, result.Length);
            Assert.IsNotNull(result[0].Inputs);

            var inputNames = result[0].Inputs!.Select(i => i.Name).ToArray();
            CollectionAssert.Contains(inputNames, "regexSearch",
                "Inputs should include 'regexSearch' parameter.");
            CollectionAssert.Contains(inputNames, "includeDescription",
                "Inputs should include 'includeDescription' parameter.");
            CollectionAssert.Contains(inputNames, "includeInputs",
                "Inputs should include 'includeInputs' parameter.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_AllTools_HaveNonEmptyNames()
        {
            var result = _tool.ListTools();

            Assert.IsNotNull(result, "Result should not be null.");
            foreach (var tool in result)
                Assert.IsNotEmpty(tool.Name, "Each tool must have a non-empty name.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ListTools_RegexFilter_CaseInsensitive_Matches()
        {
            // Tool name is lowercase "tool-list", searching in uppercase
            var result = _tool.ListTools(regexSearch: "TOOL-LIST");

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsTrue(
                result.Any(t => t.Name == Tool_Tool.ListToolsToolId),
                "Regex filter should be case-insensitive.");
            yield return null;
        }
    }
}
