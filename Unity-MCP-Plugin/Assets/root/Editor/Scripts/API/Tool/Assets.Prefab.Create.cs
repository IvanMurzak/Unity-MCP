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
            "Creates folders recursively if they do not exist. " +
            "If the source GameObject is already a prefab instance and 'connectGameObjectToPrefab' is true, a Prefab Variant is created automatically. " +
            "To create a Prefab Variant from an existing prefab asset, provide 'sourcePrefabAssetPath' instead of 'gameObjectRef'. " +
            "Use '" + Tool_GameObject.GameObjectFindToolId + "' tool to find the target GameObject first.")]
        public AssetObjectRef Create
        (
            [Description("Prefab asset path. Should be in the format 'Assets/Path/To/Prefab.prefab'.")]
            string prefabAssetPath,
            [Description("Reference to a scene GameObject to create the prefab from. " +
                "If the GameObject is already a prefab instance, a Prefab Variant is created when 'connectGameObjectToPrefab' is true. " +
                "Optional if 'sourcePrefabAssetPath' is provided.")]
            GameObjectRef? gameObjectRef = null,
            [Description("Path to an existing prefab asset to create a Prefab Variant from (e.g. 'Assets/Prefabs/Base.prefab'). " +
                "When provided, a temporary instance is created, saved as a Prefab Variant, and cleaned up. " +
                "Optional if 'gameObjectRef' is provided.")]
            string? sourcePrefabAssetPath = null,
            [Description("If true, the scene GameObject will be connected to the new prefab (becoming a prefab instance). " +
                "If the source is already a prefab instance, this creates a Prefab Variant. " +
                "If false, the prefab asset is created but the scene GameObject remains unchanged. " +
                "Ignored when 'sourcePrefabAssetPath' is used (always creates a variant).")]
            bool connectGameObjectToPrefab = true
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(prefabAssetPath))
                    throw new ArgumentException(Error.PrefabPathIsEmpty(), nameof(prefabAssetPath));

                if (!prefabAssetPath.StartsWith("Assets/"))
                    throw new ArgumentException(Error.PrefabPathIsInvalid(prefabAssetPath), nameof(prefabAssetPath));

                if (!prefabAssetPath.EndsWith(".prefab"))
                    throw new ArgumentException(Error.PrefabPathIsInvalid(prefabAssetPath), nameof(prefabAssetPath));

                if (gameObjectRef != null && !string.IsNullOrEmpty(sourcePrefabAssetPath))
                    throw new ArgumentException("Provide either 'gameObjectRef' or 'sourcePrefabAssetPath', not both.", nameof(sourcePrefabAssetPath));

                // Create parent folders recursively if they do not exist
                var lastSlash = prefabAssetPath.LastIndexOf('/');
                var directoryPath = lastSlash > 0 ? prefabAssetPath.Substring(0, lastSlash) : null;
                if (directoryPath != null && !AssetDatabase.IsValidFolder(directoryPath))
                {
                    var segments = prefabAssetPath.Split('/');
                    var currentPath = segments[0]; // "Assets"
                    for (int i = 1; i < segments.Length - 1; i++)
                    {
                        var nextPath = currentPath + "/" + segments[i];
                        if (!AssetDatabase.IsValidFolder(nextPath))
                            AssetDatabase.CreateFolder(currentPath, segments[i]);
                        currentPath = nextPath;
                    }
                }

                UnityEngine.GameObject prefabGo;

                if (!string.IsNullOrEmpty(sourcePrefabAssetPath))
                {
                    // Create a Prefab Variant from an existing prefab asset
                    var sourcePrefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(sourcePrefabAssetPath);
                    if (sourcePrefab == null)
                        throw new ArgumentException(Error.NotFoundPrefabAtPath(sourcePrefabAssetPath!), nameof(sourcePrefabAssetPath));

                    var tempInstance = PrefabUtility.InstantiatePrefab(sourcePrefab) as UnityEngine.GameObject;
                    if (tempInstance == null)
                        throw new InvalidOperationException($"Failed to instantiate prefab from '{sourcePrefabAssetPath}'.");

                    try
                    {
                        // SaveAsPrefabAssetAndConnect on a prefab instance creates a Prefab Variant
                        prefabGo = PrefabUtility.SaveAsPrefabAssetAndConnect(tempInstance, prefabAssetPath, InteractionMode.UserAction, out _);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(tempInstance);
                    }
                }
                else
                {
                    // Create from a scene GameObject
                    if (gameObjectRef == null)
                        throw new ArgumentException("Either 'gameObjectRef' or 'sourcePrefabAssetPath' must be provided.", nameof(gameObjectRef));

                    var go = gameObjectRef.FindGameObject(out var error);
                    if (go == null)
                        throw new ArgumentException(error, nameof(gameObjectRef));

                    prefabGo = connectGameObjectToPrefab
                        ? PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabAssetPath, InteractionMode.UserAction, out _)
                        : PrefabUtility.SaveAsPrefabAsset(go, prefabAssetPath);

                    if (connectGameObjectToPrefab && prefabGo != null)
                        EditorUtility.SetDirty(go);
                }

                if (prefabGo == null)
                    throw new Exception(Error.NotFoundPrefabAtPath(prefabAssetPath));

                EditorUtils.RepaintAllEditorWindows();

                return new AssetObjectRef(prefabGo);
            });
        }
    }
}
