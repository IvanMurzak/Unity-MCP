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
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.DependencyResolver
{
    /// <summary>
    /// Entry point for NuGet dependency management. Runs on every domain reload via [InitializeOnLoad].
    ///
    /// This assembly has ZERO external dependencies — it always compiles, even when the main plugin
    /// fails due to missing or conflicting DLLs. It downloads NuGet packages directly from nuget.org,
    /// extracts DLLs, skips assemblies Unity already provides, and sets the UNITY_MCP_READY define
    /// so the main plugin assemblies can compile.
    ///
    /// Flow:
    ///   1. [InitializeOnLoad] fires on domain reload
    ///   2. Deferred via EditorApplication.delayCall (safe timing)
    ///   3. NuGetPackageRestorer checks if all packages are installed
    ///   4. Downloads and installs any missing packages
    ///   5. Sets UNITY_MCP_READY scripting define
    ///   6. If packages were installed: triggers AssetDatabase.Refresh() → domain reload
    ///   7. On next reload: everything is in place, main plugin compiles
    /// </summary>
    [InitializeOnLoad]
    static class NuGetDependencyResolver
    {
        const string Tag = "[Unity-MCP DependencyResolver]";
        const string ReadyDefine = "UNITY_MCP_READY";
        const string ResolvingKey = "NuGetDependencyResolver_Resolving";

        static NuGetDependencyResolver()
        {
            // After a resolver-triggered reload, just set the define and exit.
            if (SessionState.GetBool(ResolvingKey, false))
            {
                SessionState.SetBool(ResolvingKey, false);
                EnsureScriptingDefine();
                return;
            }

            EditorApplication.delayCall += Resolve;
        }

        static void Resolve()
        {
            try
            {
                // Quick check: if all packages are already installed, just ensure the define is set.
                if (NuGetPackageRestorer.AllPackagesInstalled())
                {
                    EnsureScriptingDefine();
                    return;
                }

                // Full restore: download and install missing packages.
                Debug.Log($"{Tag} Restoring NuGet packages...");
                var changed = NuGetPackageRestorer.Restore();

                EnsureScriptingDefine();

                if (changed)
                {
                    SessionState.SetBool(ResolvingKey, true);
                    Debug.Log($"{Tag} Packages restored. Triggering domain reload...");
                    EditorApplication.delayCall += () => AssetDatabase.Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Failed: {ex.Message}\n{ex.StackTrace}");
                // Set the define even on error so the plugin can attempt to compile.
                EnsureScriptingDefine();
            }
        }

        /// <summary>
        /// Ensures the UNITY_MCP_READY scripting define is set.
        /// This define gates the main plugin assemblies via defineConstraints.
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
            Debug.Log($"{Tag} Added '{ReadyDefine}' scripting define.");
        }
    }
}
