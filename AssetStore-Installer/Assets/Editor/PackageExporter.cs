using UnityEngine;
using UnityEditor;
using System.IO;

namespace com.IvanMurzak.Unity.MCP.Installer
{
    public static class PackageExporter
    {
        public static void ExportPackage()
        {
            var packagePath = "Assets/com.IvanMurzak/AI Game Dev Installer";
            var outputPath = "build/AI-Game-Dev-Installer.unitypackage";

            // Ensure build directory exists
            var buildDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            // Export the package
            AssetDatabase.ExportPackage(packagePath, outputPath, ExportPackageOptions.Recurse);

            Debug.Log($"Package exported to: {outputPath}");
        }
    }
}