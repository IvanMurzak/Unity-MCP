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
using System.Collections.Generic;
using System.Text.Json;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;

namespace com.IvanMurzak.Unity.MCP.Runtime.Extensions
{
    public static class ExtensionsRequestCallTool
    {
        public static RequestCallTool SetName(this RequestCallTool data, string name)
        {
            data.Name = name;
            return data;
        }
        public static RequestCallTool SetOrAddParameter(this RequestCallTool data, string name, object? value)
        {
            data.Arguments ??= value == null
                ? new Dictionary<string, JsonElement>()
                : new Dictionary<string, JsonElement>() { [name] = value.ToJsonElement(McpPlugin.McpPlugin.Instance?.McpManager.Reflector) };
            return data;
        }
        // public static IRequestData BuildRequest(this IRequestTool data)
        //     => new RequestData(data as RequestTool ?? throw new System.InvalidOperationException("CommandData is null"));
    }
}
