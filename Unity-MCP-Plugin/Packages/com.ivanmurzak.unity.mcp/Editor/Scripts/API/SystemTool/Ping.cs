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
        [McpPluginSkillDescription("Lightweight readiness probe. Returns the input `message` echoed back, or `'pong'` " +
            "when omitted. Useful for CLI health checks and SignalR connectivity smoke tests.")]
        [McpPluginSkillBody("Lightweight readiness probe. Returns the input message or 'pong' if omitted.\n\n" +
            "## Inputs\n\n" +
            "- `message` (optional) — when present, echoed back verbatim.\n\n" +
            "## Behavior\n\n" +
            "No I/O, no Unity API calls — pure echo. Ideal for measuring round-trip latency or confirming the MCP " +
            "transport is alive before invoking a heavier tool.")]
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
