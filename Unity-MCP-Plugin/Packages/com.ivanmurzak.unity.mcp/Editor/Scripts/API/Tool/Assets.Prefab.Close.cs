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
using AIGD;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Prefab
    {
        public const string AssetsPrefabCloseToolId = "assets-prefab-close";
        [AiTool
        (
            AssetsPrefabCloseToolId,
            Title = "Assets / Prefab / Close"
        )]
        [AiSkillDescription("Close the currently opened prefab edit stage. " +
            "Optionally saves changes back to the prefab asset before closing. " +
            "Pair with '" + AssetsPrefabOpenToolId + "' to enter the edit mode first.")]
        [AiSkillBody("Close currently opened prefab. " +
            "Use it when you are in prefab editing mode in Unity Editor. " +
            "Use '" + AssetsPrefabOpenToolId + "' tool to open a prefab first.\n\n" +
            "## Inputs\n\n" +
            "- `save` (default `true`) — when `true`, calls `PrefabUtility.SaveAsPrefabAsset` before exiting the stage; " +
            "when `false`, the save is skipped. The prefab stage's dirtiness is always cleared at the end, so any " +
            "unsaved changes are discarded when `save` is `false`.\n\n" +
            "## Behavior\n\n" +
            "Throws when no prefab stage is currently open. Returns an `AssetObjectRef` for the closed prefab asset.")]
        [Description("Close currently opened prefab. " +
            "Use it when you are in prefab editing mode in Unity Editor. " +
            "Use '" + AssetsPrefabOpenToolId + "' tool to open a prefab first.")]
        public AssetObjectRef Close
        (
            [Description("True to save prefab. False to discard changes.")]
            bool save = true
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage == null)
                    throw new InvalidOperationException(Error.PrefabStageIsNotOpened());

                var prefabGo = prefabStage.prefabContentsRoot;
                if (prefabGo == null)
                    throw new InvalidOperationException(Error.PrefabStageIsNotOpened());

                var assetPath = prefabStage.assetPath;

                if (save)
                    PrefabUtility.SaveAsPrefabAsset(prefabGo, assetPath);

                prefabStage.ClearDirtiness();

                StageUtility.GoBackToPreviousStage();

                EditorUtils.RepaintAllEditorWindows();

                var prefabAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(assetPath);

                return new AssetObjectRef(prefabAsset);
            });
        }
    }
}
