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
using System.Text.Json.Serialization;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using AIGD;

namespace AIGD
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
}
