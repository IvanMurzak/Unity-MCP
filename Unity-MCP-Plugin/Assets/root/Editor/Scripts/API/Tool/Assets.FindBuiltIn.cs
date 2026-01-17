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
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityObject = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsFindBuiltInToolId = "assets-find-built-in";
        [McpPluginTool
        (
            AssetsFindBuiltInToolId,
            Title = "Assets / Find (Built-in)"
        )]
        [Description("Search the built-in assets of the Unity Editor located in the built-in resources: " +
            ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath + ". " +
            "Doesn't support GUIDs since built-in assets do not have them.")]
        public List<AssetObjectRef> FindBuiltIn
        (
            [Description("The name of the asset to filter by.")]
            string? name = null,
            [Description("The type of the asset to filter by.")]
            Type? type = null,
            [Description("Maximum number of assets to return. If the number of found assets exceeds this limit, the result will be truncated.")]
            int maxResults = 10
        )
        {
            if (maxResults <= 0)
                throw new ArgumentException($"{nameof(maxResults)} must be greater than zero.");

            return MainThread.Instance.Run(() =>
            {
                var nameWords = string.IsNullOrEmpty(name)
                    ? Array.Empty<string>()
                    : name!.Split(new[] { ' ', '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);

                return BuiltInAssetCache.GetAllAssets()
                    .Where(obj => obj != null && !string.IsNullOrEmpty(obj.name))
                    .Where(obj => type == null || type.IsAssignableFrom(obj.GetType()))
                    .Select(obj => (obj, priority: GetMatchPriority(obj, name, nameWords)))
                    .Where(x => x.priority >= 0)
                    .OrderBy(x => x.priority)
                    .Take(maxResults)
                    .Select(x => ToAssetObjectRef(x.obj))
                    .ToList();
            });
        }

        private static int GetMatchPriority(UnityObject obj, string? name, string[] nameWords)
        {
            if (string.IsNullOrEmpty(name))
                return 0; // No name filter, all items have same priority

            if (obj.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return 0; // Exact match - top priority

            if (obj.name.Contains(name, StringComparison.OrdinalIgnoreCase))
                return 1; // Partial match - medium priority

            if (nameWords.Length > 0)
            {
                var matchedWords = nameWords.Count(word =>
                    obj.name.Contains(word, StringComparison.OrdinalIgnoreCase));

                if (matchedWords == nameWords.Length)
                    return 2; // All words match - higher word match priority

                if (matchedWords > 0)
                    return 3; // Some words match - lower word match priority
            }

            return -1; // No match
        }

        private static AssetObjectRef ToAssetObjectRef(UnityObject obj)
        {
            var assetObjRef = new AssetObjectRef(obj)
            {
                AssetGuid = null // Unity built-in assets do not have GUIDs
            };

            // Distinguish built-in assets: if the path doesn't end with the asset name, append it.
            // This handles cases where multiple built-in assets map to "Resources/unity_builtin_extra".
            if (!string.IsNullOrEmpty(assetObjRef.AssetPath) && !assetObjRef.AssetPath!.EndsWith("/" + obj.name))
            {
                var extension = BuiltInAssetCache.GetExtensionForAsset(obj);
                assetObjRef.AssetPath = $"{assetObjRef.AssetPath}/{obj.name}{extension}";
            }

            return assetObjRef;
        }
    }
}