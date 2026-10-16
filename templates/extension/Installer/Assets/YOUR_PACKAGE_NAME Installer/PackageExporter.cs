/*
┌────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                       │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-Package-Template) │
│  Copyright (c) 2025 Ivan Murzak                                            │
│  Licensed under the MIT License.                                           │
│  See the LICENSE file in the project root for more information.            │
└────────────────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using UnityEngine;
using UnityEditor;
using System.IO;

namespace YOUR_PACKAGE_ID.Installer
{
    public static class PackageExporter
    {
        public static void ExportPackage()
        {
            Debug.Log("Starting Package Export...");

            var packagePath = "Assets/YOUR_PACKAGE_NAME_INSTALLER";
            var outputPath = "build/YOUR_PACKAGE_NAME_INSTALLER_FILE.unitypackage";

            // Ensure build directory exists
            var buildDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            // Collect all asset GUIDs under the package path, excluding Tests folders
            var guids = AssetDatabase.FindAssets("", new[] { packagePath })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => !path.Replace('\\', '/').Contains("/Tests"))
                .ToArray();

            foreach (var path in guids)
            {
                Debug.Log($"Including asset: {path}");
            }

            // Export the package
            AssetDatabase.ExportPackage(packagePath, outputPath, ExportPackageOptions.Recurse);

            Debug.Log($"Package exported to: {outputPath}");
        }
    }
}