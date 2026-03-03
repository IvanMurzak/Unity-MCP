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
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Runtime.API
{
    public partial class Tool_Tool
    {
        public const string ListToolsToolId = "tool-list";
        [McpPluginTool
        (
            ListToolsToolId,
            Title = "Tool / List",
            ReadOnlyHint = true,
            DestructiveHint = false
        )]
        [Description("Returns a list of all available MCP tools.")]
        public ToolData[] ListTools
        (
            [Description("Regex to filter tools by name, description, " +
                "argument name, or argument description. null means no filter.")]
            string? regexSearch = null,

            [Description("If true, includes description of each tool. Default: false")]
            bool? includeDescription = false,

            [Description("Specifies whether to include tool inputs and their descriptions in the output.")]
            InputRequest includeInputs = InputRequest.None
        )
        {
            if (!UnityMcpPluginEditor.HasInstance)
                throw new System.InvalidOperationException("UnityMcpPluginEditor is not initialized.");

            var toolManager = UnityMcpPluginEditor.Instance.McpPluginInstance?.McpManager?.ToolManager
                ?? throw new System.InvalidOperationException("ToolManager is not available.");

            var regex = !string.IsNullOrEmpty(regexSearch)
                ? new Regex(regexSearch, RegexOptions.IgnoreCase, System.TimeSpan.FromSeconds(2))
                : null;

            var result = new List<ToolData>();

            foreach (var tool in toolManager.GetAllTools())
            {
                if (tool.InputSchema is not JsonObject schemaObj)
                    continue;

                var properties = schemaObj.TryGetPropertyValue(JsonSchema.Properties, out var propertiesNode)
                    ? propertiesNode is JsonObject propsObj
                        ? propsObj
                        : null
                    : null;

                if (regex != null)
                {
                    var matches =
                        regex.IsMatch(tool.Name ?? string.Empty) ||
                        regex.IsMatch(tool.Description ?? string.Empty);

                    if (!matches && properties != null)
                    {
                        foreach (var prop in properties)
                        {
                            var argName = prop.Key ?? string.Empty;
                            if (prop.Value is JsonObject propObj)
                            {
                                var argDesc = propObj?[JsonSchema.Description]?.ToString() ?? string.Empty;

                                if (regex.IsMatch(argName) || regex.IsMatch(argDesc))
                                {
                                    matches = true;
                                    break;
                                }
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
                                ? (prop.Value as JsonObject)?[JsonSchema.Description]?.ToString()
                                : null
                        });
                    }
                    toolData.Inputs = inputs.ToArray();
                }

                result.Add(toolData);
            }

            return result.ToArray();
        }

        public enum InputRequest
        {
            [Description("Only include tool names, no arguments.")]
            None,
            [Description("Include tool inputs without descriptions.")]
            Inputs,
            [Description("Include tool inputs with descriptions.")]
            InputsWithDescription
        }

        public class InputData
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        public class ToolData
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public InputData[]? Inputs { get; set; }
        }
    }
}
