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
using AIGD;
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
            Title = "GameObject / Component / List All",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [McpPluginSkillDescription("List the fully-qualified C# type names of every concrete `UnityEngine.Component` " +
            "subclass available in the project. Paginated (default 5/page, max 500). " +
            "Use this to find a valid `componentName` for '" + GameObjectComponentAddToolId + "'.")]
        [McpPluginSkillBody("List C# class names extended from UnityEngine.Component. " +
            "Use this to find component type names for '" + GameObjectComponentAddToolId + "' tool. " +
            "Results are paginated to avoid overwhelming responses.\n\n" +
            "## Inputs\n\n" +
            "- `search` (optional) — case-insensitive substring filter on type names.\n" +
            "- `page` (default 0, 0-based) — page index.\n" +
            "- `pageSize` (default 5, range 1..500) — items per page.\n\n" +
            "## Behavior\n\n" +
            "Enumerates `AllComponentTypes` (every non-abstract subclass of `UnityEngine.Component`), filters by " +
            "`search` if supplied, then returns a `ComponentListResult` containing the requested page plus " +
            "`TotalCount` / `TotalPages` so the caller can iterate.")]
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

    }
}