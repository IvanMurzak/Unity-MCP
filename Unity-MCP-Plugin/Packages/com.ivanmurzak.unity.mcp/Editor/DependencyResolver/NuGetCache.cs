/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.   |
+------------------------------------------------------------------+
*/

#nullable enable
using System.IO;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Manages the local .nupkg file cache in Library/NuGetCache/.
    /// Avoids re-downloading packages that have already been fetched.
    /// </summary>
    static class NuGetCache
    {
        /// <summary>
        /// Gets the full path to the cached .nupkg file for a package.
        /// </summary>
        public static string GetCachedPath(NuGetPackage package)
            => Path.Combine(NuGetConfig.CachePath, package.CacheFileName);

        /// <summary>
        /// Returns true if the .nupkg is already cached locally.
        /// </summary>
        public static bool IsCached(NuGetPackage package)
            => File.Exists(GetCachedPath(package));

        /// <summary>
        /// Ensures the cache directory exists.
        /// </summary>
        public static void EnsureCacheDirectory()
        {
            if (!Directory.Exists(NuGetConfig.CachePath))
                Directory.CreateDirectory(NuGetConfig.CachePath);
        }
    }
}
