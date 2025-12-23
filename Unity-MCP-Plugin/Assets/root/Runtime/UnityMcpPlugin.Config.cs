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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin
    {
        protected readonly object configMutex = new();

        protected UnityConnectionConfig unityConnectionConfig;

        public class UnityConnectionConfig : ConnectionConfig
        {
            public static string DefaultHost => $"http://localhost:{GeneratePortFromDirectory()}";

            public static Dictionary<string, bool> DefaultTools => new();
            public static Dictionary<string, bool> DefaultPrompts => new();
            public static Dictionary<string, bool> DefaultResources => new();

            public LogLevel LogLevel { get; set; } = LogLevel.Warning;
            public Dictionary<string, bool> Tools { get; set; } = new();
            public Dictionary<string, bool> Prompts { get; set; } = new();
            public Dictionary<string, bool> Resources { get; set; } = new();

            public UnityConnectionConfig()
            {
                SetDefault();
            }

            public UnityConnectionConfig SetDefault()
            {
                Host = DefaultHost;
                KeepConnected = true;
                LogLevel = LogLevel.Warning;
                TimeoutMs = Consts.Hub.DefaultTimeoutMs;
                Tools = DefaultTools;
                Prompts = DefaultPrompts;
                Resources = DefaultResources;
                return this;
            }
        }
    }
}
