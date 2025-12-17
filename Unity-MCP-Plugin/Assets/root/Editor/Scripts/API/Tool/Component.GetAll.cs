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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Component
    {
        [McpPluginTool
        (
            "component-list",
            Title = "Components / List"
        )]
        [Description("List C# class names extended from UnityEngine.Component.")]
        public string[] List
        (
            [Description("Substring for searching components. Could be empty.")]
            string? search = null
        )
        {
            var componentTypes = AllComponentTypes
                .Select(type => type.GetTypeId());

            if (!string.IsNullOrEmpty(search))
            {
                componentTypes = componentTypes
                    .Where(typeName => typeName != null && typeName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return componentTypes.ToArray();
        }
    }
}