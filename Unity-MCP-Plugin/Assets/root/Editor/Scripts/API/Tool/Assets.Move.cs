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
            Title = "Assets / Move"
        )]
        [Description("Move the assets at paths in the project. " +
            "Should be used for asset rename. " +
            "Does AssetDatabase.Refresh() at the end. " +
            "Use '" + AssetsFindToolId + "' tool to find assets before moving.")]
        public string[] Move
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

                var logs = new string[sourcePaths.Length];

                for (int i = 0; i < sourcePaths.Length; i++)
                {
                    var error = AssetDatabase.MoveAsset(sourcePaths[i], destinationPaths[i]);
                    if (string.IsNullOrEmpty(error))
                    {
                        logs[i] = $"[Success] Moved asset from {sourcePaths[i]} to {destinationPaths[i]}.";
                    }
                    else
                    {
                        logs[i] = $"[Error] Failed to move asset from {sourcePaths[i]} to {destinationPaths[i]}: {error}.";
                    }
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorUtils.RepaintAllEditorWindows();
                return logs;
            });
        }
    }
}
