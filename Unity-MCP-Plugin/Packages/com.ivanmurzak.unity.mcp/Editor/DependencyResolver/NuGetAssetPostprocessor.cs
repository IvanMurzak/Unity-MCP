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
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.DependencyResolver
{
    /// <summary>
    /// AssetPostprocessor that configures PluginImporter settings for NuGet DLLs.
    /// When Unity imports a DLL from Assets/Plugins/NuGet/, this postprocessor:
    ///   - Sets platform compatibility (editor-only vs all platforms) based on NuGetConfig
    ///   - Marks the asset as processed to avoid re-processing
    ///
    /// This runs during Unity's import phase, BEFORE compilation — the ideal time
    /// to configure DLL settings (NuGetForUnity uses the same pattern).
    /// </summary>
    class NuGetAssetPostprocessor : AssetPostprocessor
    {
        const string ProcessedLabel = "UnityMCP-NuGet";

        void OnPreprocessAsset()
        {
            // Only process DLLs in our NuGet install directory
            if (!assetPath.StartsWith(NuGetConfig.InstallPath + "/"))
                return;

            if (!assetPath.EndsWith(".dll"))
                return;

            var importer = assetImporter as PluginImporter;
            if (importer == null)
                return;

            // Skip if already processed
            if (AssetDatabase.GetLabels(importer).Contains(ProcessedLabel))
                return;

            // Determine if this DLL should be included in builds
            var includeInBuild = ShouldIncludeInBuild(assetPath);

            if (includeInBuild)
            {
                // Runtime DLL: include in all platforms
                importer.SetCompatibleWithAnyPlatform(true);
                importer.SetExcludeEditorFromAnyPlatform(false);
            }
            else
            {
                // Editor-only DLL: exclude from builds
                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(true);
            }

            // Mark as processed
            var labels = AssetDatabase.GetLabels(importer).ToList();
            labels.Add(ProcessedLabel);
            AssetDatabase.SetLabels(importer, labels.ToArray());
        }

        /// <summary>
        /// Determines if a DLL should be included in game builds based on the package config.
        /// Checks the parent directory name to match against NuGetConfig.Packages.
        /// </summary>
        static bool ShouldIncludeInBuild(string dllPath)
        {
            // Path format: Assets/Plugins/NuGet/{PackageId}.{Version}/{dll}
            var dirName = Path.GetFileName(Path.GetDirectoryName(dllPath));
            if (dirName == null)
                return false;

            foreach (var package in NuGetConfig.Packages)
            {
                if (dirName.StartsWith(package.Id, System.StringComparison.OrdinalIgnoreCase))
                    return package.IncludeInBuild;
            }

            // Transitive dependency — check if any runtime-required package depends on it
            // Default: editor-only (safer — don't pollute builds with unnecessary DLLs)
            return false;
        }
    }
}
