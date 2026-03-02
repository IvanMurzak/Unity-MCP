using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

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