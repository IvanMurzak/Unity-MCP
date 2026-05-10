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
    ///
    /// Since #733 the on-disk layout is flat — every DLL sits directly under
    /// <see cref="NuGetConfig.InstallPath"/> with the versioned filename
    /// <c>{stem}.{packageVersion}.dll</c>. The package → DLL mapping is
    /// recorded in <see cref="NuGetInstallManifest"/> at the root of the
    /// install path; the installer reads/writes that manifest as the
    /// authoritative source of truth.
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
        /// Returns true if any new DLLs were extracted, OR if any stale-version DLLs of this
        /// package's Id were removed from the install path (both require a domain reload).
        /// Always reads the dependency graph (from the cached .nupkg) and records the resolved
        /// (id, version) in <see cref="installedThisSession"/>, even when the package is already
        /// present on disk — otherwise transitive deps of already-installed packages would be
        /// missing from the closure, and stale-version DLLs of those transitives would be kept
        /// by RemoveUnnecessaryPackages.
        /// </summary>
        public static bool Install(NuGetPackage package, HashSet<string>? visitedIds = null)
        {
            return Install(package, NuGetConfig.InstallPath, visitedIds, manifest: null);
        }

        /// <summary>
        /// Test seam — Install() with the install path and the in-memory manifest
        /// injected. Production callers use the no-args overload above. Tests
        /// drive a temp directory and an in-memory manifest so they don't touch
        /// the real Assets/Plugins/NuGet path.
        /// </summary>
        internal static bool Install(NuGetPackage package, string installPath, HashSet<string>? visitedIds, InstallManifest? manifest)
        {
            visitedIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            manifest ??= NuGetInstallManifest.Load(installPath);

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

                // Download (cached in Library/NuGetCache across sessions). Needed even when
                // the package is already on disk, so GetDependencies() can build the full closure.
                var nupkgPath = NuGetDownloader.Download(package);

                var dependencies = NuGetExtractor.GetDependencies(nupkgPath);
                foreach (var dep in dependencies)
                    anyInstalled |= Install(dep, installPath, visitedIds, manifest);

                // Remove any stale-version DLLs for this package Id BEFORE extracting the new
                // payload. Mirrors the legacy stale-version cleanup, just driven by the manifest
                // instead of by directory naming. Still flips anyInstalled because deletion is a
                // material Asset change that requires a domain reload.
                if (RemoveStaleSiblingVersions(installPath, package.Id, package.Version, manifest))
                    anyInstalled = true;

                // Development-only dependencies (Roslyn analyzers, source generators, build
                // tooling) ship their payload under analyzers/, build/, tools/ etc. — never
                // under lib/<tfm>/ — so there are no runtime DLLs for the resolver to extract.
                // Record them in the closure but do not extract anything; the manifest carries
                // an entry with an empty DLL list so AllPackagesInstalled() short-circuits cleanly
                // on the next pass.
                if (NuGetExtractor.IsDevelopmentDependency(nupkgPath))
                {
                    installedThisSession[package.Id] = package.Version;
                    UpsertManifestEntry(manifest, package.Id, package.Version, dlls: Array.Empty<string>());
                    NuGetInstallManifest.Save(installPath, manifest);
                    return anyInstalled;
                }

                // Plan DLL paths up front so synthetic-owner reconciliation (below) can
                // operate on the same filenames the collision check would compare against.
                var planned = NuGetExtractor.PlanDllPaths(nupkgPath, installPath, package.Version);

                // Filesystem fallback for stale DLLs the manifest doesn't know about
                // (manifest deleted, partial-restore failure, AV quarantine).
                if (RemoveStaleVersionDllsByStem(installPath, planned, package.Version, package.Id))
                    anyInstalled = true;

                // Disaster-recovery reconciliation: TryRebuildFromDisk has no way to recover
                // multi-DLL package IDs from filenames alone (e.g., Microsoft.Bcl.Memory ships
                // System.Memory.dll / System.Buffers.dll / System.Runtime.CompilerServices.Unsafe.dll),
                // so it keys those DLLs under their own stems as synthetic package IDs. When the
                // real owner re-installs at the same version, those synthetic entries would trip
                // the collision check below; remove them and let the real owner take over.
                // Persist immediately on a successful migration so a downstream halt (collision
                // check, long-path overflow, extraction failure) cannot leave the manifest
                // desynced from disk.
                if (MigrateSyntheticOwnerEntries(manifest, package.Id, package.Version, planned))
                    NuGetInstallManifest.Save(installPath, manifest);

                // alreadyOnDisk requires a full match against the planned set, not just
                // "every recorded DLL exists". After MigrateSyntheticOwnerEntries pulls
                // synthetic stem-keyed entries into the real-owner entry, that entry's
                // Dlls list only covers whichever DLLs the disaster-recovery rebuild
                // actually observed on disk. If a strict subset of a multi-DLL package's
                // files survived (manifest deleted + one DLL lost to AV / partial cleanup),
                // a recorded-DLLs-only check would short-circuit extraction and leave the
                // missing files unrecovered. Requiring planned ⊆ manifestEntry.Dlls forces
                // re-extraction in that case.
                var alreadyOnDisk = false;
                if (manifest.Packages.TryGetValue(package.Id, out var manifestEntry)
                    && string.Equals(manifestEntry.Version, package.Version, StringComparison.OrdinalIgnoreCase)
                    && manifestEntry.Dlls.Count > 0
                    && planned.All(p => manifestEntry.Dlls.Contains(p.FileName, StringComparer.OrdinalIgnoreCase))
                    && manifestEntry.Dlls.All(d => File.Exists(Path.Combine(installPath, d))))
                {
                    alreadyOnDisk = true;
                }

                // Extract DLLs only when not already present at this exact version.
                if (!alreadyOnDisk)
                {
                    // Defense-in-depth: collision detection. With versioned filenames a real
                    // collision requires two distinct packages shipping the same DLL stem at
                    // the same package version — essentially impossible in practice. Keep the
                    // detection branch anyway so the failure mode is loud rather than silent.
                    foreach (var planEntry in planned)
                    {
                        if (TryFindDllOwner(manifest, planEntry.FileName, package.Id, out var owner))
                        {
                            Debug.LogError(
                                $"{Tag} Refusing to install {package.Id} {package.Version}: " +
                                $"DLL '{planEntry.FileName}' is already owned by package '{owner}' " +
                                $"in the manifest. Loud failure beats silent overwrite — investigate the conflict.");
                            return anyInstalled;
                        }
                    }

                    // Long-path pre-flight (Windows MAX_PATH). Throws on overflow; the catch
                    // below logs and continues — the rest of the closure can still install,
                    // and the user gets a single actionable error in the Console.
                    foreach (var planEntry in planned)
                        NuGetLongPathPreflight.Check(planEntry.TargetPath, package.Id);

                    var extractedDlls = NuGetExtractor.ExtractDlls(nupkgPath, installPath, package.Version);
                    if (extractedDlls.Count > 0)
                    {
                        Debug.Log($"{Tag} Installed {package.Id} {package.Version} ({extractedDlls.Count} DLL(s))");
                        UpsertManifestEntry(manifest, package.Id, package.Version, extractedDlls);
                        NuGetInstallManifest.Save(installPath, manifest);
                        anyInstalled = true;
                    }
                    else
                    {
                        Debug.LogWarning($"{Tag} No DLLs extracted for {package.Id} {package.Version}");
                    }
                }
                else
                {
                    // Already on disk at the right version — just keep the manifest entry
                    // around (it was the source of truth that decided alreadyOnDisk).
                    UpsertManifestEntry(manifest, package.Id, package.Version, manifestEntry!.Dlls);
                    NuGetInstallManifest.Save(installPath, manifest);
                }

                installedThisSession[package.Id] = package.Version;
            }
            catch (InstallPathTooLongException ex)
            {
                // Pre-flight overflow — the message is fully self-contained.
                Debug.LogError(ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Failed to install {package}: {ex.Message}\n{ex.StackTrace}");
            }

            return anyInstalled;
        }

        /// <summary>
        /// Removes manifest entries (and their on-disk DLLs / .meta files) that
        /// should no longer exist:
        ///   1.  Stale-version entries for any package in the resolved session closure
        ///       (e.g., a leftover "Microsoft.AspNetCore.SignalR.Common" at 10.0.3 when the
        ///       current closure resolves that transitive to 8.0.15). Keeping both produces
        ///       duplicate-assembly conflicts.
        ///   2.  Entries whose DLLs are now all provided by Unity (e.g., after a Unity upgrade
        ///       that bundled the BCL assembly) and whose package ID is not in the closure.
        ///   3.  Skip-listed packages — always removed regardless of closure membership.
        ///
        /// <paramref name="requiredVersionByPackageId"/> must contain the full resolved closure
        /// (pass <see cref="InstalledThisSession"/> from the restorer after Install() calls).
        /// Returns true if anything was removed.
        /// </summary>
        public static bool RemoveUnnecessaryPackages(IReadOnlyDictionary<string, string> requiredVersionByPackageId)
        {
            return RemoveUnnecessaryPackages(requiredVersionByPackageId, NuGetConfig.InstallPath, manifest: null);
        }

        internal static bool RemoveUnnecessaryPackages(
            IReadOnlyDictionary<string, string> requiredVersionByPackageId,
            string installPath,
            InstallManifest? manifest)
        {
            if (!Directory.Exists(installPath))
                return false;

            manifest ??= NuGetInstallManifest.Load(installPath);
            var anyRemoved = false;

            // Iterate a snapshot so we can mutate the manifest mid-loop.
            foreach (var packageId in manifest.Packages.Keys.ToList())
            {
                var entry = manifest.Packages[packageId];

                // Case 0: explicitly skipped package — always remove from disk.
                if (IsSkipped(packageId))
                {
                    Debug.Log($"{Tag} Removing {packageId} {entry.Version} — package is in SkipPackages exclusion list.");
                    DeleteEntryFiles(installPath, entry);
                    manifest.Packages.Remove(packageId);
                    anyRemoved = true;
                    continue;
                }

                // Case 1: configured package with a stale version — delete the stale one.
                if (requiredVersionByPackageId.TryGetValue(packageId, out var requiredVersion))
                {
                    if (string.Equals(entry.Version, requiredVersion, StringComparison.OrdinalIgnoreCase))
                        continue;

                    Debug.Log($"{Tag} Removing {packageId} {entry.Version} — stale version; closure requires {packageId} {requiredVersion}.");
                    DeleteEntryFiles(installPath, entry);
                    manifest.Packages.Remove(packageId);
                    anyRemoved = true;
                    continue;
                }

                // Case 2: unrequired package — remove only when Unity provides every DLL it ships.
                // Using the NuGet package ID directly is unreliable because a package often ships
                // DLLs with names that differ from the package ID (e.g., Microsoft.Bcl.Memory
                // ships System.Memory / System.Buffers / System.Runtime.CompilerServices.Unsafe).
                if (entry.Dlls.Count == 0)
                {
                    // Empty manifest entry (development dependency that's no longer in the closure).
                    Debug.Log($"{Tag} Removing {packageId} {entry.Version} — package no longer required.");
                    manifest.Packages.Remove(packageId);
                    anyRemoved = true;
                    continue;
                }

                var allProvidedByUnity = entry.Dlls.All(dll =>
                {
                    // Strip the version tail so the lookup matches the assembly's
                    // manifest name, not the on-disk versioned filename.
                    if (!NuGetInstallManifest.TryParseInstalledDllName(dll, out var stem, out _) || stem == null)
                        stem = Path.GetFileNameWithoutExtension(dll);
                    return UnityAssemblyResolver.IsAlreadyImported(stem!);
                });
                if (!allProvidedByUnity)
                    continue;

                Debug.Log($"{Tag} Removing {packageId} {entry.Version} — Unity now provides all of its assemblies.");
                DeleteEntryFiles(installPath, entry);
                manifest.Packages.Remove(packageId);
                anyRemoved = true;
            }

            if (anyRemoved)
                NuGetInstallManifest.Save(installPath, manifest);

            return anyRemoved;
        }

        /// <summary>
        /// Manifest-driven version of the legacy directory-based stale-sibling
        /// scan. Removes any DLLs the manifest records under
        /// <paramref name="packageId"/> at a version OTHER than
        /// <paramref name="keepVersion"/>, deletes their <c>.meta</c> sidecars,
        /// and removes the manifest entry — the caller re-adds it after
        /// extracting the new DLLs at <paramref name="keepVersion"/>.
        ///
        /// Returns true when at least one DLL was removed.
        /// </summary>
        internal static bool RemoveStaleSiblingVersions(string installPath, string packageId, string keepVersion)
        {
            var manifest = NuGetInstallManifest.Load(installPath);
            var removed = RemoveStaleSiblingVersions(installPath, packageId, keepVersion, manifest);
            if (removed)
                NuGetInstallManifest.Save(installPath, manifest);
            return removed;
        }

        internal static bool RemoveStaleSiblingVersions(string installPath, string packageId, string keepVersion, InstallManifest manifest)
        {
            if (!Directory.Exists(installPath))
                return false;
            if (!manifest.Packages.TryGetValue(packageId, out var entry))
                return false;

            // Same exact version — keep it. Downstream extraction owns this entry.
            if (string.Equals(entry.Version, keepVersion, StringComparison.OrdinalIgnoreCase))
                return false;

            Debug.Log($"{Tag} Removing stale {packageId} {entry.Version} — superseded by {packageId} {keepVersion}.");
            DeleteEntryFiles(installPath, entry);
            manifest.Packages.Remove(packageId);
            return true;
        }

        /// <summary>
        /// Filesystem-driven complement to <see cref="RemoveStaleSiblingVersions"/>:
        /// removes any <c>{stem}.dll</c> or <c>{stem}.{anyOtherVersion}.dll</c> on
        /// disk for the DLL stems this package ships, leaving only the canonical
        /// <c>{stem}.{keepVersion}.dll</c>. Catches stale DLLs the manifest
        /// doesn't know about (manifest deleted, partial-restore failure, AV
        /// quarantine) — without this, the freshly extracted current-version
        /// copy and the orphan stale copy coexist and Unity errors with CS0436.
        /// </summary>
        internal static bool RemoveStaleVersionDllsByStem(string installPath, IReadOnlyList<PlannedDll> planned, string keepVersion, string packageId)
        {
            if (!Directory.Exists(installPath) || planned.Count == 0)
                return false;

            var originalStems = new HashSet<string>(
                planned
                    .Select(p => Path.GetFileNameWithoutExtension(Path.GetFileName(p.EntryFullName)))
                    .Where(s => !string.IsNullOrEmpty(s)),
                StringComparer.OrdinalIgnoreCase);

            if (originalStems.Count == 0)
                return false;

            var canonicalNames = new HashSet<string>(
                planned.Select(p => p.FileName),
                StringComparer.OrdinalIgnoreCase);

            var anyRemoved = false;
            foreach (var dllPath in Directory.GetFiles(installPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(dllPath);
                if (canonicalNames.Contains(fileName))
                    continue;

                // Reuse the manifest's filename parser so the version-tail
                // grammar stays defined in one place. Unversioned legacy
                // {stem}.dll files (e.g. McpPlugin.dll dropped in by hand)
                // fall through to GetFileNameWithoutExtension.
                var stem = NuGetInstallManifest.TryParseInstalledDllName(fileName, out var parsedStem, out _) && parsedStem != null
                    ? parsedStem
                    : Path.GetFileNameWithoutExtension(fileName);

                if (!originalStems.Contains(stem))
                    continue;

                Debug.Log($"{Tag} Removing stale '{fileName}' from install path — superseded by {packageId} {keepVersion}.");
                TryDeleteFile(dllPath);
                TryDeleteFile(dllPath + ".meta");
                anyRemoved = true;
            }

            return anyRemoved;
        }

        static void DeleteEntryFiles(string installPath, InstalledPackage entry)
        {
            foreach (var dll in entry.Dlls)
            {
                var dllPath = Path.Combine(installPath, dll);
                TryDeleteFile(dllPath);
                TryDeleteFile(dllPath + ".meta");
            }
        }

        static void TryDeleteFile(string path)
        {
            if (!File.Exists(path))
                return;
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{Tag} Failed to delete '{path}': {ex.Message}");
            }
        }

        static bool TryFindDllOwner(InstallManifest manifest, string dllFileName, string excludePackageId, out string? owner)
        {
            foreach (var (id, entry) in manifest.Packages)
            {
                if (string.Equals(id, excludePackageId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (entry.Dlls.Any(d => string.Equals(d, dllFileName, StringComparison.OrdinalIgnoreCase)))
                {
                    owner = id;
                    return true;
                }
            }
            owner = null;
            return false;
        }

        /// <summary>
        /// Reconciles synthetic manifest entries left behind by
        /// <see cref="NuGetInstallManifest.TryRebuildFromDisk"/> with the real
        /// owning package. The rebuild keys multi-DLL packages under each DLL's
        /// stem (because filenames carry no package-id metadata); when the real
        /// owner re-installs at the same version, those stem-keyed entries are
        /// the same physical DLLs we are about to extract — drop them so the
        /// downstream collision check does not flag the package as a duplicate.
        ///
        /// Match criteria for "synthetic owner":
        ///   * the manifest entry's id is NOT this package's id;
        ///   * the entry version matches the package version exactly;
        ///   * every DLL the entry lists is also one of this package's planned
        ///     filenames (so we never strip a real, distinct package that
        ///     happens to share a version number).
        ///
        /// On a hit we also migrate any DLLs that already exist on disk into a
        /// real entry under <paramref name="realPackageId"/>, which lets the
        /// caller's <c>alreadyOnDisk</c> check short-circuit re-extraction.
        ///
        /// Returns <c>true</c> when the manifest was mutated (caller MUST persist
        /// via <see cref="NuGetInstallManifest.Save"/>); <c>false</c> on a no-op.
        ///
        /// <para>
        /// Same-version cross-package DLL collision: this migrator is the only
        /// place where a hypothetical legitimate distinct package shipping a
        /// same-named DLL at the same package version is silently absorbed into
        /// the real owner — the downstream collision check (in
        /// <see cref="Install"/>) cannot fire for entries this method already
        /// migrated. The collision is essentially impossible in practice
        /// (versioned filenames + the same-version constraint), but the silent
        /// absorption is intentional for the disaster-recovery scenario.
        /// </para>
        /// </summary>
        internal static bool MigrateSyntheticOwnerEntries(
            InstallManifest manifest,
            string realPackageId,
            string version,
            IReadOnlyList<PlannedDll> planned)
        {
            if (planned.Count == 0)
                return false;

            var plannedFileNames = new HashSet<string>(
                planned.Select(p => p.FileName),
                StringComparer.OrdinalIgnoreCase);

            var migratedDlls = new List<string>();
            foreach (var id in manifest.Packages.Keys.ToList())
            {
                if (string.Equals(id, realPackageId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var entry = manifest.Packages[id];
                if (!string.Equals(entry.Version, version, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (entry.Dlls.Count == 0 || !entry.Dlls.All(d => plannedFileNames.Contains(d)))
                    continue;

                // Synthetic entry confirmed — surface the reconciliation so a stuck
                // user can correlate it with the rebuild log.
                Debug.Log(
                    $"{Tag} Reconciling disaster-recovery manifest entry '{id}' {version} into real owner '{realPackageId}'.");
                migratedDlls.AddRange(entry.Dlls);
                manifest.Packages.Remove(id);
            }

            if (migratedDlls.Count == 0)
                return false;

            // Merge into any pre-existing real entry rather than overwriting it.
            if (!manifest.Packages.TryGetValue(realPackageId, out var realEntry)
                || !string.Equals(realEntry.Version, version, StringComparison.OrdinalIgnoreCase))
            {
                realEntry = new InstalledPackage(version);
                manifest.Packages[realPackageId] = realEntry;
            }
            foreach (var dll in migratedDlls)
            {
                if (!realEntry.Dlls.Contains(dll, StringComparer.OrdinalIgnoreCase))
                    realEntry.Dlls.Add(dll);
            }

            return true;
        }

        static void UpsertManifestEntry(InstallManifest manifest, string packageId, string version, IReadOnlyList<string> dlls)
        {
            var newEntry = new InstalledPackage(version);
            foreach (var dll in dlls)
                newEntry.Dlls.Add(dll);
            manifest.Packages[packageId] = newEntry;
        }

        /// <summary>
        /// Extracts the package ID from a directory name like "System.Text.Json.8.0.5"
        /// or "Microsoft.AspNetCore.SignalR.Protocols.Json.8.0.15".
        /// Scans left-to-right for the FIRST (leftmost) segment that starts with a digit AND
        /// where all segments from there to the end parse as a System.Version. This greedily
        /// consumes the entire version tail (e.g., "10.0.3") rather than a shorter suffix
        /// (e.g., "0.3") that would also satisfy System.Version.TryParse.
        ///
        /// Retained post-#733 because <see cref="NuGetLegacyMigration"/> needs it
        /// to detect legacy <c>{Id}.{Version}/</c> directories during the one-time
        /// migration. Production install paths never use this layout anymore.
        /// </summary>
        internal static string? ExtractPackageIdFromDirName(string dirName)
        {
            var parts = dirName.Split('.');
            for (var i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 0 || !char.IsDigit(parts[i][0]))
                    continue;

                var versionPart = string.Join(".", parts.Skip(i));
                // System.Version.TryParse only accepts plain Major.Minor[.Build[.Revision]];
                // it rejects SemVer prerelease / build-metadata suffixes like
                // "1.0.0-preview" or "1.2.3+build.42". NuGet packages legitimately use
                // those (e.g. Microsoft.AspNetCore.SignalR.Client.8.0.15-preview), so
                // fall through to a SemVer-shape regex when System.Version refuses,
                // otherwise the migration silently leaves those legacy folders on disk
                // and the user ends up with duplicate-DLL compile errors.
                if (System.Version.TryParse(versionPart, out _) || SemVerShape.IsMatch(versionPart))
                    return string.Join(".", parts.Take(i));
            }
            return null;
        }

        // Matches a SemVer 2.0 version tail: at least Major.Minor (numeric, dot-separated),
        // optionally followed by a `-prerelease` or `+build-metadata` segment whose body
        // can include word chars, dots, dashes, and pluses. Used as a fallback when
        // System.Version.TryParse refuses an otherwise-valid NuGet folder version.
        static readonly System.Text.RegularExpressions.Regex SemVerShape = new System.Text.RegularExpressions.Regex(
            @"^\d+(\.\d+)+([-+][\w.+-]+)?$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

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
