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

namespace com.IvanMurzak.Unity.MCP.DependencyResolver
{
    /// <summary>
    /// Configuration for the NuGet dependency resolver.
    /// Contains the list of required NuGet packages and path settings.
    /// </summary>
    static class NuGetConfig
    {
        /// <summary>
        /// NuGet v3 flat container base URL.
        /// Download URL pattern: {base}/{id}/{version}/{id}.{version}.nupkg
        /// </summary>
        public const string NuGetBaseUrl = "https://api.nuget.org/v3-flatcontainer";

        /// <summary>
        /// Where extracted NuGet DLLs are installed (mutable location inside Unity's asset pipeline).
        /// PluginImporter requires files to be under Assets/ to work.
        /// </summary>
        public const string InstallPath = "Assets/Plugins/NuGet";

        /// <summary>
        /// Where downloaded .nupkg files are cached.
        /// Library/ survives domain reloads but is not tracked by git.
        /// </summary>
        public const string CachePath = "Library/NuGetCache";

        /// <summary>
        /// Top-level NuGet package dependencies.
        /// Transitive dependencies are resolved automatically from .nuspec metadata.
        /// </summary>
        /// <summary>
        /// Top-level NuGet package dependencies.
        /// Transitive dependencies are resolved automatically from .nuspec metadata.
        ///
        /// includeInBuild: true  = DLL included in game builds (runtime dependency)
        /// includeInBuild: false = editor-only DLL (excluded from builds)
        /// </summary>
        public static readonly NuGetPackage[] Packages =
        {
            // --- Runtime dependencies (included in game builds) ---
            new NuGetPackage("System.Text.Json",                                   "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.AspNetCore.SignalR.Client",                 "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.AspNetCore.SignalR.Protocols.Json",         "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Logging",                       "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Logging.Abstractions",          "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.DependencyInjection.Abstractions", "10.0.3", includeInBuild: true),
            new NuGetPackage("R3",                                                 "1.3.0",  includeInBuild: true),

            // --- Editor-only dependencies (excluded from builds) ---
            new NuGetPackage("Microsoft.Bcl.Memory",                               "10.0.3"),
            new NuGetPackage("Microsoft.CodeAnalysis.CSharp",                      "4.14.0"),
            new NuGetPackage("Microsoft.Extensions.Caching.Abstractions",          "10.0.3"),
            new NuGetPackage("Microsoft.Extensions.Hosting.Abstractions",          "10.0.3"),
        };

        /// <summary>
        /// Target framework priority for selecting DLLs from .nupkg lib/ folders.
        /// First match wins. Ordered by preference for Unity compatibility.
        /// </summary>
        public static readonly string[] TargetFrameworkPriority =
        {
            "netstandard2.1",
            "netstandard2.0",
            "net48",
            "net472",
            "net471",
            "net47",
            "net462",
            "net461",
            "net46",
            "net45",
            "netstandard1.3",
            "netstandard1.1",
            "netstandard1.0",
            "",  // fallback: root lib/ folder
        };
    }
}
