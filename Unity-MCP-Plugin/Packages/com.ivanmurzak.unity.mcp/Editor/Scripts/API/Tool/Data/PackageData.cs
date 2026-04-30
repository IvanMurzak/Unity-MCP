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
}
