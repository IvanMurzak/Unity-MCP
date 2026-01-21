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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsGetDataToolId = "assets-get-data";
        [McpPluginTool
        (
            AssetsGetDataToolId,
            Title = "Assets / Get Data"
        )]
        [Description("Get asset data from the asset file in the Unity project. " +
            "It includes all serializable fields and properties of the asset. " +
            "Use '" + AssetsFindToolId + "' tool to find asset before using this tool.")]
        public SerializedMember GetData(AssetObjectRef assetRef)
        {
            if (assetRef == null)
                throw new ArgumentNullException(nameof(assetRef));

            if (!assetRef.IsValid(out var error))
                throw new ArgumentException(error, nameof(assetRef));

            return MainThread.Instance.Run(() =>
            {
                var asset = assetRef.FindAssetObject();
                if (asset == null)
                {
                    // Built-in assets fallback (uses cached assets to avoid repeated expensive LoadAllAssetsAtPath calls)
                    if (!string.IsNullOrEmpty(assetRef.AssetPath) && assetRef.AssetPath!.StartsWith(ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath))
                    {
                        var targetName = System.IO.Path.GetFileNameWithoutExtension(assetRef.AssetPath);
                        var ext = System.IO.Path.GetExtension(assetRef.AssetPath);
                        asset = BuiltInAssetCache.FindAssetByExtension(targetName, ext);
                    }
                }

                if (asset == null)
                    throw new Exception(Error.NotFoundAsset(assetRef.AssetPath!, assetRef.AssetGuid ?? "N/A"));

                var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

                return reflector.Serialize(
                    obj: asset,
                    name: asset.name,
                    recursive: true,
                    logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Assets>()
                );
            });
        }
    }
}