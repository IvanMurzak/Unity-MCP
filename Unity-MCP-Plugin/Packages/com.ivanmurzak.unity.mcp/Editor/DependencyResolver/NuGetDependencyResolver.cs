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
    /// NuGet DLLs are bundled directly in the package (Plugins/NuGet/).
    /// This resolver handles:
    ///   1. Detecting Unity-provided assemblies that overlap with bundled DLLs
    ///   2. Configuring PluginImporter settings (editor-only vs builds, conflict resolution)
    ///   3. Setting the UNITY_MCP_READY scripting define so main assemblies can compile
    ///
    /// This assembly has ZERO external dependencies — it always compiles, even when the
    /// main plugin fails due to missing or conflicting DLLs.
    /// </summary>
    [InitializeOnLoad]
    static class NuGetDependencyResolver
    {
        const string Tag = "[Unity-MCP DependencyResolver]";
        const string ReadyDefine = "UNITY_MCP_READY";

        static NuGetDependencyResolver()
        {
            EditorApplication.update += ResolveOnce;
        }

        static void ResolveOnce()
        {
            EditorApplication.update -= ResolveOnce;

            try
            {
                NuGetPluginConfigurator.ConfigureAll();
                EnsureScriptingDefine();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Failed: {ex.Message}\n{ex.StackTrace}");
                EnsureScriptingDefine();
            }
        }

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
