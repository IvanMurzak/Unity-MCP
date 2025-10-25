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
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public static class ResponseListToolExtensions
    {
        public static ResponseListTool[] Log(this ResponseListTool[] response, ILogger logger, Exception? ex = null)
        {
            if (!logger.IsEnabled(LogLevel.Information))
                return response;

            foreach (var item in response)
                logger.LogInformation(ex, $"Tool: {item.Name}:\n{item.ToJsonOrEmptyJsonObject(McpPlugin.Instance?.McpRunner.Reflector)}");

            return response;
        }

        public static IResponseData<ResponseListTool[]> Pack(this ResponseListTool[] response, string requestId, string? message = null)
            => ResponseData<ResponseListTool[]>.Success(requestId, message ?? "List Tool execution completed.")
                .SetData(response);
    }
}
