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
        [McpPluginTool
        (
            "Assets_Copy",
            Title = "Assets Copy"
        )]
        [Description(@"Copy the asset at path and stores it at newPath. Does AssetDatabase.Refresh() at the end.")]
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
                        response.errors ??= new();
                        response.errors.Add(Error.SourceOrDestinationPathIsEmpty());
                        continue;
                    }
                    if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
                    {
                        response.errors ??= new();
                        response.errors.Add($"[Error] Failed to copy asset from {sourcePath} to {destinationPath}.");
                        continue;
                    }
                    var newAssetType = AssetDatabase.GetMainAssetTypeAtPath(destinationPath);
                    var newAsset = AssetDatabase.LoadAssetAtPath(destinationPath, newAssetType);

                    response.copiedAssets ??= new();
                    response.copiedAssets?.Add(new AssetObjectRef(newAsset));
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                UnityEditor.EditorApplication.RepaintProjectWindow();
                UnityEditor.EditorApplication.RepaintHierarchyWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return response;
            });
        }

        public class CopyAssetsResponse
        {
            public List<AssetObjectRef>? copiedAssets;
            public List<string>? errors;
        }
    }
}