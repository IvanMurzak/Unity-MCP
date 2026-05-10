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
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Extracts DLLs from .nupkg files (which are zip archives).
    /// Selects the best target framework match and skips irrelevant content.
    /// Also parses the .nuspec for transitive dependency information.
    ///
    /// Install layout: DLLs are written FLAT under the install directory
    /// using the original <c>{stem}.dll</c> filename from the .nupkg's
    /// <c>lib/&lt;tfm&gt;/</c> folder — the package version does NOT appear
    /// in the filename. The version is tracked exclusively in
    /// <see cref="NuGetInstallManifest"/>'s per-package <c>version</c> field;
    /// the resolver compares that against <see cref="NuGetConfig.Packages"/>
    /// to decide when a DLL needs to be replaced.
    ///
    /// Consuming asmdef <c>precompiledReferences</c> reference the unversioned
    /// filename (e.g. <c>System.Text.Json.dll</c>) and never need to be edited
    /// when a configured package version bumps — the resolver re-extracts the
    /// new version into the same filename and the manifest's <c>version</c>
    /// field tracks the change.
    /// </summary>
    static class NuGetExtractor
    {
        /// <summary>
        /// Plans the flat-layout DLL paths a package would produce without
        /// extracting anything. Used by the long-path pre-flight to reject an
        /// install before any disk write happens (see issue #733).
        ///
        /// Returns the list of (zip-entry, planned-on-disk-path) pairs. The
        /// list is empty for development-only dependencies and packages that
        /// ship no compatible framework folder; both cases are valid no-op
        /// installs that the caller treats as "nothing to do".
        /// </summary>
        public static List<PlannedDll> PlanDllPaths(string nupkgPath, string installDirectory)
        {
            var planned = new List<PlannedDll>();

            using var zip = ZipFile.OpenRead(nupkgPath);

            var libEntries = CollectLibEntries(zip);
            var bestFramework = SelectBestFramework(libEntries.Keys);
            if (bestFramework == null || !libEntries.TryGetValue(bestFramework, out var frameworkEntries))
                return planned;

            foreach (var entry in frameworkEntries)
            {
                // Snapshot FullName / Name into the PlannedDll BEFORE the zip
                // disposes — accessing them on the entry after Dispose throws.
                // The on-disk filename is the .nupkg's original lib-folder
                // entry name; version is tracked in the manifest, not here.
                var fileName = entry.Name;
                var fullName = entry.FullName;
                var targetPath = Path.Combine(installDirectory, fileName);
                planned.Add(new PlannedDll(fullName, fileName, targetPath));
            }

            return planned;
        }

        /// <summary>
        /// Extracts DLLs from a .nupkg file FLAT under the install directory
        /// using the original <c>{stem}.dll</c> filename. Returns the list of
        /// extracted filenames — relative, no directory prefix. Empty list
        /// when the package has no DLLs in any compatible framework folder.
        /// </summary>
        public static List<string> ExtractDlls(string nupkgPath, string installDirectory)
        {
            var extracted = new List<string>();

            if (!Directory.Exists(installDirectory))
                Directory.CreateDirectory(installDirectory);

            var planned = PlanDllPaths(nupkgPath, installDirectory);
            if (planned.Count == 0)
                return extracted;

            using var zip = ZipFile.OpenRead(nupkgPath);

            // Re-resolve entries by full name from the freshly opened archive.
            // Holding ZipArchiveEntry references across two ZipFile.OpenRead
            // calls is unsafe (the underlying stream is owned by the first
            // archive's Dispose). PlanDllPaths captured FullName up front; we
            // just look those names up in this archive.
            var entriesByName = zip.Entries.ToDictionary(e => e.FullName, StringComparer.OrdinalIgnoreCase);
            foreach (var p in planned)
            {
                if (!entriesByName.TryGetValue(p.EntryFullName, out var entry))
                    continue;

                entry.ExtractToFile(p.TargetPath, overwrite: true);
                extracted.Add(p.FileName);
            }

            return extracted;
        }

        /// <summary>
        /// Returns true if the .nupkg is marked as a development-only dependency
        /// (<c>&lt;developmentDependency&gt;true&lt;/developmentDependency&gt;</c> in its .nuspec).
        /// These packages ship analyzers, source generators, or build-time assets under
        /// <c>analyzers/</c>, <c>build/</c>, <c>tools/</c> etc. — never runtime DLLs under
        /// <c>lib/&lt;tfm&gt;/</c>. They are legitimately empty from the resolver's point of view
        /// and should not trigger "No compatible framework" / "No DLLs extracted" warnings.
        /// Returns false on any parse error so callers fall back to the normal extraction path.
        /// </summary>
        internal static bool IsDevelopmentDependency(string nupkgPath)
        {
            try
            {
                using var zip = ZipFile.OpenRead(nupkgPath);

                // .nuspec is always at the archive root (exactly one per package).
                // Normalize backslashes to forward slashes so archives that use '\' as the
                // path separator are matched the same way as ExtractDlls() treats them.
                var nuspecEntry = zip.Entries.FirstOrDefault(e =>
                {
                    var fullName = e.FullName.Replace('\\', '/');
                    return fullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) &&
                           !fullName.Contains('/');
                });

                if (nuspecEntry == null)
                    return false;

                using var stream = nuspecEntry.Open();
                var doc = XDocument.Load(stream);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                var devDep = doc.Root?
                    .Element(ns + "metadata")?
                    .Element(ns + "developmentDependency")?
                    .Value;

                return string.Equals(devDep?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // Corrupt / unreadable nupkg — treat as "not a dev dep" so the normal
                // extraction path runs and surfaces the real error.
                return false;
            }
        }

        /// <summary>
        /// Reads the .nuspec from a .nupkg and returns the transitive dependencies
        /// for the best matching target framework group.
        /// </summary>
        public static List<NuGetPackage> GetDependencies(string nupkgPath)
        {
            var dependencies = new List<NuGetPackage>();

            using (var zip = ZipFile.OpenRead(nupkgPath))
            {
                // Find the .nuspec file (there should be exactly one at the root)
                var nuspecEntry = zip.Entries.FirstOrDefault(e =>
                    e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) &&
                    !e.FullName.Contains('/'));

                if (nuspecEntry == null)
                    return dependencies;

                using (var stream = nuspecEntry.Open())
                {
                    var doc = XDocument.Load(stream);
                    var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                    // Find dependency groups
                    var metadata = doc.Root?.Element(ns + "metadata");
                    var dependenciesElement = metadata?.Element(ns + "dependencies");
                    if (dependenciesElement == null)
                        return dependencies;

                    // Try to find the best framework-specific dependency group
                    var groups = dependenciesElement.Elements(ns + "group").ToList();
                    if (groups.Count > 0)
                    {
                        var bestGroup = SelectBestDependencyGroup(groups, ns);
                        if (bestGroup != null)
                            AddDependenciesFromElements(bestGroup.Elements(ns + "dependency"), dependencies);
                    }
                    else
                    {
                        AddDependenciesFromElements(dependenciesElement.Elements(ns + "dependency"), dependencies);
                    }
                }
            }

            return dependencies;
        }

        static Dictionary<string, List<ZipArchiveEntry>> CollectLibEntries(ZipArchive zip)
        {
            var libEntries = new Dictionary<string, List<ZipArchiveEntry>>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in zip.Entries)
            {
                var entryName = entry.FullName.Replace('\\', '/');

                if (!entryName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Skip directories, non-DLL files, and files we don't need
                if (string.IsNullOrEmpty(entry.Name) || ShouldSkip(entryName))
                    continue;

                var parts = entryName.Split('/');
                if (parts.Length < 3)
                    continue;

                var framework = parts[1];
                if (!libEntries.TryGetValue(framework, out var entries))
                {
                    entries = new List<ZipArchiveEntry>();
                    libEntries[framework] = entries;
                }
                entries.Add(entry);
            }

            return libEntries;
        }

        /// <summary>
        /// Selects the best target framework from available options using the priority list.
        /// </summary>
        static string? SelectBestFramework(IEnumerable<string> availableFrameworks)
        {
            // Materialize once to preserve a stable ordering for deterministic fallback.
            var availableList = availableFrameworks.ToList();
            var available = new HashSet<string>(availableList, StringComparer.OrdinalIgnoreCase);

            foreach (var preferred in NuGetConfig.TargetFrameworkPriority)
            {
                if (available.Contains(preferred))
                    return preferred;
            }

            // Deterministic fallback: lexicographically smallest framework (case-insensitive)
            // to keep installs stable across machines/runs regardless of HashSet enumeration order.
            return availableList
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        /// <summary>
        /// Selects the best dependency group based on target framework priority.
        /// </summary>
        static XElement? SelectBestDependencyGroup(List<XElement> groups, XNamespace ns)
        {
            // Pre-compute normalized TFMs to avoid repeated NormalizeTfm calls in the inner loop.
            var normalizedGroups = new List<(string tfm, XElement group)>(groups.Count);
            foreach (var group in groups)
            {
                var tfm = group.Attribute("targetFramework")?.Value ?? "";
                normalizedGroups.Add((NormalizeTfm(tfm), group));
            }

            foreach (var preferred in NuGetConfig.TargetFrameworkPriority)
            {
                foreach (var (tfm, group) in normalizedGroups)
                {
                    if (string.Equals(tfm, preferred, StringComparison.OrdinalIgnoreCase))
                        return group;
                }
            }

            // Fallback: group with no targetFramework attribute (universal dependencies)
            return groups.FirstOrDefault(g => string.IsNullOrEmpty(g.Attribute("targetFramework")?.Value))
                   ?? groups.FirstOrDefault();
        }

        /// <summary>
        /// Normalizes target framework monikers from .nuspec format to short format.
        /// e.g., ".NETStandard2.1" → "netstandard2.1", ".NETFramework4.7.2" → "net472"
        /// </summary>
        static string NormalizeTfm(string tfm)
        {
            if (string.IsNullOrEmpty(tfm))
                return "";

            // .NETStandard,Version=v2.1 or .NETStandard2.1
            if (tfm.StartsWith(".NETStandard", StringComparison.OrdinalIgnoreCase))
            {
                var version = tfm.Replace(".NETStandard", "").Replace(",Version=v", "").Replace(".", "");
                if (version.Length == 2) // "21" → "2.1"
                    version = version[0] + "." + version[1];
                return "netstandard" + version;
            }

            // .NETFramework,Version=v4.7.2 or .NETFramework4.7.2
            if (tfm.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase))
            {
                var version = tfm.Replace(".NETFramework", "").Replace(",Version=v", "").Replace(".", "");
                return "net" + version;
            }

            return tfm.ToLowerInvariant();
        }

        static void AddDependenciesFromElements(IEnumerable<XElement> elements, List<NuGetPackage> dependencies)
        {
            foreach (var dep in elements)
            {
                var id = dep.Attribute("id")?.Value;
                var version = dep.Attribute("version")?.Value;
                if (id != null && version != null)
                    dependencies.Add(new NuGetPackage(id, CleanVersionRange(version)));
            }
        }

        /// <summary>
        /// Cleans NuGet version range syntax to a simple version string.
        /// e.g., "[1.0.0, )" → "1.0.0", "(, 2.0.0]" → "2.0.0", "1.0.0" → "1.0.0"
        /// </summary>
        static string CleanVersionRange(string version)
        {
            if (string.IsNullOrEmpty(version))
                return version;

            // Remove brackets and parentheses
            version = version.Trim('[', ']', '(', ')', ' ');

            // If it's a range like "1.0.0, 2.0.0", take the first (lower bound)
            var commaIndex = version.IndexOf(',');
            if (commaIndex >= 0)
            {
                var lower = version.Substring(0, commaIndex).Trim();
                if (!string.IsNullOrEmpty(lower))
                    return lower;

                // No lower bound, use upper
                return version.Substring(commaIndex + 1).Trim();
            }

            return version;
        }

        /// <summary>
        /// Returns true if this zip entry should be skipped during extraction.
        /// </summary>
        static bool ShouldSkip(string entryPath)
        {
            // Skip non-DLL files (we only need assemblies)
            if (!entryPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return true;

            // Skip localization satellite assemblies (lib/{framework}/{lang-code}/*.dll)
            var parts = entryPath.Split('/');
            if (parts.Length >= 4)
            {
                var possibleLangCode = parts[2];
                if (possibleLangCode.Length == 2 ||
                    (possibleLangCode.Length >= 4 && possibleLangCode[2] == '-'))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// One DLL the extractor is about to write under the install directory in
    /// the flat-layout style. Returned by <see cref="NuGetExtractor.PlanDllPaths"/>.
    ///
    /// The struct deliberately stores strings rather than the live
    /// <see cref="ZipArchiveEntry"/> reference: <see cref="NuGetExtractor.PlanDllPaths"/>
    /// disposes its zip before returning, and accessing entry properties on
    /// a disposed archive throws.
    /// </summary>
    readonly struct PlannedDll
    {
        /// <summary>
        /// Full archive-relative path of the source entry (e.g.
        /// <c>lib/netstandard2.0/System.Text.Json.dll</c>). Used by
        /// <see cref="NuGetExtractor.ExtractDlls"/> to re-resolve the entry
        /// in a freshly opened zip.
        /// </summary>
        public string EntryFullName { get; }

        /// <summary>
        /// The DLL filename relative to the install directory
        /// (e.g. <c>System.Text.Json.dll</c>).
        /// </summary>
        public string FileName { get; }

        /// <summary>The full on-disk path of the planned write.</summary>
        public string TargetPath { get; }

        public PlannedDll(string entryFullName, string fileName, string targetPath)
        {
            EntryFullName = entryFullName;
            FileName = fileName;
            TargetPath = targetPath;
        }
    }
}
