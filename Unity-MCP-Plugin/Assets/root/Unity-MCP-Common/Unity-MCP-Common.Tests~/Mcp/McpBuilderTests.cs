/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;
using com.IvanMurzak.Unity.MCP.Common.Tests.Infrastructure;
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Common;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Mcp
{
    public class McpBuilderTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Version version = new Version();

        public McpBuilderTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Version_DefaultValues_ShouldBeInitialized()
        {
            var loggerProvider = new XunitTestOutputLoggerProvider(_output);

            // Arrange
            var toolName = "NoArgsVoid";
            var toolTitle = "No args void method";
            var classType = typeof(SampleData.Method_NoArgs_Void);
            var method = typeof(SampleData.Method_NoArgs_Void).GetMethod(nameof(SampleData.Method_NoArgs_Void.Do))!;
            var reflector = new ReflectorNet.Reflector();
            var mcpPluginBuilder = new McpPluginBuilder(version, loggerProvider)
                .AddLogging(b => b.AddXunitTestOutput(_output))
                .AddMcpRunner();

            // Act
            mcpPluginBuilder.WithTool(
                name: toolName,
                title: toolTitle,
                classType: classType,
                method: method);

            var mcpPlugin = mcpPluginBuilder.Build(reflector);

            var requestListTool = new RequestListTool();
            var listToolTask = mcpPlugin.McpRunner.RunListTool(requestListTool);

            // Assert
            listToolTask.Should().NotBeNull();
            var response = await listToolTask;
            response.Should().NotBeNull();
            response.RequestID.Should().Be(requestListTool.RequestID);
        }
    }
}
