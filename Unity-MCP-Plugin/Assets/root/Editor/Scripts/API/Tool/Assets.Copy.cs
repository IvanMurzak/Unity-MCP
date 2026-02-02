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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsCopyToolId = "assets-copy";
        [McpPluginTool
        (
            AssetsCopyToolId,
            Title = "Assets / Copy"
        )]
        [Description("Copy the asset at path and stores it at newPath. " +
            "Does AssetDatabase.Refresh() at the end. " +
            "Use '" + AssetsFindToolId + "' tool to find assets before copying.")]
        public CopyAssetsResponse Copy
        (
            [Description("The paths of the asset to copy.")]
            string[] sourcePaths,
            [Description("The paths to store the copied asset.")]
            string[] destinationPaths
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (sourcePaths.Length == 0)
                    throw new System.Exception(Error.SourcePathsArrayIsEmpty());

                if (sourcePaths.Length != destinationPaths.Length)
                    throw new System.Exception(Error.SourceAndDestinationPathsArrayMustBeOfTheSameLength());

                var response = new CopyAssetsResponse();

                for (var i = 0; i < sourcePaths.Length; i++)
                {
                    var sourcePath = sourcePaths[i];
                    var destinationPath = destinationPaths[i];

                    if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
                    {
                        response.Errors ??= new();
                        response.Errors.Add(Error.SourceOrDestinationPathIsEmpty());
                        continue;
                    }
                    if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
                    {
                        response.Errors ??= new();
                        response.Errors.Add($"[Error] Failed to copy asset from {sourcePath} to {destinationPath}.");
                        continue;
                    }
                    var newAssetType = AssetDatabase.GetMainAssetTypeAtPath(destinationPath);
                    var newAsset = AssetDatabase.LoadAssetAtPath(destinationPath, newAssetType);

                    response.CopiedAssets ??= new();
                    response.CopiedAssets.Add(new AssetObjectRef(newAsset));
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                UnityMcpPlugin.NotifyToolExecuted();
                EditorUtils.RepaintAllEditorWindows();

                return response;
            });
        }

        public class CopyAssetsResponse
        {
            [Description("List of copied assets.")]
            public List<AssetObjectRef>? CopiedAssets { get; set; }
            [Description("List of errors encountered during copy operations.")]
            public List<string>? Errors { get; set; }
        }
    }
}