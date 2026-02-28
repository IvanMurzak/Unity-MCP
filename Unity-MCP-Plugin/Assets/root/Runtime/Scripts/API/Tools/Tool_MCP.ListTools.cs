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
            InputRequest? includeInputs = InputRequest.None
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
                regex = new Regex(regexSearch, RegexOptions.IgnoreCase);

            var result = new List<ToolData>();

            foreach (var tool in toolManager.GetAllTools())
            {
                
                var inputArray = tool.InputSchema as JsonArray;

                
                if (regex != null)
                {
                    bool matches =
                        regex.IsMatch(tool.Name ?? "") ||
                        regex.IsMatch(tool.Description ?? "");

                    
                    if (!matches && inputArray != null)
                    {
                        foreach (var node in inputArray)  
                        {
                            var obj = node as JsonObject;
                            if (obj == null) continue;

                            var argName = obj["name"]?.ToString() ?? "";
                            var argDesc = obj["description"]?.ToString() ?? "";

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

                
                if (includeInputs != InputRequest.None && inputArray != null)
                {
                    var inputs = new List<InputData>();
                    foreach (var node in inputArray)  
                    {
                        var obj = node as JsonObject;
                        if (obj == null) continue;

                        inputs.Add(new InputData
                        {
                            Name = obj["name"]?.ToString() ?? string.Empty,
                            Description = includeInputs == InputRequest.InputsWithDescription
                                ? obj["description"]?.ToString()
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
