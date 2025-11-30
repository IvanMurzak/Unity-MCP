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

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public static class EditorFile
    {
        public static readonly string[] PackageLogoIcon =
        {
            "Packages/com.ivanmurzak.unity.mcp/Editor/Gizmos/logo_32.png",
            "Assets/root/Editor/Gizmos/logo_32.png"
        };

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
