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

using System;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Caches built-in Unity Editor assets to avoid repeated expensive LoadAllAssetsAtPath calls.
    /// Built-in assets don't change during an editor session, so caching is safe.
    /// </summary>
    public static class BuiltInAssetCache
    {
        private static UnityEngine.Object[]? _cachedAssets;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets all built-in assets, loading and caching them on first access.
        /// </summary>
        public static UnityEngine.Object[] GetAllAssets()
        {
            if (_cachedAssets != null)
                return _cachedAssets;

            lock (_lock)
            {
                if (_cachedAssets != null)
                    return _cachedAssets;

                _cachedAssets = AssetDatabase.LoadAllAssetsAtPath(ExtensionsRuntimeObject.UnityEditorBuiltInResourcesPath);
            }

            return _cachedAssets;
        }

        /// <summary>
        /// Finds a built-in asset by name and optional type.
        /// </summary>
        /// <param name="name">The name of the asset to find.</param>
        /// <param name="type">Optional type to filter by. If null, returns the first asset with matching name.</param>
        /// <returns>The found asset, or null if not found.</returns>
        public static UnityEngine.Object? FindAsset(string name, Type? type = null)
        {
            var assets = GetAllAssets();
            foreach (var obj in assets)
            {
                if (obj == null || obj.name != name)
                    continue;

                if (type == null)
                    return obj;

                if (type.IsAssignableFrom(obj.GetType()))
                    return obj;
            }
            return null;
        }

        /// <summary>
        /// Finds a built-in asset by name and file extension (used to disambiguate types).
        /// </summary>
        /// <param name="name">The name of the asset to find.</param>
        /// <param name="extension">File extension like ".mat", ".shader", etc.</param>
        /// <returns>The found asset, or null if not found.</returns>
        public static UnityEngine.Object? FindAssetByExtension(string name, string? extension)
        {
            var assets = GetAllAssets();
            foreach (var obj in assets)
            {
                if (obj == null || obj.name != name)
                    continue;

                if (string.IsNullOrEmpty(extension))
                    return obj;

                // Match extension to type
                var matches = extension switch
                {
                    ".mat" => obj is Material,
                    ".shader" => obj is Shader,
                    ".compute" => obj is ComputeShader,
                    ".anim" => obj is AnimationClip,
                    ".wav" => obj is AudioClip,
                    _ => true // Unknown extension, accept any type
                };

                if (matches)
                    return obj;
            }
            return null;
        }

        /// <summary>
        /// Gets the file extension for a built-in asset based on its type.
        /// </summary>
        /// <param name="obj">The Unity object to get the extension for.</param>
        /// <returns>File extension like ".mat", ".shader", etc., or empty string for unknown types.</returns>
        public static string GetExtensionForAsset(UnityEngine.Object obj)
        {
            return obj switch
            {
                Material => ".mat",
                Shader => ".shader",
                ComputeShader => ".compute",
                AnimationClip => ".anim",
                AudioClip => ".wav",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Clears the cache. Useful if you need to force a reload (rarely needed).
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _cachedAssets = null;
            }
        }
    }
}
