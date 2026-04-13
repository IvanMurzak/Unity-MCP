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
    ///   2. Subtract assemblies we bundled ourselves
    ///   3. Result = assemblies Unity provides natively
    /// </summary>
    static class UnityAssemblyResolver
    {
        static HashSet<string>? cachedUnityAssemblies;

        /// <summary>
        /// Paths where our bundled NuGet DLLs live.
        /// </summary>
        static readonly string[] BundledDllPaths =
        {
            "Packages/com.ivanmurzak.unity.mcp/Plugins/NuGet",
            "Packages/com.ivanmurzak.unity.mcp/Plugins/com.IvanMurzak.McpPlugin",
            "Packages/com.ivanmurzak.unity.mcp/Plugins/com.IvanMurzak.ReflectorNet",
            "Assets/Plugins/NuGet",
        };

        public static bool IsAlreadyImported(string assemblyName)
        {
            return GetUnityProvidedAssemblies().Contains(assemblyName);
        }

        public static HashSet<string> GetUnityProvidedAssemblies()
        {
            if (cachedUnityAssemblies != null)
                return cachedUnityAssemblies;

            var bundledByUs = GetBundledAssemblyNames();

            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(a => a.flags != AssemblyFlags.EditorAssembly);

            var unityProvided = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in playerAssemblies)
            {
                foreach (var reference in assembly.allReferences)
                {
                    var name = Path.GetFileNameWithoutExtension(reference);
                    if (!bundledByUs.Contains(name))
                        unityProvided.Add(name);
                }
            }

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

        static HashSet<string> GetBundledAssemblyNames()
        {
            var bundled = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var path in BundledDllPaths)
            {
                if (!Directory.Exists(path))
                    continue;
                foreach (var dll in Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories))
                    bundled.Add(Path.GetFileNameWithoutExtension(dll));
            }

            return bundled;
        }
    }
}
