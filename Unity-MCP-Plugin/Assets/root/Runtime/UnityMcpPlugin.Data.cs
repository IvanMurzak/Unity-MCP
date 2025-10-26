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
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Utils;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin
    {
        public class Data
        {
            public static string DefaultHost => $"http://localhost:{Consts.Hub.DefaultPort}";

            public string Host { get; set; } = DefaultHost;
            public bool KeepConnected { get; set; } = true;
            public LogLevel LogLevel { get; set; } = LogLevel.Warning;
            public int TimeoutMs { get; set; } = Consts.Hub.DefaultTimeoutMs;

            public Data SetDefault()
            {
                Host = DefaultHost;
                KeepConnected = true;
                LogLevel = LogLevel.Warning;
                TimeoutMs = Consts.Hub.DefaultTimeoutMs;
                return this;
            }
        }
    }
}
