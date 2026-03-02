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
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using com.IvanMurzak.Unity.MCP.Runtime.API;

namespace com.IvanMurzak.Unity.MCP.Runtime.Tests
{
    public partial class Tool_MCP_ListTools_Test
    {
        // Instance of the tool being tested
        Tool_MCP tool = new Tool_MCP();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.Log($"[{nameof(Tool_MCP_ListTools_Test)}] SetUp");
            // Build and start the plugin so ToolManager is available before each test
            UnityMcpPlugin.BuildAndStart();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"[{nameof(Tool_MCP_ListTools_Test)}] TearDown");
            yield return null;
        }

        // Test 1: Verifies that the mcp-list-tools tool includes itself in the returned list.
        // This confirms the tool is properly registered with the ToolManager.
        [UnityTest]
        public IEnumerator ListTools_Returns_Itself()
        {
            Debug.Log($"[{nameof(Tool_MCP_ListTools_Test)}] Testing: tool lists itself");

            var result = tool.ListTools();

            // The tool should be able to find itself in the list of registered tools
            var found = result.Any(t => t.Name == "mcp-list-tools");
            Assert.IsTrue(found, "mcp-list-tools should be present in the list of tools.");

            yield return null;
        }

        // Test 2: Verifies that the tool returns a non-empty list.
        // At minimum, the tool itself should always be present.
        [UnityTest]
        public IEnumerator ListTools_Returns_AtLeastOneTool()
        {
            Debug.Log($"[{nameof(Tool_MCP_ListTools_Test)}] Testing: returns at least one tool");

            var result = tool.ListTools();

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.Greater(result.Count, 0, "Should return at least one tool.");

            yield return null;
        }

        // Test 3: Verifies that regex filtering works correctly.
        // When filtering by "mcp", only tools whose name contains "mcp" should be returned.
        [UnityTest]
        public IEnumerator ListTools_RegexFilter_Works()
        {
            Debug.Log($"[{nameof(Tool_MCP_ListTools_Test)}] Testing: regex filtering");

            var result = tool.ListTools(regexSearch: "mcp");

            Assert.IsNotNull(result, "Result should not be null.");
            Assert.Greater(result.Count, 0, "Regex 'mcp' should match at least one tool.");

            // Every returned tool must match the regex pattern
            foreach (var t in result)
                Assert.IsTrue(
                    t.Name.Contains("mcp", System.StringComparison.OrdinalIgnoreCase),
                    $"Tool '{t.Name}' does not match regex 'mcp'.");

            yield return null;
        }

        // Test 4: Verifies that the includeDescription flag works correctly.
        // When true, tool descriptions should be populated.
        // When false, all descriptions should be null.
        [UnityTest]
        public IEnumerator ListTools_IncludeDescription_Works()
        {
            Debug.Log($"[{nameof(Tool_MCP_ListTools_Test)}] Testing: includeDescription");

            var withDesc = tool.ListTools(includeDescription: true);
            var withoutDesc = tool.ListTools(includeDescription: false);

            // At least one tool should have a description when includeDescription is true
            Assert.IsTrue(
                withDesc.Any(t => t.Description != null),
                "At least one tool should have a description when includeDescription=true.");

            // No tool should have a description when includeDescription is false
            Assert.IsTrue(
                withoutDesc.All(t => t.Description == null),
                "No tool should have a description when includeDescription=false.");

            yield return null;
        }

        // Test 5: Verifies that the includeInputs enum modes work correctly.
        // None  = no inputs returned
        // Inputs = input argument names are returned
        [UnityTest]
        public IEnumerator ListTools_IncludeInputs_Works()
        {
            Debug.Log($"[{nameof(Tool_MCP_ListTools_Test)}] Testing: includeInputs modes");

            // InputRequest.None — no tool should have inputs populated
            var noneMode = tool.ListTools(includeInputs: Tool_MCP.InputRequest.None);
            Assert.IsTrue(
                noneMode.All(t => t.Inputs == null),
                "No tool should have inputs when InputRequest.None.");

            // InputRequest.Inputs — at least one tool should have inputs populated
            var inputsMode = tool.ListTools(includeInputs: Tool_MCP.InputRequest.Inputs);
            Assert.IsTrue(
                inputsMode.Any(t => t.Inputs != null),
                "At least one tool should have inputs when InputRequest.Inputs.");

            yield return null;
        }

        // Test 6: Verifies that passing an invalid regex pattern throws an ArgumentException.
        // This ensures the tool fails gracefully instead of crashing Unity.
        [UnityTest]
        public IEnumerator ListTools_InvalidRegex_ThrowsArgumentException()
        {
            Debug.Log($"[{nameof(Tool_MCP_ListTools_Test)}] Testing: invalid regex");

            // An unclosed bracket is an invalid regex pattern
            Assert.Throws<System.ArgumentException>(() =>
            {
                tool.ListTools(regexSearch: "[invalid regex");
            }, "Invalid regex should throw ArgumentException.");

            yield return null;
        }
    }
}
