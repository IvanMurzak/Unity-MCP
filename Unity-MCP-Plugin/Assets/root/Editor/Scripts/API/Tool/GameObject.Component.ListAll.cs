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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        public static IEnumerable<Type> AllComponentTypes => TypeUtils.AllTypes
            .Where(type => typeof(UnityEngine.Component).IsAssignableFrom(type) && !type.IsAbstract);

        public const string ComponentListToolId = "gameobject-component-list-all";
        [McpPluginTool
        (
            ComponentListToolId,
            Title = "GameObject / Component / List All"
        )]
        [Description("List C# class names extended from UnityEngine.Component. " +
            "Use this to find component type names for '" + GameObjectComponentAddToolId + "' tool.")]
        public string[] ListAll
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