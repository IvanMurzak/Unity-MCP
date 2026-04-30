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
    public class ShaderPropertyData
    {
        [Description("Property name as used in shader code (e.g. '_MainTex', '_Color').")]
        public string? Name { get; set; }

        [Description("Human-readable description/display name of the property.")]
        public string? Description { get; set; }

        [Description("Property type (e.g. 'Color', 'Float', 'Range', 'Texture', 'Vector', 'Int').")]
        public string? Type { get; set; }

        [Description("Property flags (e.g. 'None', 'HideInInspector', 'PerRendererData').")]
        public string? Flags { get; set; }

        [Description("The unique name ID for this property.")]
        public int NameId { get; set; }

        [Description("Minimum value for Range properties. Null for non-range properties.")]
        public float? RangeMin { get; set; }

        [Description("Maximum value for Range properties. Null for non-range properties.")]
        public float? RangeMax { get; set; }

        [Description("Default texture name for Texture properties. Null if not applicable.")]
        public string? DefaultTextureName { get; set; }

        [Description("Custom attributes applied to this property. Null if none.")]
        public List<string>? Attributes { get; set; }
    }
}
