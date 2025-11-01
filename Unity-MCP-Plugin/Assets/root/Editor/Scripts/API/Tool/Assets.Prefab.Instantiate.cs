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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Prefab
    {
        [McpPluginTool
        (
            "Assets_Prefab_Instantiate",
            Title = "Instantiate prefab in the current active scene"
        )]
        [Description("Instantiates prefab in a scene.")]
        public string Instantiate
        (
            [Description("Prefab asset path.")]
            string prefabAssetPath,
            [Description("GameObject path in the current active scene.")]
            string gameObjectPath,
            [Description("Transform position of the GameObject.")]
            Vector3? position = default,
            [Description("Transform rotation of the GameObject. Euler angles in degrees.")]
            Vector3? rotation = default,
            [Description("Transform scale of the GameObject.")]
            Vector3? scale = default,
            [Description("World or Local space of transform.")]
            bool isLocalSpace = false
        )
        => MainThread.Instance.Run(() =>
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
            if (prefab == null)
                return Error.NotFoundPrefabAtPath(prefabAssetPath);

            var parentGo = default(GameObject);
            if (StringUtils.Path_ParseParent(gameObjectPath, out var parentPath, out var name))
            {
                parentGo = GameObjectUtils.FindByPath(parentPath);
                if (parentGo == null)
                    return Tool_GameObject.Error.NotFoundGameObjectAtPath(parentPath);
            }

            position ??= Vector3.zero;
            rotation ??= Vector3.zero;
            scale ??= Vector3.one;

            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            go.name = name ?? prefab.name;
            if (parentGo != null)
                go.transform.SetParent(parentGo.transform, false);
            go.SetTransform(position, rotation, scale, isLocalSpace);

            var bounds = go.CalculateBounds();

            EditorUtility.SetDirty(go);
            EditorApplication.RepaintHierarchyWindow();

            return $"[Success] Prefab successfully instantiated.\n{go.Print()}";
        });
    }
}
