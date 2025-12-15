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

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    public class SceneData : SceneDataShallow
    {
        public List<GameObjectData>? RootGameObjects { get; set; } = null;

        public SceneData() { }
        public SceneData(
            UnityEngine.SceneManagement.Scene scene,
            bool includeRootGameObjects = false,
            int includeChildrenDepth = 0,
            bool deepSerialization = false,
            bool includeBounds = false,
            bool includeData = false)
            : base(scene)
        {
            if (includeRootGameObjects)
            {
                this.RootGameObjects = scene.GetRootGameObjects()
                    .Select(go => go.ToGameObjectData(
                        includeData: includeData,
                        includeBounds: includeBounds,
                        includeHierarchy: includeChildrenDepth > 0,
                        hierarchyDepth: includeChildrenDepth,
                        deepSerialization: deepSerialization
                    ))
                    .ToList();
            }
        }
    }

    public static class SceneDataExtensions
    {
        public static SceneData ToSceneData(
            this UnityEngine.SceneManagement.Scene scene,
            bool includeRootGameObjects = false)
        {
            return new SceneData(scene, includeRootGameObjects: includeRootGameObjects);
        }
    }
}