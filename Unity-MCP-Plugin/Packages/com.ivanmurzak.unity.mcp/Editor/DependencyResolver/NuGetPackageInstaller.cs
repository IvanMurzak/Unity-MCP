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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.DependencyResolver
{
    /// <summary>
    /// Installs NuGet packages: downloads .nupkg, extracts DLLs, resolves transitive dependencies.
    /// Skips packages that Unity already provides (detected by UnityAssemblyResolver).
    /// Uses highest-version-wins strategy for dependency conflicts.
    /// </summary>
    static class NuGetPackageInstaller
    {
        const string Tag = "[NuGet]";

        /// <summary>
        /// Tracks installed packages during a single install session to avoid re-processing.
        /// Key: lowercase package ID, Value: installed version.
        /// </summary>
        static readonly Dictionary<string, string> installedThisSession = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Installs a package and its transitive dependencies.
        /// Returns true if any new DLLs were installed (requires domain reload).
        /// </summary>
        public static bool Install(NuGetPackage package, HashSet<string>? visitedIds = null)
        {
            visitedIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Prevent circular dependencies
            if (!visitedIds.Add(package.Id))
                return false;

            var anyInstalled = false;

            try
            {
                // 1. Skip if Unity already provides this assembly
                if (UnityAssemblyResolver.IsAlreadyImported(package.Id))
                {
                    Debug.Log($"{Tag} Skipping {package.Id} — Unity already provides it.");
                    return false;
                }

                // 2. Skip if already installed at the correct version
                var installDir = Path.Combine(NuGetConfig.InstallPath, package.InstallDirectoryName);
                if (Directory.Exists(installDir) && Directory.GetFiles(installDir, "*.dll").Length > 0)
                    return false; // Already installed

                // 3. Skip if we've already processed this package in this session (higher version wins)
                if (installedThisSession.TryGetValue(package.Id, out var existingVersion))
                {
                    if (CompareVersions(existingVersion, package.Version) >= 0)
                        return false; // Already have same or higher version

                    // Remove old version, install new one
                    var oldDir = Path.Combine(NuGetConfig.InstallPath, $"{package.Id}.{existingVersion}");
                    if (Directory.Exists(oldDir))
                        Directory.Delete(oldDir, recursive: true);
                }

                // 4. Download .nupkg
                var nupkgPath = NuGetDownloader.Download(package);

                // 5. Resolve and install transitive dependencies FIRST
                var dependencies = NuGetExtractor.GetDependencies(nupkgPath);
                foreach (var dep in dependencies)
                    anyInstalled |= Install(dep, visitedIds);

                // 6. Re-check if Unity provides it (dependencies may have changed the state)
                if (UnityAssemblyResolver.IsAlreadyImported(package.Id))
                {
                    Debug.Log($"{Tag} Skipping {package.Id} — Unity already provides it (detected after dependency resolution).");
                    return anyInstalled;
                }

                // 7. Extract DLLs
                var extractedDlls = NuGetExtractor.ExtractDlls(nupkgPath, installDir);
                if (extractedDlls.Count > 0)
                {
                    Debug.Log($"{Tag} Installed {package.Id} {package.Version} ({extractedDlls.Count} DLL(s))");
                    installedThisSession[package.Id] = package.Version;
                    anyInstalled = true;
                }
                else
                {
                    Debug.LogWarning($"{Tag} No DLLs extracted for {package.Id} {package.Version}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Failed to install {package}: {ex.Message}\n{ex.StackTrace}");
            }

            return anyInstalled;
        }

        /// <summary>
        /// Removes packages that are installed but no longer needed
        /// (not in the config and not a transitive dependency).
        /// </summary>
        public static void RemoveUnnecessaryPackages(HashSet<string> requiredPackageIds)
        {
            if (!Directory.Exists(NuGetConfig.InstallPath))
                return;

            foreach (var dir in Directory.GetDirectories(NuGetConfig.InstallPath))
            {
                var dirName = Path.GetFileName(dir);
                var lastDot = dirName.LastIndexOf('.');
                if (lastDot <= 0)
                    continue;

                // Try to parse as {packageId}.{version}
                // Find the split point: package IDs can contain dots, version starts with a digit
                var packageId = ExtractPackageIdFromDirName(dirName);
                if (packageId == null || requiredPackageIds.Contains(packageId))
                    continue;

                // Check if Unity now provides this assembly
                if (UnityAssemblyResolver.IsAlreadyImported(packageId))
                {
                    Debug.Log($"{Tag} Removing {dirName} — Unity now provides this assembly.");
                    Directory.Delete(dir, recursive: true);
                    // Also delete .meta file
                    var metaFile = dir + ".meta";
                    if (File.Exists(metaFile))
                        File.Delete(metaFile);
                }
            }
        }

        /// <summary>
        /// Extracts the package ID from a directory name like "System.Text.Json.10.0.3".
        /// The version starts after the last segment that begins with a digit.
        /// </summary>
        static string? ExtractPackageIdFromDirName(string dirName)
        {
            var parts = dirName.Split('.');
            for (var i = parts.Length - 1; i >= 1; i--)
            {
                if (parts[i].Length > 0 && char.IsDigit(parts[i][0]))
                {
                    // Check if this and all remaining parts form a valid version
                    var versionPart = string.Join(".", parts.Skip(i));
                    if (System.Version.TryParse(versionPart, out _))
                        return string.Join(".", parts.Take(i));
                }
            }
            return null;
        }

        /// <summary>
        /// Compares two version strings. Returns -1, 0, or 1.
        /// </summary>
        static int CompareVersions(string a, string b)
        {
            if (System.Version.TryParse(a, out var va) && System.Version.TryParse(b, out var vb))
                return va.CompareTo(vb);
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resets the session tracking. Call at the start of a new restore cycle.
        /// </summary>
        public static void ResetSession()
        {
            installedThisSession.Clear();
        }
    }
}
