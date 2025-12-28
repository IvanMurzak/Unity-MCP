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
        [Description("Filter for package source in listing operations.")]
        public enum PackageSourceFilter
        {
            [Description("Show all packages regardless of source.")]
            All,

            [Description("Packages from the Unity Registry (official Unity packages).")]
            Registry,

            [Description("Packages embedded directly in the project's Packages folder.")]
            Embedded,

            [Description("Packages referenced from a local folder path.")]
            Local,

            [Description("Packages installed from a Git repository URL.")]
            Git,

            [Description("Built-in Unity packages that come with the editor.")]
            BuiltIn,

            [Description("Packages installed from a local tarball (.tgz) file.")]
            LocalTarball
        }

        [Description("Package information returned from package list operation.")]
        public class PackageData
        {
            [Description("The official Unity name of the package used as the package ID.")]
            public string Name { get; set; } = string.Empty;

            [Description("The display name of the package.")]
            public string DisplayName { get; set; } = string.Empty;

            [Description("The version of the package.")]
            public string Version { get; set; } = string.Empty;

            [Description("A brief description of the package.")]
            public string Description { get; set; } = string.Empty;

            [Description("The source of the package (Registry, Embedded, Local, Git, etc.).")]
            public string Source { get; set; } = string.Empty;

            [Description("The category of the package.")]
            public string Category { get; set; } = string.Empty;

            public static PackageData FromPackageInfo(UnityEditor.PackageManager.PackageInfo info)
            {
                return new PackageData
                {
                    Name = info.name,
                    DisplayName = info.displayName ?? info.name,
                    Version = info.version,
                    Description = info.description ?? string.Empty,
                    Source = info.source.ToString(),
                    Category = info.category ?? string.Empty
                };
            }
        }

        [McpPluginTool
        (
            "package-list",
            Title = "Package Manager / List Installed"
        )]
        [Description(@"List all packages installed in the Unity project (UPM packages).
Returns information about each installed package including name, version, source, and description.
Use this to check which packages are currently installed before adding or removing packages.")]
        public async Task<List<PackageData>> List
        (
            [Description("Filter packages by source.")]
            PackageSourceFilter sourceFilter = PackageSourceFilter.All,
            [Description("Filter packages by name (case-insensitive substring match). Example: 'textmesh' will match 'com.unity.textmeshpro'.")]
            string? nameFilter = null,
            [Description("Include only direct dependencies (packages in manifest.json). If false, includes all resolved packages. Default: false")]
            bool directDependenciesOnly = false
        )
        {
            return await MainThread.Instance.RunAsync(async () =>
            {
                var listRequest = Client.List(directDependenciesOnly);

                while (!listRequest.IsCompleted)
                    await Task.Yield();

                if (listRequest.Status == StatusCode.Failure)
                    throw new Exception(Error.PackageListFailed(listRequest.Error?.message ?? "Unknown error"));

                var packages = listRequest.Result.AsEnumerable();

                // Apply source filter
                if (sourceFilter != PackageSourceFilter.All)
                {
                    var unitySource = sourceFilter switch
                    {
                        PackageSourceFilter.Registry => PackageSource.Registry,
                        PackageSourceFilter.Embedded => PackageSource.Embedded,
                        PackageSourceFilter.Local => PackageSource.Local,
                        PackageSourceFilter.Git => PackageSource.Git,
                        PackageSourceFilter.BuiltIn => PackageSource.BuiltIn,
                        PackageSourceFilter.LocalTarball => PackageSource.LocalTarball,
                        _ => PackageSource.Unknown
                    };
                    if (unitySource != PackageSource.Unknown)
                        packages = packages.Where(p => p.source == unitySource);
                }

                // Apply name filter
                if (!string.IsNullOrEmpty(nameFilter))
                {
                    packages = packages.Where(p =>
                        p.name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase) ||
                        (p.displayName?.Contains(nameFilter, StringComparison.OrdinalIgnoreCase) ?? false));
                }

                return packages
                    .OrderBy(p => p.name)
                    .Select(PackageData.FromPackageInfo)
                    .ToList();
            }).Unwrap();
        }
    }
}
