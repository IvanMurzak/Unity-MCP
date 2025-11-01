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
using System.Linq;
using System.Reflection;
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Utils;
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

            var json = $@"
            {{
              ""gameObjectRef"": ""{{ \""instanceID\"": {child.GetInstanceID()} }}"",
              ""briefData"": false
            }}";
            Debug.Log($"Stringified request JSON:\n{json}");

            var parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            var toolName = typeof(Tool_GameObject)
                .GetMethod(nameof(Tool_GameObject.Find))
                .GetCustomAttribute<McpPluginToolAttribute>()
                .Name;

            var task = McpPlugin.Instance.McpRunner.RunCallTool(new RequestCallTool(toolName, parameters));
            if (!task.IsCompleted)
                yield return null;

            var result = task.Result.Message;
            ResultValidation(result);

            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetComponents()
        {
            var child = new GameObject(GO_ParentName).AddChild(GO_Child1Name);

            var meshRenderer = child.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

            var result = new Tool_GameObject().Find(
                gameObjectRef: new Common.Model.Unity.GameObjectRef
                {
                    InstanceID = child.GetInstanceID()
                },
                briefData: false);
            ResultValidation(result);

            Assert.IsTrue(result.Contains(GO_Child1Name), $"{GO_Child1Name} should be found in the path");
            yield return null;
        }
    }
}
