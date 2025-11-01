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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Prefab
    {
        [McpPluginTool
        (
            "Assets_Prefab_Create",
            Title = "Create prefab from a GameObject in a scene"
        )]
        [Description("Create a prefab from a GameObject in a scene. The prefab will be saved in the project assets at the specified path.")]
        public string Create
        (
            [Description("Prefab asset path. Should be in the format 'Assets/Path/To/Prefab.prefab'.")]
            string prefabAssetPath,
            GameObjectRef gameObjectRef,
            [Description("If true, the prefab will replace the GameObject in the scene.")]
            bool replaceGameObjectWithPrefab = true
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(prefabAssetPath))
                return Error.PrefabPathIsEmpty();

            if (!prefabAssetPath.EndsWith(".prefab"))
                return Error.PrefabPathIsInvalid(prefabAssetPath);

            var go = gameObjectRef.FindGameObject(out var error);
            if (go == null)
                return $"[Error] {error}";

            var prefabGo = replaceGameObjectWithPrefab
                ? PrefabUtility.SaveAsPrefabAsset(go, prefabAssetPath)
                : PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabAssetPath, InteractionMode.UserAction, out _);

            if (prefabGo == null)
                return Error.NotFoundPrefabAtPath(prefabAssetPath);

            EditorUtility.SetDirty(go);
            EditorApplication.RepaintHierarchyWindow();

            var result = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.Serialize(
                prefabGo,
                recursive: false,
                logger: McpPlugin.McpPlugin.Instance.Logger
            );

            return $"[Success] Prefab '{prefabAssetPath}' created from GameObject '{go.name}' (InstanceID: {go.GetInstanceID()}).\n" +
                   $"Prefab GameObject:\n{result}";
        });
    }
}
