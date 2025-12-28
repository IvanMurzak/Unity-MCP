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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor.PackageManager;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Package
    {
        [Description("Package search result with available versions.")]
        public class PackageSearchResult
        {
            [Description("The official Unity name of the package used as the package ID.")]
            public string Name { get; set; } = string.Empty;

            [Description("The display name of the package.")]
            public string DisplayName { get; set; } = string.Empty;

            [Description("The latest version available in the registry.")]
            public string LatestVersion { get; set; } = string.Empty;

            [Description("A brief description of the package.")]
            public string Description { get; set; } = string.Empty;

            [Description("Whether this package is already installed in the project.")]
            public bool IsInstalled { get; set; } = false;

            [Description("The currently installed version (if installed).")]
            public string? InstalledVersion { get; set; }

            [Description("Available versions of this package (up to 5 most recent).")]
            public List<string> AvailableVersions { get; set; } = new();
        }

        [McpPluginTool
        (
            "package-search",
            Title = "Package Manager / Search"
        )]
        [Description(@"Search for packages in both Unity Package Manager registry and installed packages.
Use this to find packages by name before installing them. Returns available versions and installation status.
Searches both the Unity registry and locally installed packages (including Git, local, and embedded sources).
Results are prioritized: exact name match, exact display name match, name substring, display name substring, description substring.
Note: Online mode fetches exact matches from live registry, then supplements with cached substring matches.")]
        public async Task<List<PackageSearchResult>> Search
        (
            [Description(@"The package id, name, or description. Can be:
- Full package id: 'com.unity.textmeshpro'
- Full package name: 'TextMesh Pro'
- Partial name: 'TextMesh' (will search in Unity registry and installed packages)
- Description keyword: 'rendering' (searches in package descriptions)")]
            string query,
            [Description("Maximum number of results to return. Default: 10")]
            int maxResults = 10,
            [Description("Whether to perform the search in offline mode (uses cached registry data only). Default: true. Set to false to fetch latest exact matches from Unity registry.")]
            bool offlineMode = true
        )
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Search query cannot be empty. Please provide a package name or search term.");

            if (maxResults <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxResults), "Maximum results must be greater than zero.");

            return await MainThread.Instance.RunAsync(async () =>
            {
                // First, get list of installed packages
                var listRequest = Client.List(offlineMode: true);
                while (!listRequest.IsCompleted)
                    await Task.Yield();

                var installedPackages = listRequest.Status == StatusCode.Success
                    ? listRequest.Result.ToList()
                    : new List<PackageInfo>();

                var installedByName = installedPackages.ToDictionary(p => p.name, p => p);

                var results = new List<PackageSearchResult>();
                var addedPackageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Online mode: first fetch exact matches from live registry, then supplement with cached substring matches
                if (!offlineMode)
                {
                    var onlineSearchRequest = Client.Search(query, offlineMode: false);
                    while (!onlineSearchRequest.IsCompleted)
                        await Task.Yield();

                    if (onlineSearchRequest.Status == StatusCode.Success)
                    {
                        var onlineResults = onlineSearchRequest.Result
                            .Select(p => (pkg: p, priority: GetSearchPriority(p.name, p.displayName, p.description, query)))
                            .Where(x => x.priority > 0)
                            .OrderBy(x => x.priority)
                            .Take(maxResults)
                            .Select(x => x.pkg);

                        foreach (var pkg in onlineResults)
                        {
                            results.Add(CreateSearchResult(pkg, installedByName));
                            addedPackageNames.Add(pkg.name);
                        }
                    }
                }

                // Search cached registry data for substring matches (both online and offline modes)
                // This provides substring search that the online API doesn't support
                if (results.Count < maxResults)
                {
                    var cachedSearchRequest = Client.SearchAll(offlineMode: true);
                    while (!cachedSearchRequest.IsCompleted)
                        await Task.Yield();

                    if (cachedSearchRequest.Status == StatusCode.Success)
                    {
                        var cachedResults = cachedSearchRequest.Result
                            .Where(p => !addedPackageNames.Contains(p.name))
                            .Select(p => (pkg: p, priority: GetSearchPriority(p.name, p.displayName, p.description, query)))
                            .Where(x => x.priority > 0)
                            .OrderBy(x => x.priority)
                            .Take(maxResults - results.Count)
                            .Select(x => x.pkg);

                        foreach (var pkg in cachedResults)
                        {
                            results.Add(CreateSearchResult(pkg, installedByName));
                            addedPackageNames.Add(pkg.name);
                        }
                    }
                }

                // Also search through installed packages that match the query
                // This catches packages from Git, local, embedded sources not in registry
                var matchingInstalled = installedPackages
                    .Where(p => !addedPackageNames.Contains(p.name))
                    .Select(p => (pkg: p, priority: GetSearchPriority(p.name, p.displayName, p.description, query)))
                    .Where(x => x.priority > 0)
                    .OrderBy(x => x.priority)
                    .Take(maxResults - results.Count)
                    .Select(x => x.pkg);

                foreach (var pkg in matchingInstalled)
                {
                    var result = new PackageSearchResult
                    {
                        Name = pkg.name,
                        DisplayName = pkg.displayName ?? pkg.name,
                        LatestVersion = pkg.version,
                        Description = TruncateDescription(pkg.description ?? string.Empty, 200),
                        IsInstalled = true,
                        InstalledVersion = pkg.version,
                        AvailableVersions = pkg.versions?.compatible?.Take(5).ToList() ?? new List<string>()
                    };

                    results.Add(result);
                }

                return results;
            }).Unwrap();
        }

        private static PackageSearchResult CreateSearchResult(PackageInfo pkg, Dictionary<string, PackageInfo> installedByName)
        {
            return new PackageSearchResult
            {
                Name = pkg.name,
                DisplayName = pkg.displayName ?? pkg.name,
                LatestVersion = pkg.version,
                Description = TruncateDescription(pkg.description ?? string.Empty, 200),
                IsInstalled = installedByName.ContainsKey(pkg.name),
                InstalledVersion = installedByName.TryGetValue(pkg.name, out var installed)
                    ? installed.version
                    : null,
                AvailableVersions = pkg.versions?.compatible?.Take(5).ToList() ?? new List<string>()
            };
        }

        private static string TruncateDescription(string description, int maxLength)
        {
            if (string.IsNullOrEmpty(description) || description.Length <= maxLength)
                return description;

            return description.Substring(0, maxLength - 3) + "...";
        }
    }
}
