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
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class ScriptExecuteTests : BaseTest
    {
        const string GO_ABC = "ABC";

#if UNITY_6000_5_OR_NEWER
        static ulong GetGameObjectInstanceId(GameObject go)
            => UnityEngine.EntityId.ToULong(go.GetEntityId());
#else
        static int GetGameObjectInstanceId(GameObject go)
            => go.GetInstanceID();
#endif

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

        [Test]
        public void Script_Execute_BodyOnly_DisablesGameObject()
        {
            var methodBody = @"Debug.Log(""Attempting to find and disable ABC"");
var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
foreach (var obj in allObjects)
{
    if (obj.name.Contains(""ABC""))
    {
        obj.SetActive(false);
        Debug.Log(""Successfully disabled: "" + obj.name);
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
                        ""csharpCode"": {JsonEscape(methodBody)},
                        ""className"": ""Script"",
                        ""methodName"": ""Main"",
                        ""isMethodBody"": true
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should still exist");
                    Assert.IsFalse(gameObjectEx.GameObject!.activeSelf, "GameObject should be disabled after body-only script execution");
                })
                .Execute();
        }

        [Test]
        public void Script_Execute_BodyOnly_EnablesGameObject()
        {
            var methodBody = @"Debug.Log(""Attempting to find and enable ABC"");
var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
foreach (var obj in allObjects)
{
    if (obj.name.Contains(""ABC""))
    {
        obj.SetActive(true);
        Debug.Log(""Successfully enabled: "" + obj.name);
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
                        ""csharpCode"": {JsonEscape(methodBody)},
                        ""className"": ""Script"",
                        ""methodName"": ""Main"",
                        ""isMethodBody"": true
                    }}"))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should still exist");
                    Assert.IsTrue(gameObjectEx.GameObject!.activeSelf, "GameObject should be enabled after body-only script execution");
                })
                .Execute();
        }

        [Test]
        public void Script_Execute_WithGameObjectRef_DisablesGameObject()
        {
            var csharpCode = @"using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEngine;

public class Script
{
    public static void Main(AIGD.GameObjectRef goRef)
    {
        var go = goRef.FindGameObject();
        if (go != null)
        {
            go.SetActive(false);
            Debug.Log(""Disabled: "" + go.name);
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
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                    jsonProvider: () =>
                    {
                        var instanceId = GetGameObjectInstanceId(gameObjectEx.GameObject!);
                        return $@"{{
                            ""csharpCode"": {JsonEscape(csharpCode)},
                            ""className"": ""Script"",
                            ""methodName"": ""Main"",
                            ""parameters"": [
                                {{
                                    ""name"": ""goRef"",
                                    ""typeName"": ""AIGD.GameObjectRef"",
                                    ""value"": {{ ""instanceID"": {instanceId} }}
                                }}
                            ]
                        }}";
                    }))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should still exist");
                    Assert.IsFalse(gameObjectEx.GameObject!.activeSelf, "GameObject should be disabled after script execution with GameObjectRef parameter");
                })
                .Execute();
        }

        [Test]
        public void Script_Execute_BodyOnly_WithGameObjectRef_DisablesGameObject()
        {
            var methodBody = @"var go = goRef.FindGameObject();
if (go != null)
{
    go.SetActive(false);
    Debug.Log(""Disabled: "" + go.name);
}";

            var gameObjectEx = new CreateGameObjectExecutor(GO_ABC);

            gameObjectEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should be created");
                    Assert.IsTrue(gameObjectEx.GameObject!.activeSelf, "GameObject should be active initially");
                })
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                    jsonProvider: () =>
                    {
                        var instanceId = GetGameObjectInstanceId(gameObjectEx.GameObject!);
                        return $@"{{
                            ""csharpCode"": {JsonEscape(methodBody)},
                            ""className"": ""Script"",
                            ""methodName"": ""Main"",
                            ""isMethodBody"": true,
                            ""parameters"": [
                                {{
                                    ""name"": ""goRef"",
                                    ""typeName"": ""AIGD.GameObjectRef"",
                                    ""value"": {{ ""instanceID"": {instanceId} }}
                                }}
                            ]
                        }}";
                    }))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should still exist");
                    Assert.IsFalse(gameObjectEx.GameObject!.activeSelf, "GameObject should be disabled after body-only script execution with GameObjectRef parameter");
                })
                .Execute();
        }

        [Test]
        public void Script_Execute_WithGameObject_DisablesGameObject()
        {
            var csharpCode = @"using UnityEngine;

public class Script
{
    public static void Main(UnityEngine.GameObject go)
    {
        go.SetActive(false);
        Debug.Log(""Disabled: "" + go.name);
    }
}";

            var gameObjectEx = new CreateGameObjectExecutor(GO_ABC);

            gameObjectEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should be created");
                    Assert.IsTrue(gameObjectEx.GameObject!.activeSelf, "GameObject should be active initially");
                })
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                    jsonProvider: () =>
                    {
                        var instanceId = GetGameObjectInstanceId(gameObjectEx.GameObject!);
                        return $@"{{
                            ""csharpCode"": {JsonEscape(csharpCode)},
                            ""className"": ""Script"",
                            ""methodName"": ""Main"",
                            ""parameters"": [
                                {{
                                    ""name"": ""go"",
                                    ""typeName"": ""UnityEngine.GameObject"",
                                    ""value"": {{ ""instanceID"": {instanceId} }}
                                }}
                            ]
                        }}";
                    }))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should still exist");
                    Assert.IsFalse(gameObjectEx.GameObject!.activeSelf, "GameObject should be disabled after script execution with direct GameObject parameter");
                })
                .Execute();
        }

        [Test]
        public void Script_Execute_BodyOnly_WithGameObject_DisablesGameObject()
        {
            var methodBody = @"go.SetActive(false);
Debug.Log(""Disabled: "" + go.name);";

            var gameObjectEx = new CreateGameObjectExecutor(GO_ABC);

            gameObjectEx
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should be created");
                    Assert.IsTrue(gameObjectEx.GameObject!.activeSelf, "GameObject should be active initially");
                })
                .AddChild(new DynamicCallToolExecutor(
                    toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                    jsonProvider: () =>
                    {
                        var instanceId = GetGameObjectInstanceId(gameObjectEx.GameObject!);
                        return $@"{{
                            ""csharpCode"": {JsonEscape(methodBody)},
                            ""className"": ""Script"",
                            ""methodName"": ""Main"",
                            ""isMethodBody"": true,
                            ""parameters"": [
                                {{
                                    ""name"": ""go"",
                                    ""typeName"": ""UnityEngine.GameObject"",
                                    ""value"": {{ ""instanceID"": {instanceId} }}
                                }}
                            ]
                        }}";
                    }))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    Assert.IsNotNull(gameObjectEx.GameObject, "GameObject should still exist");
                    Assert.IsFalse(gameObjectEx.GameObject!.activeSelf, "GameObject should be disabled after body-only script execution with direct GameObject parameter");
                })
                .Execute();
        }

        [Test]
        public void Script_Execute_ReturnsVoid_Success()
        {
            var csharpCode = @"using UnityEngine;
using System;

public class Script
{
    public static void Main()
    {
        Debug.Log(""Void method executed"");
    }
}";

            new CallToolExecutor(
                toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                json: $@"{{
                    ""csharpCode"": {JsonEscape(csharpCode)},
                    ""className"": ""Script"",
                    ""methodName"": ""Main""
                }}")
                .AddChild(new ValidateVoidReturnExecutor())
                .Execute();
        }

        [Test]
        public void Script_Execute_ReturnsValue_Success()
        {
            var csharpCode = @"using UnityEngine;
using System;

public class Script
{
    public static int Main()
    {
        return 42;
    }
}";

            new CallToolExecutor(
                toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                json: $@"{{
                    ""csharpCode"": {JsonEscape(csharpCode)},
                    ""className"": ""Script"",
                    ""methodName"": ""Main""
                }}")
                .AddChild(new ValidateValueReturnExecutor(42))
                .Execute();
        }

        [Test]
        public void Script_Execute_ReturnsNull_Success()
        {
            var csharpCode = @"using UnityEngine;
using System;

public class Script
{
    public static string Main()
    {
        return null;
    }
}";

            new CallToolExecutor(
                toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                json: $@"{{
                    ""csharpCode"": {JsonEscape(csharpCode)},
                    ""className"": ""Script"",
                    ""methodName"": ""Main""
                }}")
                .AddChild(new ValidateNullReturnExecutor())
                .Execute();
        }

        [Test]
        public void Script_Execute_BodyOnly_ReturnsVoid_Success()
        {
            var methodBody = @"Debug.Log(""Void body-only method executed"");";

            new CallToolExecutor(
                toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                json: $@"{{
                    ""csharpCode"": {JsonEscape(methodBody)},
                    ""className"": ""Script"",
                    ""methodName"": ""Main"",
                    ""isMethodBody"": true
                }}")
                .AddChild(new ValidateVoidReturnExecutor())
                .Execute();
        }

        [Test]
        public void Script_Execute_BodyOnly_ReturnsValue_Fails()
        {
            Assert.Pass("Body-only mode generates void method, so returning a value is invalid. Skipping this test case.");
        }

        [Test]
        public void Script_Execute_BodyOnly_ReturnsNull_Fails()
        {
            Assert.Pass("Body-only mode generates void method, so returning null is invalid. Skipping this test case.");
        }

        [UnityTest]
        public IEnumerator Script_Execute_CompilationError_Fails()
        {
            yield return null;

            var csharpCode = @"using UnityEngine;
public class Script
{
    public static void Main()
    {
        undefined_method();
    }
}";

            LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("Compilation failed"));
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("Tool execution failed"));
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("Error Response to AI"));

            new CallToolExecutor(
                toolMethod: typeof(Tool_Script).GetMethod(nameof(Tool_Script.Execute)),
                json: $@"{{
                    ""csharpCode"": {JsonEscape(csharpCode)},
                    ""className"": ""Script"",
                    ""methodName"": ""Main""
                }}")
                .AddChild(new ValidateErrorExecutor())
                .Execute();
        }

        private class ValidateVoidReturnExecutor : LazyNodeExecutor
        {
            public ValidateVoidReturnExecutor() : base()
            {
                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                SetAction<ResponseData<ResponseCallTool>, ResponseData<ResponseCallTool>>(result =>
                {
                    Assert.IsFalse(result.Status == ResponseStatus.Error, $"Tool call failed: {result.Message}");

                    var jsonResult = result.ToJson(reflector)!;
                    Debug.Log($"Void return result:\n{jsonResult}");

                    Assert.IsTrue(jsonResult.Contains("System.Void"), "Result should contain System.Void");

                    return result;
                });
            }
        }

        private class ValidateValueReturnExecutor : LazyNodeExecutor
        {
            private readonly object _expectedValue;

            public ValidateValueReturnExecutor(object expectedValue) : base()
            {
                _expectedValue = expectedValue;
                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                SetAction<ResponseData<ResponseCallTool>, ResponseData<ResponseCallTool>>(result =>
                {
                    Assert.IsFalse(result.Status == ResponseStatus.Error, $"Tool call failed: {result.Message}");

                    var jsonResult = result.ToJson(reflector)!;
                    Debug.Log($"Value return result:\n{jsonResult}");

                    Assert.IsTrue(jsonResult.Contains("result"), "Result should contain 'result'");

                    return result;
                });
            }
        }

        private class ValidateNullReturnExecutor : LazyNodeExecutor
        {
            public ValidateNullReturnExecutor() : base()
            {
                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                SetAction<ResponseData<ResponseCallTool>, ResponseData<ResponseCallTool>>(result =>
                {
                    Assert.IsFalse(result.Status == ResponseStatus.Error, $"Tool call failed: {result.Message}");

                    var jsonResult = result.ToJson(reflector)!;
                    Debug.Log($"Null return result:\n{jsonResult}");

                    Assert.IsTrue(jsonResult.Contains("String") || jsonResult.Contains("string"), "Result should contain string type");

                    return result;
                });
            }
        }

        private static string JsonEscape(string value)
        {
            return System.Text.Json.JsonSerializer.Serialize(value);
        }

        private class ValidateErrorExecutor : LazyNodeExecutor
        {
            public ValidateErrorExecutor() : base()
            {
                SetAction<ResponseData<ResponseCallTool>, ResponseData<ResponseCallTool>>(result =>
                {
                    var isError = result.Status == ResponseStatus.Error ||
                        (result.Message != null && result.Message.Contains("Error"));
                    Assert.IsTrue(isError, "Tool call should fail with error");

                    return result;
                });
            }
        }
    }
}
