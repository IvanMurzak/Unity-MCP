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

using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TokenCounterTests : BaseTest
    {
        [Test]
        public void FormatTokenCount_LessThan1000_ReturnsPlainNumber()
        {
            // Act
            var formatted = UIMcpUtils.FormatTokenCount(345);

            // Assert
            Assert.AreEqual("345", formatted);
        }

        [Test]
        public void FormatTokenCount_1000OrMore_ReturnsKFormat()
        {
            // Arrange & Act
            var formatted1K = UIMcpUtils.FormatTokenCount(1000);
            var formatted15K = UIMcpUtils.FormatTokenCount(1500);
            var formatted2K = UIMcpUtils.FormatTokenCount(2345);

            // Assert
            Assert.AreEqual("1K", formatted1K);
            Assert.AreEqual("1.5K", formatted15K);
            Assert.AreEqual("2.3K", formatted2K);
        }

        [Test]
        public void AllTools_TokenCount_ArePositive()
        {
            // Arrange
            var toolManager = McpPlugin.McpPlugin.Instance!.McpManager.ToolManager;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            var tools = toolManager!.GetAllTools();
            Assert.IsNotEmpty(tools, "There should be registered tools");

            // Assert
            foreach (var tool in tools)
                Assert.Greater(tool.TokenCount, 0, $"Tool '{tool.Name}' should have a positive token count");
        }

        [Test]
        public void AllTools_TokenCount_AreWithinReasonableRange()
        {
            // Arrange
            var tools = McpPlugin.McpPlugin.Instance!.McpManager.ToolManager!.GetAllTools();

            // Assert
            foreach (var tool in tools)
                Assert.Less(tool.TokenCount, 10000, $"Tool '{tool.Name}' has an unexpectedly high token count");
        }

        [Test]
        public void AllTools_TotalTokenCount_IsReasonable()
        {
            // Arrange
            var tools = McpPlugin.McpPlugin.Instance!.McpManager.ToolManager!.GetAllTools();

            // Act
            var totalTokens = tools.Sum(t => t.TokenCount);

            // Assert
            Assert.Greater(totalTokens, 1000, "Total token count across all tools should be significant");
            Assert.Less(totalTokens, 500000, "Total token count should not be absurdly large");
        }
    }
}
