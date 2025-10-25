/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Common.Tests.Infrastructure;
using com.IvanMurzak.Unity.MCP.Common.Tests.Utils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Mcp
{
    public class McpBuilderTests_ListTool
    {
        private readonly ITestOutputHelper _output;
        private readonly XunitTestOutputLoggerProvider _loggerProvider;
        private readonly Version version = new Version();

        public McpBuilderTests_ListTool(ITestOutputHelper output)
        {
            _output = output;
            _loggerProvider = new XunitTestOutputLoggerProvider(output);
        }

        async Task ValidateListToolResponse(RequestListTool request, Task<IResponseData<ResponseListTool[]>>? listToolTask, string expectedToolName, string expectedToolTitle)
        {
            listToolTask.Should().NotBeNull();

            var response = await listToolTask;
            response.Should().NotBeNull();
            response.RequestID.Should().Be(request.RequestID);
            response.Status.Should().Be(ResponseStatus.Success);
            response.Message.Should().NotBeNull();
            response.Value.Should().NotBeNull();
            response.Value!.Length.Should().Be(1);
            response.Value![0].Name.Should().Be(expectedToolName);
            response.Value![0].Title.Should().Be(expectedToolTitle);
        }

        private static void CompareJsonElements(string name, JsonElement? actual, JsonElement? expected)
        {
            if (expected == null)
            {
                actual.Should().BeNull(name);
            }
            else
            {
                actual.Should().NotBeNull(name);
                actual.Value.ValueKind.Should().Be(expected.Value.ValueKind, name);

                // First try exact string comparison for performance
                if (actual.ToString() == expected.ToString())
                    return;

                // If strings don't match, do deep comparison (order-insensitive)
                CompareJsonElementsRecursive(name, actual.Value, expected.Value);
            }
        }

        private static void CompareJsonElementsRecursive(string path, JsonElement actual, JsonElement expected)
        {
            actual.ValueKind.Should().Be(expected.ValueKind, $"{path}: ValueKind mismatch");

            switch (expected.ValueKind)
            {
                case JsonValueKind.Object:
                    var expectedProps = expected.EnumerateObject().ToList();
                    var actualProps = actual.EnumerateObject().ToList();

                    actualProps.Count.Should().Be(expectedProps.Count, $"{path}: Object property count mismatch");

                    foreach (var expectedProp in expectedProps)
                    {
                        var actualProp = actualProps.FirstOrDefault(p => p.Name == expectedProp.Name);
                        actualProp.Should().NotBeNull($"{path}: Missing property '{expectedProp.Name}'");

                        CompareJsonElementsRecursive($"{path}.{expectedProp.Name}", actualProp.Value, expectedProp.Value);
                    }
                    break;

                case JsonValueKind.Array:
                    var expectedArray = expected.EnumerateArray().ToList();
                    var actualArray = actual.EnumerateArray().ToList();

                    actualArray.Count.Should().Be(expectedArray.Count, $"{path}: Array length mismatch");

                    for (int i = 0; i < expectedArray.Count; i++)
                    {
                        CompareJsonElementsRecursive($"{path}[{i}]", actualArray[i], expectedArray[i]);
                    }
                    break;

                case JsonValueKind.String:
                    actual.GetString().Should().Be(expected.GetString(), $"{path}: String value mismatch");
                    break;

                case JsonValueKind.Number:
                    if (expected.TryGetInt32(out int expectedInt))
                    {
                        actual.GetInt32().Should().Be(expectedInt, $"{path}: Integer value mismatch");
                    }
                    else if (expected.TryGetInt64(out long expectedLong))
                    {
                        actual.GetInt64().Should().Be(expectedLong, $"{path}: Long value mismatch");
                    }
                    else if (expected.TryGetDouble(out double expectedDouble))
                    {
                        actual.GetDouble().Should().Be(expectedDouble, $"{path}: Double value mismatch");
                    }
                    else
                    {
                        actual.GetDecimal().Should().Be(expected.GetDecimal(), $"{path}: Decimal value mismatch");
                    }
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    actual.GetBoolean().Should().Be(expected.GetBoolean(), $"{path}: Boolean value mismatch");
                    break;

                case JsonValueKind.Null:
                    // Both are null, nothing to compare
                    break;

                default:
                    throw new ArgumentException($"{path}: Unsupported JsonValueKind: {expected.ValueKind}");
            }
        }

        private static async Task ValidateListToolSchema(Task<IResponseData<ResponseListTool[]>>? listToolTask, JsonElement? expectedInputSchema = null, JsonElement? expectedOutputSchema = null)
        {
            listToolTask.Should().NotBeNull();

            var response = await listToolTask;
            response.Should().NotBeNull();
            response.Value.Should().NotBeNull();
            response.Value!.Length.Should().Be(1);

            CompareJsonElements(nameof(ResponseListTool.InputSchema), response.Value![0].InputSchema, expectedInputSchema);
            CompareJsonElements(nameof(ResponseListTool.OutputSchema), response.Value![0].OutputSchema, expectedOutputSchema);
        }

        IMcpPlugin? BuildMcpPluginWithTool(string toolName, string toolTitle)
        {
            // Arrange
            var classType = typeof(SampleData.Method_NoArgs_Void);
            var method = typeof(SampleData.Method_NoArgs_Void).GetMethod(nameof(SampleData.Method_NoArgs_Void.Do))!;
            var reflector = new Reflector();
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
            var toolName = classType.GetTypeShortName();
            var toolTitle = $"Title of {toolName}";

            var reflector = new Reflector();
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

            var request = new RequestListTool();
            var listToolTask = mcpPlugin.McpRunner.RunListTool(request);

            // Assert
            await ValidateListToolResponse(request, listToolTask, toolName, toolTitle);
            await ValidateListToolSchema(
                listToolTask: listToolTask,
                expectedInputSchema: expectedInputSchema,
                expectedOutputSchema: expectedOutputSchema);
        }

        [Fact]
        public async Task ListTool_NoArgsVoidMethod_ShouldReturnEmptyInputSchema_AndNullOutputSchema()
        {
            await BuildAndValidateTool(
                classType: typeof(SampleData.Method_NoArgs_Void),
                method: typeof(SampleData.Method_NoArgs_Void).GetMethod(nameof(SampleData.Method_NoArgs_Void.Do))!,
                expectedInputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .BuildJsonElement(),
                expectedOutputSchema: null);
        }

        [Fact]
        public async Task ListTool_OneArgIntReturnMethod_ShouldBeListed()
        {
            await BuildAndValidateTool(
                classType: typeof(SampleData.Method_OneArg_IntReturn),
                method: typeof(SampleData.Method_OneArg_IntReturn).GetMethod(nameof(SampleData.Method_OneArg_IntReturn.AddOne))!,
                expectedInputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddSimpleProperty("value", JsonSchema.Integer, required: true)
                    .BuildJsonElement(),
                expectedOutputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddSimpleProperty(JsonSchema.Result, JsonSchema.Integer, required: true)
                    .BuildJsonElement());
        }

        [Fact]
        public async Task ListTool_TwoArgsStringReturnMethod_ShouldBeListed()
        {
            await BuildAndValidateTool(
                classType: typeof(SampleData.Method_TwoArgs_StringReturn),
                method: typeof(SampleData.Method_TwoArgs_StringReturn).GetMethod(nameof(SampleData.Method_TwoArgs_StringReturn.Concat))!,
                expectedInputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddSimpleProperty("left", JsonSchema.String, required: true)
                    .AddSimpleProperty("right", JsonSchema.String, required: true)
                    .BuildJsonElement(),
                expectedOutputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddSimpleProperty(JsonSchema.Result, JsonSchema.String, required: true)
                    .BuildJsonElement());
        }

        [Fact]
        public async Task ListTool_GenericMethod_ShouldBeListed()
        {
            var classType = typeof(SampleData.Method_Generic_T_Return<int>);
            var genericMethod = classType.GetMethod(nameof(SampleData.Method_Generic_T_Return<int>.Echo))!;
            var constructed = genericMethod.IsGenericMethodDefinition
                ? genericMethod.MakeGenericMethod(typeof(int))
                : genericMethod;

            await BuildAndValidateTool(
                classType: classType,
                method: constructed,
                expectedInputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddSimpleProperty("value", JsonSchema.Integer, required: true)
                    .BuildJsonElement(),
                expectedOutputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddSimpleProperty(JsonSchema.Result, JsonSchema.Integer, required: true)
                    .BuildJsonElement());
        }

        [Fact]
        public async Task ListTool_GenericMethodWithComplexType_ShouldBeListed()
        {
            var classType = typeof(SampleData.Method_Generic_T_Return<SampleData.Company>);
            var genericMethod = classType.GetMethod(nameof(SampleData.Method_Generic_T_Return<SampleData.Company>.Echo))!;
            var constructed = genericMethod.IsGenericMethodDefinition
                ? genericMethod.MakeGenericMethod(typeof(SampleData.Company))
                : genericMethod;

            await BuildAndValidateTool(
                classType: classType,
                method: constructed,
                expectedInputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddRefProperty<SampleData.Company>("value", required: true)
                    .AddCompanyDefine()
                    .BuildJsonElement(),
                expectedOutputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddRefProperty<SampleData.Company>(JsonSchema.Result, required: false) // TODO: `required` should be true for .NET 5.0+
                    .AddCompanyDefine()
                    .BuildJsonElement());
        }

        [Fact]
        public async Task ListTool_AsyncTaskMethod_ShouldBeListed()
        {
            await BuildAndValidateTool(
                classType: typeof(SampleData.Method_Async_Task),
                method: typeof(SampleData.Method_Async_Task).GetMethod(nameof(SampleData.Method_Async_Task.DoAsync))!,
                expectedInputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .BuildJsonElement(),
                expectedOutputSchema: null);
        }

        [Fact]
        public async Task ListTool_AsyncTaskOfIntMethod_ShouldBeListed()
        {
            await BuildAndValidateTool(
                classType: typeof(SampleData.Method_Async_TaskOfInt),
                method: typeof(SampleData.Method_Async_TaskOfInt).GetMethod(nameof(SampleData.Method_Async_TaskOfInt.ComputeAsync))!,
                expectedInputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddSimpleProperty("a", JsonSchema.Integer, required: true)
                    .AddSimpleProperty("b", JsonSchema.Integer, required: true)
                    .BuildJsonElement(),
                expectedOutputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddSimpleProperty(JsonSchema.Result, JsonSchema.Integer, required: true)
                    .BuildJsonElement());
        }

        [Fact]
        public async Task ListTool_NoArgsListOfIntReturnMethod_ShouldBeListed()
        {
            await BuildAndValidateTool(
                classType: typeof(SampleData.Method_NoArgs_ListOfIntReturn),
                method: typeof(SampleData.Method_NoArgs_ListOfIntReturn).GetMethod(nameof(SampleData.Method_NoArgs_ListOfIntReturn.Do))!,
                expectedInputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .BuildJsonElement(),
                expectedOutputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddRefProperty(JsonSchema.Result, "System.Int32_Array", required: true)
                    .AddArrayDefinition("System.Int32_Array", JsonSchema.Integer)
                    .BuildJsonElement());
        }

        [Fact]
        public async Task ListTool_NoArgsListOfGenericReturnMethod_ShouldBeListed()
        {
            var classType = typeof(SampleData.Method_NoArgs_ListOfGenericReturn<string>);
            var method = classType.GetMethod(nameof(SampleData.Method_NoArgs_ListOfGenericReturn<string>.Do))!;

            await BuildAndValidateTool(
                classType: classType,
                method: method,
                expectedInputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .BuildJsonElement(),
                expectedOutputSchema: new JsonObjectBuilder()
                    .SetTypeObject()
                    .AddRefProperty(JsonSchema.Result, "System.String_Array", required: true)
                    .AddArrayDefinition("System.String_Array", JsonSchema.String)
                    .BuildJsonElement());
        }
    }
}
