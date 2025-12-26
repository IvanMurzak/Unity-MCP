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
using System;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        [McpPluginTool
        (
            "scene-get-data",
            Title = "Scene / Get Data"
        )]
        [Description("This tool retrieves the list of root GameObjects in the specified scene.")]
        public SceneData GetData
        (
            [Description("Name of the opened scene. If empty or null, the active scene will be used.")]
            string? openedSceneName = null,
            [Description("If true, includes root GameObjects in the scene data.")]
            bool includeRootGameObjects = false,
            [Description("Determines the depth of the hierarchy to include.")]
            int includeChildrenDepth = 3,
            [Description("If true, performs deep serialization of GameObjects.")]
            bool deepSerialization = false,
            [Description("If true, includes bounding box information for GameObjects.")]
            bool includeBounds = false,
            [Description("If true, includes component data for GameObjects.")]
            bool includeData = false
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var scene = string.IsNullOrEmpty(openedSceneName)
                    ? UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                    : UnityEngine.SceneManagement.SceneManager.GetSceneByName(openedSceneName);

                if (!scene.IsValid())
                    throw new ArgumentException(Error.NotFoundSceneWithName(openedSceneName));

                return new SceneData(
                    scene: scene,
                    reflector: McpPlugin.McpPlugin.Instance!.McpManager.Reflector,
                    includeRootGameObjects: includeRootGameObjects,
                    includeChildrenDepth: includeChildrenDepth,
                    deepSerialization: deepSerialization,
                    includeBounds: includeBounds,
                    includeData: includeData,
                    logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Scene>()
                );
            });
        }
    }
}
