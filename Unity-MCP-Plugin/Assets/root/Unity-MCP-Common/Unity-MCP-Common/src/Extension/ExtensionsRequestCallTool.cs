/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System.Collections.Generic;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class ExtensionsRequestCallTool
    {
        public static IRequestCallTool SetName(this IRequestCallTool data, string name)
        {
            data.Name = name;
            return data;
        }
        public static IRequestCallTool SetOrAddParameter(this IRequestCallTool data, string name, object? value)
        {
            data.Arguments ??= value == null
                ? new Dictionary<string, JsonElement>()
                : new Dictionary<string, JsonElement>() { [name] = value.ToJsonElement(McpPlugin.Instance?.McpRunner.Reflector) };
            return data;
        }
        // public static IRequestData BuildRequest(this IRequestTool data)
        //     => new RequestData(data as RequestTool ?? throw new System.InvalidOperationException("CommandData is null"));
    }
}
