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
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    [Description("MCP tool information.")]
    public class ToolInfoData
    {
        [JsonInclude, JsonPropertyName("name")]
        [Description("Tool name.")]
        public string Name { get; set; } = string.Empty;

        [JsonInclude, JsonPropertyName("description")]
        [Description("Tool description.")]
        public string? Description { get; set; }

        [JsonInclude, JsonPropertyName("inputs")]
        [Description("Tool input arguments.")]
        public ToolInputData[]? Inputs { get; set; }
    }
}
