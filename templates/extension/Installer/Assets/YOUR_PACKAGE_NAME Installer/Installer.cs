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

namespace YOUR_PACKAGE_ID.Installer
{
    [InitializeOnLoad]
    public static partial class Installer
    {
        public const string PackageId = "YOUR_PACKAGE_ID_LOWERCASE";
        public const string Version = "1.0.0";

        static Installer()
        {
#if !IVAN_MURZAK_INSTALLER_PROJECT
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
    }
}
