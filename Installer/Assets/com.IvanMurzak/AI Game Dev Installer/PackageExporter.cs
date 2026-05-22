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
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Installer
{
    public static class PackageExporter
    {
        // Source folder inside the Installer Unity project that contains the
        // self-contained bootstrap scripts shipped to end users.
        // NOTE: This is the Installer's OWN package folder — it is intentionally
        // independent from the Unity-MCP-Plugin source at
        // `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp`. The Installer
        // only adds `com.ivanmurzak.unity.mcp` as an OpenUPM dependency in the
        // user's manifest.json; it never ships the plugin source directly.
        const string PackagePath = "Assets/com.IvanMurzak/AI Game Dev Installer";
        const string OutputPath = "build/AI-Game-Dev-Installer.unitypackage";
        const string TestsFolderSegment = "/Tests";

        public static void ExportPackage()
        {
            // Ensure build directory exists
            var buildDir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(buildDir) && !Directory.Exists(buildDir))
                Directory.CreateDirectory(buildDir);

            // Collect all asset paths under the package path, excluding Tests folders.
            // Tests must not ship to end users because:
            // - They would pollute the user's Assets folder on import.
            // - Their asmdef is gated on UNITY_INCLUDE_TESTS and nunit, which users may not have.
            var assetPaths = AssetDatabase.FindAssets(string.Empty, new[] { PackagePath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => path.Replace('\\', '/'))
                .Where(path => !path.Contains(TestsFolderSegment))
                .Distinct()
                .ToArray();

            if (assetPaths.Length == 0)
            {
                Debug.LogError($"[PackageExporter] No assets found under '{PackagePath}'. Aborting export.");
                return;
            }

            foreach (var path in assetPaths)
                Debug.Log($"[PackageExporter] Including asset: {path}");

            // Use the array overload so the Tests filter is actually honored.
            // ExportPackageOptions.Default preserves the explicit path list without
            // forcing folder recursion that would re-add filtered-out assets.
            AssetDatabase.ExportPackage(assetPaths, OutputPath, ExportPackageOptions.Default);

            Debug.Log($"[PackageExporter] Package exported to: {OutputPath} ({assetPaths.Length} asset(s))");
        }
    }
}
