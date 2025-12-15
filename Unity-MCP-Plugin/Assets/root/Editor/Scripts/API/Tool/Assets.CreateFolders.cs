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
            "assets-createfolder",
            Title = "Assets / Create Folder"
        )]
        [Description(@"Create folders at specific locations in the project.
Use it to organize scripts and assets in the project. Does AssetDatabase.Refresh() at the end.")]
        public string CreateFolders
        (
            [Description("The paths for the folders to create.")]
            string[] paths
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (paths.Length == 0)
                    return Error.SourcePathsArrayIsEmpty();

                var stringBuilder = new StringBuilder();

                for (var i = 0; i < paths.Length; i++)
                {
                    if (string.IsNullOrEmpty(paths[i]))
                    {
                        stringBuilder.AppendLine(Error.SourcePathIsEmpty());
                        continue;
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                UnityEditor.EditorApplication.RepaintHierarchyWindow();
                UnityEditor.EditorApplication.RepaintProjectWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                return stringBuilder.ToString();
            });
        }
    }
}
