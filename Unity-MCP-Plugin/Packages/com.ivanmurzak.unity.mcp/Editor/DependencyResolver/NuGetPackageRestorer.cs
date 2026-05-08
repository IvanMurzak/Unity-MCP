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
    /// Compares the required packages (from NuGetConfig) against the on-disk manifest at
    /// <c>Assets/Plugins/NuGet/.nuget-installed.json</c>. Downloads and installs any missing
    /// packages. Also removes packages that Unity now provides natively.
    ///
    /// Since #733 the on-disk layout is flat — see <see cref="NuGetExtractor"/> and
    /// <see cref="NuGetInstallManifest"/>. The restorer also runs the one-shot
    /// migration from the legacy per-package layout via <see cref="NuGetLegacyMigration"/>
    /// at the start of every Restore() pass; the migration is idempotent and is a
    /// no-op once the legacy folders are gone.
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

                // One-shot migration from the legacy {Id}.{Version}/ layout. After the first
                // successful pass this short-circuits at NoLegacyState in O(directory listing).
                var migration = NuGetLegacyMigration.Run(NuGetConfig.InstallPath);
                if (migration.Outcome == NuGetLegacyMigration.Outcome.AbortedFileLock)
                {
                    // Cannot continue safely — extracting flat-layout DLLs alongside the
                    // surviving legacy folders would brick the project with duplicate-assembly
                    // errors. Leave everything where it is and report; the next domain reload
                    // (after the user closes whatever held the lock) will retry.
                    return false;
                }
                if (migration.Outcome == NuGetLegacyMigration.Outcome.Migrated)
                    anyChanged = true;

                // Manifest disaster recovery: rebuild from on-disk versioned
                // filenames when .nuget-installed.json is missing. The
                // {stem}.{packageVersion}.dll pattern carries enough metadata
                // for TryRebuildFromDisk to reconstruct a manifest matching the
                // steady state, so Install()'s alreadyOnDisk check then short-
                // circuits and no re-extraction churn happens. Persist the
                // rebuilt manifest immediately so subsequent passes see it.
                if (!File.Exists(NuGetInstallManifest.GetPath(NuGetConfig.InstallPath)))
                {
                    var rebuilt = NuGetInstallManifest.TryRebuildFromDisk(NuGetConfig.InstallPath);
                    if (rebuilt.Packages.Count > 0)
                    {
                        Debug.Log($"{Tag} Manifest missing — rebuilt {rebuilt.Packages.Count} entries from on-disk filenames.");
                        NuGetInstallManifest.Save(NuGetConfig.InstallPath, rebuilt);
                    }
                }

                // Install configured packages. Install() populates InstalledThisSession with the
                // full resolved closure (direct + transitive) by always reading the dep graph,
                // including for packages already on disk from a previous session.
                foreach (var package in NuGetConfig.Packages)
                    anyChanged |= NuGetPackageInstaller.Install(package);

                // Invalidate assembly cache only after packages may have changed,
                // so RemoveUnnecessaryPackages sees the current state.
                if (anyChanged)
                    UnityAssemblyResolver.InvalidateCache();

                // Remove stale-version manifest entries for anything in the closure
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
        /// with no stale-version entries of configured packages present in the manifest, AND is the
        /// full transitive closure present?
        /// Used to skip the full restore when everything is up to date. Returning false here forces
        /// the full Restore() path, which runs migration + reconciliation.
        /// </summary>
        public static bool AllPackagesInstalled()
        {
            if (!Directory.Exists(NuGetConfig.InstallPath))
                return false;

            // Legacy per-package directories on disk → migration is pending → force a full restore.
            // Cheap enough on the steady-state path because there are no legacy directories left
            // after the first migration run.
            foreach (var dir in Directory.GetDirectories(NuGetConfig.InstallPath))
            {
                var dirName = Path.GetFileName(dir);
                if (NuGetPackageInstaller.ExtractPackageIdFromDirName(dirName) != null)
                    return false;
            }

            // Manifest is the source of truth for the flat layout. If it's missing we need a full
            // restore — Restore() will rebuild it from on-disk filenames as the first step, then
            // the closure-completeness check below runs against the rebuilt entries.
            if (!File.Exists(NuGetInstallManifest.GetPath(NuGetConfig.InstallPath)))
                return false;

            var manifest = NuGetInstallManifest.Load(NuGetConfig.InstallPath);

            var skipSet = new HashSet<string>(NuGetConfig.SkipPackages, StringComparer.OrdinalIgnoreCase);

            // No skip-listed package may sit in the manifest.
            foreach (var packageId in manifest.Packages.Keys)
            {
                if (skipSet.Contains(packageId))
                    return false;
            }

            // Every configured package must be installed at the configured version, UNLESS the
            // package is a development-only dependency (analyzers, source generators, build
            // tooling). The installer intentionally leaves an empty-Dlls manifest entry for those.
            foreach (var package in NuGetConfig.Packages)
            {
                if (IsCachedDevelopmentDependency(package))
                {
                    // Dev-deps are still tracked in the manifest with an empty DLL list — that
                    // marker tells AllPackagesInstalled() to skip the on-disk DLL check for them.
                    if (!manifest.Packages.TryGetValue(package.Id, out var devEntry))
                        return false;
                    if (!string.Equals(devEntry.Version, package.Version, StringComparison.OrdinalIgnoreCase))
                        return false;
                    continue;
                }

                if (!manifest.Packages.TryGetValue(package.Id, out var entry))
                    return false;
                if (!string.Equals(entry.Version, package.Version, StringComparison.OrdinalIgnoreCase))
                    return false;

                // Every recorded DLL must still be on disk.
                foreach (var dll in entry.Dlls)
                {
                    if (!File.Exists(Path.Combine(NuGetConfig.InstallPath, dll)))
                        return false;
                }
            }

            // Walk the transitive closure from each configured top-level package via cached
            // .nuspec files and verify every declared dep has a manifest entry. Catches the
            // case where a transitive-dep DLL was deleted externally or a prior restore
            // failed mid-install; without this we'd incorrectly return true and skip the
            // fix-up pass in Restore().
            var installedPackageIds = new HashSet<string>(manifest.Packages.Keys, StringComparer.OrdinalIgnoreCase);
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
        /// through its cached .nuspec dependency list has a manifest entry (any version —
        /// the resolved version may differ from the declared one due to highest-version-wins,
        /// but the ID must be present).
        /// </summary>
        static bool HasTransitiveClosure(
            NuGetPackage package,
            HashSet<string> visited,
            HashSet<string> installedPackageIds,
            HashSet<string> skipSet)
        {
            if (!visited.Add(package.Id))
                return true;

            // Skip-listed packages are expected to be absent.
            if (skipSet.Contains(package.Id))
                return true;

            var cachedPath = NuGetCache.IsCached(package)
                ? NuGetCache.GetCachedPath(package)
                : null;

            // Development-only dependencies (analyzers, source generators, build tooling)
            // legitimately have an empty DLL list in the manifest.
            if (cachedPath != null && NuGetExtractor.IsDevelopmentDependency(cachedPath))
                return true;

            if (!installedPackageIds.Contains(package.Id))
                return false;

            // If this specific version's .nupkg isn't cached, the package may have been
            // superseded by a higher version from another chain (which caused Install() to
            // early-return without downloading this version). The manifest check above
            // already confirmed the ID is present, so stop recursing here.
            if (cachedPath == null)
                return true;

            List<NuGetPackage> deps;
            try
            {
                deps = NuGetExtractor.GetDependencies(cachedPath);
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

        /// <summary>
        /// Returns true when <paramref name="package"/>'s .nupkg is on disk in the NuGet cache
        /// and its .nuspec declares <c>&lt;developmentDependency&gt;true&lt;/developmentDependency&gt;</c>.
        /// </summary>
        static bool IsCachedDevelopmentDependency(NuGetPackage package)
        {
            if (!NuGetCache.IsCached(package))
                return false;

            return NuGetExtractor.IsDevelopmentDependency(NuGetCache.GetCachedPath(package));
        }
    }
}
