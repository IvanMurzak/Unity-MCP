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
using System.Text.Json.Serialization;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using AIGD;

namespace AIGD
{
    /// <summary>Input of the deprecated <c>tool-set-enabled-state</c>; <see cref="ToolVisibilityInput"/> is the current one.</summary>
    public class ToolToggleInput
    {
        [Description("Name of the MCP tool to show or hide.")]
        public string Name { get; set; } = string.Empty;

        [Description("Whether the tool is listed to MCP clients (true) or hidden from tools/list (false). A hidden tool still runs when called by name.")]
        public bool Enabled { get; set; }
    }
}
