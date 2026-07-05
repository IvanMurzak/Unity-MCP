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
#if UNITY_EDITOR && UNITY_6000_5_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.McpPlugin.Common;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static partial class GameObjectUtils
    {
        /// <summary>
        /// Find Root GameObject in opened Prefab. Of array of GameObjects in a scene.
        /// </summary>
        /// <param name="scene">Scene for the search, if null every opened scene is used
        /// (plus the DontDestroyOnLoad scene while in Play Mode)</param>
        /// <returns>Array of root GameObjects</returns>
        public static UnityEngine.GameObject[] FindRootGameObjects(Scene? scene = null)
        {
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                return prefabStage.prefabContentsRoot.MakeArray();

            if (scene != null)
                return scene.Value.GetRootGameObjects();

            // No specific scene requested: search every opened scene, not just the
            // active one (GetActiveScene() alone misses additively-loaded scenes),
            // plus DontDestroyOnLoad, which isn't part of SceneManager's scene list
            // and only exists once the Editor/Player is in Play Mode.
            var roots = new List<UnityEngine.GameObject>();
            foreach (var openedScene in SceneUtils.GetAllOpenedScenes())
                roots.AddRange(openedScene.GetRootGameObjects());

            if (UnityEngine.Application.isPlaying)
            {
                // Every GameObject is disjoint from the sets above: an object lives
                // in exactly one scene, and DontDestroyOnLoad is not enumerable via
                // SceneManager, so no dedupe against 'roots' is needed here.
                var ddolRoots = UnityEngine.Object
                    .FindObjectsByType<UnityEngine.GameObject>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)
                    .Where(go => go.transform.parent == null && go.scene.name == "DontDestroyOnLoad");
                roots.AddRange(ddolRoots);
            }

            return roots.ToArray();
        }
        public static UnityEngine.GameObject? FindByInstanceID(UnityEngine.EntityId instanceID)
        {
            if (instanceID == UnityEngine.EntityId.None)
                return null;

            var obj = UnityEditor.EditorUtility.EntityIdToObject(instanceID);
            if (obj is not UnityEngine.GameObject go)
                return null;

            return go;
        }
    }
}
#endif
