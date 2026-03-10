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
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ScriptExecuteTests : BaseTest
    {
        const string GO_ABC = "ABC";

        [Test]
        public void Script_Execute_DisablesGameObject()
        {
            var csharpCode = @"using UnityEngine;
using System;

public class Script
{
    public static void Main()
    {
        Debug.Log(""Attempting to find and disable ABC"");
        var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains(""ABC""))
            {
                obj.SetActive(false);
                Debug.Log(""Successfully disabled: "" + obj.name);
            }
        }
    }
}";

            var gameObjectEx = new CreateGameObjectExecutor(GO_ABC);

            gameObjectEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should be created");
                    Assert.IsTrue(gameObjectEx.GameObject!.activeSelf, "GameObject should be active initially");
                })
                .AddChild(new CallToolExecutor(
                    toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                    json: $@"{{
                        ""csharpCode"": {JsonEscape(csharpCode)},
                        ""className"": ""Script"",
                        ""methodName"": ""Main""
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should still exist");
                    Assert.IsFalse(gameObjectEx.GameObject!.activeSelf, "GameObject should be disabled after script execution");
                })
                .Execute();
        }

        [Test]
        public void Script_Execute_EnablesGameObject()
        {
            var csharpCode = @"using UnityEngine;
using System;

public class Script
{
    public static void Main()
    {
        Debug.Log(""Attempting to find and enable ABC"");
        var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains(""ABC""))
            {
                obj.SetActive(true);
                Debug.Log(""Successfully enabled: "" + obj.name);
            }
        }
    }
}";

            var gameObjectEx = new CreateGameObjectExecutor(GO_ABC, isActive: false);

            gameObjectEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should be created");
                    Assert.IsFalse(gameObjectEx.GameObject!.activeSelf, "GameObject should be inactive initially");
                })
                .AddChild(new CallToolExecutor(
                    toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                    json: $@"{{
                        ""csharpCode"": {JsonEscape(csharpCode)},
                        ""className"": ""Script"",
                        ""methodName"": ""Main""
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should still exist");
                    Assert.IsTrue(gameObjectEx.GameObject!.activeSelf, "GameObject should be enabled after script execution");
                })
                .Execute();
        }

        private static string JsonEscape(string value)
        {
            return System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}
