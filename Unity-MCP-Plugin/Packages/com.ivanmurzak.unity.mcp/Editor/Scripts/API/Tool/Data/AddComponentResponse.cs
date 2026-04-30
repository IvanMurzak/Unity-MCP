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
    public class AddComponentResponse
    {
        [Description("List of successfully added components.")]
        public List<ComponentDataShallow> AddedComponents { get; set; } = new List<ComponentDataShallow>();

        [Description("List of success messages for added components.")]
        public List<string>? Messages { get; set; }

        [Description("List of warnings encountered during component addition.")]
        public List<string>? Warnings { get; set; }

        [Description("List of errors encountered during component addition.")]
        public List<string>? Errors { get; set; }
    }
}
