/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP
{
    public class SceneMetadata
    {
        public string path;
        public string name;
        public bool isDirty;
        public bool isLoaded;
        public List<GameObjectMetadata> rootGameObjects = new();

        public string Print(int limit = Consts.MCP.Plugin.LinesLimit)
        {
            var sb = new StringBuilder();

            // Add table header
            sb.AppendLine("# Scene Information");
            sb.AppendLine("name: " + name);
            sb.AppendLine("path: " + path);
            sb.AppendLine("isDirty: " + isDirty + ", isLoaded: " + isLoaded);
            sb.AppendLine();
            sb.AppendLine("# Scene Hierarchy of GameObjects");
            sb.AppendLine("-------------------------------------------------------------------------");
            sb.AppendLine("instanceID | activeInHierarchy | activeSelf | tag       | name");
            sb.AppendLine("-----------|-------------------|------------|-----------|----------------");

            // Add the current GameObject's metadata
            foreach (var rootGameObject in rootGameObjects)
            {
                if (limit <= 0)
                {
                    sb.AppendLine("... [Limit reached] ...");
                    return sb.ToString();
                }
                limit--;
                GameObjectMetadata.AppendMetadata(sb, rootGameObject, depth: 0, ref limit);
            }

            return sb.ToString();
        }
        public static SceneMetadata FromScene(Scene scene, int includeChildrenDepth = 3)
        {
            if (!scene.IsValid())
                return null;

            return new SceneMetadata
            {
                path = scene.path,
                name = scene.name,
                isDirty = scene.isDirty,
                isLoaded = scene.isLoaded,
                rootGameObjects = GameObjectUtils.FindRootGameObjects(scene)
                    .Select(x => x.ToMetadata(includeChildrenDepth))
                    .ToList()
            };
        }
    }
}
