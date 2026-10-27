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
            path = ScenePath(path);
            var scene = EditorSceneManager.NewScene(empty ? NewSceneSetup.EmptyScene : NewSceneSetup.DefaultGameObjects,
                additive ? NewSceneMode.Additive : NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new ArgumentException($"could not save the new scene at '{path}'");
            return scene;
        }

        // Call validator (RQL runs it before the call, on dry runs too): a NewScene that cannot be saved leaves the open scenes alone.
        public string? ValidateNewScene(string path, bool empty, bool additive) => Rejection(() =>
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("NewScene needs a path such as \"Assets/Scenes/X.unity\"");
            EnsureFolder(System.IO.Path.GetDirectoryName(NewAssetPath(path, ".unity")), create: false);
        });

        /// <summary>A new scene path: normalized, ".unity", not taken, its folder created. Unity would overwrite an existing scene file.</summary>
        internal static string ScenePath(string path)
        {
            path = NewAssetPath(path, ".unity");
            EnsureFolder(System.IO.Path.GetDirectoryName(path));
            return path;
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
