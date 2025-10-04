/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
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
        public static ResponseCallTool Refresh
        (
            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallTool.Error("Original request with valid RequestID must be provided.");

            return MainThread.Instance.Run(() =>
            {
                try
                {
                    // Check if compilation is currently in progress or failed
                    if (EditorUtility.scriptCompilationFailed)
                    {
                        return ResponseCallTool.Error("Cannot refresh assets while there are compilation errors. Please fix compilation errors first.").SetRequestID(requestId);
                    }

                    // Perform the asset refresh
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    
                    var assetCount = AssetDatabase.GetAllAssetPaths().Length;
                    
                    // Check if compilation started after refresh
                    if (EditorApplication.isCompiling)
                    {
                        // Compilation was triggered, schedule notification for when it completes
                        ScriptUtils.SchedulePostCompilationNotification(requestId, "AssetDatabase", "Asset refresh");
                        
                        return ResponseCallTool.Processing($"AssetDatabase refreshed. {assetCount} assets found. Compilation started, waiting for completion...").SetRequestID(requestId);
                    }
                    else
                    {
                        // No compilation triggered, return success immediately
                        return ResponseCallTool.Success($"[Success] AssetDatabase refreshed. {assetCount} assets found. Use 'Assets_Search' for more details.").SetRequestID(requestId);
                    }
                }
                catch (System.Exception ex)
                {
                    return ResponseCallTool.Error($"[Error] Failed to refresh AssetDatabase: {ex.Message}").SetRequestID(requestId);
                }
            });
        }
    }
}