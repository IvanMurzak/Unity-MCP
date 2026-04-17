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

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Represents a NuGet package dependency with its ID and version.
    /// </summary>
    readonly struct NuGetPackage
    {
        /// <summary>NuGet package ID (e.g., "System.Text.Json").</summary>
        public readonly string Id;

        /// <summary>Package version (e.g., "10.0.3").</summary>
        public readonly string Version;

        /// <summary>
        /// If true, this DLL is included in game builds (all platforms).
        /// If false, the DLL is editor-only (excluded from builds via PluginImporter).
        /// </summary>
        public readonly bool IncludeInBuild;

        public NuGetPackage(string id, string version, bool includeInBuild = false)
        {
            Id = id;
            Version = version;
            IncludeInBuild = includeInBuild;
        }

        /// <summary>
        /// The .nupkg download URL from NuGet v3 flat container API.
        /// </summary>
        public string DownloadUrl
        {
            get
            {
                var lowerId = Id.ToLowerInvariant();
                var lowerVersion = Version.ToLowerInvariant();
                return $"{NuGetConfig.NuGetBaseUrl}/{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg";
            }
        }

        /// <summary>
        /// Cached .nupkg filename.
        /// </summary>
        public string CacheFileName => $"{Id}.{Version}.nupkg";

        /// <summary>
        /// The install directory name under the install path.
        /// </summary>
        public string InstallDirectoryName => $"{Id}.{Version}";

        public override string ToString() => $"{Id} {Version}";
    }
}
