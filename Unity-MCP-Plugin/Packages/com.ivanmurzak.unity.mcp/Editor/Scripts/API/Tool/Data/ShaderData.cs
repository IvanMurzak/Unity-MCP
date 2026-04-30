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
    public class ShaderData
    {
        [Description("Reference to the shader asset for future operations.")]
        public AssetObjectRef? Reference { get; set; }

        [Description("Full name of the shader (e.g. 'Standard', 'Universal Render Pipeline/Lit').")]
        public string? Name { get; set; }

        [Description("Whether the shader is supported on the current GPU and platform.")]
        public bool IsSupported { get; set; }

        [Description("The render queue value of the shader.")]
        public int RenderQueue { get; set; }

        [Description("Whether the shader has any compilation errors.")]
        public bool HasErrors { get; set; }

        [Description("Number of properties exposed by the shader.")]
        public int PropertyCount { get; set; }

        [Description("Total number of passes in the shader.")]
        public int PassCount { get; set; }

        [Description("The RenderType tag value from the first pass, if set.")]
        public string? RenderType { get; set; }

        [Description("Compilation messages including errors and warnings. Null if no messages.")]
        public List<ShaderMessageData>? Messages { get; set; }

        [Description("List of shader properties (uniforms). Null if the shader has no properties.")]
        public List<ShaderPropertyData>? Properties { get; set; }

        [Description("List of subshaders with their passes. Null if shader data is unavailable.")]
        public List<SubshaderData>? Subshaders { get; set; }

        [Description("Path-scoped read or view-query result, populated when 'paths' or 'viewQuery' is supplied. " +
            "Null otherwise.")]
        public SerializedMember? View { get; set; }
    }
}
