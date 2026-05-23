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
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets
    {
        public const string AssetsRefreshToolId = "assets-refresh";
        [AiTool
        (
            AssetsRefreshToolId,
            Title = "Assets / Refresh",
            IdempotentHint = true
        )]
        [AiSkillDescription("Refresh the Unity AssetDatabase. " +
            "Use after files were added or updated outside of the Unity API, or to force script recompilation " +
            "when a '.cs' file changed. Returns a processing/success response and waits for compilation when triggered.")]
        [AiSkillBody("Refreshes the AssetDatabase. " +
            "Use it if any file was added or updated in the project outside of Unity API. " +
            "Use it if need to force scripts recompilation when '.cs' file changed.\n\n" +
            "## Inputs\n\n" +
            "- `options` — `ImportAssetOptions` flag (default `ForceSynchronousImport`).\n" +
            "- `requestId` — required for processing-mode responses; the tool returns `Processing` immediately and " +
            "delivers a follow-up completion when compilation finishes.\n\n" +
            "## Behavior\n\n" +
            "Runs `AssetDatabase.Refresh(options)`. If `EditorApplication.isCompiling` is true after the refresh, " +
            "schedules a post-compilation notification and returns a `Processing` response. If compilation already " +
            "failed (`EditorUtility.scriptCompilationFailed`), returns `Success` with the compilation error details. " +
            "Otherwise returns a plain `Success`.")]
        [Description("Refreshes the AssetDatabase. " +
            "Use it if any file was added or updated in the project outside of Unity API. " +
            "Use it if need to force scripts recompilation when '.cs' file changed.")]
        public async Task<ResponseCallTool> Refresh
        (
            [Description("Asset import options.")]
            ImportAssetOptions? options = ImportAssetOptions.ForceSynchronousImport,
            [RequestID]
            string? requestId = null
        )
        {
            if (requestId == null || string.IsNullOrWhiteSpace(requestId))
                return ResponseCallTool.Error("Original request with valid RequestID must be provided.");

            return await MainThread.Instance.RunAsync<ResponseCallTool>(() =>
            {
                AssetDatabase.Refresh(options ?? ImportAssetOptions.ForceSynchronousImport);

                if (EditorApplication.isCompiling)
                {
                    ScriptUtils.SchedulePostCompilationNotification(requestId, "AssetDatabase", "Assets refresh");
                    return ResponseCallTool.Processing("AssetDatabase refreshed. Compilation in progress, waiting for completion...").SetRequestID(requestId);
                }

                if (EditorUtility.scriptCompilationFailed)
                {
                    var errorDetails = ScriptUtils.GetCompilationErrorDetails();
                    return ResponseCallTool.Success($"[Warning] AssetDatabase refreshed, but compilation errors exist:\n\n{errorDetails}").SetRequestID(requestId);
                }

                return ResponseCallTool.Success("AssetDatabase refreshed successfully.").SetRequestID(requestId);
            });
        }
    }
}
