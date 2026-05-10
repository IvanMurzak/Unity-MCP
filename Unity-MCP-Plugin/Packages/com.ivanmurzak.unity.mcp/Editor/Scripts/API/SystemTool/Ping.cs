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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Ping
    {
        public const string PingToolId = "ping";
        [McpPluginTool
        (
            PingToolId,
            Title = "Ping",
            ReadOnlyHint = true,
            IdempotentHint = true,
            ToolType = McpToolType.System
        )]
        [Description("Lightweight readiness probe. Returns the input message or 'pong' if omitted.")]
        public string Ping
        (
            [Description("Optional message to echo back.")]
            string? message = null
        )
        {
            return message ?? "pong";
        }
    }
}
