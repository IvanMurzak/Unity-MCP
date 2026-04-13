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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace com.IvanMurzak.Unity.MCP.DependencyResolver
{
    /// <summary>
    /// Detects assemblies that Unity already provides (built-in BCL, engine modules, other packages).
    /// Uses the same approach as NuGetForUnity's UnityPreImportedLibraryResolver:
    ///   1. Get all player assembly references via CompilationPipeline
    ///   2. Subtract assemblies we installed ourselves
    ///   3. Result = assemblies Unity provides — these should NOT be installed from NuGet
    ///
    /// This prevents conflicts like CS1705 (version mismatch) and "multiple assemblies" errors.
    /// </summary>
    static class UnityAssemblyResolver
    {
        static HashSet<string>? cachedUnityAssemblies;

        /// <summary>
        /// Returns true if the given assembly name is already provided by Unity
        /// (either as a BCL assembly or from another installed package).
        /// </summary>
        public static bool IsAlreadyImported(string assemblyName)
        {
            return GetUnityProvidedAssemblies().Contains(assemblyName);
        }

        /// <summary>
        /// Invalidates the cached set of Unity-provided assemblies.
        /// Call this after installing or removing packages.
        /// </summary>
        public static void InvalidateCache()
        {
            cachedUnityAssemblies = null;
        }

        /// <summary>
        /// Gets the set of assembly names that Unity already provides.
        /// Caches the result for performance (invalidated between domain reloads).
        /// </summary>
        public static HashSet<string> GetUnityProvidedAssemblies()
        {
            if (cachedUnityAssemblies != null)
                return cachedUnityAssemblies;

            // Get assemblies we've installed ourselves (to exclude them)
            var installedByUs = GetInstalledAssemblyNames();

            // Get all assemblies referenced by player builds (includes BCL, engine, other packages)
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(a => (a.flags & AssemblyFlags.EditorAssembly) == 0);

            var unityProvided = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in playerAssemblies)
            {
                foreach (var reference in assembly.allReferences)
                {
                    var name = Path.GetFileNameWithoutExtension(reference);
                    if (!installedByUs.Contains(name))
                        unityProvided.Add(name);
                }
            }

            // Hard-coded additions (following NuGetForUnity's pattern)
            var compatLevel = PlayerSettings.GetApiCompatibilityLevel(
                UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup));

            if (compatLevel == ApiCompatibilityLevel.NET_Standard_2_0)
            {
                unityProvided.Add("NETStandard.Library");
                unityProvided.Add("Microsoft.NETCore.Platforms");
            }
            unityProvided.Add("Microsoft.CSharp");

            cachedUnityAssemblies = unityProvided;
            return unityProvided;
        }

        /// <summary>
        /// Gets the set of assembly names that we've installed into Assets/Plugins/NuGet/.
        /// </summary>
        static HashSet<string> GetInstalledAssemblyNames()
        {
            var installed = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(NuGetConfig.InstallPath))
                return installed;

            foreach (var dll in Directory.EnumerateFiles(NuGetConfig.InstallPath, "*.dll", SearchOption.AllDirectories))
                installed.Add(Path.GetFileNameWithoutExtension(dll));

            return installed;
        }
    }
}
