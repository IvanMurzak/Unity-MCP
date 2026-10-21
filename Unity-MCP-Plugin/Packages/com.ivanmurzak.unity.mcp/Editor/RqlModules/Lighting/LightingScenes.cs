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
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting
{
    /// <summary>
    /// Where baked lighting lives: Unity stores lightmaps, the Lighting Data asset and baked reflection probes in a folder
    /// named after the scene, next to it ("Assets/Scenes/Level" for "Assets/Scenes/Level.unity"), so a bake needs saved scenes.
    /// </summary>
    public static class LightingScenes
    {
        /// <summary>The scene's lighting folder, or null for an untitled scene.</summary>
        public static string? Folder(Scene scene)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path)) return null;
            return (Path.GetDirectoryName(scene.path) + "/" + Path.GetFileNameWithoutExtension(scene.path)).Replace('\\', '/');
        }

        /// <summary>Throws with a fix-it hint when a loaded scene has no path yet (or the Editor is in Play Mode).</summary>
        public static void RequireSaved(string what)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException($"{what} is not possible in Play Mode (editor | set isPlaying = false)");
            var untitled = RqlUnity.Scenes().Where(s => string.IsNullOrEmpty(s.path)).ToList();
            if (untitled.Count > 0)
                throw new InvalidOperationException(
                    $"{what} needs saved scenes: Unity stores baked lighting in a folder next to the scene. Save it first, e.g. " +
                    "scenes | where path == \"\" | call SaveAs(\"Assets/Scenes/Level.unity\")");
        }

        /// <summary>The active scene marked modified, so the next save writes the change (no-op in Play Mode).</summary>
        public static void MarkActiveSceneDirty()
        {
            if (EditorApplication.isPlaying) return;
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid()) EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>A file name without characters the file system rejects.</summary>
        public static string SafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        }
    }
}
