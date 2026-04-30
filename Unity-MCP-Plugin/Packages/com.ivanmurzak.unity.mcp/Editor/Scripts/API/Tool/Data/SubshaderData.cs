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
    public class SubshaderData
    {
        [Description("Index of this subshader within the shader.")]
        public int Index { get; set; }

        [Description("Number of passes in this subshader.")]
        public int PassCount { get; set; }

        [Description("List of passes in this subshader. Null if no passes.")]
        public List<PassData>? Passes { get; set; }
    }
}
