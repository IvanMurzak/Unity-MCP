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
using System.Text.Json;
using System;
using System.Reflection;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Mcp
{
    public class McpBuilderTests_ListTool
    {
        private readonly ITestOutputHelper _output;
        private readonly XunitTestOutputLoggerProvider _loggerProvider;
        private readonly Version version = new Version();
        private readonly JsonElement emptyJsonObject = JsonDocument.Parse("{}").RootElement;
        private readonly JsonElement emptyJsonArray = JsonDocument.Parse("[]").RootElement;
        private readonly JsonElement emptyJsonSchemaObject = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;

        public McpBuilderTests_ListTool(ITestOutputHelper output)
        {
            _output = output;
            _loggerProvider = new XunitTestOutputLoggerProvider(output);
        }

        async Task ValidateListToolResponse(RequestListTool requestListTool, Task<IResponseData<ResponseListTool[]>>? listToolTask, string expectedToolName, string expectedToolTitle)
        {
            listToolTask.Should().NotBeNull();

            var response = await listToolTask;
            response.Should().NotBeNull();
            response.RequestID.Should().Be(requestListTool.RequestID);
            response.Status.Should().Be(ResponseStatus.Success);
            response.Message.Should().NotBeNull();
            response.Value.Should().NotBeNull();
            response.Value!.Length.Should().Be(1);
            response.Value![0].Name.Should().Be(expectedToolName);
            response.Value![0].Title.Should().Be(expectedToolTitle);
        }

        void CompareJsonElements(JsonElement? actual, JsonElement? expected)
        {
            if (expected == null)
            {
                actual.Should().BeNull();
            }
            else
            {
                actual.Should().NotBeNull();
                actual.Value.ValueKind.Should().Be(expected.Value.ValueKind);
                actual.ToString().Should().Be(expected.ToString());
            }
        }

        async Task ValidateListToolSchema(Task<IResponseData<ResponseListTool[]>>? listToolTask, JsonElement? expectedInputSchema = null, JsonElement? expectedOutputSchema = null)
        {
            listToolTask.Should().NotBeNull();

            var response = await listToolTask;
            response.Should().NotBeNull();
            response.Value.Should().NotBeNull();
            response.Value!.Length.Should().Be(1);

            CompareJsonElements(response.Value![0].InputSchema, expectedInputSchema);
            CompareJsonElements(response.Value![0].OutputSchema, expectedOutputSchema);
        }

        IMcpPlugin? BuildMcpPluginWithTool(string toolName, string toolTitle)
        {
            // Arrange
            var classType = typeof(SampleData.Method_NoArgs_Void);
            var method = typeof(SampleData.Method_NoArgs_Void).GetMethod(nameof(SampleData.Method_NoArgs_Void.Do))!;
            var reflector = new ReflectorNet.Reflector();
            var mcpPluginBuilder = new McpPluginBuilder(version, _loggerProvider)
                .AddLogging(b => b.AddXunitTestOutput(_output))
                .AddMcpRunner();

            // Act
            mcpPluginBuilder.WithTool(
                name: toolName,
                title: toolTitle,
                classType: classType,
                method: method);

            return mcpPluginBuilder.Build(reflector);
        }

        async Task BuildAndValidateTool(Type classType, MethodInfo method, JsonElement? expectedInputSchema = null, JsonElement? expectedOutputSchema = null)
        {
            var toolName = classType.Name;
            var toolTitle = $"Title of {toolName}";

            var reflector = new ReflectorNet.Reflector();
            var mcpPluginBuilder = new McpPluginBuilder(version, _loggerProvider)
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
            await ValidateListToolResponse(requestListTool, listToolTask, toolName, toolTitle);
            await ValidateListToolSchema(
                listToolTask: listToolTask,
                expectedInputSchema: expectedInputSchema,
                expectedOutputSchema: expectedOutputSchema);
        }

        [Fact]
        public async Task Version_DefaultValues_ShouldBeInitialized()
        {
            await BuildAndValidateTool(
                classType: typeof(SampleData.Method_NoArgs_Void),
                method: typeof(SampleData.Method_NoArgs_Void).GetMethod(nameof(SampleData.Method_NoArgs_Void.Do))!,
                expectedInputSchema: emptyJsonSchemaObject,
                expectedOutputSchema: null);
        }
    }
}
