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
    /// Restores NuGet packages on domain reload.
    /// Compares the required packages (from NuGetConfig) against what's currently installed
    /// at Assets/Plugins/NuGet/. Downloads and installs any missing packages.
    /// Also removes packages that Unity now provides natively.
    /// </summary>
    static class NuGetPackageRestorer
    {
        const string Tag = NuGetConfig.LogTag;

        /// <summary>
        /// Performs a full package restore. Returns true if any packages were installed
        /// or removed (meaning a domain reload is needed).
        /// </summary>
        public static bool Restore()
        {
            var anyChanged = false;

            try
            {
                NuGetPackageInstaller.ResetSession();

                // Ensure install directory exists
                if (!Directory.Exists(NuGetConfig.InstallPath))
                    Directory.CreateDirectory(NuGetConfig.InstallPath);

                // Install configured packages. Install() populates InstalledThisSession with the
                // full resolved closure (direct + transitive) by always reading the dep graph,
                // including for packages already on disk from a previous session.
                foreach (var package in NuGetConfig.Packages)
                    anyChanged |= NuGetPackageInstaller.Install(package);

                // Invalidate assembly cache only after packages may have changed,
                // so RemoveUnnecessaryPackages sees the current state.
                if (anyChanged)
                    UnityAssemblyResolver.InvalidateCache();

                // Remove stale-version directories of anything in the closure
                // and packages whose DLLs are now all provided by Unity.
                var anyRemoved = NuGetPackageInstaller.RemoveUnnecessaryPackages(
                    NuGetPackageInstaller.InstalledThisSession);
                anyChanged |= anyRemoved;

                if (anyChanged)
                    Debug.Log($"{Tag} Package restore complete. Changes applied (installed and/or removed packages).");
                else
                    Debug.Log($"{Tag} All packages up to date.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Package restore failed: {ex.Message}\n{ex.StackTrace}");
            }

            return anyChanged;
        }

        /// <summary>
        /// Quick check: are all configured packages already installed at their configured version,
        /// with no stale-version directories of configured packages present on disk, AND is the
        /// full transitive closure present?
        /// Used to skip the full restore when everything is up to date. Returning false here forces
        /// the full Restore() path, which deletes stale-version directories via RemoveUnnecessaryPackages
        /// and re-installs any missing transitive dependencies.
        /// </summary>
        public static bool AllPackagesInstalled()
        {
            if (!Directory.Exists(NuGetConfig.InstallPath))
                return false;

            // Every configured package must be installed at the configured version.
            foreach (var package in NuGetConfig.Packages)
            {
                var installDir = Path.Combine(NuGetConfig.InstallPath, package.InstallDirectoryName);
                if (!Directory.Exists(installDir) || Directory.GetFiles(installDir, "*.dll").Length == 0)
                    return false;
            }

            // Collect all package IDs present on disk (at any version). Also detect duplicate-version
            // collisions and skip-listed packages, which both force a full restore:
            //   - duplicate versions (e.g., "SignalR.Common.8.0.15" + ".10.0.3") produce
            //     duplicate-assembly conflicts in Unity
            //   - skip-listed packages must be deleted by RemoveUnnecessaryPackages
            var installedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var skipSet = new HashSet<string>(NuGetConfig.SkipPackages, StringComparer.OrdinalIgnoreCase);
            foreach (var dir in Directory.GetDirectories(NuGetConfig.InstallPath))
            {
                var packageId = NuGetPackageInstaller.ExtractPackageIdFromDirName(Path.GetFileName(dir));
                if (packageId == null)
                    continue;
                if (!seenPackageIds.Add(packageId))
                    return false;
                if (skipSet.Contains(packageId))
                    return false;
                installedPackageIds.Add(packageId);
            }

            // Walk the transitive closure from each configured top-level package via cached
            // .nuspec files and verify every declared dep has SOME install dir on disk.
            // This catches the case where a transitive-dep folder was deleted externally or
            // a prior restore failed mid-install; without this check we'd incorrectly return
            // true and skip the fix-up pass in Restore().
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var package in NuGetConfig.Packages)
            {
                if (!HasTransitiveClosure(package, visited, installedPackageIds, skipSet))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Recursively verifies that <paramref name="package"/> and every package reachable
        /// through its cached .nuspec dependency list has SOME install directory on disk
        /// (any version — the resolved version may differ from the declared one due to
        /// highest-version-wins, but the ID must be present).
        ///
        /// Returns false when any required package is missing, or when we can't read a
        /// cached .nuspec we were expected to find — forcing the caller to run the full
        /// Restore() path.
        /// </summary>
        static bool HasTransitiveClosure(
            NuGetPackage package,
            HashSet<string> visited,
            HashSet<string> installedPackageIds,
            HashSet<string> skipSet)
        {
            if (!visited.Add(package.Id))
                return true;

            // Skip-listed packages are expected to be absent from disk.
            if (skipSet.Contains(package.Id))
                return true;

            if (!installedPackageIds.Contains(package.Id))
                return false;

            // If this specific version's .nupkg isn't cached, the package may have been
            // superseded by a higher version from another chain (which caused Install() to
            // early-return without downloading this version). The install-dir check above
            // already confirmed some version is present, so stop recursing here rather than
            // forcing an unnecessary restore.
            if (!NuGetCache.IsCached(package))
                return true;

            List<NuGetPackage> deps;
            try
            {
                deps = NuGetExtractor.GetDependencies(NuGetCache.GetCachedPath(package));
            }
            catch
            {
                // Corrupted cache — let Restore() re-download and re-validate.
                return false;
            }

            foreach (var dep in deps)
            {
                if (!HasTransitiveClosure(dep, visited, installedPackageIds, skipSet))
                    return false;
            }
            return true;
        }
    }
}
