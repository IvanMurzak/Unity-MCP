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
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Tilemap.Installer
{
    [InitializeOnLoad]
    public static partial class Installer
    {
        public const string PackageId = "com.ivanmurzak.unity.mcp.tilemap";
        public const string Version = "2.0.1";

        static Installer()
        {
#if !IVAN_MURZAK_INSTALLER_PROJECT
            EnsureUnityPackageCompatibility(ManifestPath, Application.unityVersion);
            if (AddScopedRegistryIfNeeded(ManifestPath))
                DeleteSelf();
#endif
        }

        static void DeleteSelf([CallerFilePath] string callerFilePath = "")
        {
            var dataPath = Application.dataPath.Replace("\\", "/");
            var filePath = callerFilePath.Replace("\\", "/");

            if (!filePath.StartsWith(dataPath))
            {
                Debug.LogWarning($"[Installer] Cannot determine asset path for: {filePath}");
                return;
            }

            var relativePath = "Assets" + filePath.Substring(dataPath.Length);
            var installerFolder = Path.GetDirectoryName(relativePath)!.Replace("\\", "/");

            EditorApplication.delayCall += () =>
            {
                if (!AssetDatabase.IsValidFolder(installerFolder))
                    return;

                AssetDatabase.DeleteAsset(installerFolder);
                Debug.Log($"[Installer] Deleted installer folder: {installerFolder}");

                var parentFolder = Path.GetDirectoryName(installerFolder)!.Replace("\\", "/");
                if (AssetDatabase.IsValidFolder(parentFolder))
                {
                    var remaining = AssetDatabase.FindAssets("", new[] { parentFolder });
                    if (remaining.Length == 0)
                    {
                        AssetDatabase.DeleteAsset(parentFolder);
                        Debug.Log($"[Installer] Cleaned up empty parent folder: {parentFolder}");
                    }
                }

                AssetDatabase.Refresh();
            };
        }

        // Conditional UPM dependencies are not expressible in package.json. Upgrade before
        // the self-contained installer asks Unity to import packages; preserve newer/custom pins.
        internal static bool EnsureUnityPackageCompatibility(string manifestPath, string unityVersion)
        {
            var editor = System.Text.RegularExpressions.Regex.Match(unityVersion, @"^(\d+)\.(\d+)");
            if (!editor.Success || int.Parse(editor.Groups[1].Value) < 6000
                || (int.Parse(editor.Groups[1].Value) == 6000 && int.Parse(editor.Groups[2].Value) < 6))
                return false;
            if (!System.IO.File.Exists(manifestPath)) return false;
            var manifest = SimpleJSON.JSONNode.Parse(System.IO.File.ReadAllText(manifestPath)).AsObject;
            if (manifest == null) throw new System.InvalidOperationException("Invalid Unity manifest");
            var dependencies = manifest[Dependencies].AsObject;
            if (dependencies == null)
            {
                dependencies = new SimpleJSON.JSONObject();
                manifest[Dependencies] = dependencies;
            }
            const string dependency = "com.unity.2d.tilemap.extras";
            const string required = "7.0.1";
            var current = dependencies[dependency].Value;
            if (!string.IsNullOrEmpty(current) && (!System.Version.TryParse(current, out var prior)
                || System.Version.Parse(required) <= prior)) return false;
            dependencies[dependency] = required;
            System.IO.File.WriteAllText(manifestPath, manifest.ToString(2));
            return true;
        }
    }
}
