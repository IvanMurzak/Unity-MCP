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
            Title = "Assets / Get Data",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [Description("Get asset data from the asset file in the Unity project. " +
            "It includes all serializable fields and properties of the asset. " +
            "Use '" + AssetsFindToolId + "' tool to find asset before using this tool.\n\n" +
            "Path-scoped reads (token-saving): supply '" + "paths" + "' (a list of paths) to read only the listed " +
            "fields/elements via Reflector.TryReadAt, or '" + "viewQuery" + "' (a ViewQuery) to navigate to a " +
            "subtree and/or filter by name regex / max depth / type via Reflector.View. " +
            "These two parameters are mutually exclusive — supply at most one. " +
            "When neither is supplied the full asset is serialized as before (backwards compatible).\n" +
            "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. Leading '#/' is stripped.")]
        public SerializedMember GetData
        (
            AssetObjectRef assetRef,
            [Description("Optional. List of paths to read individually via Reflector.TryReadAt. " +
                "Path syntax: 'fieldName', 'nested/field', 'arrayField/[i]', 'dictField/[key]'. " +
                "Mutually exclusive with '" + "viewQuery" + "'.")]
            List<string>? paths = null,
            [Description("Optional. View-query filter routed through Reflector.View — combines a starting Path, " +
                "a case-insensitive NamePattern regex, MaxDepth, and an optional TypeFilter. " +
                "Mutually exclusive with '" + "paths" + "'.")]
            ViewQuery? viewQuery = null
        )
        {
            if (assetRef == null)
                throw new ArgumentNullException(nameof(assetRef));

            if (!assetRef.IsValid(out var error))
                throw new ArgumentException(error, nameof(assetRef));

            var hasPaths = paths != null && paths.Count > 0;
            var hasViewQuery = viewQuery != null;
            if (hasPaths && hasViewQuery)
                throw new ArgumentException(
                    $"'{"paths"}' and '{"viewQuery"}' are mutually exclusive — supply at most one.");

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

                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Assets>();

                if (hasPaths)
                    return PathReadHelper.BuildPathReadAggregate(reflector, asset, asset.name, paths!, logger);

                if (hasViewQuery)
                    return reflector.View(asset, viewQuery, logs: null, logger: logger)
                        ?? new SerializedMember { name = asset.name, typeName = asset.GetType().FullName ?? string.Empty };

                return reflector.Serialize(
                    obj: asset,
                    name: asset.name,
                    recursive: true,
                    logger: logger
                );
            });
        }
    }
}
