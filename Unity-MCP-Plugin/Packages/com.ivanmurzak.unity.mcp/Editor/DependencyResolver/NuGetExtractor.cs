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
    /// </summary>
    static class NuGetExtractor
    {
        /// <summary>
        /// Extracts DLLs from a .nupkg file to the install directory.
        /// Returns the list of extracted DLL file paths, joined under <paramref name="installDirectory"/>
        /// (so the result is relative or absolute in the same form as the caller's install directory).
        /// </summary>
        public static List<string> ExtractDlls(string nupkgPath, string installDirectory)
        {
            var extractedDlls = new List<string>();

            if (!Directory.Exists(installDirectory))
                Directory.CreateDirectory(installDirectory);

            using (var zip = ZipFile.OpenRead(nupkgPath))
            {
                // Collect lib/ entries grouped by target framework
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

                // Select the best target framework
                var bestFramework = SelectBestFramework(libEntries.Keys);
                if (bestFramework == null)
                {
                    Debug.LogWarning($"[NuGet] No compatible framework found in {Path.GetFileName(nupkgPath)}. " +
                                     $"Available: {string.Join(", ", libEntries.Keys)}");
                    return extractedDlls;
                }

                // Extract DLLs from the selected framework
                if (libEntries.TryGetValue(bestFramework, out var frameworkEntries))
                {
                    foreach (var entry in frameworkEntries)
                    {
                        var targetPath = Path.Combine(installDirectory, entry.Name);
                        entry.ExtractToFile(targetPath, overwrite: true);
                        extractedDlls.Add(targetPath);
                    }
                }
            }

            return extractedDlls;
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
}
