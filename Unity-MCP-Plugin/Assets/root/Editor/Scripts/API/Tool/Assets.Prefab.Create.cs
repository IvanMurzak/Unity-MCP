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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Prefab
    {
        public const string AssetsPrefabCreateToolId = "assets-prefab-create";
        [McpPluginTool
        (
            AssetsPrefabCreateToolId,
            Title = "Assets / Prefab / Create"
        )]
        [Description("Create a prefab from a GameObject in the current active scene. " +
            "The prefab will be saved in the project assets at the specified path. " +
            "Use '" + Tool_GameObject.GameObjectFindToolId + "' tool to find the target GameObject first.")]
        public AssetObjectRef Create
        (
            [Description("Prefab asset path. Should be in the format 'Assets/Path/To/Prefab.prefab'.")]
            string prefabAssetPath,
            GameObjectRef gameObjectRef,
            [Description("If true, the prefab will replace the GameObject in the scene.")]
            bool replaceGameObjectWithPrefab = true
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(prefabAssetPath))
                    throw new ArgumentException(Error.PrefabPathIsEmpty(), nameof(prefabAssetPath));

                if (!prefabAssetPath.EndsWith(".prefab"))
                    throw new ArgumentException(Error.PrefabPathIsInvalid(prefabAssetPath), nameof(prefabAssetPath));

                var go = gameObjectRef.FindGameObject(out var error);
                if (go == null)
                    throw new ArgumentException(error, nameof(gameObjectRef));

                var prefabGo = replaceGameObjectWithPrefab
                    ? PrefabUtility.SaveAsPrefabAsset(go, prefabAssetPath)
                    : PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabAssetPath, InteractionMode.UserAction, out _);

                if (prefabGo == null)
                    throw new Exception(Error.NotFoundPrefabAtPath(prefabAssetPath));

                EditorUtility.SetDirty(go);

                UnityMcpPlugin.NotifyToolExecuted();
                EditorUtils.RepaintAllEditorWindows();

                var assetPrefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(prefabAssetPath);
                return new AssetObjectRef(assetPrefab);
            });
        }
    }
}
