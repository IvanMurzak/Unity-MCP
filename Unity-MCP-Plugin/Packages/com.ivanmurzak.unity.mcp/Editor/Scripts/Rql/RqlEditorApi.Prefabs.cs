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
        [RqlCallable("instantiate a prefab asset (path \"Assets/…/X.prefab\") as a linked instance, optionally under a parent at a local position, " +
                     "rotation (Euler degrees) and scale; returns it (undoable)")]
        public GameObject InstantiatePrefab(string path, GameObject? parent = null, Vector3? position = null, Vector3? rotation = null, Vector3? scale = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) ?? throw new ArgumentException(NoPrefab(path));
            return Place((GameObject)PrefabUtility.InstantiatePrefab(prefab), parent, position, rotation, scale, "RQL instantiate " + prefab.name);
        }

        [RqlCallable("instantiate a prefab asset placed by an object literal {parent, position, rotation (Euler degrees), scale}, " +
                     "e.g. InstantiatePrefab(\"Assets/Prefabs/Coin.prefab\", {position: [0, 1, 0]}); returns it (undoable)")]
        public GameObject InstantiatePrefab(string path, RqlPlacement? placement)
        {
            if (!string.IsNullOrEmpty(placement?.primitive)) throw new ArgumentException(PrefabTakesNoPrimitive);
            return InstantiatePrefab(path, placement?.parent, placement?.position, placement?.rotation, placement?.scale);
        }

        // Checked for every item before the first call (and on dry runs).
        public string? ValidateInstantiatePrefab(string path, GameObject? parent, Vector3? position, Vector3? rotation, Vector3? scale) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path) == null ? NoPrefab(path) : null;

        public string? ValidateInstantiatePrefab(string path, RqlPlacement? placement) =>
            !string.IsNullOrEmpty(placement?.primitive) ? PrefabTakesNoPrimitive
            : ValidateInstantiatePrefab(path, placement?.parent, placement?.position, placement?.rotation, placement?.scale);

        private static string NoPrefab(string path) => $"no prefab at '{path}'";

        private const string PrefabTakesNoPrimitive = "a prefab instance takes no primitive (CreateGameObject makes primitives)";

        [RqlCallable("open a prefab asset in prefab editing mode; its contents are then the prefab source")]
        public GameObject OpenPrefab(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new ArgumentException(NoPrefab(path));
            var stage = PrefabStageUtility.OpenPrefab(path) ?? throw new InvalidOperationException($"could not open '{path}'");
            return stage.prefabContentsRoot;
        }

        [RqlCallable("save the prefab being edited (prefab editing mode)")]
        public string SavePrefab()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage() ?? throw new InvalidOperationException("no prefab is open (OpenPrefab first)");
            Save(stage);
            return stage.assetPath;
        }

        // Every rql-mutate already saved the open prefab when it finished, so save = false only drops changes made outside RQL.
        [RqlCallable("leave the prefab being edited, back to the previous stage (save = true saves first; false drops unsaved changes, " +
                     "but each rql-mutate has already saved its own edits to the prefab: undo them first to revert)")]
        public void ClosePrefab(bool save = true)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage() ?? throw new InvalidOperationException("no prefab is open");
            if (save) Save(stage);
            // A dirty stage makes Unity ask "save changes?" in a modal dialog on leaving, which blocks the main thread.
            stage.ClearDirtiness();
            // Back one stage, not to the main stage: leaving the outer stages of a nested prefab edit would ask the same
            // modal "save changes?" for each dirty one.
            StageUtility.GoBackToPreviousStage();
        }

        private static void Save(PrefabStage stage)
        {
            if (RqlUnity.SavePrefabStage(stage) is string problem) throw new InvalidOperationException(problem);
        }

        /// <summary>The <c>prefab</c> source: the root of the prefab open in prefab editing mode, or nothing.</summary>
        public static GameObject? OpenPrefabRoot() => PrefabStageUtility.GetCurrentPrefabStage()?.prefabContentsRoot;
    }
}
