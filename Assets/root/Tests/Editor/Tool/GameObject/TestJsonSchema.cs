using System;
using System.Collections;
using System.Globalization;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Model.Unity;
using NUnit.Framework;
using UnityEngine.TestTools;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common;
using System.Text.Json;
using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestJsonSchema : BaseTest
    {
        static void ValidateType<T>(Reflector reflector) => ValidateType(reflector, typeof(T));
        static void ValidateType(Reflector reflector, Type type)
        {
            ValidateSchema(
                reflector: reflector,
                schema: reflector.GetSchema(type),
                type: type);
        }
        static void ValidateSchema(Reflector reflector, JsonNode schema, Type type)
        {
            UnityEngine.Debug.Log($"Schema for '{type.GetTypeName(pretty: true)}': {schema}");

            Assert.IsNotNull(schema, $"Schema for '{type.GetTypeName(pretty: true)}' is null");

            Assert.IsFalse(schema.ToJsonString().Contains($"\"{JsonSchema.Error}\":"),
                $"Schema for '{type.GetTypeName(pretty: true)}' contains {JsonSchema.Error} string");

            var typeNodes = JsonSchema.FindAllProperties(schema, JsonSchema.Type);
            foreach (var typeNode in typeNodes)
            {
                UnityEngine.Debug.Log($"Type node for '{type.GetTypeName(pretty: true)}': {typeNode}");
                switch (typeNode)
                {
                    case JsonValue value:
                        var typeValue = value.ToString();
                        Assert.IsFalse(string.IsNullOrEmpty(typeValue), $"Type node for '{type.GetTypeName(pretty: true)}' is empty");
                        Assert.IsFalse(typeValue == "null", $"Type node for '{type.GetTypeName(pretty: true)}' is \"null\" string");
                        Assert.IsFalse(typeValue.Contains($"\"{JsonSchema.Error}\""), $"Type node for '{type.GetTypeName(pretty: true)}' contains error string");
                        break;
                    default:
                        if (typeNode is JsonObject typeObject)
                        {
                            if (typeObject.TryGetPropertyValue("enum", out var enumValue))
                                continue; // Skip enum types
                        }
                        Assert.Fail($"Unexpected type node for '{type.GetTypeName(pretty: true)}'.\nThe '{JsonSchema.Type}' node has the type '{typeNode.GetType().GetTypeShortName()}':\n{typeNode}");
                        break;
                }
            }
        }
        static void ValidateMethodInputSchema(JsonElement schema)
        {
            ValidateMethodInputSchema(JsonNode.Parse(schema.ToString()));
        }
        static void ValidateMethodInputSchema(JsonNode schema)
        {
            UnityEngine.Debug.Log($"  Schema: {schema}");

            Assert.IsNotNull(schema, $"Schema is null");

            var json = schema.ToJsonString();

            Assert.IsNotNull(json, $"Json is null");
            Assert.IsFalse(json.Contains($"\"{JsonSchema.Error}\":"), $"Json contains {JsonSchema.Error} string");
        }

        [UnityTest]
        public IEnumerator Primitives()
        {
            var reflector = McpPlugin.Instance!.McpRunner.Reflector;

            ValidateType<int>(reflector);
            ValidateType<float>(reflector);
            ValidateType<bool>(reflector);
            ValidateType<string>(reflector);
            ValidateType<CultureTypes>(reflector); // enum

            yield return null;
        }

        [UnityTest]
        public IEnumerator Classes()
        {
            var reflector = McpPlugin.Instance!.McpRunner.Reflector;

            ValidateType<ObjectRef>(reflector);

            ValidateType<GameObjectRef>(reflector);
            ValidateType<GameObjectRefList>(reflector);
            ValidateType<GameObjectComponentsRef>(reflector);
            ValidateType<GameObjectComponentsRefList>(reflector);

            ValidateType<ComponentData>(reflector);
            ValidateType<ComponentDataLight>(reflector);
            ValidateType<ComponentRef>(reflector);
            ValidateType<ComponentRefList>(reflector);

            ValidateType<MethodData>(reflector);
            ValidateType<MethodRef>(reflector);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Structs()
        {
            var reflector = McpPlugin.Instance!.McpRunner.Reflector;

            ValidateType<DateTime>(reflector);
            ValidateType<TimeSpan>(reflector);

            yield return null;
        }

        [UnityTest]
        public IEnumerator UnityStructs()
        {
            var reflector = McpPlugin.Instance!.McpRunner.Reflector;

            ValidateType<UnityEngine.Color32>(reflector);
            ValidateType<UnityEngine.Color>(reflector);
            ValidateType<UnityEngine.Vector3>(reflector);
            ValidateType<UnityEngine.Vector3Int>(reflector);
            ValidateType<UnityEngine.Vector2>(reflector);
            ValidateType<UnityEngine.Vector2Int>(reflector);
            ValidateType<UnityEngine.Quaternion>(reflector);
            ValidateType<UnityEngine.Matrix4x4>(reflector);
            ValidateType<UnityEngine.Rect>(reflector);
            ValidateType<UnityEngine.RectInt>(reflector);
            ValidateType<UnityEngine.Bounds>(reflector);
            ValidateType<UnityEngine.BoundsInt>(reflector);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Unity()
        {
            var reflector = McpPlugin.Instance!.McpRunner.Reflector;

            ValidateType<UnityEngine.Object>(reflector);
            ValidateType<UnityEngine.Rigidbody>(reflector);
            ValidateType<UnityEngine.Animation>(reflector);
            ValidateType<UnityEngine.Material>(reflector);
            ValidateType<UnityEngine.Transform>(reflector);
            ValidateType<UnityEngine.SpriteRenderer>(reflector);
            ValidateType<UnityEngine.MeshRenderer>(reflector);

            yield return null;
        }

        [UnityTest]
        public IEnumerator MCP_Tools()
        {
            var task = McpPlugin.Instance.McpRunner.RunListTool(new RequestListTool());
            while (!task.IsCompleted)
            {
                yield return null; // Wait for the task to complete
            }
            var toolResponse = task.Result;
            var tools = toolResponse.Value;

            Assert.IsNotNull(tools, "Tool response is null");
            Assert.IsNotEmpty(tools, "Tool response is empty");

            // Validate the array of tools doesn't have duplicated tool names
            var toolNames = new HashSet<string>();

            foreach (var tool in tools)
            {
                UnityEngine.Debug.Log($"Tool: {tool.Name} - {tool.Description}");
                ValidateMethodInputSchema(tool.InputSchema);

                Assert.IsFalse(toolNames.Contains(tool.Name), $"Duplicate tool name found: {tool.Name}");
                toolNames.Add(tool.Name);
            }
            Assert.IsTrue(toolNames.Count > 0, "No tools found in the response");
        }
    }
}