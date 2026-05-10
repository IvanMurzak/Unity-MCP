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
    public class ModifyComponentResponse
    {
        [Description("Whether the modification was successful.")]
        public bool Success { get; set; } = false;

        [Description("Reference to the modified component.")]
        public ComponentRef? Reference { get; set; }

        [Description("Index of the component in the GameObject's component list.")]
        public int Index { get; set; }

        [Description("Updated component information after modification.")]
        public ComponentDataShallow? Component { get; set; }
        [Description("Log of modifications made and any warnings/errors encountered.")]
        public string[]? Logs { get; set; }
    }
}
