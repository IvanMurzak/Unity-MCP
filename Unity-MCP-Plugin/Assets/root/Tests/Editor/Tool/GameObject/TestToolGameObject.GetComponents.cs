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
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolGameObject : BaseTest
    {
        [UnityTest]
        public IEnumerator GetComponentsStringified()
        {
            var child = new GameObject(GO_ParentName).AddChild(GO_Child1Name);
            Assert.IsNotNull(child, "Child GameObject should be created");

            var json = $@"
            {{
              ""gameObjectRef"": ""{{ \""instanceID\"": {child!.GetInstanceID()} }}"",
              ""briefData"": false,
              ""requestId"": ""test-req-id""
            }}";
            Debug.Log($"Stringified request JSON:\n{json}");

            var parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            Assert.IsNotNull(parameters, "Parameters should be deserialized");

            var toolName = typeof(Tool_GameObject)
                .GetMethod(nameof(Tool_GameObject.Find))
                .GetCustomAttribute<McpPluginToolAttribute>()
                .Name;

            var task = McpPlugin.McpPlugin.Instance!.McpManager.ToolManager!.RunCallTool(new RequestCallTool(toolName, parameters!));
            while (!task.IsCompleted) yield return null;

            var result = task.Result.Message;
            Assert.IsNotNull(result);
            Assert.IsTrue(result!.Contains(GO_Child1Name), $"Result should contain {GO_Child1Name}");

            // The message is just success status now. The data is in Value.
            // But RunCallTool returns ResponseCallTool which wraps the result.
            // We can check if the result is not error.
            Assert.IsFalse(task.Result.Status == ResponseStatus.Error);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetComponents()
        {
            var child = new GameObject(GO_ParentName).AddChild(GO_Child1Name);
            Assert.IsNotNull(child, "Child GameObject should be created");

            var meshRenderer = child!.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

            var response = new Tool_GameObject().Find(
                gameObjectRef: new Runtime.Data.GameObjectRef
                {
                    InstanceID = child.GetInstanceID()
                },
                includeHierarchy: true,
                includeComponents: true);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response!.Hierarchy);

            Assert.IsTrue(response!.Hierarchy!.Print().Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
            yield return null;
        }
    }
}
