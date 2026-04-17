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
        /// with no stale-version directories of configured packages present on disk?
        /// Used to skip the full restore when everything is up to date. Returning false here forces
        /// the full Restore() path, which deletes stale-version directories via RemoveUnnecessaryPackages.
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

            // No package ID may have multiple version directories on disk. This catches stale
            // versions of BOTH configured packages and transitive dependencies
            // (e.g., "Microsoft.AspNetCore.SignalR.Common.8.0.15" + ".10.0.3") — any duplicate
            // (id → multiple versions) would produce duplicate-assembly conflicts in Unity.
            // Also force the full restore path if any skip-listed package is still on disk,
            // so RemoveUnnecessaryPackages gets a chance to delete it.
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
            }

            return true;
        }
    }
}
