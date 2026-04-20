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
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsMoveToolId = "assets-move";
        [McpPluginTool
        (
            AssetsMoveToolId,
            Title = "Assets / Move",
            Enabled = false
        )]
        [Description("Move the assets at paths in the project. " +
            "Should be used for asset rename. " +
            "Does AssetDatabase.Refresh() at the end. " +
            "Use '" + AssetsFindToolId + "' tool to find assets before moving.")]
        public MoveAssetsResponse Move
        (
            [Description("The paths of the assets to move. Separate multiple paths with '|' character. " +
                "Example: 'Assets/Foo.mat|Assets/Bar.prefab'.")]
            string sourcePaths,
            [Description("The paths of moved assets. Separate multiple paths with '|' character. " +
                "Must match the number of source paths. Example: 'Assets/NewFoo.mat|Assets/NewBar.prefab'.")]
            string destinationPaths
        )
        {
            var sourcePathList = sourcePaths.Split('|');
            var destinationPathList = destinationPaths.Split('|');

            return MainThread.Instance.Run(() =>
            {
                if (sourcePathList.Length == 0)
                    throw new ArgumentException(Error.SourcePathsArrayIsEmpty(), nameof(sourcePaths));

                if (sourcePathList.Length != destinationPathList.Length)
                    throw new ArgumentException(Error.SourceAndDestinationPathsArrayMustBeOfTheSameLength());

                var response = new MoveAssetsResponse();

                for (int i = 0; i < sourcePathList.Length; i++)
                {
                    var error = AssetDatabase.MoveAsset(sourcePathList[i], destinationPathList[i]);
                    if (string.IsNullOrEmpty(error))
                    {
                        response.MovedPaths ??= new();
                        response.MovedPaths.Add(destinationPathList[i]);
                    }
                    else
                    {
                        response.Errors ??= new();
                        response.Errors.Add($"Failed to move asset from {sourcePathList[i]} to {destinationPathList[i]}: {error}.");
                    }
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorUtils.RepaintAllEditorWindows();
                return response;
            });
        }

        public class MoveAssetsResponse
        {
            [Description("List of destination paths of successfully moved assets.")]
            public List<string>? MovedPaths { get; set; }
            [Description("List of errors encountered during move operations.")]
            public List<string>? Errors { get; set; }
        }
    }
}
