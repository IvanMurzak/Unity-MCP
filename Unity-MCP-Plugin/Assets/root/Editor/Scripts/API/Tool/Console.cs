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
using com.IvanMurzak.McpPlugin.Common;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Console
    {
        public static class Error
        {
            public static string InvalidMaxEntries(int entriesCount)
                => $"[Error] Invalid maxEntries value '{entriesCount}'. Must be between 1 and {LogUtils.MaxLogEntries}.";

            public static string InvalidLogTypeFilter(string logType)
                => $"[Error] Invalid logType filter '{logType}'. Valid values: All, Error, Assert, Warning, Log, Exception.";
        }
    }
}
