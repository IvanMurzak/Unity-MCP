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
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsDeleteToolId = "assets-delete";
        [McpPluginTool
        (
            AssetsDeleteToolId,
            Title = "Assets / Delete"
        )]
        [Description(@"Delete the assets at paths from the project. Does AssetDatabase.Refresh() at the end.")]
        public DeleteAssetsResponse Delete
        (
            [Description("The paths of the assets")]
            string[] paths
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (paths.Length == 0)
                    throw new System.Exception(Error.SourcePathsArrayIsEmpty());

                var response = new DeleteAssetsResponse();
                var outFailedPaths = new List<string>();
                var success = AssetDatabase.DeleteAssets(paths, outFailedPaths);

                if (!success)
                {
                    response.errors ??= new();
                    foreach (var failedPath in outFailedPaths)
                        response.errors.Add($"Failed to delete asset at {failedPath}.");
                }

                // Add successfully deleted paths
                foreach (var path in paths)
                {
                    if (!outFailedPaths.Contains(path))
                    {
                        response.deletedPaths ??= new();
                        response.deletedPaths.Add(path);
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                UnityEditor.EditorApplication.RepaintProjectWindow();
                UnityEditor.EditorApplication.RepaintHierarchyWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return response;
            });
        }

        public class DeleteAssetsResponse
        {
            public List<string>? deletedPaths;
            public List<string>? errors;
        }
    }
}