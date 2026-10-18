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

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Detects assemblies that Unity already provides (built-in BCL, engine modules, other packages).
    /// Uses the same approach as NuGetForUnity's UnityPreImportedLibraryResolver:
    ///   1. Get all player assembly references via CompilationPipeline
    ///   2. Skip references that resolve from our NuGet install folder (those are our own duplicates)
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
        /// <summary>
        /// True when <paramref name="assemblyName"/> can actually be loaded by the EDITOR runtime from somewhere
        /// other than our NuGet install folder.
        ///
        /// <para>On Mono editors this always returns true, keeping the player-reference based decision of
        /// <see cref="IsAlreadyImported"/> unchanged. On CoreCLR editors (Unity 7+, where
        /// <c>RuntimeInformation.FrameworkDescription</c> starts with ".NET") the player-profile BCLExtensions
        /// (<c>Editor/Data/BCLExtensions/runtime/netstandard2.1/*.dll</c>) that satisfy the player reference check
        /// are not part of the editor's load context. Marking our copy "Exclude Editor" then leaves the editor
        /// without the assembly at all: <c>Microsoft.Bcl.AsyncInterfaces, Version=8.0.0.0</c> (required by R3)
        /// threw <c>FileNotFoundException</c> while constructing <c>McpPlugin</c> on Unity 7000.0.0a7.</para>
        /// </summary>
        public static bool IsResolvableInEditorRuntime(string assemblyName)
        {
            var framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription ?? string.Empty;
            if (!framework.StartsWith(".NET", System.StringComparison.Ordinal))
                return true; // Mono: original behaviour

            try
            {
                var assembly = System.Reflection.Assembly.Load(new System.Reflection.AssemblyName(assemblyName));
                var location = assembly?.Location;
                if (string.IsNullOrEmpty(location))
                    return assembly != null; // in-memory / platform assembly without a path: provided by the runtime

                var installRoot = Path.GetFullPath(NuGetConfig.InstallPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return !Path.GetFullPath(location).StartsWith(installRoot, System.StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false; // not loadable in the editor runtime: our copy must stay enabled for the Editor
            }
        }

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
        public static IReadOnlyCollection<string> GetUnityProvidedAssemblies()
        {
            if (cachedUnityAssemblies != null)
                return cachedUnityAssemblies;

            // Get all assemblies referenced by player builds (includes BCL, engine, other packages)
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(a => (a.flags & AssemblyFlags.EditorAssembly) == 0);

            var unityProvided = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            // Canonicalize the install root to an absolute path once. `assembly.allReferences`
            // paths are typically absolute, while NuGetConfig.InstallPath is project-relative,
            // so a plain StartsWith comparison would never match for our own NuGet DLLs —
            // we'd incorrectly record them as Unity-provided and later disable them.
            var installRootAbs = Path.GetFullPath(NuGetConfig.InstallPath).Replace('\\', '/');
            if (!installRootAbs.EndsWith("/"))
                installRootAbs += "/";

            foreach (var assembly in playerAssemblies)
            {
                foreach (var reference in assembly.allReferences)
                {
                    var name = Path.GetFileNameWithoutExtension(reference);

                    // Resolve references whose path is outside our NuGet install folder
                    // (these are genuinely Unity-provided and would conflict with NuGet duplicates).
                    // If the reference path points into our NuGet install folder we skip it here —
                    // but we still want to detect the *name* as Unity-provided if any other
                    // reference resolves the same name outside our install folder.
                    var fullRef = Path.GetFullPath(reference).Replace('\\', '/');
                    if (fullRef.StartsWith(installRootAbs, System.StringComparison.OrdinalIgnoreCase))
                        continue;

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

    }
}
