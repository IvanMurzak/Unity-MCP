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
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        [McpPluginTool
        (
            "Assets_Move",
            Title = "Assets / Move"
        )]
        [Description(@"Move the assets at paths in the project. Should be used for asset rename. Does AssetDatabase.Refresh() at the end.")]
        public string Move
        (
            [Description("The paths of the assets to move.")]
            string[] sourcePaths,
            [Description("The paths of moved assets.")]
            string[] destinationPaths
        )
        => MainThread.Instance.Run(() =>
        {
            if (sourcePaths.Length == 0)
                return Error.SourcePathsArrayIsEmpty();

            if (sourcePaths.Length != destinationPaths.Length)
                return Error.SourceAndDestinationPathsArrayMustBeOfTheSameLength();

            var stringBuilder = new StringBuilder();

            for (int i = 0; i < sourcePaths.Length; i++)
            {
                var error = AssetDatabase.MoveAsset(sourcePaths[i], destinationPaths[i]);
                if (string.IsNullOrEmpty(error))
                {
                    stringBuilder.AppendLine($"[Success] Moved asset from {sourcePaths[i]} to {destinationPaths[i]}.");
                }
                else
                {
                    stringBuilder.AppendLine($"[Error] Failed to move asset from {sourcePaths[i]} to {destinationPaths[i]}: {error}.");
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            UnityEditor.EditorApplication.RepaintProjectWindow();
            UnityEditor.EditorApplication.RepaintHierarchyWindow();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            return stringBuilder.ToString();
        });
    }
}
