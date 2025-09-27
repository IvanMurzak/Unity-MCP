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
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Common
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class McpPluginPromptAttribute : Attribute
    {
        public string Name { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.User;

        // Not used for now
        // public string? Title { get; set; }

        public McpPluginPromptAttribute() { }
    }
}
