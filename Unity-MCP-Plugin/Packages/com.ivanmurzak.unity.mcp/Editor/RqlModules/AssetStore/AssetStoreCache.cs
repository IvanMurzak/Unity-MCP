/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    /// <summary>A downloaded asset package in the local cache: <c>&lt;cache&gt;/&lt;publisher&gt;/&lt;category&gt;/&lt;name&gt;.unitypackage</c>.</summary>
    public sealed class AssetStoreCachedFile
    {
        public string path = "";
        public string name = "";
        public string publisher = "";
        public string category = "";
        public long sizeBytes;
    }

    /// <summary>
    /// The asset package cache the Editor downloads Asset Store packages into. Its location: the ASSETSTORE_CACHE_PATH
    /// environment variable when set, otherwise the per-OS default (Unity Manual, "Asset package cache location").
    /// </summary>
    public static class AssetStoreCache
    {
        public const string EnvironmentVariable = "ASSETSTORE_CACHE_PATH";

        /// <summary>The cache folder (it may not exist yet).</summary>
        public static string Root
        {
            get
            {
                var custom = Environment.GetEnvironmentVariable(EnvironmentVariable);
                if (!string.IsNullOrWhiteSpace(custom))
                    return custom!.Trim();
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                switch (UnityEngine.Application.platform)
                {
                    case UnityEngine.RuntimePlatform.WindowsEditor:
                        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", "Asset Store-5.x");
                    case UnityEngine.RuntimePlatform.OSXEditor:
                        return Path.Combine(home, "Library", "Unity", "Asset Store-5.x");
                    default:
                        return Path.Combine(home, ".local", "share", "unity3d", "Asset Store-5.x");
                }
            }
        }

        /// <summary>Every .unitypackage under <paramref name="root"/>.</summary>
        public static List<AssetStoreCachedFile> Scan(string root)
        {
            var list = new List<AssetStoreCachedFile>();
            if (!Directory.Exists(root))
                return list;
            var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var file in Directory.EnumerateFiles(rootFull, "*.unitypackage", SearchOption.AllDirectories))
            {
                var info = new FileInfo(file);
                var relative = file.Substring(rootFull.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                list.Add(new AssetStoreCachedFile
                {
                    path = info.FullName.Replace('\\', '/'),
                    name = Path.GetFileNameWithoutExtension(file),
                    publisher = parts.Length >= 3 ? parts[0] : "",
                    category = parts.Length >= 3 ? parts[1] : parts.Length == 2 ? parts[0] : "",
                    sizeBytes = info.Length
                });
            }
            return list.OrderBy(f => f.name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>The path the Editor writes a product's download to.</summary>
        public static string DownloadPath(string root, AssetStoreDownloadTarget target)
            => Path.Combine(root, target.publisher, target.category, target.packageName + ".unitypackage").Replace('\\', '/');

        /// <summary>
        /// A name reduced to lower-case letters and digits, so a store display name ("DOTween (HOTween v2)") matches its
        /// filename-safe cache name ("DOTween HOTween v2").
        /// </summary>
        public static string MatchKey(string name)
        {
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }
    }
}
