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

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Installs NuGet packages: downloads .nupkg, extracts DLLs, resolves transitive dependencies.
    /// Skips packages that Unity already provides (detected by UnityAssemblyResolver).
    /// Uses highest-version-wins strategy for dependency conflicts.
    /// </summary>
    static class NuGetPackageInstaller
    {
        const string Tag = NuGetConfig.LogTag;

        /// <summary>
        /// Resolved (id → version) closure for the current session, including both top-level
        /// configured packages and their transitive dependencies. Populated by Install() for
        /// every package it processes, whether newly extracted or already on disk. Used by
        /// RemoveUnnecessaryPackages() as the authoritative "keep list".
        /// </summary>
        static readonly Dictionary<string, string> installedThisSession = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, string> InstalledThisSession => installedThisSession;

        /// <summary>
        /// Installs a package and its transitive dependencies.
        /// Returns true if any new DLLs were extracted (requires domain reload).
        /// Always reads the dependency graph (from the cached .nupkg) and records the resolved
        /// (id, version) in <see cref="installedThisSession"/>, even when the package is already
        /// present on disk — otherwise transitive deps of already-installed packages would be
        /// missing from the closure, and stale-version directories of those transitives would
        /// be kept by RemoveUnnecessaryPackages.
        /// </summary>
        public static bool Install(NuGetPackage package, HashSet<string>? visitedIds = null)
        {
            visitedIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Prevent circular dependencies while still allowing the same package Id to be
            // re-entered at a *different* version (so higher-version-wins can replace an
            // already-installed lower version encountered earlier in the graph).
            if (!visitedIds.Add($"{package.Id}:{package.Version}"))
                return false;

            // Skip packages explicitly excluded from the dependency closure.
            // We do NOT recurse into their transitive deps — anything only they need is also
            // unwanted. Packages legitimately needed by other chains will still resolve via
            // those chains.
            if (IsSkipped(package.Id))
                return false;

            var anyInstalled = false;

            try
            {
                // Higher-version-wins when a second chain requests the same Id at a lower version.
                if (installedThisSession.TryGetValue(package.Id, out var existingVersion)
                    && CompareVersions(existingVersion, package.Version) >= 0)
                {
                    return false;
                }

                var installDir = Path.Combine(NuGetConfig.InstallPath, package.InstallDirectoryName);
                var alreadyOnDisk = Directory.Exists(installDir)
                                 && Directory.GetFiles(installDir, "*.dll").Length > 0;

                // Download (cached in Library/NuGetCache across sessions). Needed even when
                // the package is already on disk, so GetDependencies() can build the full closure.
                var nupkgPath = NuGetDownloader.Download(package);

                var dependencies = NuGetExtractor.GetDependencies(nupkgPath);
                foreach (var dep in dependencies)
                    anyInstalled |= Install(dep, visitedIds);

                // If a previous version of the same Id is installed this session (higher-wins path),
                // remove its directory before extracting the new one. Deletion is a material
                // Asset change that must trigger a domain reload, so flip anyInstalled here —
                // otherwise a restore cycle whose only change is cleanup would skip the reload.
                if (existingVersion != null)
                {
                    var oldDir = Path.Combine(NuGetConfig.InstallPath, $"{package.Id}.{existingVersion}");
                    if (Directory.Exists(oldDir))
                    {
                        DeleteDirAndMeta(oldDir);
                        anyInstalled = true;
                    }
                }

                // Extract DLLs only when not already on disk at this exact version.
                if (!alreadyOnDisk)
                {
                    var extractedDlls = NuGetExtractor.ExtractDlls(nupkgPath, installDir);
                    if (extractedDlls.Count > 0)
                    {
                        Debug.Log($"{Tag} Installed {package.Id} {package.Version} ({extractedDlls.Count} DLL(s))");
                        anyInstalled = true;
                    }
                    else
                    {
                        Debug.LogWarning($"{Tag} No DLLs extracted for {package.Id} {package.Version}");
                    }
                }

                installedThisSession[package.Id] = package.Version;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Failed to install {package}: {ex.Message}\n{ex.StackTrace}");
            }

            return anyInstalled;
        }

        /// <summary>
        /// Removes previously-installed NuGet package directories that should no longer exist:
        ///   1. Stale-version directories for any package in the resolved session closure —
        ///      both configured packages AND their transitive dependencies
        ///      (e.g., a leftover "Microsoft.AspNetCore.SignalR.Common.10.0.3" when the current
        ///      closure resolves that transitive to "8.0.15"). Keeping both produces
        ///      duplicate-assembly conflicts.
        ///   2. Directories whose DLLs are now all provided by Unity (e.g., after a Unity
        ///      upgrade that bundled the BCL assembly) and whose package ID is not in the closure.
        ///
        /// <paramref name="requiredVersionByPackageId"/> must contain the full resolved closure
        /// (pass <see cref="InstalledThisSession"/> from the restorer after Install() calls).
        /// Returns true if any installed package directories were removed.
        /// </summary>
        public static bool RemoveUnnecessaryPackages(IReadOnlyDictionary<string, string> requiredVersionByPackageId)
        {
            if (!Directory.Exists(NuGetConfig.InstallPath))
                return false;

            var anyRemoved = false;

            foreach (var dir in Directory.GetDirectories(NuGetConfig.InstallPath))
            {
                var dirName = Path.GetFileName(dir);
                var lastDot = dirName.LastIndexOf('.');
                if (lastDot <= 0)
                    continue;

                // Try to parse as {packageId}.{version}
                // Find the split point: package IDs can contain dots, version starts with a digit
                var packageId = ExtractPackageIdFromDirName(dirName);
                if (packageId == null)
                    continue;

                // Case 0: explicitly skipped package — always remove from disk.
                if (IsSkipped(packageId))
                {
                    Debug.Log($"{Tag} Removing {dirName} — package is in SkipPackages exclusion list.");
                    DeleteDirAndMeta(dir);
                    anyRemoved = true;
                    continue;
                }

                // Case 1: configured package with a stale on-disk version — delete the stale one.
                if (requiredVersionByPackageId.TryGetValue(packageId, out var requiredVersion))
                {
                    var dirVersion = dirName.Substring(packageId.Length + 1);
                    if (string.Equals(dirVersion, requiredVersion, StringComparison.OrdinalIgnoreCase))
                        continue;

                    Debug.Log($"{Tag} Removing {dirName} — stale version; config requires {packageId} {requiredVersion}.");
                    DeleteDirAndMeta(dir);
                    anyRemoved = true;
                    continue;
                }

                // Case 2: unrequired package — remove only when Unity provides every DLL it ships.
                // Using the NuGet package ID directly is unreliable because a package often ships
                // DLLs with names that differ from the package ID (e.g., Microsoft.Bcl.Memory
                // ships System.Memory / System.Buffers / System.Runtime.CompilerServices.Unsafe).
                var dllFiles = Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories);
                if (dllFiles.Length == 0)
                    continue;

                var allProvidedByUnity = dllFiles.All(dll =>
                    UnityAssemblyResolver.IsAlreadyImported(Path.GetFileNameWithoutExtension(dll)));
                if (!allProvidedByUnity)
                    continue;

                Debug.Log($"{Tag} Removing {dirName} — Unity now provides all of its assemblies.");
                DeleteDirAndMeta(dir);
                anyRemoved = true;
            }

            return anyRemoved;
        }

        static void DeleteDirAndMeta(string dir)
        {
            // Directory.Delete and File.Delete can throw in Unity due to file locks (antivirus,
            // the importer pipeline holding handles, Windows sharing violations). Log and
            // continue so a single failure doesn't abort the rest of the cleanup pass.
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Failed to delete directory '{dir}': {ex.Message}");
            }

            var metaFile = dir + ".meta";
            if (File.Exists(metaFile))
            {
                try
                {
                    File.Delete(metaFile);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{Tag} Failed to delete meta file '{metaFile}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Extracts the package ID from a directory name like "System.Text.Json.8.0.5"
        /// or "Microsoft.AspNetCore.SignalR.Protocols.Json.8.0.15".
        /// Scans left-to-right for the FIRST (leftmost) segment that starts with a digit AND
        /// where all segments from there to the end parse as a System.Version. This greedily
        /// consumes the entire version tail (e.g., "10.0.3") rather than a shorter suffix
        /// (e.g., "0.3") that would also satisfy System.Version.TryParse.
        /// </summary>
        internal static string? ExtractPackageIdFromDirName(string dirName)
        {
            var parts = dirName.Split('.');
            for (var i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 0 || !char.IsDigit(parts[i][0]))
                    continue;

                var versionPart = string.Join(".", parts.Skip(i));
                if (System.Version.TryParse(versionPart, out _))
                    return string.Join(".", parts.Take(i));
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

        static bool IsSkipped(string packageId)
        {
            foreach (var skip in NuGetConfig.SkipPackages)
            {
                if (string.Equals(packageId, skip, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
