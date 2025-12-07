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
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolGameObject : BaseTest
    {
        [UnityTest]
        public IEnumerator FindByInstanceId()
        {
            var child = new GameObject(GO_ParentName).AddChild(GO_Child1Name);
            Assert.IsNotNull(child, "Child GameObject should be created");

            var task = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = child!.GetInstanceID()
                });

            while (!task.IsCompleted) yield return null;
            var response = task.Result;

            Assert.AreEqual(ResponseStatus.Success, response.Status);
            Assert.IsNotNull(response.StructuredContent);
            var findResponse = DeserializeResponse(response.StructuredContent);
            Assert.IsNotNull(findResponse);
            Assert.IsNotNull(findResponse!.Hierarchy);
            var result = findResponse!.Hierarchy!.Print();
            Debug.Log($"DEBUG RESULT: {result}");
            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
        }

        [UnityTest]
        public IEnumerator FindByPath()
        {
            var child = new GameObject(GO_ParentName).AddChild(GO_Child1Name);
            var task = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    Path = $"{GO_ParentName}/{GO_Child1Name}"
                });

            while (!task.IsCompleted) yield return null;
            var response = task.Result;

            Assert.AreEqual(ResponseStatus.Success, response.Status);
            Assert.IsNotNull(response.StructuredContent);
            var findResponse = DeserializeResponse(response.StructuredContent);
            Assert.IsNotNull(findResponse);
            Assert.IsNotNull(findResponse!.Hierarchy);
            var result = findResponse!.Hierarchy!.Print();

            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
        }

        [UnityTest]
        public IEnumerator FindByName()
        {
            var child = new GameObject(GO_ParentName).AddChild(GO_Child1Name);
            var task = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    Name = GO_Child1Name
                });

            while (!task.IsCompleted) yield return null;
            var response = task.Result;

            Assert.AreEqual(ResponseStatus.Success, response.Status);
            Assert.IsNotNull(response.StructuredContent);
            var findResponse = DeserializeResponse(response.StructuredContent);
            Assert.IsNotNull(findResponse);
            Assert.IsNotNull(findResponse!.Hierarchy);
            var result = findResponse!.Hierarchy!.Print();

            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
        }

        [UnityTest]
        public IEnumerator FindByInstanceId_HierarchyDepth_1_DeepSerialization_True()
        {
            var go = new GameObject(GO_ParentName);
            go.AddChild(GO_Child1Name)!.AddComponent<SphereCollider>();
            go.AddChild(GO_Child2Name)!.AddComponent<SphereCollider>();
            go.AddComponent<SolarSystem>();
            yield return null;
            var task = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = go.GetInstanceID()
                },
                hierarchyDepth: 1,
                deepSerialization: true);

            while (!task.IsCompleted) yield return null;
            var response = task.Result;

            Assert.AreEqual(ResponseStatus.Success, response.Status);
            Assert.IsNotNull(response.StructuredContent);
            var findResponse = DeserializeResponse(response.StructuredContent);
            Assert.IsNotNull(findResponse);
            Assert.IsNotNull(findResponse!.Hierarchy);
            var result = findResponse!.Hierarchy!.Print();

            Assert.IsTrue(result.Contains(GO_ParentName), $"{GO_ParentName} should be found in the path");
            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
            Assert.IsTrue(result.Contains(GO_Child2Name), $"{GO_Child2Name} should be found in the path");
        }

        [UnityTest]
        public IEnumerator FindByInstanceId_DeepSerialization_False()
        {
            var go = new GameObject(GO_ParentName);
            go.AddComponent<SolarSystem>();
            yield return null;
            var task = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = go.GetInstanceID()
                },
                deepSerialization: false);

            while (!task.IsCompleted) yield return null;
            var response = task.Result;

            Assert.AreEqual(ResponseStatus.Success, response.Status);
            Assert.IsNotNull(response.StructuredContent);
            var findResponse = DeserializeResponse(response.StructuredContent);
            Assert.IsNotNull(findResponse);
            Assert.IsNotNull(findResponse!.Data);

            // Shallow serialization should produce less data than deep serialization
            Assert.IsTrue(response.StructuredContent!.ToJsonString().Length > 0, "Response should contain data");
        }

        [UnityTest]
        public IEnumerator FindByInstanceId_DeepSerialization_ProducesMoreDataThanShallow()
        {
            var go = new GameObject(GO_ParentName);
            go.AddComponent<SolarSystem>();
            yield return null;

            // Get deep serialization result
            var deepTask = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = go.GetInstanceID()
                },
                deepSerialization: true);

            while (!deepTask.IsCompleted) yield return null;
            var deepResponse = deepTask.Result;
            var deepJsonString = deepResponse.StructuredContent!.ToJsonString();

            // Get shallow serialization result
            var shallowTask = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = go.GetInstanceID()
                },
                deepSerialization: false);

            while (!shallowTask.IsCompleted) yield return null;
            var shallowResponse = shallowTask.Result;
            var shallowJsonString = shallowResponse.StructuredContent!.ToJsonString();

            // Deep serialization should produce more data than shallow
            Assert.Greater(deepJsonString.Length, shallowJsonString.Length,
                "Deep serialization should produce more data than shallow serialization");
        }

        ResponseData<ResponseCallTool> FindByJson(string json) => RunTool("GameObject_Find", json);

        [UnityTest]
        public IEnumerator FindByJson_HierarchyDepth_0_DeepSerialization_False()
        {
            var go = new GameObject(GO_ParentName);
            var json = $@"
            {{
              ""gameObjectRef"": {{
                ""instanceID"": {go.GetInstanceID()}
              }},
              ""hierarchyDepth"": 0,
              ""deepSerialization"": false
            }}";
            FindByJson(json);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FindByJson_HierarchyDepth_0_DeepSerialization_True()
        {
            var go = new GameObject(GO_ParentName);
            var json = $@"
            {{
              ""gameObjectRef"": {{
                ""instanceID"": {go.GetInstanceID()}
              }},
              ""hierarchyDepth"": 0,
              ""deepSerialization"": true
            }}";
            FindByJson(json);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FindByJson_HierarchyDepth_0_DefaultSerialization()
        {
            var go = new GameObject(GO_ParentName);
            var json = $@"
            {{
              ""gameObjectRef"": {{
                ""instanceID"": {go.GetInstanceID()}
              }},
              ""hierarchyDepth"": 0
            }}";
            FindByJson(json);
            yield return null;
        }

        private Tool_GameObject.GameObjectFindResponse? DeserializeResponse(System.Text.Json.Nodes.JsonNode? structuredContent)
        {
            if (structuredContent == null) return null;
            var jsonString = structuredContent.ToJsonString();
            var contentToDeserialize = jsonString;
            try
            {
                using var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.TryGetProperty(JsonSchema.Result, out var resultProp))
                {
                    contentToDeserialize = resultProp.GetRawText();
                }
            }
            catch { }

            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                PropertyNameCaseInsensitive = true
            };
            return System.Text.Json.JsonSerializer.Deserialize<Tool_GameObject.GameObjectFindResponse>(contentToDeserialize, options);
        }
    }
}
