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
            public static string DefaultHost => $"http://localhost:{Consts.Hub.DefaultPort}";
            public static List<string> DefaultEnabledTools => new() { "*" };
            public static List<string> DefaultEnabledPrompts => new() { "*" };
            public static List<string> DefaultEnabledResources => new() { "*" };

            public LogLevel LogLevel { get; set; } = LogLevel.Warning;
            public List<string> EnabledTools { get; set; } = DefaultEnabledTools;
            public List<string> EnabledPrompts { get; set; } = DefaultEnabledPrompts;
            public List<string> EnabledResources { get; set; } = DefaultEnabledResources;

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
                EnabledTools = DefaultEnabledTools;
                EnabledPrompts = DefaultEnabledPrompts;
                EnabledResources = DefaultEnabledResources;
                return this;
            }
        }
    }
}
