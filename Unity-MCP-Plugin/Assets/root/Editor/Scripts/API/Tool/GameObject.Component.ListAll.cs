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
            "Use this to find component type names for '" + GameObjectComponentAddToolId + "' tool. " +
            "Results are paginated to avoid overwhelming responses.")]
        public ComponentListResult ListAll
        (
            [Description("Substring for searching components. Could be empty.")]
            string? search = null,
            [Description("Page number (0-based). Default is 0.")]
            int page = 0,
            [Description("Number of items per page. Default is 5. Max is 500.")]
            int pageSize = 5
        )
        {
            // Clamp pageSize to valid range
            pageSize = Math.Clamp(pageSize, 1, 500);
            page = Math.Max(0, page);

            var componentTypes = AllComponentTypes
                .Select(type => type.GetTypeId())
                .Where(typeName => typeName != null);

            if (!string.IsNullOrEmpty(search))
            {
                componentTypes = componentTypes
                    .Where(typeName => typeName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var allItems = componentTypes.Cast<string>().ToList();
            var totalCount = allItems.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedItems = allItems
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToArray();

            return new ComponentListResult
            {
                Items = pagedItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

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
}