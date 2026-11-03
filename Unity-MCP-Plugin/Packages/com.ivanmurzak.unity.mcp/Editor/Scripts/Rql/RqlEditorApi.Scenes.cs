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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        // The content hash of each open scene's file as the Editor last loaded or saved it, by path. Kept in SessionState so it
        // survives domain reloads: a refresh by any other tool (script-update-or-create, tests-run, …) imports a changed scene
        // file without reloading the open scene, so RefreshAssets compares the file with this, not with the last import.
        private const string SceneFileHashesKey = "com.IvanMurzak.Unity.MCP.Rql.SceneFileHashes";

        [InitializeOnLoadMethod]
        private static void TrackSceneFiles()
        {
            EditorSceneManager.sceneOpened += (scene, _) => RememberSceneFile(scene);
            // A save to another path (Save As, a copy) does not write the scene's own file: its recorded hash stays.
            EditorSceneManager.sceneSaving += (scene, path) => _savingTo = path;
            EditorSceneManager.sceneSaved += scene =>
            {
                if (_savingTo == null || _savingTo == scene.path) RememberSceneFile(scene);
                _savingTo = null;
            };
            // Scenes the Editor opened before this domain registered the callbacks (Editor start): their file as it is now.
            EditorApplication.delayCall += () =>
            {
                foreach (var scene in RqlUnity.Scenes())
                    if (scene.isLoaded && !scene.isDirty && !string.IsNullOrEmpty(scene.path) && !SceneFileHashes().ContainsKey(scene.path))
                        RememberSceneFile(scene);
            };
        }

        private static string? _savingTo;

        private static Dictionary<string, string> SceneFileHashes()
        {
            var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in SessionState.GetString(SceneFileHashesKey, "").Split('\n'))
                if (line.Split('\t') is { Length: 2 } entry) hashes[entry[0]] = entry[1];
            return hashes;
        }

        private static void RememberSceneFile(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.path)) return;
            var hashes = SceneFileHashes();
            if (FileHash(scene.path) is string hash) hashes[scene.path] = hash;
            else hashes.Remove(scene.path); // unreadable: the import hash decides
            SessionState.SetString(SceneFileHashesKey, string.Join("\n", hashes.Select(p => p.Key + "\t" + p.Value)));
        }

        // The content hash of a scene asset's file (streamed: scenes can be large), or null when it is missing or unreadable. A
        // Packages/ path is resolved to the package's folder on disk.
        // A reloaded scene's file is the new baseline (whether or not Unity raised sceneOpened for the reload).
        private static bool Remembered(string path)
        {
            RememberSceneFile(SceneManager.GetSceneByPath(path));
            return true;
        }

        private static string? FileHash(string path)
        {
            try
            {
                var file = SceneFile(path);
                if (!File.Exists(file)) return null;
                using var stream = File.OpenRead(file);
                using var sha = SHA256.Create();
                return Convert.ToBase64String(sha.ComputeHash(stream));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static string SceneFile(string path) => FileUtil.GetPhysicalPath(path);

        // The import hash of each open scene's file: the fallback for a scene with no recorded file hash. Unity imports a scene
        // file again when its content changed, so a hash that differs after this call's AssetDatabase.Refresh means the file changed.
        private static Dictionary<string, Hash128> OpenSceneHashes()
        {
            var hashes = new Dictionary<string, Hash128>(StringComparer.Ordinal);
            foreach (var scene in RqlUnity.Scenes())
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
                    hashes[scene.path] = AssetDatabase.GetAssetDependencyHash(scene.path);
            return hashes;
        }

        /// <summary>
        /// Reloads the open scenes whose file changed on disk since the Editor loaded or saved it (a scene with unsaved changes is
        /// kept: it would lose them) and says what happened, e.g. "refreshed; reloaded 'Assets/A.unity' (its file changed on disk)".
        /// </summary>
        private static string ReloadChangedScenes(Dictionary<string, Hash128> importHashes)
        {
            var notes = new List<string>();
            var known = SceneFileHashes();
            foreach (var pair in importHashes)
            {
                var path = pair.Key;
                var changed = known.TryGetValue(path, out var hash)
                    ? FileHash(path) != hash
                    : AssetDatabase.GetAssetDependencyHash(path) != pair.Value;
                if (!changed) continue;
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (!File.Exists(SceneFile(path)))
                    notes.Add($"'{path}' was deleted or moved on disk: kept the open scene (scenes | where path == \"{path}\" | call SaveAs(\"{path}\") " +
                              "writes it again, as a new asset)");
                else if (scene.isDirty)
                    notes.Add($"'{path}' changed on disk but has unsaved changes in the Editor: kept the Editor's version (saving it " +
                              $"overwrites the file); {LoadFileHint(path)} loads the file instead and drops those changes");
                else if (EditorApplication.isPlaying)
                    notes.Add($"'{path}' changed on disk: exit Play Mode, then call RefreshAssets() again");
                else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                    notes.Add($"'{path}' changed on disk: close the open prefab (editor | call ClosePrefab(), which saves it; ClosePrefab(false) " +
                              "drops its unsaved edits), then call RefreshAssets() again");
                else
                    notes.Add(Reload(scene) && Remembered(path)
                        ? $"reloaded '{path}' (its file changed on disk)"
                        : $"'{path}' changed on disk but could not be reloaded: {LoadFileHint(path)} loads it");
            }
            return notes.Count == 0 ? "refreshed" : "refreshed; " + string.Join("; ", notes);
        }

        // The call that loads a scene's file: OpenScene when it is the only loaded scene (Single mode closes the others), else
        // Close and an additive OpenScene (the scene then comes last in the Hierarchy).
        private static string LoadFileHint(string path) => SceneManager.loadedSceneCount == 1
            ? $"editor | call OpenScene(\"{path}\")" + (SceneManager.sceneCount > 1 ? " (it also removes the unloaded scenes)" : "")
            : $"scenes | where path == \"{path}\" | call Close() and then editor | call OpenScene(\"{path}\", true)";

        // Loads the scene's file again in place: the only loaded scene through the scene setup (unloaded scenes in the Hierarchy
        // stay); one of several is unloaded and loaded again, keeping its place in the Hierarchy and the active scene.
        private static bool Reload(Scene scene)
        {
            var path = scene.path;
            if (SceneManager.loadedSceneCount == 1)
            {
                try { EditorSceneManager.RestoreSceneManagerSetup(EditorSceneManager.GetSceneManagerSetup()); }
                catch (Exception e) when (e is ArgumentException || e is InvalidOperationException) { return false; }
                return SceneManager.GetSceneByPath(path).isLoaded;
            }
            var active = SceneManager.GetActiveScene() == scene;
            if (!EditorSceneManager.CloseScene(scene, removeScene: false)) return false;
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
