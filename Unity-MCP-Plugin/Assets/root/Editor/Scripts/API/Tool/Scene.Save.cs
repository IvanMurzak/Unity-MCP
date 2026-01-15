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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        public const string SceneSaveToolId = "scene-save";
        [McpPluginTool
        (
            SceneSaveToolId,
            Title = "Scene / Save"
        )]
        [Description("Save Opened scene to the asset file. " +
            "Use '" + SceneListOpenedToolId + "' tool to get the list of all opened scenes.")]
        public void Save
        (
            [Description("Name of the opened scene that should be saved. Could be empty if need to save the current active scene.")]
            string? openedSceneName = null,
            [Description("Path to the scene file. Should end with \".unity\". If null or empty save to the existed scene asset file.")]
            string? path = null
        )
        {
            MainThread.Instance.Run(() =>
            {
                if (string.IsNullOrEmpty(path))
                    throw new System.Exception(Error.ScenePathIsEmpty());

                var scene = string.IsNullOrEmpty(openedSceneName)
                    ? SceneUtils.GetActiveScene()
                    : SceneUtils.GetAllOpenedScenes()
                        .FirstOrDefault(scene => scene.name == openedSceneName);

                if (!scene.IsValid())
                    throw new System.Exception(Error.NotFoundSceneWithName(openedSceneName));

                if (string.IsNullOrEmpty(path))
                    path = scene.path;

                if (path!.EndsWith(".unity") == false)
                    throw new System.Exception(Error.FilePathMustEndsWithUnity());

                bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
                if (!saved)
                    throw new System.Exception($"Failed to save scene at '{path}'.\n{OpenedScenesText}");

                UnityEditor.EditorApplication.RepaintHierarchyWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            });
        }
    }
}
