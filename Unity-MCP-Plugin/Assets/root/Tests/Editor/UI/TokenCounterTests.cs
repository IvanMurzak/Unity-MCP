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
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TokenCounterTests
    {
        [Test]
        public void EstimateToolTokens_WithBasicTool_ReturnsPositiveCount()
        {
            // Arrange
            var toolName = "TestTool";
            var description = "A simple test tool for demonstration";
            var inputSchema = JsonNode.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""input1"": { ""type"": ""string"", ""description"": ""First input"" },
                    ""input2"": { ""type"": ""number"", ""description"": ""Second input"" }
                }
            }");

            // Act
            var tokenCount = TokenCounter.EstimateToolTokens(toolName, description, inputSchema);

            // Assert
            Assert.Greater(tokenCount, 0, "Token count should be positive");
            Assert.Greater(tokenCount, 10, "Token count should be reasonable for the given input");
        }

        [Test]
        public void EstimateToolTokens_WithNullInputs_ReturnsMinimalCount()
        {
            // Act
            var tokenCount = TokenCounter.EstimateToolTokens(null, null, null, null);

            // Assert
            Assert.AreEqual(10, tokenCount, "Token count should be the overhead value when all inputs are null");
        }

        [Test]
        public void EstimateToolTokens_WithOutputSchema_IncludesOutputInCount()
        {
            // Arrange
            var toolName = "TestTool";
            var inputSchema = JsonNode.Parse(@"{ ""type"": ""object"" }");
            var outputSchema = JsonNode.Parse(@"{ ""type"": ""object"", ""properties"": { ""result"": { ""type"": ""string"" } } }");

            // Act
            var countWithoutOutput = TokenCounter.EstimateToolTokens(toolName, null, inputSchema);
            var countWithOutput = TokenCounter.EstimateToolTokens(toolName, null, inputSchema, outputSchema);

            // Assert
            Assert.Greater(countWithOutput, countWithoutOutput, "Token count with output schema should be higher");
        }

        [Test]
        public void FormatTokenCount_LessThan1000_ReturnsPlainNumber()
        {
            // Act
            var formatted = TokenCounter.FormatTokenCount(345);

            // Assert
            Assert.AreEqual("345", formatted);
        }

        [Test]
        public void FormatTokenCount_1000OrMore_ReturnsKFormat()
        {
            // Arrange & Act
            var formatted1K = TokenCounter.FormatTokenCount(1000);
            var formatted15K = TokenCounter.FormatTokenCount(1500);
            var formatted2K = TokenCounter.FormatTokenCount(2345);

            // Assert
            Assert.AreEqual("1K", formatted1K);
            Assert.AreEqual("1.5K", formatted15K);
            Assert.AreEqual("2.3K", formatted2K);
        }

        [UnityTest]
        public IEnumerator EstimateToolTokens_WithRealToolSchema_ReturnsReasonableCount()
        {
            // Wait for MCP plugin to be initialized
            yield return new UnityEngine.WaitForSeconds(0.1f);

            // Arrange - Using a realistic tool schema
            var toolName = "GameObject.Find";
            var description = "Find GameObjects in the scene by name, tag, or layer";
            var inputSchema = JsonNode.Parse(@"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { 
                        ""type"": ""string"", 
                        ""description"": ""Name of the GameObject to find"" 
                    },
                    ""tag"": { 
                        ""type"": ""string"", 
                        ""description"": ""Tag of GameObjects to find"" 
                    },
                    ""layer"": { 
                        ""type"": ""integer"", 
                        ""description"": ""Layer of GameObjects to find"" 
                    }
                },
                ""required"": [""name""]
            }");

            // Act
            var tokenCount = TokenCounter.EstimateToolTokens(toolName, description, inputSchema);

            // Assert
            Assert.Greater(tokenCount, 50, "Realistic tool should have significant token count");
            Assert.Less(tokenCount, 500, "Token count should be reasonable, not excessive");
        }
    }
}
