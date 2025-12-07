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
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Utils
{
    public class CallToolExecutor : LazyNodeExecutor
    {
        public CallToolExecutor(MethodInfo toolMethod, string json, Reflector? reflector = null) : this(
            toolName: toolMethod.GetCustomAttribute<McpPluginToolAttribute>()?.Name
                ?? throw new ArgumentException("Tool method must have a McpPluginTool attribute."),
            json: json,
            reflector: reflector)
        {
            // none
        }

        public CallToolExecutor(string toolName, string json, Reflector? reflector = null) : base()
        {
            if (toolName == null) throw new ArgumentNullException(nameof(toolName));
            if (json == null) throw new ArgumentNullException(nameof(json));

            reflector ??= McpPlugin.McpPlugin.Instance!.McpManager.Reflector ??
                throw new ArgumentNullException(nameof(reflector), "Reflector cannot be null. Ensure McpPlugin is initialized before using this executor.");

            SetAction(() =>
            {
                Debug.Log($"{toolName} Started with JSON:\n{JsonTestUtils.Prettify(json)}");

                var parameters = reflector.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                var request = new RequestCallTool(toolName, parameters!);

                var task = McpPlugin.McpPlugin.Instance!.McpManager.ToolManager!.RunCallTool(request);
                var result = task.Result;

                Debug.Log($"{toolName} Completed");

                return result;
            });
        }
    }
}
