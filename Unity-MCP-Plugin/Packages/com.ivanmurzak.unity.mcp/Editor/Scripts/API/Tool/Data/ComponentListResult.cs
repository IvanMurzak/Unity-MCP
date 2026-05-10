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
    public class ComponentListResult
    {
        [Description("Array of component type names for the current page.")]
        public string[] Items { get; set; } = Array.Empty<string>();

        [Description("Current page number (0-based).")]
        public int Page { get; set; }

        [Description("Number of items per page.")]
        public int PageSize { get; set; }

        [Description("Total number of matching components.")]
        public int TotalCount { get; set; }

        [Description("Total number of pages available.")]
        public int TotalPages { get; set; }
    }
}
