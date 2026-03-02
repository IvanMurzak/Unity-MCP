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
using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Runtime.API
{
    public partial class Tool_MCP
    {
        [McpPluginTool
        (
            "mcp-list-tools",
            Title = "MCP / List Tools"
        )]
        [Description("Returns a list of all available MCP tools. " +
            "Supports filtering by name, description, or argument using regex.")]
        public List<ToolData> ListTools
        (
            [Description("Regex to filter tools by name, description, " +
                "argument name, or argument description. null means no filter.")]
            string? regexSearch = null,

            [Description("If true, includes description of each tool. Default: false")]
            bool? includeDescription = false,

            [Description("None = only names, Inputs = with argument names, " +
                "InputsWithDescription = with argument names and descriptions")]
            InputRequest includeInputs = InputRequest.None
        )
        {
            if (!UnityMcpPlugin.HasInstance)
                throw new System.InvalidOperationException(
                    "[Error] UnityMcpPlugin is not initialized.");

            var toolManager = UnityMcpPlugin.Instance
                .McpPluginInstance?.McpManager?.ToolManager;

            if (toolManager == null)
                throw new System.InvalidOperationException(
                    "[Error] ToolManager is not initialized.");

            Regex? regex = null;
            if (!string.IsNullOrEmpty(regexSearch))
            {
                try
                {
                    regex = new Regex(regexSearch, RegexOptions.IgnoreCase, System.TimeSpan.FromSeconds(2));
                }
                catch (System.ArgumentException)
                {
                    throw new System.ArgumentException($"[Error] Invalid Regex Pattern: {regexSearch}");
                }
            }

            var result = new List<ToolData>();

            foreach (var tool in toolManager.GetAllTools())
            {
                var schemaObj = tool.InputSchema as JsonObject;
                var properties = schemaObj?["properties"] as JsonObject;

                if (regex != null)
                {
                    bool matches =
                        regex.IsMatch(tool.Name ?? "") ||
                        regex.IsMatch(tool.Description ?? "");

                    
                    if (!matches && properties != null)
                    {
                        foreach (var prop in properties)
                        {
                            var argName = prop.Key ?? "";
                            var argDesc = (prop.Value as JsonObject)?["description"]?.ToString() ?? "";

                            if (regex.IsMatch(argName) || regex.IsMatch(argDesc))
                            {
                                matches = true;
                                break;
                            }
                        }
                    }

                    if (!matches) continue;
                }


                var toolData = new ToolData
                {
                    Name = tool.Name ?? string.Empty,
                    Description = includeDescription == true
                        ? tool.Description
                        : null
                };

                if (includeInputs != InputRequest.None && properties != null)
                {
                    var inputs = new List<InputData>();
                    foreach (var prop in properties)
                    {
                        inputs.Add(new InputData
                        {
                            Name = prop.Key ?? string.Empty,
                            Description = includeInputs == InputRequest.InputsWithDescription
                                ? (prop.Value as JsonObject)?["description"]?.ToString()
                                : null
                        });
                    }
                    toolData.Inputs = inputs.ToArray();
                }

                result.Add(toolData);
            }

            return result;
        }
    }
}
