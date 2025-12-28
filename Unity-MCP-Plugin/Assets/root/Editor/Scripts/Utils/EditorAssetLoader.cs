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
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Utility class for loading Unity Editor assets with fallback paths.
    /// </summary>
    public static class EditorAssetLoader
    {
        private const string PackagePathPrefix = "Packages/com.ivanmurzak.unity.mcp/";
        private const string AssetsPathPrefix = "Assets/root/";

        public static readonly string[] PackageLogoIcon = GetEditorAssetPaths("Editor/Gizmos/logo_window_icon.png");

        /// <summary>
        /// Generates an array of paths for an editor asset, with both package and development paths.
        /// </summary>
        /// <param name="relativePath">The path relative to the package/assets root (e.g., "Editor/Gizmos/logo.png").</param>
        /// <returns>An array containing both the package path and the assets path.</returns>
        public static string[] GetEditorAssetPaths(string relativePath)
        {
            return new[]
            {
                PackagePathPrefix + relativePath,
                AssetsPathPrefix + relativePath
            };
        }

        /// <summary>
        /// Attempts to load an asset from multiple paths, trying each in order until one succeeds.
        /// </summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="paths">Collection of paths to try in order.</param>
        /// <param name="logger">Optional logger for diagnostic messages.</param>
        /// <returns>The loaded asset, or null if not found in any path.</returns>
        public static T? LoadAssetAtPath<T>(IEnumerable<string> paths, ILogger? logger = null) where T : UnityEngine.Object
        {
            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    logger?.LogTrace("{method} Loaded file from: {path}",
                        nameof(LoadAssetAtPath), path);
                    return asset;
                }
            }

            logger?.LogWarning("{method} File not found. Checked: {paths}",
                nameof(LoadAssetAtPath), string.Join(", ", paths));

            return null;
        }
    }
}
