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
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Installer
{
    [InitializeOnLoad]
    public static partial class Installer
    {
        public const string PackageId = "com.ivanmurzak.unity.mcp";
        public const string Version = "0.72.0";

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
