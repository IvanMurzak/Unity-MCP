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
#if UNITY_EDITOR && !UNITY_6000_5_OR_NEWER
using System.Collections.Generic;
using com.IvanMurzak.McpPlugin.Common;
using UnityEngine;
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
        public static GameObject[] FindRootGameObjects(Scene? scene = null)
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
            var roots = new List<GameObject>();
            foreach (var openedScene in SceneUtils.GetAllOpenedScenes())
                roots.AddRange(openedScene.GetRootGameObjects());

            if (Application.isPlaying)
            {
                // DontDestroyOnLoad is never enumerable via SceneManager.GetSceneAt
                // or GetSceneByName (it isn't part of that registry at all), so the
                // only way to get its Scene handle is from an object already known
                // to live there. GetDontDestroyOnLoadScene() probes for it once and
                // caches the handle, avoiding a FindObjectsByType world-scan on
                // every no-scene find call while playing.
                var ddolScene = GetDontDestroyOnLoadScene();
                if (ddolScene.IsValid())
                {
                    // Object.Destroy(s_ddolProbe) below is deferred to end-of-frame, so
                    // on the call that (re)builds the cache the probe may still be a
                    // root here. Filter it by reference rather than relying on destroy
                    // timing.
                    foreach (var root in ddolScene.GetRootGameObjects())
                    {
                        if (root != s_ddolProbe)
                            roots.Add(root);
                    }
                }
            }

            return roots.ToArray();
        }

        private static Scene? s_ddolSceneCache;
        private static GameObject? s_ddolProbe;

        private static Scene GetDontDestroyOnLoadScene()
        {
            if (s_ddolSceneCache.HasValue && s_ddolSceneCache.Value.IsValid())
                return s_ddolSceneCache.Value;

            var probe = new GameObject("~DDOLSceneProbe") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(probe);
            var scene = probe.scene;
            Object.Destroy(probe);

            s_ddolSceneCache = scene;
            s_ddolProbe = probe;
            return scene;
        }
        public static GameObject? FindByInstanceID(int instanceID)
        {
            if (instanceID == 0)
                return null;

#if UNITY_6000_3_OR_NEWER
            var obj = UnityEditor.EditorUtility.EntityIdToObject((UnityEngine.EntityId)instanceID);
#else
            var obj = UnityEditor.EditorUtility.InstanceIDToObject(instanceID);
#endif
            if (obj is not GameObject go)
                return null;

            return go;
        }
    }
}
#endif
