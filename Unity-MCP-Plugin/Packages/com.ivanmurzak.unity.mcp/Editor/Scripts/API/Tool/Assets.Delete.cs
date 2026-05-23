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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsDeleteToolId = "assets-delete";
        [AiTool
        (
            AssetsDeleteToolId,
            Title = "Assets / Delete",
            DestructiveHint = true,
            Enabled = false
        )]
        [AiSkillDescription("Delete the assets at the given project paths. " +
            "Refreshes the AssetDatabase at the end. " +
            "Use '" + AssetsFindToolId + "' to locate the assets first.")]
        [AiSkillBody("Delete the assets at paths from the project. " +
            "Does AssetDatabase.Refresh() at the end. " +
            "Use '" + AssetsFindToolId + "' tool to find assets before deleting.\n\n" +
            "## Inputs\n\n" +
            "- `paths` — project-relative asset paths to delete. Must be non-empty.\n\n" +
            "## Behavior\n\n" +
            "Routes through `AssetDatabase.DeleteAssets`, which deletes the batch atomically. " +
            "Paths Unity reports as failed are surfaced in `response.Errors`; successfully deleted paths " +
            "are surfaced in `response.DeletedPaths`. The tool is destructive (removes files from disk).")]
        [Description("Delete the assets at paths from the project. " +
            "Does AssetDatabase.Refresh() at the end. " +
            "Use '" + AssetsFindToolId + "' tool to find assets before deleting.")]
        public DeleteAssetsResponse Delete
        (
            [Description("The paths of the assets")]
            string[] paths
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Assets>();

                if (paths.Length == 0)
                    throw new System.Exception(Error.SourcePathsArrayIsEmpty());

                logger.LogInformation("Deleting {Count} asset(s): {Paths}", paths.Length, string.Join(", ", paths));

                var response = new DeleteAssetsResponse();
                var outFailedPaths = new List<string>();
                var success = AssetDatabase.DeleteAssets(paths, outFailedPaths);

                if (!success)
                {
                    response.Errors ??= new();
                    foreach (var failedPath in outFailedPaths)
                    {
                        logger.LogWarning("Failed to delete asset at '{Path}'", failedPath);
                        response.Errors.Add($"Failed to delete asset at {failedPath}.");
                    }
                }

                // Add successfully deleted paths
                foreach (var path in paths)
                {
                    if (!outFailedPaths.Contains(path))
                    {
                        logger.LogInformation("Successfully deleted asset at '{Path}'", path);
                        response.DeletedPaths ??= new();
                        response.DeletedPaths.Add(path);
                    }
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorUtils.RepaintAllEditorWindows();

                return response;
            });
        }

    }
}