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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Reflection;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        [McpPluginTool
        (
            "Assets_Read",
            Title = "Read asset file content"
        )]
        [Description(@"Read file asset in the project.")]
        public string Read
        (
            [Description("Path to the asset. See 'Assets_Search' for more details. Starts with 'Assets/'. Priority: 1. (Recommended)")]
            string? assetPath = null,
            [Description("GUID of the asset. Priority: 2.")]
            string? assetGuid = null
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(assetPath) && string.IsNullOrEmpty(assetGuid))
                return Error.NeitherProvided_AssetPath_AssetGuid();

            if (string.IsNullOrEmpty(assetPath))
                assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

            if (string.IsNullOrEmpty(assetGuid))
                assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return Error.NotFoundAsset(assetPath, assetGuid);

            var reflector = McpPlugin.Instance!.McpRunner.Reflector;

            var serialized = reflector.Serialize(
                asset,
                name: asset.name,
                recursive: true,
                logger: McpPlugin.Instance.Logger
            );
            var json = serialized.ToJson(reflector);

            return $"[Success] Loaded asset at path '{assetPath}'.\n{json}";

            //             var instanceID = asset.GetInstanceID();
            //             return @$"[Success] Loaded asset.
            // # Asset path: {assetPath}
            // # Asset GUID: {assetGuid}
            // # Asset instanceID: {instanceID}";
        });
    }
}