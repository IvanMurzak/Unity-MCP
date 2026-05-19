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
        [McpPluginSkillDescription("Move or rename assets at the given project paths. " +
            "Refreshes the AssetDatabase at the end. " +
            "Use '" + AssetsFindToolId + "' to locate the assets first.")]
        [McpPluginSkillBody("Move the assets at paths in the project. " +
            "Should be used for asset rename. " +
            "Does AssetDatabase.Refresh() at the end. " +
            "Use '" + AssetsFindToolId + "' tool to find assets before moving.\n\n" +
            "## Inputs\n\n" +
            "- `sourcePaths` — paths of the assets to move.\n" +
            "- `destinationPaths` — target paths (must match `sourcePaths` length).\n\n" +
            "## Behavior\n\n" +
            "Each pair is moved independently via `AssetDatabase.MoveAsset`. " +
            "Per-pair failures (Unity's `MoveAsset` returns a non-empty error string) are surfaced in " +
            "`response.Errors`; successful moves accumulate into `response.MovedPaths`.")]
        [Description("Move the assets at paths in the project. " +
            "Should be used for asset rename. " +
            "Does AssetDatabase.Refresh() at the end. " +
            "Use '" + AssetsFindToolId + "' tool to find assets before moving.")]
        public MoveAssetsResponse Move
        (
            [Description("The paths of the assets to move.")]
            string[] sourcePaths,
            [Description("The paths of moved assets.")]
            string[] destinationPaths
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (sourcePaths.Length == 0)
                    throw new ArgumentException(Error.SourcePathsArrayIsEmpty(), nameof(sourcePaths));

                if (sourcePaths.Length != destinationPaths.Length)
                    throw new ArgumentException(Error.SourceAndDestinationPathsArrayMustBeOfTheSameLength());

                var response = new MoveAssetsResponse();

                for (int i = 0; i < sourcePaths.Length; i++)
                {
                    var error = AssetDatabase.MoveAsset(sourcePaths[i], destinationPaths[i]);
                    if (string.IsNullOrEmpty(error))
                    {
                        response.MovedPaths ??= new();
                        response.MovedPaths.Add(destinationPaths[i]);
                    }
                    else
                    {
                        response.Errors ??= new();
                        response.Errors.Add($"Failed to move asset from {sourcePaths[i]} to {destinationPaths[i]}: {error}.");
                    }
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorUtils.RepaintAllEditorWindows();
                return response;
            });
        }

    }
}
