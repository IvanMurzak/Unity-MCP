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
using com.IvanMurzak.ReflectorNet;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    public class SceneData : SceneDataShallow
    {
        public List<GameObjectData>? RootGameObjects { get; set; } = null;

        public SceneData() { }
        public SceneData(
            UnityEngine.SceneManagement.Scene scene,
            Reflector reflector,
            bool includeRootGameObjects = false,
            int includeChildrenDepth = 0,
            bool deepSerialization = false,
            bool includeBounds = false,
            bool includeData = false,
            ILogger? logger = null)
            : base(scene)
        {
            if (includeRootGameObjects)
            {
                this.RootGameObjects = scene.GetRootGameObjects()
                    .Select(go => go.ToGameObjectData(
                        reflector: reflector,
                        includeData: includeData,
                        includeBounds: includeBounds,
                        includeHierarchy: includeChildrenDepth > 0,
                        hierarchyDepth: includeChildrenDepth,
                        deepSerialization: deepSerialization,
                        logger: logger
                    ))
                    .ToList();
            }
        }
    }

    public static class SceneDataExtensions
    {
        public static SceneData ToSceneData(
            this UnityEngine.SceneManagement.Scene scene,
            Reflector reflector,
            bool includeRootGameObjects = false,
            ILogger? logger = null)
        {
            return new SceneData(
                scene: scene,
                reflector: reflector,
                includeRootGameObjects: includeRootGameObjects,
                logger: logger);
        }
    }
}