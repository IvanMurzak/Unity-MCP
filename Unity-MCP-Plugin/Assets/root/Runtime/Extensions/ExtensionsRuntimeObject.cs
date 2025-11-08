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

namespace com.IvanMurzak.Unity.MCP.Runtime.Extensions
{
    public static class ExtensionsRuntimeObject
    {
        public static bool IsAsset(this UnityEngine.Object? obj)
        {
            if (obj == null)
                return false;

#if UNITY_EDITOR
            if (obj == null) return false;
            if (!UnityEditor.EditorUtility.IsPersistent(obj))
                return false; // not stored on disk

            var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            return !string.IsNullOrEmpty(path) && (path.StartsWith("Assets/") || path.StartsWith("Packages/"));
#else
            return false;
#endif
        }
    }
}
