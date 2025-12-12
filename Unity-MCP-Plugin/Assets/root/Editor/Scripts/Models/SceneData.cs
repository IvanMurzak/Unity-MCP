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
using System.ComponentModel;
using System.Linq;

namespace com.IvanMurzak.Unity.MCP.Editor.Models
{
    public class SceneData
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsLoaded { get; set; }
        public bool IsDirty { get; set; }
        public bool IsSubScene { get; set; } = false;

        [Description(@"Whether this is a valid Scene. A Scene may be invalid if, for example, you tried
to open a Scene that does not exist. In this case, the Scene returned from EditorSceneManager.OpenScene
would return False for IsValid.")]
        public bool IsValid { get; set; } = true;
        public int BuildIndex { get; set; } = -1;
        public int RootCount { get; set; } = 0;
        public List<GameObjectData>? RootGameObjects { get; set; } = null;
    }

    public static class SceneDataExtensions
    {
        public static SceneData ToSceneData(
            this UnityEngine.SceneManagement.Scene scene,
            bool includeRootGameObjects = false)
        {
            var sceneData = new SceneData
            {
                Name = scene.name,
                Path = scene.path,
                IsLoaded = scene.isLoaded,
                IsDirty = scene.isDirty,
                BuildIndex = scene.buildIndex,
                RootCount = scene.rootCount,
                IsSubScene = scene.isSubScene,
                IsValid = scene.IsValid()
            };
            if (includeRootGameObjects)
            {
                sceneData.RootGameObjects = scene.GetRootGameObjects()
                    .Select(go => go.ToGameObjectData(
                        includeData: false,
                        includeBounds: false,
                        includeHierarchy: false,
                        hierarchyDepth: 0,
                        deepSerialization: false
                    )).ToList();
            }
            return sceneData;
        }
    }
}