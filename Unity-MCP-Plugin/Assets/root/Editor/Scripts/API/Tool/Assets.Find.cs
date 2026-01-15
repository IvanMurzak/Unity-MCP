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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsFindToolId = "assets-find";
        [McpPluginTool
        (
            AssetsFindToolId,
            Title = "Assets / Find"
        )]
        [Description("Search the asset database using the search filter string. " +
            "Allows you to search for Assets. The string argument can provide names, labels or types (classnames).")]
        public List<AssetObjectRef> Find
        (
            // <ref>https://docs.unity3d.com/ScriptReference/AssetDatabase.FindAssets.html</ref>
            [Description("The filter string can contain search data. Could be empty. " +
                "Name: Filter assets by their filename (without extension). Words separated by whitespace are treated as separate name searches. " +
                "Labels (l:): Assets can have labels attached to them. Use 'l:' before each label. " +
                "Types (t:): Find assets based on explicitly identified types. Use 't:' keyword. Available types: AnimationClip, AudioClip, AudioMixer, ComputeShader, Font, GUISkin, Material, Mesh, Model, PhysicMaterial, Prefab, Scene, Script, Shader, Sprite, Texture, VideoClip, VisualEffectAsset, VisualEffectSubgraph. " +
                "AssetBundles (b:): Find assets which are part of an Asset bundle. " +
                "Area (a:): Find assets in a specific area. Valid values are 'all', 'assets', and 'packages'. " +
                "Globbing (glob:): Use globbing to match specific rules. " +
                "Note: Searching is case insensitive.")]
            string? filter = null,
            [Description("The folders where the search will start. If null, the search will be performed in all folders.")]
            string[]? searchInFolders = null,
            [Description("Maximum number of assets to return. If the number of found assets exceeds this limit, the result will be truncated.")]
            int maxResults = 10
        )
        {
            if (maxResults <= 0)
                throw new System.ArgumentException($"{nameof(maxResults)} must be greater than zero.");

            return MainThread.Instance.Run(() =>
            {
                var assetGuids = (searchInFolders?.Length ?? 0) == 0
                    ? AssetDatabase.FindAssets(filter ?? string.Empty)
                    : AssetDatabase.FindAssets(filter ?? string.Empty, searchInFolders);

                var response = new List<AssetObjectRef>();

                for (var i = 0; i < assetGuids.Length && i < maxResults; i++)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                    var assetObject = AssetDatabase.LoadAssetAtPath(assetPath, assetType);
                    if (assetObject == null)
                        continue;

                    response.Add(new AssetObjectRef(assetObject));
                }

                return response;
            });
        }
    }
}