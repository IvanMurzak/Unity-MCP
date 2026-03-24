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

            [Description("Result of each tool operation. Key: original input name as provided by the caller (case preserved as-is). Value: true if the enable/disable operation completed successfully, false if the name was unknown, ambiguous, or empty.")]
            public Dictionary<string, bool> Success { get; set; } = new();
        }

        static (Dictionary<string, string> exact, Dictionary<string, List<string>> caseInsensitive) BuildToolLookup(IToolManager toolManager)
        {
            var exact = new Dictionary<string, string>(StringComparer.Ordinal);
            var caseInsensitive = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            var allTools = toolManager.GetAllTools();
            if (allTools == null)
                return (exact, caseInsensitive);

            foreach (var tool in allTools)
            {
                exact[tool.Name] = tool.Name;
                if (!caseInsensitive.TryGetValue(tool.Name, out var list))
                {
                    list = new List<string>();
                    caseInsensitive[tool.Name] = list;
                }
                list.Add(tool.Name);
            }

            return (exact, caseInsensitive);
        }

        static string? ResolveToolName(Dictionary<string, string> exact, Dictionary<string, List<string>> caseInsensitive, string input, Logs? logs)
        {
            if (exact.TryGetValue(input, out var exactMatch))
                return exactMatch;

            if (caseInsensitive.TryGetValue(input, out var matches))
            {
                if (matches.Count == 1)
                    return matches[0];

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
