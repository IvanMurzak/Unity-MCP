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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.Unity.MCP.Reflection.Converter;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestSerializer : BaseTest
    {
        static void PrintSerializers<TTarget>()
        {
            Debug.Log($"Serialize <b>[{typeof(TTarget)}]</b> priority:\n" + string.Join("\n", McpPlugin.McpPlugin.Instance!.McpManager.Reflector.Converters.GetAllSerializers()
                .Select(x => $"{x.GetType()}: Priority: {x.SerializationPriority(typeof(TTarget))}")
                .ToList()));
        }
        static void TestGetConverter<TTarget, TSerializer>()
        {
            PrintSerializers<TTarget>();
            TestGetConverter(typeof(TTarget), typeof(TSerializer));

            PrintSerializers<IEnumerable<TTarget>>();
            TestGetConverter(typeof(IEnumerable<TTarget>), typeof(UnityArrayReflectionConverter));

            PrintSerializers<List<TTarget>>();
            TestGetConverter(typeof(List<TTarget>), typeof(UnityArrayReflectionConverter));

            PrintSerializers<TTarget[]>();
            TestGetConverter(typeof(TTarget[]), typeof(UnityArrayReflectionConverter));

            Debug.Log($"-------------------------------------------");
        }
        static void TestGetConverter(Type type, Type serializerType)
        {
            var converter = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.Converters.GetConverter(type);
            Assert.IsNotNull(converter, $"{type}: Converter should not be null.");
            Assert.AreEqual(serializerType, converter!.GetType(), $"{type}: The Converter should be {serializerType}.");
        }

        [UnityTest]
        public IEnumerator RS_SerializersOrder()
        {
            TestGetConverter<int, PrimitiveReflectionConverter>();
            TestGetConverter<float, PrimitiveReflectionConverter>();
            TestGetConverter<bool, PrimitiveReflectionConverter>();
            TestGetConverter<string, PrimitiveReflectionConverter>();
            TestGetConverter<DateTime, PrimitiveReflectionConverter>();
            TestGetConverter<CultureTypes, PrimitiveReflectionConverter>(); // enum
            TestGetConverter<object, UnityGenericReflectionConverter<object>>();
            TestGetConverter<ObjectRef, UnityGenericReflectionConverter<object>>();

            TestGetConverter<UnityEngine.Object, UnityEngine_Object_ReflectionConverter>();
            TestGetConverter<UnityEngine.Vector3, UnityEngine_Vector3_ReflectionConverter>();
            TestGetConverter<UnityEngine.Rigidbody, UnityEngine_Component_ReflectionConverter>();
            TestGetConverter<UnityEngine.Animation, UnityEngine_Component_ReflectionConverter>();
            TestGetConverter<UnityEngine.Material, UnityEngine_Material_ReflectionConverter>();
            TestGetConverter<UnityEngine.Transform, UnityEngine_Transform_ReflectionConverter>();
            TestGetConverter<UnityEngine.SpriteRenderer, UnityEngine_Renderer_ReflectionConverter>();
            TestGetConverter<UnityEngine.MeshRenderer, UnityEngine_Renderer_ReflectionConverter>();

            yield return null;
        }

        [UnityTest]
        public IEnumerator SerializeMaterial()
        {
            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            var material = new Material(Shader.Find("Standard"));

            var serialized = reflector.Serialize(material);
            var json = serialized.ToJson(reflector);
            Debug.Log($"[{nameof(TestSerializer)}] Result:\n{json}");

            var glossinessValue = 1.0f;
            var colorValue = new Color(1.0f, 0.0f, 0.0f, 0.5f);

            serialized.SetPropertyValue(reflector, "_Glossiness", glossinessValue);
            serialized.SetPropertyValue(reflector, "_Color", colorValue);

            var objMaterial = (object)material;
            var stringBuilder = new StringBuilder();
            reflector.TryPopulate(
                ref objMaterial,
                data: serialized,
                stringBuilder: stringBuilder,
                logger: McpPlugin.McpPlugin.Instance.Logger);

            Assert.AreEqual(glossinessValue, material.GetFloat("_Glossiness"), 0.001f, $"Material property '_Glossiness' should be {glossinessValue}.");
            Assert.AreEqual(colorValue, material.GetColor("_Color"), $"Material property '_Glossiness' should be {glossinessValue}.");

            var stringResult = stringBuilder.ToString();

            Assert.IsTrue(stringResult.Contains("[Success]"), $"String result should contain '[Success]'. Result: {stringResult}");
            Assert.IsFalse(stringResult.Contains("[Error]"), $"String result should not contain '[Error]'. Result: {stringResult}");

            yield return null;
        }


        [UnityTest]
        public IEnumerator SerializeMaterialArray()
        {
            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            var material1 = new Material(Shader.Find("Standard"));
            var material2 = new Material(Shader.Find("Standard"));

            var materials = new[] { material1, material2 };

            var serialized = reflector.Serialize(materials, logger: McpPlugin.McpPlugin.Instance.Logger);
            var json = serialized.ToJson(reflector);
            Debug.Log($"[{nameof(TestSerializer)}] Result:\n{json}");

            // var glossinessValue = 1.0f;
            // var colorValue = new Color(1.0f, 0.0f, 0.0f, 0.5f);

            // serialized.SetPropertyValue("_Glossiness", glossinessValue);
            // serialized.SetPropertyValue("_Color", colorValue);

            // var objMaterial = (object)material;
            // Serializer.Populate(ref objMaterial, serialized);

            // Assert.AreEqual(glossinessValue, material.GetFloat("_Glossiness"), 0.001f, $"Material property '_Glossiness' should be {glossinessValue}.");
            // Assert.AreEqual(colorValue, material.GetColor("_Color"), $"Material property '_Glossiness' should be {glossinessValue}.");

            yield return null;
        }

        void Test_Serialize_Deserialize<T>(T sourceObj)
        {
            var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

            var type = typeof(T);
            var serializedObj = reflector.Serialize(sourceObj, logger: McpPlugin.McpPlugin.Instance.Logger);
            var deserializedObj = reflector.Deserialize(serializedObj, logger: McpPlugin.McpPlugin.Instance.Logger);
            Debug.Log($"[{type.Name}] Source:\n```json\n{sourceObj.ToJson(reflector)}\n```");
            Debug.Log($"[{type.Name}] Serialized:\n```json\n{serializedObj.ToJson(reflector)}\n```");
            Debug.Log($"[{type.Name}] Deserialized:\n```json\n{deserializedObj.ToJson(reflector)}\n```");

            Assert.AreEqual(sourceObj?.GetType(), deserializedObj?.GetType(), $"Object type should be {sourceObj?.GetType().Name ?? "null"}.");

            foreach (var field in reflector.GetSerializableFields(type) ?? Enumerable.Empty<FieldInfo>())
            {
                try
                {
                    var sourceValue = field.GetValue(sourceObj);
                    var targetValue = field.GetValue(deserializedObj);
                    Assert.AreEqual(sourceValue, targetValue, $"Field '{field.Name}' should be equal. Expected: {sourceValue}, Actual: {targetValue}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error getting field '{type.Name}{field.Name}'\n{ex}");
                    throw ex;
                }
            }
            foreach (var prop in reflector.GetSerializableProperties(type) ?? Enumerable.Empty<PropertyInfo>())
            {
                try
                {
                    var sourceValue = prop.GetValue(sourceObj);
                    var targetValue = prop.GetValue(deserializedObj);
                    Assert.AreEqual(sourceValue, targetValue, $"Property '{prop.Name}' should be equal. Expected: {sourceValue}, Actual: {targetValue}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error getting property '{type.Name}{prop.Name}'\n{ex}");
                    throw ex;
                }
            }
        }

        [UnityTest]
        public IEnumerator Serialize_Deserialize()
        {
            Test_Serialize_Deserialize(100);
            Test_Serialize_Deserialize(true);
            Test_Serialize_Deserialize("hello world");
            Test_Serialize_Deserialize(new UnityEngine.Vector3(1, 2, 3));
            Test_Serialize_Deserialize(new UnityEngine.Color(1, 0.5f, 0, 1));
            Test_Serialize_Deserialize(new UnityEditor.Build.NamedBuildTarget());

            yield return null;
        }
    }
}
