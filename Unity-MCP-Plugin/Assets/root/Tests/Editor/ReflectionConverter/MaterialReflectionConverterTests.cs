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
using System.Linq.Expressions;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class MaterialReflectionConverterTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Debug.Log($"[{nameof(DemoTest)}] SetUp");
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"[{nameof(DemoTest)}] TearDown");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Always_Valid_Test()
        {
            var goName = "DemoGO";
            var goRef = new Runtime.Data.GameObjectRef() { Name = goName };
            var materialEx = new CreateMaterialExecutor(
                materialName: "TestMaterial__.mat",
                shaderName: "Standard",
                "Assets", "Unity-MCP-Test", "Materials"
            );

            materialEx
                .AddChild(new CreateGameObjectExecutor(goName))
                .AddChild(new AddComponentExecutor<MeshRenderer>(goRef))
                .AddChild(new CallToolExecutor(
                    toolMethod: typeof(Tool_GameObject).GetMethod(nameof(Tool_GameObject.Modify)),
                    json: JsonTestUtils.Fill(@"{
                        ""gameObjectRefs"": ""{gameObjectRefs}"",
                        ""gameObjectDiffs"": ""{gameObjectDiffs}""
                    }",
                    new System.Collections.Generic.Dictionary<string, object?>
                    {
                        { "{gameObjectRefs}", new Runtime.Data.GameObjectRef[] { goRef } },
                        { "{gameObjectDiffs}", new SerializedMemberList() }
                    }))
                )
                .AddChild(new ValidateToolResultExecutor())
                .Execute();
            yield return null;
        }
    }
}
