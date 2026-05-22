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
using AIGD;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        public const string SceneListOpenedToolId = "scene-list-opened";
        [McpPluginTool
        (
            SceneListOpenedToolId,
            Title = "Scene / List Opened",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [McpPluginSkillDescription("List every scene currently opened in the Unity Editor as a shallow snapshot " +
            "(name, path, build flags). Use '" + SceneGetDataToolId + "' for the deep view of a specific scene.")]
        [McpPluginSkillBody("Returns the list of currently opened scenes in Unity Editor. " +
            "Use '" + SceneGetDataToolId + "' tool to get detailed information about a specific scene.\n\n" +
            "## Behavior\n\n" +
            "Maps `OpenedScenes` through `ToSceneDataShallow()` on the main thread and returns the resulting array. " +
            "No filtering or pagination — every opened scene is included.")]
        [Description("Returns the list of currently opened scenes in Unity Editor. " +
            "Use '" + SceneGetDataToolId + "' tool to get detailed information about a specific scene.")]
        public SceneDataShallow[] ListOpened(string? nothing = null)
        {
            return MainThread.Instance.Run(() =>
            {
                return OpenedScenes
                    .Select(scene => scene.ToSceneDataShallow())
                    .ToArray();
            });
        }
    }
}
