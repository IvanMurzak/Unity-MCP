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
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    public sealed partial class RqlEditorApi
    {
        [RqlCallable("open a scene asset (path \"Assets/…/X.unity\"); additive keeps the loaded scenes")]
        public Scene OpenScene(string path, bool additive = false)
        {
            var scene = EditorSceneManager.OpenScene(path, additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
            if (!scene.IsValid()) throw new ArgumentException($"no scene at '{path}'");
            return scene;
        }

        [RqlCallable("create a scene and save it at a new path (\"Assets/…/X.unity\", missing folders are created); empty = no camera / light; additive keeps the loaded scenes and makes the new one active (new GameObjects go there)")]
        public Scene NewScene(string path, bool empty = false, bool additive = false)
        {
            // Checked before the scene exists: a non-additive NewScene replaces the open scenes, so a save that fails afterwards
            // would leave the user with an untitled scene. An empty path would make SaveScene open the modal "Save Scene" dialog.
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("NewScene needs a path such as \"Assets/Scenes/X.unity\"");
            path = NewAssetPath(path, ".unity"); // Unity would overwrite an existing scene file
            return InFolderOf(path, () =>
            {
                var scene = EditorSceneManager.NewScene(empty ? NewSceneSetup.EmptyScene : NewSceneSetup.DefaultGameObjects,
                    additive ? NewSceneMode.Additive : NewSceneMode.Single);
                if (EditorSceneManager.SaveScene(scene, path)) return scene;
                if (additive) EditorSceneManager.CloseScene(scene, removeScene: true); // the open scenes stay as they were
                throw new ArgumentException($"could not save the new scene at '{path}'");
            });
        }

        // Call validator (RQL runs it before the call, on dry runs too): a NewScene that cannot be saved leaves the open scenes alone.
        public string? ValidateNewScene(string path, bool empty, bool additive) => Rejection(() =>
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("NewScene needs a path such as \"Assets/Scenes/X.unity\"");
            EnsureFolder(System.IO.Path.GetDirectoryName(NewAssetPath(path, ".unity")), create: false);
        });

        // The import hash of each open scene's file. Unity imports a scene file again when its content changed (a save in the
        // Editor imports it right away), so a hash that differs after AssetDatabase.Refresh means the file was changed outside.
        private static Dictionary<string, Hash128> OpenSceneHashes()
        {
            var hashes = new Dictionary<string, Hash128>(StringComparer.Ordinal);
            foreach (var scene in RqlUnity.Scenes())
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
                    hashes[scene.path] = AssetDatabase.GetAssetDependencyHash(scene.path);
            return hashes;
        }

        /// <summary>
        /// Reloads the open scenes whose file changed since <paramref name="before"/> (a scene with unsaved changes is kept: it
        /// would lose them) and says what happened, e.g. "refreshed; reloaded 'Assets/A.unity' (its file changed on disk)".
        /// </summary>
        private static string ReloadChangedScenes(Dictionary<string, Hash128> before)
        {
            var notes = new List<string>();
            foreach (var pair in before)
            {
                if (AssetDatabase.GetAssetDependencyHash(pair.Key) == pair.Value) continue;
                var scene = SceneManager.GetSceneByPath(pair.Key);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (scene.isDirty)
                    notes.Add($"'{pair.Key}' changed on disk but has unsaved changes in the Editor: kept the Editor's version (saving it " +
                              $"overwrites the file); editor | call OpenScene(\"{pair.Key}\") loads the file instead and drops those changes");
                else if (EditorApplication.isPlaying)
                    notes.Add($"'{pair.Key}' changed on disk: exit Play Mode, then call RefreshAssets() again or OpenScene(\"{pair.Key}\") to load it");
                else
                    notes.Add(Reload(scene)
                        ? $"reloaded '{pair.Key}' (its file changed on disk)"
                        : $"'{pair.Key}' changed on disk but could not be reloaded: editor | call OpenScene(\"{pair.Key}\") loads it");
            }
            return notes.Count == 0 ? "refreshed" : "refreshed; " + string.Join("; ", notes);
        }

        // Loads the scene's file again in place: the only open scene is reopened; one of several is unloaded and loaded again,
        // keeping its place in the Hierarchy and the active scene.
        private static bool Reload(Scene scene)
        {
            var path = scene.path;
            if (SceneManager.sceneCount == 1)
                return EditorSceneManager.OpenScene(path, OpenSceneMode.Single).IsValid();
            var active = SceneManager.GetActiveScene() == scene;
            if (!EditorSceneManager.CloseScene(scene, removeScene: false)) return false; // the last loaded scene cannot be closed
            var reloaded = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            if (active && reloaded.IsValid()) SceneManager.SetActiveScene(reloaded);
            return reloaded.IsValid();
        }

        [RqlCallable("save every modified loaded scene; returns false when a scene could not be saved (untitled scenes: SaveAs on the scene)")]
        public bool SaveScenes()
        {
            // Not SaveOpenScenes: it opens the modal "Save Scene" dialog for an untitled scene, which blocks the main thread.
            var problems = new List<string>();
            RqlUnity.SaveDirtyScenes(problems);
            return problems.Count == 0;
        }
    }
}
