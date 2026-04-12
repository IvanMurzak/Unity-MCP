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
#if !UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using com.IvanMurzak.McpPlugin.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static partial class GameObjectUtils
    {
        public static GameObject[]? FindRootGameObjects(Scene? scene = null)
        {
            if (scene == null)
            {
                // Not supported in runtime build
                return null;
            }
            else
            {
                return scene.Value.GetRootGameObjects();
            }
        }
        public static GameObject? FindByInstanceID(int instanceID)
        {
            if (instanceID == 0)
                return null;

            // Not supported in runtime build
            return null;
        }
    }
}
#endif
