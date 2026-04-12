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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;
using static UnityEditor.Compilation.CompilationPipeline;

namespace com.IvanMurzak.Unity.MCP.DependencyResolver
{
    /// <summary>
    /// Automatically resolves NuGet DLL conflicts with Unity's built-in BCL assemblies.
    /// This resolver runs on every domain reload and detects when a NuGet package provides
    /// a DLL that Unity already ships as a built-in system assembly (e.g., System.Text.Json
    /// in Unity 6.5+). When a conflict is found, the NuGet DLL is disabled via PluginImporter
    /// so Unity uses its built-in version instead.
    ///
    /// This assembly has ZERO external dependencies and always compiles, even when the main
    /// plugin assembly fails due to DLL version conflicts (CS1705).
    /// </summary>
    [InitializeOnLoad]
    static class NuGetDependencyResolver
    {
        const string Tag = "[Unity-MCP DependencyResolver]";
        const string ReadyDefine = "UNITY_MCP_READY";
        const string ResolvingKey = "NuGetDependencyResolver_Resolving";
        const string ResolvedCountKey = "NuGetDependencyResolver_ResolvedCount";

        static NuGetDependencyResolver()
        {
            // Prevent re-entry during a resolver-triggered reload.
            if (SessionState.GetBool(ResolvingKey, false))
            {
                // Clear the flag so the resolver can run again on the next fresh reload.
                SessionState.SetBool(ResolvingKey, false);
                EnsureScriptingDefine();
                return;
            }

            // Defer to next editor frame to avoid issues during domain reload.
            EditorApplication.delayCall += Resolve;
        }

        static void Resolve()
        {
            try
            {
                var conflicts = DetectConflicts();
                var changed = DisableConflictingDlls(conflicts);

                EnsureScriptingDefine();

                if (changed)
                {
                    SessionState.SetBool(ResolvingKey, true);
                    Debug.Log($"{Tag} Resolved {conflicts.Count} conflict(s). Triggering domain reload...");
                    EditorApplication.delayCall += () => AssetDatabase.Refresh();
                }
                else
                {
                    var previousCount = SessionState.GetInt(ResolvedCountKey, -1);
                    if (previousCount != conflicts.Count)
                    {
                        SessionState.SetInt(ResolvedCountKey, conflicts.Count);
                        if (conflicts.Count > 0)
                            Debug.Log($"{Tag} {conflicts.Count} NuGet DLL(s) already disabled (Unity provides them as built-in).");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Failed to resolve dependencies: {ex.Message}\n{ex.StackTrace}");
                // Ensure the define is set even on error so the plugin can still attempt to compile.
                EnsureScriptingDefine();
            }
        }

        /// <summary>
        /// Detects NuGet-provided DLLs that conflict with Unity's built-in system assemblies.
        /// Uses CompilationPipeline to compare system assemblies against user-imported assemblies.
        /// </summary>
        static List<ConflictInfo> DetectConflicts()
        {
            // Get Unity's built-in system assemblies (BCL, shipped with the editor).
            var systemPaths = CompilationPipeline.GetPrecompiledAssemblyPaths(
                PrecompiledAssemblySources.SystemAssembly);

            var systemAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in systemPaths)
                systemAssemblyNames.Add(Path.GetFileNameWithoutExtension(path));

            // Get user-imported precompiled assemblies (includes NuGet packages from OpenUPM).
            var userPaths = CompilationPipeline.GetPrecompiledAssemblyPaths(
                PrecompiledAssemblySources.UserAssembly);

            var conflicts = new List<ConflictInfo>();
            foreach (var userDllPath in userPaths)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(userDllPath);
                if (systemAssemblyNames.Contains(assemblyName))
                {
                    conflicts.Add(new ConflictInfo
                    {
                        AssemblyName = assemblyName,
                        UserDllPath = userDllPath
                    });
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Disables conflicting NuGet DLLs via PluginImporter.
        /// Returns true if any DLL was actually changed (requires domain reload).
        /// </summary>
        static bool DisableConflictingDlls(List<ConflictInfo> conflicts)
        {
            var changed = false;

            foreach (var conflict in conflicts)
            {
                var importer = AssetImporter.GetAtPath(conflict.UserDllPath) as PluginImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"{Tag} Could not get PluginImporter for '{conflict.UserDllPath}'. " +
                                     "Falling back to manifest.json removal.");
                    changed |= TryRemoveFromManifest(conflict.AssemblyName);
                    continue;
                }

                if (!importer.GetCompatibleWithEditor())
                    continue; // Already disabled.

                Debug.Log($"{Tag} Disabling '{conflict.AssemblyName}' from NuGet package " +
                          $"- Unity provides it as a built-in system assembly.");

                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(false);
                importer.SaveAndReimport();
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Fallback: removes the conflicting NuGet package from Packages/manifest.json.
        /// Used when PluginImporter cannot handle the DLL (e.g., immutable package cache).
        /// </summary>
        static bool TryRemoveFromManifest(string assemblyName)
        {
            var manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
                return false;

            var nugetPackageName = "org.nuget." + assemblyName.ToLowerInvariant();
            var content = File.ReadAllText(manifestPath);

            if (!content.Contains(nugetPackageName))
                return false;

            // Remove the line containing the NuGet package reference.
            var lines = content.Split('\n').ToList();
            var removed = false;
            for (var i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Contains($"\"{nugetPackageName}\""))
                {
                    // Remove trailing comma from previous line if this was the last entry.
                    lines.RemoveAt(i);
                    removed = true;
                    Debug.Log($"{Tag} Removed '{nugetPackageName}' from manifest.json " +
                              "- Unity provides the assembly as built-in.");
                    break;
                }
            }

            if (removed)
            {
                File.WriteAllText(manifestPath, string.Join("\n", lines));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Ensures the UNITY_MCP_READY scripting define is set for all build targets.
        /// This define gates the main plugin assemblies via defineConstraints in their .asmdef files,
        /// ensuring they only compile after the dependency resolver has run.
        /// </summary>
        static void EnsureScriptingDefine()
        {
            var buildTarget = NamedBuildTarget.FromBuildTargetGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);

            PlayerSettings.GetScriptingDefineSymbols(buildTarget, out var defines);

            if (defines.Contains(ReadyDefine))
                return;

            var newDefines = new string[defines.Length + 1];
            Array.Copy(defines, newDefines, defines.Length);
            newDefines[defines.Length] = ReadyDefine;

            PlayerSettings.SetScriptingDefineSymbols(buildTarget, newDefines);
            Debug.Log($"{Tag} Added '{ReadyDefine}' scripting define for {buildTarget}.");
        }

        struct ConflictInfo
        {
            public string AssemblyName;
            public string UserDllPath;
        }
    }
}
