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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.ReflectorNet.Utils;
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
        public string Copy
        (
            [Description("The paths of the asset to copy.")]
            string[] sourcePaths,
            [Description("The paths to store the copied asset.")]
            string[] destinationPaths
        )
        => MainThread.Instance.Run(() =>
        {
            if (sourcePaths.Length == 0)
                return Error.SourcePathsArrayIsEmpty();

            if (sourcePaths.Length != destinationPaths.Length)
                return Error.SourceAndDestinationPathsArrayMustBeOfTheSameLength();

            var stringBuilder = new StringBuilder();

            for (var i = 0; i < sourcePaths.Length; i++)
            {
                var sourcePath = sourcePaths[i];
                var destinationPath = destinationPaths[i];

                if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destinationPath))
                {
                    stringBuilder.AppendLine(Error.SourceOrDestinationPathIsEmpty());
                    continue;
                }
                if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
                {
                    stringBuilder.AppendLine($"[Error] Failed to copy asset from {sourcePath} to {destinationPath}.");
                    continue;
                }
                stringBuilder.AppendLine($"[Success] Copied asset from {sourcePath} to {destinationPath}.");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return stringBuilder.ToString();
        });
    }
}