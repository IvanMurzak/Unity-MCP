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
        [Description(@"Search for packages available in the Unity Package Manager registry.
Use this to find packages by name before installing them. Returns available versions and installation status.
The search queries the Unity registry and returns matching packages with their latest versions.")]
        public async Task<List<PackageSearchResult>> Search
        (
            [Description(@"The package id, name or partial name. Can be:
- Full package id: 'com.unity.textmeshpro'
- Full package name: 'TextMesh Pro'
- Partial name: 'TextMesh' (will search in Unity registry)")]
            string query,
            [Description("Maximum number of results to return. Default: 10")]
            int maxResults = 10,
            [Description("Whether to perform the search in offline mode. Default: false")]
            bool offlineMode = false
        )
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Search query cannot be empty. Please provide a package name or search term.");

            if (maxResults <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxResults), "Maximum results must be greater than zero.");

            return await MainThread.Instance.RunAsync(async () =>
            {
                // First, get list of installed packages for comparison
                var listRequest = Client.List(offlineMode: offlineMode);
                while (!listRequest.IsCompleted)
                    await Task.Yield();

                var installedPackages = listRequest.Status == StatusCode.Success
                    ? listRequest.Result.ToDictionary(p => p.name, p => p.version)
                    : new Dictionary<string, string>();

                // Search for packages in the registry
                var searchRequest = Client.Search(packageIdOrName: query, offlineMode: offlineMode);
                while (!searchRequest.IsCompleted)
                    await Task.Yield();

                if (searchRequest.Status == StatusCode.Failure)
                    throw new Exception(Error.PackageSearchFailed(query, searchRequest.Error?.message ?? "Unknown error"));

                var results = new List<PackageSearchResult>();

                foreach (var pkg in searchRequest.Result.Take(maxResults))
                {
                    var result = new PackageSearchResult
                    {
                        Name = pkg.name,
                        DisplayName = pkg.displayName ?? pkg.name,
                        LatestVersion = pkg.version,
                        Description = TruncateDescription(pkg.description ?? string.Empty, 200),
                        IsInstalled = installedPackages.ContainsKey(pkg.name),
                        InstalledVersion = installedPackages.TryGetValue(pkg.name, out var installedVersion)
                            ? installedVersion
                            : null,
                        AvailableVersions = pkg.versions?.compatible?.Take(5).ToList() ?? new List<string>()
                    };

                    results.Add(result);
                }

                return results;
            }).Unwrap();
        }

        private static string TruncateDescription(string description, int maxLength)
        {
            if (string.IsNullOrEmpty(description) || description.Length <= maxLength)
                return description;

            return description.Substring(0, maxLength - 3) + "...";
        }
    }
}
