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

        [RqlCallable("create a scene and save it at path (\"Assets/…/X.unity\"); empty = no camera / light; additive keeps the loaded scenes")]
        public Scene NewScene(string path, bool empty = false, bool additive = false)
        {
            // An empty path would make SaveScene open the modal "Save Scene" dialog.
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("NewScene needs a path such as \"Assets/Scenes/X.unity\"");
            var scene = EditorSceneManager.NewScene(empty ? NewSceneSetup.EmptyScene : NewSceneSetup.DefaultGameObjects,
                additive ? NewSceneMode.Additive : NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new ArgumentException($"could not save the new scene at '{path}'");
            return scene;
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
