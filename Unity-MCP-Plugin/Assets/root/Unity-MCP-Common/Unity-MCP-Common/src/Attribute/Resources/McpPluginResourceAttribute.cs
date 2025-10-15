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

namespace com.IvanMurzak.Unity.MCP.Common
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class McpPluginResourceAttribute : Attribute
    {
        public string Route { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MimeType { get; set; }
        public string ListResources { get; set; } = string.Empty;

        public McpPluginResourceAttribute() { }
    }
}
