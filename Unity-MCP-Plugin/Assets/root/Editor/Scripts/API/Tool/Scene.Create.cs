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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        [McpPluginTool
        (
            "Scene_Create",
            Title = "Create new scene"
        )]
        [Description("Create new scene in the project assets.")]
        public string Create
        (
            [Description("Path to the scene file.")]
            string path
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(path))
                    return Error.ScenePathIsEmpty();

                if (path.EndsWith(".unity") == false)
                    return Error.FilePathMustEndsWithUnity();

                // Create a new empty scene
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                    UnityEditor.SceneManagement.NewSceneMode.Single);

                // Save the scene asset at the specified path
                bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
                if (!saved)
                    return $"[Error] Failed to save scene at '{path}'.\n{LoadedScenesText}";

                UnityEditor.EditorApplication.RepaintHierarchyWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return $"[Success] Scene created at '{path}'.\n{LoadedScenesText}";
            });
        }
    }
}
