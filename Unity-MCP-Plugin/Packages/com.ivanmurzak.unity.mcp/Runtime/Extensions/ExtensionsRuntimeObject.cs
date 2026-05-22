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
        public const string UnityEditorBuiltInResourcesPath = "Resources/unity_builtin_extra";

        /// <summary>
        /// Checks if the given UnityEngine.Object is an asset stored on disk.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if the object is an asset stored on disk; otherwise, false.</returns>
        public static bool IsAsset(this UnityEngine.Object? obj)
        {
            if (obj == null)
                return false;

#if UNITY_EDITOR
            if (!UnityEditor.EditorUtility.IsPersistent(obj))
                return false; // not stored on disk

            var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            return !string.IsNullOrEmpty(path) && (path.StartsWith("Assets/") || path.StartsWith("Packages/") || path.StartsWith(UnityEditorBuiltInResourcesPath));
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Gets the asset path of the given UnityEngine.Object if it is an asset stored on disk.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>The asset path if the object is an asset stored on disk; otherwise, null.</returns>
        public static string? GetAssetPath(this UnityEngine.Object? obj)
        {
            if (obj == null)
                return null;

            if (!UnityEditor.EditorUtility.IsPersistent(obj))
                return null; // not stored on disk

            var path = UnityEditor.AssetDatabase.GetAssetPath(obj);

            // UnityEditor.AssetDatabase.GetAssetPath can return empty or null string non existing assets
            // To standardize the behavior, we return null in such cases
            if (string.IsNullOrEmpty(path))
                return null;

            return path;
        }
#endif
    }
}
