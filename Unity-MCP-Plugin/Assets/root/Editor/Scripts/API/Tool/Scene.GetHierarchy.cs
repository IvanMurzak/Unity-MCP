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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        [McpPluginTool
        (
            "Scene_GetHierarchy",
            Title = "Get Scene Hierarchy"
        )]
        [Description("This tool retrieves the list of root GameObjects in the specified scene.")]
        public string GetHierarchyRoot
        (
            [Description("Determines the depth of the hierarchy to include.")]
            int includeChildrenDepth = 3,
            [Description("Name of the loaded scene. If empty string, the active scene will be used.")]
            string? loadedSceneName = null
        )
        => MainThread.Instance.Run(() =>
        {
            var scene = string.IsNullOrEmpty(loadedSceneName)
                ? UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                : UnityEngine.SceneManagement.SceneManager.GetSceneByName(loadedSceneName);

            if (!scene.IsValid())
                return Error.NotFoundSceneWithName(loadedSceneName);

            return scene.ToMetadata(includeChildrenDepth: includeChildrenDepth).Print();
        });
    }
}
