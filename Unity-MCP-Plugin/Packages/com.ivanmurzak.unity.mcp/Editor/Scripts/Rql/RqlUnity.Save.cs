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
using System.Collections.Generic;
using Rql;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    public static partial class RqlUnity
    {
        private static int _mutating;

        /// <summary>
        /// Raised by <see cref="Mutate"/> once its query finished, before assets and scenes are saved: modules that defer
        /// their own file writes during a mutation (e.g. <c>.inputactions</c> files) flush them here.
        /// </summary>
        public static event Action? Saving;

        /// <summary>True while <see cref="Mutate"/> runs a query: writes may be deferred to <see cref="Saving"/>.</summary>
        public static bool InMutation => _mutating > 0;

        /// <summary>
        /// Run a set / call query and save the result to disk: changed assets, modified scenes and the open prefab. Saving
        /// runs after failed queries too, because completed calls are not rolled back. Untitled scenes (no path yet) stay
        /// in memory and are reported as a warning.
        /// </summary>
        public static QueryResult Mutate(string query, bool dryRun = false)
        {
            _mutating++;
            QueryResult result;
            try
            {
                result = Engine.Execute(query, null, new QueryOptions { AllowMutation = true, DryRun = dryRun });
            }
            finally
            {
                _mutating--;
            }
            if (!dryRun && _mutating == 0)
                foreach (var problem in SaveChanges())
                    result.Diagnostics.Add(Diagnostic.Warning("not-saved", problem));
            return result;
        }

        /// <summary>Write every pending change to disk; returns what could not be saved.</summary>
        public static List<string> SaveChanges()
        {
            var problems = new List<string>();
            if (Saving != null)
                foreach (Action flush in Saving.GetInvocationList())
                {
                    try { flush(); }
                    catch (Exception ex) { problems.Add(ex.Message); }
                }
            AssetDatabase.SaveAssets();
            if (EditorApplication.isPlaying) return problems; // play mode scene changes are temporary by design

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty)
            {
                PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath, out var saved);
                if (saved) stage.ClearDirtiness();
                else problems.Add($"the prefab '{stage.assetPath}' could not be saved");
            }
            foreach (var scene in Scenes())
            {
                if (!scene.isDirty) continue;
                if (string.IsNullOrEmpty(scene.path))
                    problems.Add($"scene '{Name(scene)}' has no file yet; save it with scenes | where path == \"\" | call SaveAs(\"Assets/Scenes/X.unity\")");
                else if (!EditorSceneManager.SaveScene(scene))
                    problems.Add($"scene '{scene.path}' could not be saved");
            }
            return problems;
        }

        private static string Name(Scene scene) => string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
    }
}
