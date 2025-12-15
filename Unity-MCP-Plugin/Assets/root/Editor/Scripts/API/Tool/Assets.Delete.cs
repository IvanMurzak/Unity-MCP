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
            "assets-delete",
            Title = "Assets / Delete"
        )]
        [Description(@"Delete the assets at paths from the project. Does AssetDatabase.Refresh() at the end.")]
        public string Delete
        (
            [Description("The paths of the assets")]
            string[] paths
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (paths.Length == 0)
                    return Error.SourcePathsArrayIsEmpty();

                var outFailedPaths = new List<string>();
                var success = AssetDatabase.DeleteAssets(paths, outFailedPaths);
                if (!success)
                {
                    var stringBuilder = new StringBuilder();
                    foreach (var failedPath in outFailedPaths)
                        stringBuilder.AppendLine($"[Error] Failed to delete asset at {failedPath}.");
                    return stringBuilder.ToString();
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                UnityEditor.EditorApplication.RepaintProjectWindow();
                UnityEditor.EditorApplication.RepaintHierarchyWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return "[Success] Deleted assets at paths:\n" + string.Join("\n", paths);
            });
        }
    }
}