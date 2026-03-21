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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Tool
    {
        public class InputData
        {
            [Description("Name of the MCP tool to enable or disable.")]
            public string Name { get; set; } = string.Empty;

            [Description("Whether the tool should be enabled (true) or disabled (false).")]
            public bool Enabled { get; set; }
        }

        public class ResultData
        {
            [Description("Optional operation logs. Only included when 'includeLogs' is true.")]
            public Logs? Logs { get; set; }

            [Description("Result of each tool operation. Key: tool name, Value: true if successful.")]
            public Dictionary<string, bool> Success { get; set; } = new();
        }

        static string? ResolveToolName(IToolManager toolManager, string input, Logs? logs)
        {
            var allTools = toolManager.GetAllTools();
            if (allTools == null)
                return null;

            string? caseInsensitiveMatch = null;
            var caseInsensitiveCount = 0;

            foreach (var tool in allTools)
            {
                if (tool.Name.Equals(input, StringComparison.Ordinal))
                    return tool.Name;

                if (tool.Name.Equals(input, StringComparison.OrdinalIgnoreCase))
                {
                    caseInsensitiveMatch = tool.Name;
                    caseInsensitiveCount++;
                }
            }

            if (caseInsensitiveCount == 1)
                return caseInsensitiveMatch;

            if (caseInsensitiveCount > 1)
            {
                logs?.Warning($"Tool '{input}' is ambiguous. Multiple case-insensitive matches found.");
                return null;
            }

            logs?.Warning($"Tool '{input}' not found. No matching tools.");
            return null;
        }

        public static class Error
        {
            public static string ToolsArrayIsNullOrEmpty()
                => "Tools array is null or empty. Please provide at least one tool to enable or disable.";

            public static string ToolManagerNotAvailable()
                => "Tool manager is not available. UnityMcpPluginEditor may not be initialized.";
        }
    }
}
