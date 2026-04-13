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
        const string Tag = "[NuGet]";

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
                UnityAssemblyResolver.InvalidateCache();

                // Ensure install directory exists
                if (!Directory.Exists(NuGetConfig.InstallPath))
                    Directory.CreateDirectory(NuGetConfig.InstallPath);

                // Install missing packages
                var requiredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var package in NuGetConfig.Packages)
                {
                    requiredIds.Add(package.Id);
                    anyChanged |= NuGetPackageInstaller.Install(package);
                }

                // Remove packages that Unity now provides
                NuGetPackageInstaller.RemoveUnnecessaryPackages(requiredIds);

                if (anyChanged)
                    Debug.Log($"{Tag} Package restore complete. New packages were installed.");
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
        /// Quick check: are all configured packages already installed?
        /// Used to skip the full restore when everything is up to date.
        /// </summary>
        public static bool AllPackagesInstalled()
        {
            if (!Directory.Exists(NuGetConfig.InstallPath))
                return false;

            foreach (var package in NuGetConfig.Packages)
            {
                var installDir = Path.Combine(NuGetConfig.InstallPath, package.InstallDirectoryName);
                if (!Directory.Exists(installDir) || Directory.GetFiles(installDir, "*.dll").Length == 0)
                    return false;
            }

            return true;
        }
    }
}
