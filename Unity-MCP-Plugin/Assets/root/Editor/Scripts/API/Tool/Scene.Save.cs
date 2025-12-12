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
        [McpPluginTool
        (
            "Scene_Save",
            Title = "Save scene"
        )]
        [Description("Save scene from the project assets.")]
        public string Save
        (
            [Description("Path to the scene file.")]
            string path,
            [Description("Name of the opened scene. Could be empty if need to save current active scene. It is helpful when multiple scenes are opened.")]
            string? targetSceneName = null
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(path))
                return Error.ScenePathIsEmpty();

            if (path.EndsWith(".unity") == false)
                return Error.FilePathMustEndsWithUnity();

            var scene = string.IsNullOrEmpty(targetSceneName)
                ? SceneUtils.GetActiveScene()
                : SceneUtils.GetAllLoadedScenes()
                    .FirstOrDefault(scene => scene.name == targetSceneName);

            if (!scene.IsValid())
                return Error.NotFoundSceneWithName(targetSceneName);

            bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
            if (!saved)
                return $"[Error] Failed to save scene at '{path}'.\n{LoadedScenesText}";

            return $"[Success] Scene saved at '{path}'.\n{LoadedScenesText}";
        });
    }
}
