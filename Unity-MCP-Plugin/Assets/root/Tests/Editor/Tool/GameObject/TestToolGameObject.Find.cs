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
            var findResponse = JsonSerializer.Deserialize<Tool_GameObject.GameObjectFindResponse>(response.StructuredContent!.ToJsonString());
            Assert.IsNotNull(findResponse);
            Assert.IsNotNull(findResponse!.Hierarchy);
            var result = findResponse!.Hierarchy!.Print();

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
            var findResponse = JsonSerializer.Deserialize<Tool_GameObject.GameObjectFindResponse>(response.StructuredContent!.ToJsonString());
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
            var findResponse = JsonSerializer.Deserialize<Tool_GameObject.GameObjectFindResponse>(response.StructuredContent!.ToJsonString());
            Assert.IsNotNull(findResponse);
            Assert.IsNotNull(findResponse!.Hierarchy);
            var result = findResponse!.Hierarchy!.Print();

            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
        }

        [UnityTest]
        public IEnumerator FindByInstanceId_IncludeChildrenDepth_1_BriefData_False()
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
            var findResponse = JsonSerializer.Deserialize<Tool_GameObject.GameObjectFindResponse>(response.StructuredContent!.ToJsonString());
            Assert.IsNotNull(findResponse);
            Assert.IsNotNull(findResponse!.Hierarchy);
            var result = findResponse!.Hierarchy!.Print();

            Assert.IsTrue(result.Contains(GO_ParentName), $"{GO_ParentName} should be found in the path");
            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
            Assert.IsTrue(result.Contains(GO_Child2Name), $"{GO_Child2Name} should be found in the path");
        }

        ResponseData<ResponseCallTool> FindByJson(string json) => RunTool("GameObject_Find", json);

        [UnityTest]
        public IEnumerator FindByJson_IncludeChildrenDepth_0_BriefData_True()
        {
            // WORKS
            var go = new GameObject(GO_ParentName);
            var json = $@"
            {{
              ""gameObjectRef"": {{
                ""instanceID"": {go.GetInstanceID()}
              }},
              ""hierarchyDepth"": 0,
              ""briefData"": true,
              ""requestId"": ""test-req-id""
            }}";
            FindByJson(json);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FindByJson_IncludeChildrenDepth_0()
        {
            // FAILS
            var go = new GameObject(GO_ParentName);
            var json = $@"
            {{
              ""gameObjectRef"": {{
                ""instanceID"": {go.GetInstanceID()}
              }},
              ""hierarchyDepth"": 0,
              ""requestId"": ""test-req-id""
            }}";
            FindByJson(json);
            yield return null;
        }
    }
}
