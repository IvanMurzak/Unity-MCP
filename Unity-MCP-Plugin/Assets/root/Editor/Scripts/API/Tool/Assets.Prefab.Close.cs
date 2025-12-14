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
using UnityEditor.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Prefab
    {
        [McpPluginTool
        (
            "Assets_Prefab_Close",
            Title = "Assets / Prefab / Close"
        )]
        [Description("Close currently opened prefab. Use it when you are in prefab editing mode in Unity Editor.")]
        public string Close
        (
            [Description("True to save prefab. False to discard changes.")]
            bool save = true
        )
        => MainThread.Instance.Run(() =>
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null)
                return Error.PrefabStageIsNotOpened();

            var prefabGo = prefabStage.prefabContentsRoot;
            if (prefabGo == null)
                return Error.PrefabStageIsNotOpened();

            var assetPath = prefabStage.assetPath;
            var goName = prefabGo.name;

            if (save)
                PrefabUtility.SaveAsPrefabAsset(prefabGo, assetPath);

            StageUtility.GoBackToPreviousStage();

            UnityEditor.EditorApplication.RepaintHierarchyWindow();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            return @$"[Success] Prefab at asset path '{assetPath}' closed. " +
                   $"Prefab with GameObject.name '{goName}' saved: {save}.";
        });
    }
}
