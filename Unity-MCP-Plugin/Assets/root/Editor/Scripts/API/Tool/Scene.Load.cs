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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        [McpPluginTool
        (
            "Scene_Load",
            Title = "Load scene"
        )]
        [Description("Load scene from the project assets.")]
        public string Load
        (
            [Description("Path to the scene file.")]
            string path,
            [Description("Load scene mode. 0 - Single, 1 - Additive.")]
            int loadSceneMode = 0
        )
        => MainThread.Instance.Run(() =>
        {
            if (string.IsNullOrEmpty(path))
                return Error.ScenePathIsEmpty();

            if (path.EndsWith(".unity") == false)
                return Error.FilePathMustEndsWithUnity();

            if (loadSceneMode < 0 || loadSceneMode > 1)
                return Error.InvalidLoadSceneMode(loadSceneMode);

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                path,
                loadSceneMode switch
                {
                    0 => UnityEditor.SceneManagement.OpenSceneMode.Single,
                    1 => UnityEditor.SceneManagement.OpenSceneMode.Additive,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(loadSceneMode), "Invalid open scene mode.")
                });

            if (!scene.IsValid())
                return $"[Error] Failed to load scene at '{path}'.\n{LoadedScenes}";

            return $"[Success] Scene loaded at '{path}'.\n{LoadedScenes}";
        });
    }
}
