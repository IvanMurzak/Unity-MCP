/*
 *  Author: Ivan Murzak (https://github.com/IvanMurzak)
 *  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)
 *  Copyright (c) 2025 Ivan Murzak
 *  Licensed under the Apache License, Version 2.0.
 *  See the LICENSE file in the project root for more information.
 */

#nullable enable
#if UNITY_EDITOR && !UNITY_6000_5_OR_NEWER
using System.Linq;
using com.IvanMurzak.McpPlugin.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static partial class GameObjectUtils
    {
        /// <summary>
        /// Find Root GameObject in opened Prefab, active scene, and DontDestroyOnLoad.
        /// </summary>
        /// <param name="scene">Scene for the search, if null the current active scene would be used</param>
        /// <returns>Array of root GameObjects</returns>
        public static GameObject[] FindRootGameObjects(Scene? scene = null)
        {
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                return prefabStage.prefabContentsRoot.MakeArray();

            GameObject[] rootGos;

            if (scene == null)
            {
                rootGos = UnityEditor.SceneManagement.EditorSceneManager
                    .GetActiveScene()
                    .GetRootGameObjects();
            }
            else
            {
                rootGos = scene.Value.GetRootGameObjects();
            }

            // Include DontDestroyOnLoad root objects (only available at runtime)
            if (UnityEditor.EditorApplication.isPlaying)
            {
                var ddolRoots = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(go => go.scene.name == "DontDestroyOnLoad"
                        && go.transform.parent == null
                        && (go.hideFlags == HideFlags.None || go.hideFlags == HideFlags.HideInHierarchy))
                    .ToArray();

                if (ddolRoots.Length > 0)
                {
                    var combined = new GameObject[rootGos.Length + ddolRoots.Length];
                    rootGos.CopyTo(combined, 0);
                    ddolRoots.CopyTo(combined, rootGos.Length);
                    return combined;
                }
            }

            return rootGos;
        }

        public static GameObject? FindByInstanceID(int instanceID)
        {
            if (instanceID == 0)
                return null;

#if UNITY_6000_3_OR_NEWER
            var obj = UnityEditor.EditorUtility.EntityIdToObject((EntityId)instanceID);
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
