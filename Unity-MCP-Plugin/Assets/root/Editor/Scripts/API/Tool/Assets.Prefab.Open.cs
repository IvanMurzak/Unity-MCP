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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Prefab
    {
        public const string AssetsPrefabOpenToolId = "assets-prefab-open";
        [McpPluginTool
        (
            AssetsPrefabOpenToolId,
            Title = "Assets / Prefab / Open"
        )]
        [Description("Open prefab edit mode for a specific GameObject. " +
            "In the Edit mode you can modify the prefab. " +
            "The modification will be applied to all instances of the prefab across the project. " +
            "Note: Please use '" + AssetsPrefabCloseToolId + "' tool later to exit prefab editing mode.")]
        public void Open
        (
            [Description("GameObject that represents prefab instance of an original prefab GameObject.")]
            GameObjectRef gameObjectRef
        )
        {
            if (gameObjectRef?.IsValid(out var validationError) == false)
                throw new ArgumentException(validationError, nameof(gameObjectRef));

            MainThread.Instance.Run(() =>
            {
                var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                var gameObject = gameObjectRef.FindGameObject();

                if (gameObject == null)
                    throw new Exception("GameObject not found. Provide a reference to existed GameObject.");

                var prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

                prefabStage = gameObject.IsAsset()
                    ? UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(prefabAssetPath)
                    : UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(prefabAssetPath, gameObject);

                if (prefabStage == null)
                    throw new Exception("Failed to open prefab edit mode for the provided GameObject.");

                UnityEditor.EditorApplication.RepaintHierarchyWindow();
                UnityEditor.EditorApplication.RepaintProjectWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            });
        }
    }
}