/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using Rql;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    public sealed partial class RqlEditorApi
    {
        [RqlCallable("instantiate a prefab asset (path \"Assets/…/X.prefab\") as a linked instance, optionally under a parent at a local position; returns it (undoable)")]
        public GameObject InstantiatePrefab(string path, GameObject? parent = null, Vector3? position = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new ArgumentException($"no prefab at '{path}'");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent.transform, false);
            if (position.HasValue) go.transform.localPosition = position.Value;
            Undo.RegisterCreatedObjectUndo(go, "RQL instantiate " + prefab.name);
            return go;
        }

        [RqlCallable("open a prefab asset in prefab editing mode; its contents are then the prefab source")]
        public GameObject OpenPrefab(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new ArgumentException($"no prefab at '{path}'");
            var stage = PrefabStageUtility.OpenPrefab(path) ?? throw new InvalidOperationException($"could not open '{path}'");
            return stage.prefabContentsRoot;
        }

        [RqlCallable("save the prefab being edited (prefab editing mode)")]
        public string SavePrefab()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage() ?? throw new InvalidOperationException("no prefab is open (OpenPrefab first)");
            PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
            stage.ClearDirtiness();
            return stage.assetPath;
        }

        [RqlCallable("leave prefab editing mode (save = true saves first)")]
        public void ClosePrefab(bool save = true)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage() ?? throw new InvalidOperationException("no prefab is open");
            if (save) PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
            StageUtility.GoToMainStage();
        }

        /// <summary>The <c>prefab</c> source: the root of the prefab open in prefab editing mode, or nothing.</summary>
        public static GameObject? OpenPrefabRoot() => PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot;
    }
}
