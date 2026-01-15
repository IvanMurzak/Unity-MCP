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
using com.IvanMurzak.Unity.MCP.Runtime.Data;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        public const string SceneCreateToolId = "scene-create";
        [McpPluginTool
        (
            SceneCreateToolId,
            Title = "Scene / Create"
        )]
        [Description("Create new scene in the project assets.")]
        public SceneDataShallow Create
        (
            [Description("Path to the scene file. Should end with \".unity\" extension.")]
            string path,
            UnityEditor.SceneManagement.NewSceneSetup? newSceneSetup = UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode? newSceneMode = UnityEditor.SceneManagement.NewSceneMode.Single
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(path))
                    throw new System.Exception(Error.ScenePathIsEmpty());

                if (!path.EndsWith(".unity"))
                    throw new System.Exception(Error.FilePathMustEndsWithUnity());

                // Create a new empty scene
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    newSceneSetup ?? UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                    newSceneMode ?? UnityEditor.SceneManagement.NewSceneMode.Single);

                // Save the scene asset at the specified path
                bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
                if (!saved)
                    throw new System.Exception($"Failed to save scene at '{path}'.\n{OpenedScenesText}");

                UnityEditor.EditorApplication.RepaintHierarchyWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                return scene.ToSceneDataShallow();
            });
        }
    }
}
