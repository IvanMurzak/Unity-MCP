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
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolGameObject : BaseTest
    {

        [Test]
        public void FindByInstanceId()
        {
            var child = new GameObject(GO_ParentName).AddChild(GO_Child1Name);
            Assert.IsNotNull(child, "Child GameObject should be created");

            var response = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = child!.GetInstanceID()
                },
                includeHierarchy: true);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response!.Hierarchy);
            var result = response!.Hierarchy!.Print();
            Debug.Log($"DEBUG RESULT: {result}");
            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
        }

        [Test]
        public void FindByPath()
        {
            var child = new GameObject(GO_ParentName).AddChild(GO_Child1Name);
            var response = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    Path = $"{GO_ParentName}/{GO_Child1Name}"
                });

            Assert.IsNotNull(response);
            Assert.IsNotNull(response!.Hierarchy);
            var result = response!.Hierarchy!.Print();

            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
        }

        [Test]
        public void FindByName()
        {
            var child = new GameObject(GO_ParentName).AddChild(GO_Child1Name);
            var response = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    Name = GO_Child1Name
                });

            Assert.IsNotNull(response);
            Assert.IsNotNull(response!.Hierarchy);
            var result = response!.Hierarchy!.Print();

            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
        }

        [Test]
        public void FindByInstanceId_HierarchyDepth_1_DeepSerialization_True()
        {
            var go = new GameObject(GO_ParentName);
            go.AddChild(GO_Child1Name)!.AddComponent<SphereCollider>();
            go.AddChild(GO_Child2Name)!.AddComponent<SphereCollider>();
            go.AddComponent<SolarSystem>();

            var response = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = go.GetInstanceID()
                },
                hierarchyDepth: 1,
                deepSerialization: true);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response!.Hierarchy);
            var result = response!.Hierarchy!.Print();

            Assert.IsTrue(result.Contains(GO_ParentName), $"{GO_ParentName} should be found in the path");
            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
            Assert.IsTrue(result.Contains(GO_Child2Name), $"{GO_Child2Name} should be found in the path");
        }

        [Test]
        public void FindByInstanceId_DeepSerialization_False()
        {
            var go = new GameObject(GO_ParentName);
            go.AddComponent<SolarSystem>();

            var response = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = go.GetInstanceID()
                },
                includeData: true,
                deepSerialization: false);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response!.Data);

            // Shallow serialization should produce less data than deep serialization
            Assert.IsNotNull(response.Data, "Response should contain data");
        }

        [Test]
        public void FindByInstanceId_DeepSerialization_ProducesMoreDataThanShallow()
        {
            var reflector = UnityMcpPlugin.Instance.McpPluginInstance!.McpManager.Reflector;
            var go = new GameObject(GO_ParentName);
            go.AddComponent<SolarSystem>();

            // Get deep serialization result
            var deepResponse = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = go.GetInstanceID()
                },
                deepSerialization: true);

            var deepJsonString = reflector.JsonSerializer.Serialize(reflector.Serialize(deepResponse));

            // Get shallow serialization result
            var shallowResponse = new Tool_GameObject().Find(
                gameObjectRef: new GameObjectRef
                {
                    InstanceID = go.GetInstanceID()
                },
                deepSerialization: false);

            var shallowJsonString = reflector.JsonSerializer.Serialize(reflector.Serialize(shallowResponse));

            // Deep serialization should produce more data than shallow
            Assert.Greater(deepJsonString.Length, shallowJsonString.Length,
                "Deep serialization should produce more data than shallow serialization");
        }

        ResponseData<ResponseCallTool> FindByJson(string json) => RunTool("gameobject-find", json);

        [Test]
        public void FindByInstanceId_ActiveGameObject()
        {
            var gameObjectEx = new CreateGameObjectExecutor(GO_ParentName, isActive: true);

            gameObjectEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should be created");
                    Assert.IsTrue(gameObjectEx.GameObject!.activeSelf, "GameObject should be active");
                })
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Find)),
                    jsonProvider: () => $@"{{
                        ""gameObjectRef"": {{
                            ""instanceID"": {gameObjectEx.GameObject!.GetInstanceID()}
                        }}
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .Execute();
        }

        [Test]
        public void FindByInstanceId_InactiveGameObject()
        {
            var gameObjectEx = new CreateGameObjectExecutor(GO_ParentName, isActive: false);

            gameObjectEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should be created");
                    Assert.IsFalse(gameObjectEx.GameObject!.activeSelf, "GameObject should be inactive");
                })
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Find)),
                    jsonProvider: () => $@"{{
                        ""gameObjectRef"": {{
                            ""instanceID"": {gameObjectEx.GameObject!.GetInstanceID()}
                        }}
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .Execute();
        }

        [Test]
        public void FindByName_ActiveGameObject()
        {
            var gameObjectEx = new CreateGameObjectExecutor(GO_ParentName, isActive: true);

            gameObjectEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should be created");
                    Assert.IsTrue(gameObjectEx.GameObject!.activeSelf, "GameObject should be active");
                })
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Find)),
                    jsonProvider: () => $@"{{
                        ""gameObjectRef"": {{
                            ""name"": ""{GO_ParentName}""
                        }}
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .Execute();
        }

        [Test]
        public void FindByName_InactiveGameObject()
        {
            var gameObjectEx = new CreateGameObjectExecutor(GO_ParentName, isActive: false);

            gameObjectEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should be created");
                    Assert.IsFalse(gameObjectEx.GameObject!.activeSelf, "GameObject should be inactive");
                })
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Find)),
                    jsonProvider: () => $@"{{
                        ""gameObjectRef"": {{
                            ""name"": ""{GO_ParentName}""
                        }}
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .Execute();
        }

        [Test]
        public void FindByPath_ActiveGameObject_NestedHierarchy()
        {
            GameObject? child1 = null;
            GameObject? child2 = null;
            var rootEx = new CreateGameObjectExecutor(GO_ParentName, isActive: true);

            rootEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(rootEx.GameObject, "Root GameObject should be created");
                    Assert.IsTrue(rootEx.GameObject!.activeSelf, "Root GameObject should be active");

                    // Create nested hierarchy: root -> child1 -> child2
                    child1 = new GameObject(GO_Child1Name);
                    child1.transform.SetParent(rootEx.GameObject.transform);

                    child2 = new GameObject(GO_Child2Name);
                    child2.transform.SetParent(child1.transform);
                    child2.SetActive(true);

                    Assert.IsTrue(child2.activeSelf, "Child2 GameObject should be active");
                })
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Find)),
                    jsonProvider: () => $@"{{
                        ""gameObjectRef"": {{
                            ""path"": ""{GO_ParentName}/{GO_Child1Name}/{GO_Child2Name}""
                        }}
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .Execute();
        }

        [Test]
        public void FindByPath_InactiveGameObject_NestedHierarchy()
        {
            GameObject? child1 = null;
            GameObject? child2 = null;
            var rootEx = new CreateGameObjectExecutor(GO_ParentName, isActive: true);

            rootEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(rootEx.GameObject, "Root GameObject should be created");

                    // Create nested hierarchy: root -> child1 -> child2 (inactive)
                    child1 = new GameObject(GO_Child1Name);
                    child1.transform.SetParent(rootEx.GameObject!.transform);

                    child2 = new GameObject(GO_Child2Name);
                    child2.transform.SetParent(child1.transform);
                    child2.SetActive(false);

                    Assert.IsFalse(child2.activeSelf, "Child2 GameObject should be inactive");
                })
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Find)),
                    jsonProvider: () => $@"{{
                        ""gameObjectRef"": {{
                            ""path"": ""{GO_ParentName}/{GO_Child1Name}/{GO_Child2Name}""
                        }}
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .Execute();
        }

        [Test]
        public void FindByInstanceId_NonExistent_ThrowsException()
        {
            var nonExistentInstanceId = 999999999;

            var ex = Assert.Throws<System.Exception>(() =>
            {
                new Tool_GameObject().Find(
                    gameObjectRef: new GameObjectRef
                    {
                        InstanceID = nonExistentInstanceId
                    });
            });

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("Not found"), $"Exception message should contain 'Not found'. Actual: {ex.Message}");
        }

        [Test]
        public void FindByName_NonExistent_ThrowsException()
        {
            var nonExistentName = "NonExistentGameObject_12345";

            var ex = Assert.Throws<System.Exception>(() =>
            {
                new Tool_GameObject().Find(
                    gameObjectRef: new GameObjectRef
                    {
                        Name = nonExistentName
                    });
            });

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("Not found"), $"Exception message should contain 'Not found'. Actual: {ex.Message}");
        }

        [Test]
        public void FindByPath_NonExistent_ThrowsException()
        {
            var nonExistentPath = "NonExistent/Path/To/GameObject";

            var ex = Assert.Throws<System.Exception>(() =>
            {
                new Tool_GameObject().Find(
                    gameObjectRef: new GameObjectRef
                    {
                        Path = nonExistentPath
                    });
            });

            Assert.IsNotNull(ex);
            Assert.IsTrue(ex.Message.Contains("Not found"), $"Exception message should contain 'Not found'. Actual: {ex.Message}");
        }

        [Test]
        public void FindByJson_HierarchyDepth_0_DeepSerialization_False()
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
        }

        [Test]
        public void FindByJson_HierarchyDepth_0_DeepSerialization_True()
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
        }

        [Test]
        public void FindByJson_HierarchyDepth_0_DefaultSerialization()
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
        }

        private GameObjectData? DeserializeResponse(System.Text.Json.Nodes.JsonNode? structuredContent)
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
            return System.Text.Json.JsonSerializer.Deserialize<GameObjectData>(contentToDeserialize, options);
        }
    }
}
