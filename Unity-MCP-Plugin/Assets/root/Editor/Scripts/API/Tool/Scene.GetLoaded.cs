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
using com.IvanMurzak.Unity.MCP.Editor.Models;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Scene
    {
        [McpPluginTool
        (
            "Scene_GetLoaded",
            Title = "Get list of currently loaded scenes"
        )]
        [Description("Returns the list of currently loaded scenes.")]
        public SceneData[] GetLoaded()
        {
            return MainThread.Instance.Run(() =>
            {
                return LoadedScenes
                    .Select(scene =>
                    {
                        return new SceneData
                        {
                            Name = scene.name,
                            Path = scene.path,
                            IsLoaded = scene.isLoaded,
                            IsDirty = scene.isDirty,
                            BuildIndex = scene.buildIndex,
                            RootCount = scene.rootCount,
                            IsSubScene = scene.isSubScene,
                            RootGameObjects = scene.GetRootGameObjects()
                                .Select(go => go.ToGameObjectData(
                                    includeData: false,
                                    includeBounds: false,
                                    includeHierarchy: false,
                                    hierarchyDepth: 0,
                                    deepSerialization: false
                                )).ToList()
                        };
                    }).ToArray();
            });
        }
    }
}
