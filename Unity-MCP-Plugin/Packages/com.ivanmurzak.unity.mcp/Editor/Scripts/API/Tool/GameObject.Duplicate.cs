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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        public const string GameObjectDuplicateToolId = "gameobject-duplicate";
        [McpPluginTool
        (
            GameObjectDuplicateToolId,
            Title = "GameObject / Duplicate"
        )]
        [Description("Duplicate GameObjects in opened Prefab or in a Scene. " +
            "Use '" + GameObjectFindToolId + "' tool to find the target GameObjects first.")]
        public List<GameObjectRef> Duplicate(GameObjectRefList gameObjectRefs)
        {
            return MainThread.Instance.Run(() =>
            {
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                var gos = new List<GameObject>(gameObjectRefs.Count);

                for (int i = 0; i < gameObjectRefs.Count; i++)
                {
                    var gameObjectRef = gameObjectRefs[i];
                    var go = gameObjectRefs[i].FindGameObject(out var error);
                    if (error != null)
                        throw new System.Exception(error);
                    if (go == null)
                        throw new System.Exception($"GameObject by {nameof(gameObjectRefs)}[{i}] not found.");

                    gos.Add(go);
                }

#if UNITY_6000_3_OR_NEWER
                Selection.entityIds = gos.Select(go => go.GetEntityId()).ToArray();
#else
                Selection.instanceIDs = gos.Select(go => go.GetInstanceID()).ToArray();
#endif

                Unsupported.DuplicateGameObjectsUsingPasteboard();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                var modifiedScenes = Selection.gameObjects
                    .Select(go => go.scene)
                    .Distinct()
                    .ToList();

                foreach (var scene in modifiedScenes)
                    EditorSceneManager.MarkSceneDirty(scene);

                return gos
                    .Select(go => new GameObjectRef(go))
                    .ToList();
            });
        }
    }
}
