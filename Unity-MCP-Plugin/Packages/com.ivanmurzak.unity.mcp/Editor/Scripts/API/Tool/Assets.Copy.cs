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
            Title = "Assets / Copy",
            Enabled = false
        )]
        [Description("Copy assets at given paths and store them at new paths. " +
            "Does AssetDatabase.Refresh() at the end. " +
            "Use '" + AssetsFindToolId + "' tool to find assets before copying.")]
        public CopyAssetsResponse Copy
        (
            [Description("The paths of the assets to copy. Separate multiple paths with '|' character. " +
                "Example: 'Assets/Foo.mat|Assets/Bar.prefab'.")]
            string sourcePaths,
            [Description("The paths to store the copied assets. Separate multiple paths with '|' character. " +
                "Must match the number of source paths. Example: 'Assets/Foo_Copy.mat|Assets/Bar_Copy.prefab'.")]
            string destinationPaths
        )
        {
            var sourcePathList = sourcePaths.Split('|');
            var destinationPathList = destinationPaths.Split('|');

            return MainThread.Instance.Run(() =>
            {
                if (sourcePathList.Length == 0)
                    throw new System.Exception(Error.SourcePathsArrayIsEmpty());

                if (sourcePathList.Length != destinationPathList.Length)
                    throw new System.Exception(Error.SourceAndDestinationPathsArrayMustBeOfTheSameLength());

                var response = new CopyAssetsResponse();

                for (var i = 0; i < sourcePathList.Length; i++)
                {
                    var sourcePath = sourcePathList[i];
                    var destinationPath = destinationPathList[i];

                    if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
                    {
                        response.Errors ??= new();
                        response.Errors.Add(Error.SourceOrDestinationPathIsEmpty());
                        continue;
                    }
                    if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
                    {
                        response.Errors ??= new();
                        response.Errors.Add($"Failed to copy asset from {sourcePath} to {destinationPath}.");
                        continue;
                    }
                    var newAssetType = AssetDatabase.GetMainAssetTypeAtPath(destinationPath);
                    var newAsset = AssetDatabase.LoadAssetAtPath(destinationPath, newAssetType);

                    response.CopiedAssets ??= new();
                    response.CopiedAssets.Add(new AssetObjectRef(newAsset));
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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
