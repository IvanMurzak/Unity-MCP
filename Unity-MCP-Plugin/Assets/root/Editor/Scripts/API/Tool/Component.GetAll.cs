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
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin.Common;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Component
    {
        [McpPluginTool
        (
            "Component_GetAll",
            Title = "Get list of all Components"
        )]
        [Description("Returns the list of all available components in the project.")]
        public string GetAll
        (
            [Description("Substring for searching components. Could be empty.")]
            string? search = null
        )
        {
            var componentTypes = AllComponentTypes
                .Select(type => type.FullName)
                .ToList();

            if (!string.IsNullOrEmpty(search))
            {
                componentTypes = componentTypes
                    .Where(typeName => typeName != null && typeName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return string.Join("\n", componentTypes);
        }
    }
}