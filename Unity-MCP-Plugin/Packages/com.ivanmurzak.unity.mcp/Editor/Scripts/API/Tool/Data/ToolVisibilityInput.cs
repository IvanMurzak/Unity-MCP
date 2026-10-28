/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.ComponentModel;

namespace AIGD
{
    public class ToolVisibilityInput
    {
        [Description("Name of the MCP tool to show or hide.")]
        public string Name { get; set; } = string.Empty;

        [Description("Whether the tool is listed to MCP clients (true) or hidden from tools/list (false). A hidden tool still runs when called by name.")]
        public bool Visible { get; set; }
    }
}
