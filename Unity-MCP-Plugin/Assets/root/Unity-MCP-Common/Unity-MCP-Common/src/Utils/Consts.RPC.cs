/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class Consts
    {
        public static class Hub
        {
            public const int DefaultPort = 8080;
            public const int MaxPort = 65535;
            public const string DefaultEndpoint = "http://localhost:8080";
            public const string RemoteApp = "/hub/mcp-server";
            public const int DefaultTimeoutMs = 10000;
        }

        public static partial class RPC
        {
            public static class Client
            {
                public const string RunCallTool = "/mcp/run-call-tool";
                public const string RunListTool = "/mcp/run-list-tool";
                public const string RunGetPrompt = "/mcp/run-get-prompt";
                public const string RunListPrompts = "/mcp/run-list-prompts";
                public const string RunResourceContent = "/mcp/run-resource-content";
                public const string RunListResources = "/mcp/run-list-resources";
                public const string RunListResourceTemplates = "/mcp/run-list-resource-templates";

                public const string ForceDisconnect = "force-disconnect";
            }

            public static class Server
            {
                public const string OnListToolsUpdated = "OnListToolsUpdated";
                public const string OnListPromptsUpdated = "OnListPromptsUpdated";
                public const string OnListResourcesUpdated = "OnListResourcesUpdated";
                public const string OnToolRequestCompleted = "OnToolRequestCompleted";
                public const string OnVersionHandshake = "OnVersionHandshake";
            }
        }
    }
}
