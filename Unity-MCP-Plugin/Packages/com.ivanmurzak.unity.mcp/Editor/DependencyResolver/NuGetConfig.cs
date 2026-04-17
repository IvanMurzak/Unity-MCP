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
    /// Configuration for the NuGet dependency resolver.
    /// Contains the list of required NuGet packages and path settings.
    /// </summary>
    static class NuGetConfig
    {
        /// <summary>Log tag shared across all DependencyResolver classes.</summary>
        public const string LogTag = "[NuGet]";

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
        ///
        /// includeInBuild: true  = DLL included in game builds (runtime dependency)
        /// includeInBuild: false = editor-only DLL (excluded from builds)
        /// </summary>
        public static readonly NuGetPackage[] Packages =
        {
            // --- Runtime dependencies (included in game builds) ---
            // v8 pinned to match what McpPlugin.dll (netstandard2.1) is compiled against.
            // Higher versions cause MissingMethodException at runtime in Unity versions
            // whose built-in BCL doesn't override our NuGet install (e.g. Unity 6.5).
            // 6.1.0 drops the unused ModelContextProtocol dep; earlier versions drag in a
            // v10 BCL stack via MCP.Core.1.2.0 that collides with our v8 pins in Unity.
            new NuGetPackage("com.IvanMurzak.McpPlugin",                              "6.1.0",  includeInBuild: true),
            new NuGetPackage("System.Text.Json",                                      "8.0.5",  includeInBuild: true),
            new NuGetPackage("Microsoft.AspNetCore.SignalR.Client",                   "8.0.15", includeInBuild: true),
            new NuGetPackage("Microsoft.AspNetCore.SignalR.Protocols.Json",           "8.0.15", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Logging",                          "8.0.1",  includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Logging.Abstractions",             "8.0.2",  includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.DependencyInjection",              "8.0.1",  includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.DependencyInjection.Abstractions", "8.0.2",  includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Options",                          "8.0.2",  includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Caching.Abstractions",             "8.0.0",  includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Hosting.Abstractions",             "8.0.1",  includeInBuild: true),
            new NuGetPackage("R3",                                                    "1.3.0",  includeInBuild: true),

            // --- Editor-only dependencies (excluded from builds) ---
            new NuGetPackage("Microsoft.Bcl.Memory",                                  "10.0.3"),
            // Pinned to 4.8.0 so transitive System.Collections.Immutable stays at 7.0.0
            // (forward-compatible with Unity 6.6's built-in 8.0.0). Roslyn 4.14 requires
            // SCI 9.0.0 which clashes with Unity's built-in.
            new NuGetPackage("Microsoft.CodeAnalysis.CSharp",                         "4.8.0"),
        };

        /// <summary>
        /// Package IDs to exclude from transitive dependency resolution. Matched
        /// case-insensitively against the NuGet package ID (not the DLL filename).
        ///
        /// When a package ID appears here:
        ///   - the resolver skips it (and its transitive deps) during Install()
        ///   - RemoveUnnecessaryPackages() deletes its install directory if present
        ///   - AllPackagesInstalled() forces a full restore while it's still on disk
        ///
        /// Use this when a top-level package's nuspec pulls in a transitive dep that
        /// is unused by your project's source AND whose own dependency chain conflicts
        /// with your pinned versions (e.g., a netstandard2.0 package targeting a newer
        /// BCL than Unity's runtime provides).
        /// </summary>
        public static readonly string[] SkipPackages = { };

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
