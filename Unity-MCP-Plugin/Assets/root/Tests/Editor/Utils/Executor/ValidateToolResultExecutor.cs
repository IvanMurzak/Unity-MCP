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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Common;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class ValidateToolResultExecutor : LazyNodeExecutor
    {
        public ValidateToolResultExecutor(Reflector? reflector = null) : base()
        {
            reflector ??= McpPlugin.Instance!.McpRunner.Reflector ??
                throw new ArgumentNullException(nameof(reflector), "Reflector cannot be null. Ensure McpPlugin is initialized before using this executor.");

            SetAction<IResponseData<ResponseCallTool>, IResponseData<ResponseCallTool>>(result =>
            {
                var jsonResult = result.ToJson(reflector);
                Debug.Log($"Tool execution result:\n{jsonResult}");

                Assert.IsFalse(result.IsError);

                Assert.IsNotNull(result.Message);
                Assert.IsFalse(result.Message!.Contains("[Error]"), $"Tool call failed with error: {result.Message}");

                Assert.IsNotNull(result.Value);
                Assert.IsFalse(result.Value!.IsError, $"Tool call failed");

                Assert.IsFalse(jsonResult.Contains("[Error]"), $"Tool call failed with error in JSON: {jsonResult}");
                Assert.IsFalse(jsonResult.Contains("[Warning]"), $"Tool call contains warnings in JSON: {jsonResult}");

                return result;
            });
        }
    }
}
