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
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_Scene
    {
        public static IEnumerable<UnityEngine.SceneManagement.Scene> OpenedScenes => SceneUtils.GetAllOpenedScenes();
        public static string OpenedScenesText
            => $"Opened Scenes:\n{string.Join("\n", SceneUtils.GetAllOpenedScenes().Select(scene => scene.name))}";

        public static class Error
        {
            static string ScenesPrinted => string.Join("\n", SceneUtils.GetAllOpenedScenes().Select(scene => scene.name));

            public static string SceneNameIsEmpty()
                => $"Scene name is empty. Available scenes:\n{ScenesPrinted}";
            public static string NotFoundSceneWithName(string? name)
                => $"Scene '{name ?? "null"}' not found. Available scenes:\n{ScenesPrinted}";
            public static string ScenePathIsEmpty()
                => "Scene path is empty. Please provide a valid path. Sample: \"Assets/Scenes/MyScene.unity\".";
            public static string FilePathMustEndsWithUnity()
                => "File path must end with '.unity'. Please provide a valid path. Sample: \"Assets/Scenes/MyScene.unity\".";
            public static string InvalidLoadSceneMode(int loadSceneMode)
                => $"Invalid load scene mode '{loadSceneMode}'. Valid values are 0 (Single) and 1 (Additive).";
        }
    }
}
