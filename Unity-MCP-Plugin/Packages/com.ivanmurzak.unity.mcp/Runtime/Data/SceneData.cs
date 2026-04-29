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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    public class SceneData : SceneDataShallow
    {
        public List<GameObjectData>? RootGameObjects { get; set; } = null;

        [Description("Path-scoped read or view-query result, populated when 'paths' or 'viewQuery' is supplied " +
            "to the scene-get-data tool. Null otherwise.")]
        public SerializedMember? Data { get; set; } = null;

        public SceneData() { }
        public SceneData(
            UnityEngine.SceneManagement.Scene scene,
            Reflector reflector,
            bool includeRootGameObjects = false,
            int includeChildrenDepth = 0,
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
                        includeComponents: false,
                        includeBounds: includeBounds,
                        includeHierarchy: includeChildrenDepth > 0,
                        hierarchyDepth: includeChildrenDepth,
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