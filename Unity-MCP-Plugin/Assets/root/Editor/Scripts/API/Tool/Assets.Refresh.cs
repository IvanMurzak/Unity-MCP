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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        [McpPluginTool
        (
            "Assets_Refresh",
            Title = "Assets Refresh"
        )]
        [Description(@"Refreshes the AssetDatabase. Use it if any new files were added or updated in the project outside of Unity API.
Don't need to call it for Scripts manipulations.
It also triggers scripts recompilation if any changes in '.cs' files.")]
        public string Refresh() => MainThread.Instance.Run(() =>
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return @$"[Success] AssetDatabase refreshed. {AssetDatabase.GetAllAssetPaths().Length} assets found. Use 'Assets_Search' for more details.";
        });
    }
}