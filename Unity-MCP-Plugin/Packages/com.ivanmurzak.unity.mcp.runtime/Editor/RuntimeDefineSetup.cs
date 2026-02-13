/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace com.IvanMurzak.Unity.MCP.Player.Editor
{
    /// <summary>
    /// Automatically adds UNITY_MCP_RUNTIME scripting define symbol to all build targets
    /// when the runtime package is installed. This enables the main package's Runtime assembly
    /// and bundled DLLs to be included in player builds.
    /// </summary>
    [InitializeOnLoad]
    public static class RuntimeDefineSetup
    {
        const string DefineSymbol = "UNITY_MCP_RUNTIME";

        static RuntimeDefineSetup()
        {
            AddDefineSymbol();
        }

        static void AddDefineSymbol()
        {
            var buildTargets = new[]
            {
                NamedBuildTarget.Standalone,
                NamedBuildTarget.Android,
                NamedBuildTarget.iOS,
                NamedBuildTarget.WebGL,
                NamedBuildTarget.Server,
            };

            foreach (var target in buildTargets)
            {
                try
                {
                    PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
                    if (!defines.Contains(DefineSymbol))
                    {
                        var newDefines = defines.Append(DefineSymbol).ToArray();
                        PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
                    }
                }
                catch (Exception)
                {
                    // Target module may not be installed — ignore
                }
            }
        }

        /// <summary>
        /// Removes the UNITY_MCP_RUNTIME scripting define symbol from all build targets.
        /// Call this if you want to clean up after uninstalling the runtime package.
        /// The symbol is harmless if left in place (no assemblies reference it without the package).
        /// </summary>
        public static void RemoveDefineSymbol()
        {
            var buildTargets = new[]
            {
                NamedBuildTarget.Standalone,
                NamedBuildTarget.Android,
                NamedBuildTarget.iOS,
                NamedBuildTarget.WebGL,
                NamedBuildTarget.Server,
            };

            foreach (var target in buildTargets)
            {
                try
                {
                    PlayerSettings.GetScriptingDefineSymbols(target, out string[] defines);
                    var filtered = defines.Where(d => d != DefineSymbol).ToArray();
                    if (filtered.Length != defines.Length)
                        PlayerSettings.SetScriptingDefineSymbols(target, filtered);
                }
                catch (Exception)
                {
                    // Target module may not be installed — ignore
                }
            }
        }
    }
}
