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
        public const string AssetsPrefabSaveToolId = "assets-prefab-save";
        [McpPluginTool
        (
            AssetsPrefabSaveToolId,
            Title = "Assets / Prefab / Save",
            IdempotentHint = true
        )]
        [McpPluginSkillDescription("Save the currently opened prefab edit stage back to its prefab asset without exiting the stage. " +
            "Pair with '" + AssetsPrefabOpenToolId + "' to enter the edit mode first.")]
        [McpPluginSkillBody("Save a prefab. " +
            "Use it when you are in prefab editing mode in Unity Editor. " +
            "Use '" + AssetsPrefabOpenToolId + "' tool to open a prefab first.\n\n" +
            "## Behavior\n\n" +
            "Calls `PrefabUtility.SaveAsPrefabAsset` on the current prefab stage's contents root, clears the stage's " +
            "dirtiness flag, repaints editor windows, and returns an `AssetObjectRef` to the saved prefab. " +
            "Throws when no prefab stage is currently open.")]
        [Description("Save a prefab. " +
            "Use it when you are in prefab editing mode in Unity Editor. " +
            "Use '" + AssetsPrefabOpenToolId + "' tool to open a prefab first.")]
        public AssetObjectRef Save(string? nothing = null) => MainThread.Instance.Run(() =>
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null)
                throw new InvalidOperationException(Error.PrefabStageIsNotOpened());

            var prefabGo = prefabStage.prefabContentsRoot;
            if (prefabGo == null)
                throw new InvalidOperationException(Error.PrefabStageIsNotOpened());

            var assetPath = prefabStage.assetPath;
            var goName = prefabGo.name;

            PrefabUtility.SaveAsPrefabAsset(prefabGo, assetPath);
            prefabStage.ClearDirtiness();

            EditorUtils.RepaintAllEditorWindows();

            var assetPrefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(assetPath);
            return new AssetObjectRef(assetPrefab);
        });
    }
}