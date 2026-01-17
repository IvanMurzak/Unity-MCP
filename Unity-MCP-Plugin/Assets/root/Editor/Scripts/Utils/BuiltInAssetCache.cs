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

                if (MatchesExtension(obj, extension!))
                    return obj;
            }
            return null;
        }

        /// <summary>
        /// Checks if a Unity object matches the expected type for a given file extension.
        /// </summary>
        /// <param name="obj">The Unity object to check.</param>
        /// <param name="extension">File extension like ".mat", ".shader", etc.</param>
        /// <returns>True if the object type matches the extension, false otherwise.</returns>
        public static bool MatchesExtension(UnityEngine.Object obj, string extension)
        {
            // Normalize extension to lowercase for case-insensitive matching
            var ext = extension.ToLowerInvariant();

            return ext switch
            {
                // Materials
                ".mat" => obj is Material,

                // Shaders
                ".shader" => obj is Shader,
                ".compute" => obj is ComputeShader,

                // Textures & Sprites (same source formats can be either)
                ".png" or ".jpg" or ".jpeg" or ".tga" or ".psd" or ".tif" or ".tiff" or ".gif" or ".bmp"
                    => obj is Texture2D or Sprite,
                ".hdr" or ".exr" => obj is Texture2D or Cubemap,

                // Audio
                ".wav" or ".mp3" or ".ogg" or ".aif" or ".aiff" => obj is AudioClip,

                // Animation
                ".anim" => obj is AnimationClip,

                // Fonts
                ".ttf" or ".otf" or ".fontsettings" => obj is Font,

                // Meshes (built-in primitives)
                ".fbx" or ".obj" or ".dae" or ".3ds" or ".blend" => obj is Mesh,

                // GUI
                ".guiskin" => obj is GUISkin,

                // Flare
                ".flare" => obj is Flare,

                // Unknown extension - accept any type
                _ => true
            };
        }

        /// <summary>
        /// Gets the file extension for a built-in asset based on its type.
        /// Only returns extensions for types with unambiguous mappings.
        /// Types like Sprite, Texture2D, AudioClip, Mesh, Font can have multiple
        /// possible extensions, so they return empty string.
        /// </summary>
        /// <param name="obj">The Unity object to get the extension for.</param>
        /// <returns>File extension like ".mat", ".shader", etc., or empty string for ambiguous/unknown types.</returns>
        public static string GetExtensionForAsset(UnityEngine.Object obj)
        {
            // Only return extensions for types with unique/unambiguous mappings
            return obj switch
            {
                Material => ".mat",
                Shader => ".shader",
                ComputeShader => ".compute",
                AnimationClip => ".anim",
                GUISkin => ".guiskin",
                Flare => ".flare",
                // Types with multiple possible extensions (Sprite, Texture2D, Cubemap,
                // AudioClip, Mesh, Font) - cannot determine without file system access
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
